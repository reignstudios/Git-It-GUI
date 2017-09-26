using GitCommander;
using GitCommander.System;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;

namespace GitItGUI.Core
{
	/// <summary>
	/// Primary git manager
	/// </summary>
	public partial class RepoManager : IDisposable
	{
		public delegate void ReadyCallbackMethod(Dispatcher dispatcher);
		public delegate void RepoRefreshedCallbackMethod();
		public event RepoRefreshedCallbackMethod RepoRefreshedCallback;
		
		public bool lfsEnabled {get; private set;}
		public bool? isInSync {get; private set;}

		public bool signatureIsLocal {get; private set;}
		public string signatureName {get; private set;}
		public string signatureEmail {get; private set;}

		public Repository repository {get; private set;}
		private bool pauseGitCommanderStdWrites;
		private Thread thread;
		public Dispatcher dispatcher { get; private set; }

		public RepoManager(ReadyCallbackMethod readyCallback)
		{
			// create repo worker thread
			thread = new Thread(WorkerThread);
			thread.Start(readyCallback);

			// add custom error codes to git commander
			if (AppManager.settings.customErrorCodes != null)
			{
				foreach (var code in AppManager.settings.customErrorCodes.errorCodes)
				{
					repository.AddErrorCode(code);
				}
			}
		}

		private void WorkerThread(object readyCallback)
		{
			// create thread specific objects
			dispatcher = Dispatcher.CurrentDispatcher;
			repository = new Repository();

			// bind terminal callbacks
			repository.RunExeDebugLineCallback += DebugLog_RunExeDebugLineCallback;
			repository.StdCallback += DebugLog_StdCallback;
			repository.StdWarningCallback += DebugLog_StdWarningCallback;
			repository.StdErrorCallback += DebugLog_StdErrorCallback;

			// fire finished callback
			if (readyCallback != null) ((ReadyCallbackMethod)readyCallback)(dispatcher);

			// run dispatcher
			Dispatcher.Run();
		}

		public void Dispose()
		{
			lock (this)
			{
				if (dispatcher != null)
				{
					dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
				}

				if (thread != null)
				{
					if (thread.IsAlive) thread.Join();
					thread = null;
					dispatcher = null;
				}

				if (repository != null)
				{
					repository.Close();

					// un-bind terminal callbacks
					repository.RunExeDebugLineCallback -= DebugLog_RunExeDebugLineCallback;
					repository.StdCallback -= DebugLog_StdCallback;
					repository.StdWarningCallback -= DebugLog_StdWarningCallback;
					repository.StdErrorCallback -= DebugLog_StdErrorCallback;

					repository = null;
				}
			}
		}

		private void DebugLog_RunExeDebugLineCallback(string line)
		{
			DebugLog.Log(line);
		}

		private void DebugLog_StdCallback(string line)
		{
			if (!pauseGitCommanderStdWrites) DebugLog.Log(line);
		}

		private void DebugLog_StdWarningCallback(string line)
		{
			DebugLog.LogWarning(line);
		}

		private void DebugLog_StdErrorCallback(string line)
		{
			DebugLog.LogError(line);
		}

