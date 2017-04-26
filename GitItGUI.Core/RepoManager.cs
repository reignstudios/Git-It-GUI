using GitCommander;
using GitCommander.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	/// <summary>
	/// Primary git manager
	/// </summary>
	public static class RepoManager
	{
		private static object lockObj = new object();
		public static bool isRefreshing {get; private set;}

		public delegate void RepoRefreshedCallbackMethod();
		public static event RepoRefreshedCallbackMethod RepoRefreshedCallback;

		/// <summary>
		/// True if this is a Git-LFS enabled repo
		/// </summary>
		public static bool lfsEnabled {get; private set;}

		public static string signatureName {get; private set;}
		public static string signatureEmail {get; private set;}

		/// <summary>
		/// Use to open an existing repo
		/// </summary>
		/// <param name="path">Path to git repo</param>
		/// <returns>True if succeeded</returns>
		public static bool OpenRepo(string path, bool checkForSettingErros = false)
		{
			lock (lockObj)
			{
				// unload repo
				if (string.IsNullOrEmpty(path))
				{
					Dispose();
					return true;
				}

				if (!AppManager.MergeDiffToolInstalled())
				{
					Debug.LogError("Merge/Diff tool is not installed!\nGo to app settings and make sure your selected diff tool is installed.", true);
					return false;
				}

				bool refreshMode = path == Repository.repoPath;
			
				try
				{
					// load repo
					if (refreshMode) Repository.Dispose();
					if (!Repository.Open(path)) throw new Exception(Repository.lastError);
				
					// check for git lfs
					lfsEnabled = IsGitLFSRepo(false);

					// check for .gitignore file
					if (!refreshMode)
					{
						string gitIgnorePath = path + Path.DirectorySeparatorChar + ".gitignore";
						if (!File.Exists(gitIgnorePath))
						{
							Debug.LogWarning("No '.gitignore' file exists.\nAuto creating one!", true);
							File.WriteAllText(gitIgnorePath, "");
						}
					}
				
					// add repo to history
					AppManager.AddActiveRepoToHistory();

					// get signature
					if (!refreshMode)
					{
						string sigName, sigEmail;
						Repository.GetSignature(SignatureLocations.Local, out sigName, out sigEmail);
						signatureName = sigName;
						signatureEmail = sigEmail;
						if (checkForSettingErros)
						{
							if (string.IsNullOrEmpty(sigName) || string.IsNullOrEmpty(sigEmail))
							{
								Debug.LogWarning("Credentials not set, please go to the settings tab!", true);
							}
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogError("RepoManager.OpenRepo Failed: " + e.Message);
					Dispose();
					return false;
				}
			
				return RefreshInternal(refreshMode);
			}
		}

		public static bool Close()
		{
			return OpenRepo(null);
		}

		public static bool Refresh()
		{
			isRefreshing = true;
			bool result = OpenRepo(Repository.repoPath);
			isRefreshing = false;
			return result;
		}

		private static bool RefreshInternal(bool refreshMode)
		{
			if (!BranchManager.Refresh(refreshMode)) return false;
			if (!ChangesManager.Refresh()) return false;
			if (RepoRefreshedCallback != null) RepoRefreshedCallback();
			return true;
		}

		public static bool Clone(string url, string destination, out string repoPath, StdInputStreamCallbackMethod writeUsernameCallback, StdInputStreamCallbackMethod writePasswordCallback)
		{
			try
			{
				// clone
				if (!Repository.Clone(url, destination, out repoPath, writeUsernameCallback, writePasswordCallback)) throw new Exception(Repository.lastError);
				repoPath = destination + Path.DirectorySeparatorChar + repoPath;
				lfsEnabled = IsGitLFSRepo(true);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("Clone error: " + e.Message, true);
				repoPath = null;
				return false;
			}
		}
		
		internal static void Dispose()
		{
			Repository.Dispose();
		}

		public static bool UpdateSignature(string name, string email)
		{
			try
			{
				if (!Repository.SetSignature(SignatureLocations.Global, name, email)) throw new Exception(Repository.lastError);
				signatureName = name;
				signatureEmail = email;
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("Update Signature: " + e.Message, true);
				return false;
			}
		}

		private static bool IsGitLFSRepo(bool returnTrueIfValidAttributes)
		{
			string gitattributesPath = Repository.repoPath + Path.DirectorySeparatorChar + ".gitattributes";
			bool attributesExist = File.Exists(gitattributesPath);
			if (returnTrueIfValidAttributes && attributesExist)
			{
				string lines = File.ReadAllText(gitattributesPath);
				return lines.Contains("filter=lfs diff=lfs merge=lfs");
			}

			if (attributesExist && Directory.Exists(Repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar)) && File.Exists(Repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar)))
			{
				string data = File.ReadAllText(Repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar));
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
		
		public static bool AddGitLFSSupport(bool addDefaultIgnoreExts)
		{
			// check if already init
			if (lfsEnabled)
			{
				Debug.LogWarning("Git LFS already enabled on repo", true);
				return false;
			}

			try
			{
				// init git lfs
				string lfsFolder = Repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar);
				if (!Directory.Exists(lfsFolder))
				{
					if (!Repository.LFS.Install()) throw new Exception(Repository.lastError);
					if (!Directory.Exists(lfsFolder))
					{
						Debug.LogError("Git-LFS install failed! (Try manually)", true);
						lfsEnabled = false;
						return false;
					}
				}

				// add attr file if it doesn't exist
				string gitattributesPath = Repository.repoPath + Path.DirectorySeparatorChar + ".gitattributes";
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
					foreach (string ext in AppManager.settings.defaultGitLFS_Exts)
					{
						if (!Repository.LFS.Track(ext)) throw new Exception(Repository.lastError);
					}
				}
				

				// finish
				lfsEnabled = true;
			}
			catch (Exception e)
			{
				Debug.LogError("Add Git-LFS Error: " + e.Message, true);
				Environment.Exit(0);// quit for safety as application should restart
				return false;
			}
			
			return true;
		}

		public static bool RemoveGitLFSSupport(bool rebase)
		{
			// check if not init
			if (!lfsEnabled)
			{
				Debug.LogWarning("Git LFS is not enabled on repo", true);
				return false;
			}

			try
			{
				// untrack lfs filters
				string gitattributesPath = Repository.repoPath + Path.DirectorySeparatorChar + ".gitattributes";
				if (File.Exists(gitattributesPath))
				{
					string data = File.ReadAllText(gitattributesPath);
					var values = Regex.Matches(data, @"(\*\..*)? filter=lfs diff=lfs merge=lfs");
					foreach (Match value in values)
					{
						if (value.Groups.Count != 2) continue;
						if (!Repository.LFS.Untrack(value.Groups[1].Value)) throw new Exception(Repository.lastError);
					}
				}

				// remove lfs repo files
				if (!Repository.LFS.Uninstall()) throw new Exception(Repository.lastError);
				if (File.Exists(Repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar))) File.Delete(Repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar));
				if (Directory.Exists(Repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar))) Directory.Delete(Repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar), true);
					
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
				Debug.LogError("Remove Git-LFS Error: " + e.Message, true);
				Environment.Exit(0);// quit for safety as application should restart
				return false;
			}
			
			return true;
		}

		public static void OpenGitk()
		{
			try
			{
				// open gitk
				using (var process = new Process())
				{
					if (PlatformSettings.platform == Platforms.Windows)
					{
						string programFilesx86, programFilesx64;
						PlatformSettings.GetWindowsProgramFilesPath(out programFilesx86, out programFilesx64);
						process.StartInfo.FileName = programFilesx64 + string.Format("{0}Git{0}cmd{0}gitk.exe", Path.DirectorySeparatorChar);
					}
					else
					{
						throw new Exception("Unsported platform: " + PlatformSettings.platform);
					}

					process.StartInfo.WorkingDirectory = Repository.repoPath;
					process.StartInfo.Arguments = "";
					process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
					if (!process.Start())
					{
						Debug.LogError("Failed to start history tool (is it installed?)", true);
						return;
					}

					process.WaitForExit();
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to start history tool: " + e.Message, true);
				return;
			}

			Refresh();
		}

		public static int UnpackedObjectCount(out string size)
		{
			try
			{
				int count;
				if (!Repository.UnpackedObjectCount(out count, out size)) throw new Exception(Repository.lastError);
				return count;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to optamize: " + e.Message, true);
			}

			size = null;
			return -1;
		}

		public static void Optimize()
		{
			try
			{
				if (!Repository.GarbageCollect()) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to optamize: " + e.Message, true);
			}
		}
	}
}
