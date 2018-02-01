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
		public delegate void RepoRefreshedCallbackMethod(bool isQuickRefresh);
		public event RepoRefreshedCallbackMethod RepoRefreshedCallback;
		public bool disableRepoRefreshedCallback;
		
		public bool isOpen {get; private set;}

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
			
			// create git commander object
			repository = new Repository();
			repository.RunExeDebugLineCallback += DebugLog_RunExeDebugLineCallback;
			repository.StdCallback += DebugLog_StdCallback;
			repository.StdWarningCallback += DebugLog_StdWarningCallback;
			repository.StdErrorCallback += DebugLog_StdErrorCallback;

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
			dispatcher = Dispatcher.CurrentDispatcher;
			if (readyCallback != null) ((ReadyCallbackMethod)readyCallback)(dispatcher);
			Dispatcher.Run();
		}

		public void Dispose()
		{
			lock (this)
			{
				isOpen = false;

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
		public bool Open(string repoPath, bool checkForSettingErros = false)
		{
			lock (this)
			{
				isOpen = false;

				// unload repo
				if (string.IsNullOrEmpty(repoPath))
				{
					repository.Close();
					return true;
				}

				bool isRefreshMode = repoPath == repository.repoPath;
			
				try
				{
					// load repo
					if (isRefreshMode) repository.Close();
					if (!repository.Open(repoPath)) throw new Exception(repository.lastError);
				
					// check for git lfs
					lfsEnabled = repository.lfs.isEnabled;

					// check for .gitignore file
					if (!isRefreshMode)
					{
						string gitIgnorePath = Path.Combine(repoPath, ".gitignore");
						if (!File.Exists(gitIgnorePath))
						{
							DebugLog.LogWarning("No '.gitignore' file exists.\nAuto creating one!");
							File.WriteAllText(gitIgnorePath, "");
						}
					}
				
					// add repo to history
					AppManager.AddRepoToHistory(repoPath);

					// non-refresh checks
					if (!isRefreshMode)
					{
						// get signature
						string sigName, sigEmail;
						if (repository.GetSignature(SignatureLocations.Local, out sigName, out sigEmail))
						{
							signatureName = sigName;
							signatureEmail = sigEmail;
							signatureIsLocal = true;
							if (string.IsNullOrEmpty(sigName) || string.IsNullOrEmpty(sigEmail))
							{
								if (repository.GetSignature(SignatureLocations.Any, out sigName, out sigEmail))
								{
									signatureName = sigName;
									signatureEmail = sigEmail;
									signatureIsLocal = false;
								}
								else
								{
									signatureName = "<< ERROR >>";
									signatureEmail = "<< ERROR >>";
									signatureIsLocal = false;
								}
							}
						}
						else
						{
							signatureName = "<< ERROR >>";
							signatureEmail = "<< ERROR >>";
							signatureIsLocal = false;
						}

						if (checkForSettingErros)
						{
							if (string.IsNullOrEmpty(sigName) || string.IsNullOrEmpty(sigEmail))
							{
								DebugLog.LogWarning("Credentials not set, please go to the settings tab!");
							}
						}

						// submodules
						if (repository.hasSubmodules)
						{
							if (repository.areSubmodulesInit)
							{
								if (!repository.PullSubmodules())
								{
									DebugLog.LogError("Failed to pull submodules: " + repository.lastError);
									return false;
								}
							}
							else
							{
								if (!repository.InitPullSubmodules())
								{
									DebugLog.LogError("Failed to init and pull submodules: " + repository.lastError);
									return false;
								}
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

				isOpen = true;
			}

			// finish
			if (!disableRepoRefreshedCallback && RepoRefreshedCallback != null) RepoRefreshedCallback(false);
			return true;
		}

		public bool Close()
		{
			return Open(null);
		}

		public bool Refresh()
		{
			lock (this) return Open(repository.repoPath);
		}

		public bool QuickRefresh()
		{
			lock (this)
			{
				if (!isOpen) throw new Exception("Repo not open!");

				bool result = RefreshChanges();
				if (!disableRepoRefreshedCallback && RepoRefreshedCallback != null) RepoRefreshedCallback(true);
				return result;
			}
		}

		public bool AddFirstAutoCommit()
		{
			lock (this)
			{
				try
				{
					// clone
					if (!repository.Stage(".gitignore")) throw new Exception(repository.lastError);
					if (!repository.Commit("First Commit! (Auto generated by Git-It-GUI)")) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Clone error: " + e.Message);
					return false;
				}

				Refresh();
				return true;
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
					repoPath = Path.Combine(destination, repoPath);
					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Clone error: " + e.Message);
					repoPath = null;
					return false;
				}
			}
		}

		public bool Create(string repoPath)
		{
			lock (this)
			{
				try
				{
					if (!Directory.Exists(repoPath)) Directory.CreateDirectory(repoPath);
					if (!repository.Init(repoPath)) throw new Exception(repository.lastError);
					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Clone error: " + e.Message);
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
					DebugLog.LogError("Update Signature: " + e.Message);
					return false;
				}
			}
		}
		
		public bool AddGitLFSSupport(bool addLFSDefaultExts)
		{
			lock (this)
			{
				// check if already init
				if (lfsEnabled)
				{
					DebugLog.LogWarning("Git LFS already enabled on repo");
					return false;
				}

				try
				{
					// init git lfs
					string lfsFolder = Path.Combine(repository.repoPath, ".git", "lfs");
					if (!Directory.Exists(lfsFolder))
					{
						if (!repository.lfs.Install()) throw new Exception(repository.lastError);
						if (!Directory.Exists(lfsFolder))
						{
							DebugLog.LogError("Git-LFS install failed! (Try manually)");
							lfsEnabled = false;
							return false;
						}
					}

					// add attr file if it doesn't exist
					string gitattributesPath = Path.Combine(repository.repoPath, ".gitattributes");
					if (!File.Exists(gitattributesPath))
					{
						using (var writer = File.CreateText(gitattributesPath))
						{
							// this will be an empty file...
						}
					}

					// add default ext to git lfs
					if (addLFSDefaultExts && AppManager.defaultGitLFS_Exts != null && AppManager.defaultGitLFS_Exts.Count != 0)
					{
						if (!repository.lfs.Track(AppManager.defaultGitLFS_Exts)) throw new Exception(repository.lastError);
					}

					// finish
					lfsEnabled = true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Add Git-LFS Error: " + e.Message);
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
					DebugLog.LogWarning("Git LFS is not enabled on repo");
					return false;
				}

				try
				{
					// untrack lfs filters
					string gitattributesPath = Path.Combine(repository.repoPath, ".gitattributes");
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

					string lfsHookFile = Path.Combine(repository.repoPath, ".git", "hooks", "pre-push");
					if (File.Exists(lfsHookFile)) File.Delete(lfsHookFile);

					string lfsFolder = Path.Combine(repository.repoPath, ".git", "lfs");
					if (Directory.Exists(lfsFolder)) Directory.Delete(lfsFolder, true);
					
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
					DebugLog.LogError("Remove Git-LFS Error: " + e.Message);
					Environment.Exit(0);// quit for safety as application should restart
					return false;
				}
			
				return true;
			}
		}

		public void OpenGitk(string filename = null)
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
							process.StartInfo.FileName = Path.Combine(programFilesx64, "Git", "cmd", "gitk.exe");
						}
						else
						{
							throw new Exception("Unsported platform: " + PlatformInfo.platform);
						}

						process.StartInfo.WorkingDirectory = repository.repoPath;
						if (filename != null) process.StartInfo.Arguments = string.Format("\"{0}\"", filename);
						process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
						if (!process.Start())
						{
							DebugLog.LogError("Failed to start history tool (is it installed?)");
							return;
						}

						process.WaitForExit();
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to start history tool: " + e.Message);
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
					DebugLog.LogError("Failed UnpackedObjectCount: " + e.Message);
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
					DebugLog.LogError("Failed to gc: " + e.Message);
				}
			}
		}

		public int UnusedLFSFiles(out string size)
		{
			lock (this)
			{
				try
				{
					int count;
					if (!repository.lfs.PruneObjectCount(out count, out size)) throw new Exception(repository.lastError);
					return count;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed PruneObjectCount: " + e.Message);
				}

				size = null;
				return -1;
			}
		}

		public void PruneLFSFiles()
		{
			lock (this)
			{
				try
				{
					if (!repository.lfs.Prune()) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to prune lfs files: " + e.Message);
				}
			}
		}

		public bool OpenFile(string filePath)
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
				DebugLog.LogError("Failed to open file: " + ex.Message);
			}

			return false;
		}

		public bool OpenFileLocation(string filePath)
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
				DebugLog.LogError("Failed to open folder location: " + ex.Message);
			}

			return false;
		}
	}
}