		/// <summary>
		/// Use to open an existing repo
		/// </summary>
		/// <param name="repoPath">Path to git repo</param>
		/// <returns>True if succeeded</returns>
		public bool OpenRepo(string repoPath, bool checkForSettingErros = false)
		{
			lock (this)
			{
				// unload repo
				if (string.IsNullOrEmpty(repoPath))
				{
					repository.Close();
					return true;
				}

				if (!AppManager.MergeDiffToolInstalled())
				{
					DebugLog.LogError("Merge/Diff tool is not installed!\nGo to app settings and make sure your selected diff tool is installed.", true);
					return false;
				}

				bool isRefreshMode = repoPath == repository.repoPath;
			
				try
				{
					// load repo
					if (isRefreshMode) repository.Close();
					if (!repository.Open(repoPath)) throw new Exception(repository.lastError);
				
					// check for git lfs
					lfsEnabled = IsGitLFSRepo(false);

					// check for .gitignore file
					if (!isRefreshMode)
					{
						string gitIgnorePath = repoPath + Path.DirectorySeparatorChar + ".gitignore";
						if (!File.Exists(gitIgnorePath))
						{
							DebugLog.LogWarning("No '.gitignore' file exists.\nAuto creating one!", true);
							File.WriteAllText(gitIgnorePath, "");
						}
					}
				
					// add repo to history
					AppManager.AddRepoToHistory(repoPath);

					// get signature
					if (!isRefreshMode)
					{
						string sigName, sigEmail;
						if (repository.GetSignature(SignatureLocations.Local, out sigName, out sigEmail))
						{
							signatureName = sigName;
							signatureEmail = sigEmail;
							signatureIsLocal = true;
							if (string.IsNullOrEmpty(sigName) || string.IsNullOrEmpty(sigEmail))
							{
								repository.GetSignature(SignatureLocations.Any, out sigName, out sigEmail);
								signatureName = sigName;
								signatureEmail = sigEmail;
								signatureIsLocal = false;
							}
						}

						if (checkForSettingErros)
						{
							if (string.IsNullOrEmpty(sigName) || string.IsNullOrEmpty(sigEmail))
							{
								DebugLog.LogWarning("Credentials not set, please go to the settings tab!", true);
							}
						}
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("RepoManager.OpenRepo Failed: " + e.Message);
					repository.Close();
					return false;
				}
			
				// refesh partials
				if (!RefreshBranches(isRefreshMode)) return false;
				if (!RefreshChanges()) return false;

				// check sync
				if (IsUpToDateWithRemote(out bool yes)) isInSync = yes;
				else isInSync = null;
			}

			// finish
			if (RepoRefreshedCallback != null) RepoRefreshedCallback();
			return true;
		}

		public bool Close()
		{
			return OpenRepo(null);
		}

		public bool Refresh()
		{
			lock (this)
			{
				return OpenRepo(repository.repoPath);
			}
		}

		public bool Clone(string url, string destination, out string repoPath, StdInputStreamCallbackMethod writeUsernameCallback, StdInputStreamCallbackMethod writePasswordCallback)
		{
			lock (this)
			{
				try
				{
					// clone
					if (!repository.Clone(url, destination, out repoPath, writeUsernameCallback, writePasswordCallback)) throw new Exception(repository.lastError);
					repoPath = destination + Path.DirectorySeparatorChar + repoPath;
					lfsEnabled = IsGitLFSRepo(true);
					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Clone error: " + e.Message, true);
					repoPath = null;
					return false;
				}
			}
		}

		public bool UpdateSignature(string name, string email, SignatureLocations location)
		{
			lock (this)
			{
				try
				{
					// remove local sig
					if (signatureIsLocal && location == SignatureLocations.Global) repository.RemoveSettings(SignatureLocations.Local, "user");

					// update sig
					if (!repository.SetSignature(location, name, email)) throw new Exception(repository.lastError);
					signatureName = name;
					signatureEmail = email;
					signatureIsLocal = location == SignatureLocations.Local;
					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Update Signature: " + e.Message, true);
					return false;
				}
			}
		}

		private bool IsGitLFSRepo(bool returnTrueIfValidAttributes)
		{
			string gitattributesPath = repository.repoPath + Path.DirectorySeparatorChar + ".gitattributes";
			bool attributesExist = File.Exists(gitattributesPath);
			if (returnTrueIfValidAttributes && attributesExist)
			{
				string lines = File.ReadAllText(gitattributesPath);
				return lines.Contains("filter=lfs diff=lfs merge=lfs");
			}

			if (attributesExist && Directory.Exists(repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar)) && File.Exists(repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar)))
			{
				string data = File.ReadAllText(repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar));
				bool isValid = data.Contains("git-lfs");
				if (isValid)
				{
					lfsEnabled = true;
					return true;
				}
				else
				{
					lfsEnabled = false;
				}
			}

			return false;
		}
		
		public bool AddGitLFSSupport(bool addDefaultIgnoreExts)
		{
			lock (this)
			{
				// check if already init
				if (lfsEnabled)
				{
					DebugLog.LogWarning("Git LFS already enabled on repo", true);
					return false;
				}

				try
				{
					// init git lfs
					string lfsFolder = repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar);
					if (!Directory.Exists(lfsFolder))
					{
						if (!repository.lfs.Install()) throw new Exception(repository.lastError);
						if (!Directory.Exists(lfsFolder))
						{
							DebugLog.LogError("Git-LFS install failed! (Try manually)", true);
							lfsEnabled = false;
							return false;
						}
					}

					// add attr file if it doesn't exist
					string gitattributesPath = repository.repoPath + Path.DirectorySeparatorChar + ".gitattributes";
					if (!File.Exists(gitattributesPath))
					{
						using (var writer = File.CreateText(gitattributesPath))
						{
							// this will be an empty file...
						}
					}

					// add default ext to git lfs
					if (addDefaultIgnoreExts)
					{
						foreach (string ext in AppManager.defaultGitLFS_Exts)
						{
							if (!repository.lfs.Track(ext)) throw new Exception(repository.lastError);
						}
					}
				

					// finish
					lfsEnabled = true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Add Git-LFS Error: " + e.Message, true);
					Environment.Exit(0);// quit for safety as application should restart
					return false;
				}
			
				return true;
			}
		}

		public bool RemoveGitLFSSupport(bool rebase)
		{
			lock (this)
			{
				// check if not init
				if (!lfsEnabled)
				{
					DebugLog.LogWarning("Git LFS is not enabled on repo", true);
					return false;
				}

				try
				{
					// untrack lfs filters
					string gitattributesPath = repository.repoPath + Path.DirectorySeparatorChar + ".gitattributes";
					if (File.Exists(gitattributesPath))
					{
						string data = File.ReadAllText(gitattributesPath);
						var values = Regex.Matches(data, @"(\*\..*)? filter=lfs diff=lfs merge=lfs");
						foreach (Match value in values)
						{
							if (value.Groups.Count != 2) continue;
							if (!repository.lfs.Untrack(value.Groups[1].Value)) throw new Exception(repository.lastError);
						}
					}

					// remove lfs repo files
					if (!repository.lfs.Uninstall()) throw new Exception(repository.lastError);
					if (File.Exists(repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar))) File.Delete(repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar));
					if (Directory.Exists(repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar))) Directory.Delete(repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar), true);
					
					// rebase repo
					if (rebase)
					{
						// TODO
					}

					// finish
					lfsEnabled = false;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Remove Git-LFS Error: " + e.Message, true);
					Environment.Exit(0);// quit for safety as application should restart
					return false;
				}
			
				return true;
			}
		}

		public void OpenGitk()
		{
			lock (this)
			{
				try
				{
					// open gitk
					using (var process = new Process())
					{
						if (PlatformInfo.platform == Platforms.Windows)
						{
							string programFilesx86, programFilesx64;
							PlatformInfo.GetWindowsProgramFilesPath(out programFilesx86, out programFilesx64);
							process.StartInfo.FileName = programFilesx64 + string.Format("{0}Git{0}cmd{0}gitk.exe", Path.DirectorySeparatorChar);
						}
						else
						{
							throw new Exception("Unsported platform: " + PlatformInfo.platform);
						}

						process.StartInfo.WorkingDirectory = repository.repoPath;
						process.StartInfo.Arguments = "";
						process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
						if (!process.Start())
						{
							DebugLog.LogError("Failed to start history tool (is it installed?)", true);
							return;
						}

						process.WaitForExit();
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to start history tool: " + e.Message, true);
					return;
				}

				Refresh();
			}
		}

		public int UnpackedObjectCount(out string size)
		{
			lock (this)
			{
				try
				{
					int count;
					if (!repository.UnpackedObjectCount(out count, out size)) throw new Exception(repository.lastError);
					return count;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to optamize: " + e.Message, true);
				}

				size = null;
				return -1;
			}
		}

		public void Optimize()
		{
			lock (this)
			{
				try
				{
					if (!repository.GarbageCollect()) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to optamize: " + e.Message, true);
				}
			}
		}

		public bool OpenFile(string filePath)
		{
			lock (this)
			{
				try
				{
					string path = PlatformInfo.ConvertPathToPlatform(string.Format("{0}\\{1}", repository.repoPath, filePath));
					if (!File.Exists(path)) return false;

					if (PlatformInfo.platform == Platforms.Windows)
					{
						Process.Start("explorer.exe", path);
						return true;
					}
					else
					{
						throw new Exception("Unsuported platform: " + PlatformInfo.platform);
					}
				}
				catch (Exception ex)
				{
					DebugLog.LogError("Failed to open file: " + ex.Message, true);
				}

				return false;
			}
		}

		public bool OpenFileLocation(string filePath)
		{
			lock (this)
			{
				try
				{
					string path = PlatformInfo.ConvertPathToPlatform(string.Format("{0}\\{1}", repository.repoPath, filePath));
					if (!File.Exists(path)) return false;

					if (PlatformInfo.platform == Platforms.Windows)
					{
						Process.Start("explorer.exe", "/select, " + path);
						return true;
					}
					else
					{
						throw new Exception("Unsuported platform: " + PlatformInfo.platform);
					}
				}
				catch (Exception ex)
				{
					DebugLog.LogError("Failed to open folder location: " + ex.Message, true);
				}

				return false;
			}
		}
	}
}
