using LibGit2Sharp;
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
	public delegate void StatusUpdateCallbackMethod(string status);

	/// <summary>
	/// Primary git manager
	/// </summary>
	public static class RepoManager
	{
		public delegate void RepoRefreshedCallbackMethod();
		public static event RepoRefreshedCallbackMethod RepoRefreshedCallback;

		/// <summary>
		/// lib2git repo object
		/// </summary>
		public static Repository repo {get; private set;}

		/// <summary>
		/// Path to active repo
		/// </summary>
		public static string repoPath {get; private set;}

		/// <summary>
		/// True if this is a Git-LFS enabled repo
		/// </summary>
		public static bool lfsEnabled {get; private set;}

		public static bool validateGitignoreCheckbox {get; private set;}

		public static string signatureName {get; private set;}
		public static string signatureEmail {get; private set;}
		public static string credentialUsername {get; private set;}
		public static string credentialPassword {get; private set;}
		
		private static XML.RepoSettings settings;
		private static XML.RepoUserSettings userSettings;
		private static FilterRegistration lfsFilterRegistration;

		internal static Signature signature {get {return new Signature(userSettings.signatureName, userSettings.signatureEmail, DateTimeOffset.UtcNow);}}
		internal static UsernamePasswordCredentials credentials {get; private set;}

		/// <summary>
		/// Use to open an existing repo
		/// </summary>
		/// <param name="path">Path to git repo</param>
		/// <returns>True if succeeded</returns>
		public static bool OpenRepo(string path, bool checkForSettingErros = false)
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

			bool refreshMode = path == repoPath;
			
			try
			{
				// load repo
				repoPath = path;
				repo = new Repository(path);
				
				// check for git lfs
				lfsEnabled = IsGitLFSRepo(false);
				
				// load settings
				settings = Settings.Load<XML.RepoSettings>(path + Path.DirectorySeparatorChar + Settings.repoSettingsFilename);
				userSettings = Settings.Load<XML.RepoUserSettings>(path + Path.DirectorySeparatorChar + Settings.repoUserSettingsFilename);

				// check for .gitignore file
				validateGitignoreCheckbox = settings.validateGitignore;
				if (!refreshMode && settings.validateGitignore)
				{
					string gitIgnorePath = path + Path.DirectorySeparatorChar + ".gitignore";
					if (!File.Exists(gitIgnorePath))
					{
						Debug.LogWarning("No '.gitignore' file exists.\nAuto creating one!", true);
						string text = string.Format("*{0}", Settings.repoUserSettingsFilename);
						File.WriteAllText(gitIgnorePath, text);
					}
					else
					{
						string text = File.ReadAllText(gitIgnorePath);
						if (!text.Contains("*" + Settings.repoUserSettingsFilename))
						{
							text += string.Format("{0}{0}*{1}", Environment.NewLine, Settings.repoUserSettingsFilename);
							File.WriteAllText(gitIgnorePath, text);
						}
					}
				}

				// create user objects
				signatureName = userSettings.signatureName;
				signatureEmail = userSettings.signatureEmail;
				credentialUsername = userSettings.username;
				credentialPassword = userSettings.password;
				credentials = new UsernamePasswordCredentials
				{
					Username = userSettings.username,
					Password = userSettings.password
				};
				
				BranchManager.OpenRepo(repo);
				AppManager.AddActiveRepoToHistory();

				// warnings
				if (checkForSettingErros)
				{
					if (userSettings.signatureName.Contains("TODO: ") || userSettings.signatureEmail.Contains("TODO: ") || userSettings.username.Contains("TODO: "))
					{
						Debug.LogWarning("Credentials not set, please go to the settings tab!", true);
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("RepoManager.OpenRepo Failed: " + e.Message);
				Dispose();
				return false;
			}
			
			return RefreshInternal();
		}

		public static bool Close()
		{
			return OpenRepo(null);
		}

		public static bool Refresh()
		{
			return OpenRepo(repoPath);
		}

		private static bool RefreshInternal()
		{
			if (!BranchManager.Refresh()) return false;
			if (!ChangesManager.Refresh()) return false;
			if (RepoRefreshedCallback != null) RepoRefreshedCallback();
			return true;
		}

		internal static void DeleteRepoSettingsIfUnCommit()
		{
			// check for git settings file not in repo history
			string settingsPath = repoPath + Path.DirectorySeparatorChar + Settings.repoSettingsFilename;
			if (File.Exists(settingsPath))
			{
				var repoStatus = repo.RetrieveStatus(Settings.repoSettingsFilename);
				if ((repoStatus & FileStatus.NewInWorkdir) != 0)
				{
					File.Delete(settingsPath);
					while (File.Exists(settingsPath)) Thread.Sleep(250);
				}
			}
		}

		public static bool Clone(string url, string destination, string username, string password, out string repoPath, StatusUpdateCallbackMethod statusCallback)
		{
			try
			{
				// get repo name
				var match = Regex.Match(url, @"(.*)/(.*\.git)");
				repoPath = null;
				if (match.Groups.Count == 3 && !string.IsNullOrEmpty(match.Groups[2].Value))
				{
					repoPath = match.Groups[2].Value.Replace(".git", "");
					repoPath = Path.Combine(destination, repoPath);
				}
				else
				{
					Debug.LogError("Failed to parse url for repo name: " + url, true);
					return false;
				}

				// valid folder is free
				if (Directory.Exists(repoPath))
				{
					Debug.LogError("Folder already exists: " + repoPath, true);
					return false;
				}

				// create folder
				Directory.CreateDirectory(repoPath);

				// clone
				var options = new CloneOptions();
				options.IsBare = false;
				options.CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials
				{
					Username = username,
					Password = password
				};

				options.OnProgress += delegate(string serverProgressOutput)
				{
					if (statusCallback != null) statusCallback(serverProgressOutput);
					return true;
				};

				options.OnTransferProgress = delegate(TransferProgress progress)
				{
					if (statusCallback != null) statusCallback(string.Format("Downloading: {0}%", (int)((progress.ReceivedObjects / (decimal)(progress.TotalObjects+1)) * 100)));
					return true;
				};

				options.OnCheckoutProgress = delegate(string path, int completedSteps, int totalSteps)
				{
					if (statusCallback != null) statusCallback(string.Format("Checking out: {0}%", (int)((completedSteps / (decimal)(totalSteps+1)) * 100)));
				};

				RunExeCallbackMethod lfsCallback = delegate(string stdLine)
				{
					if (statusCallback != null) statusCallback(stdLine);
				};
				
				string result = Repository.Clone(url, repoPath, options);
				if (result == (repoPath + string.Format("{0}.git{0}", Path.DirectorySeparatorChar)))
				{
					RepoManager.repoPath = repoPath;
					if (IsGitLFSRepo(true))
					{
						const string errorStringHelper = "\n(please manually run these commands in order\ngit-lfs [install, fetch, checkout])";

						string errors;
						Tools.RunExeOutputErrors("git-lfs", "install", null, out errors, lfsCallback);
						if (!string.IsNullOrEmpty(errors))
						{
							Debug.LogError("Failed to init git-lfs on repo" + errorStringHelper, true);
							return false;
						}

						Tools.RunExeOutputErrors("git-lfs", "fetch", null, out errors, lfsCallback);
						if (!string.IsNullOrEmpty(errors))
						{
							Debug.LogError("Failed to fetch git-lfs files" + errorStringHelper, true);
							return false;
						}

						Tools.RunExeOutputErrors("git-lfs", "checkout", null, out errors, lfsCallback);
						if (!string.IsNullOrEmpty(errors))
						{
							Debug.LogError("Failed to checkout git-lfs files" + errorStringHelper, true);
							return false;
						}

						EnableGitLFSFilter();
					}

					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Clone error: " + e.Message, true);
				repoPath = null;
				return false;
			}
		}

		public static void ForceNewSettings()
		{
			settings = new XML.RepoSettings();
			userSettings = new XML.RepoUserSettings();
		}

		/// <summary>
		/// Saves open repo's settings
		/// </summary>
		public static void SaveSettings(string repoPathOverride = null)
		{
			bool canSave = repo != null;
			if (!string.IsNullOrEmpty(repoPathOverride)) canSave = true;
			else repoPathOverride = repoPath;
			if (canSave && !string.IsNullOrEmpty(repoPathOverride))
			{
				Settings.Save<XML.RepoSettings>(repoPathOverride + Path.DirectorySeparatorChar + Settings.repoSettingsFilename, settings);
				Settings.Save<XML.RepoUserSettings>(repoPathOverride + Path.DirectorySeparatorChar + Settings.repoUserSettingsFilename, userSettings);
			}
		}
		
		internal static void Dispose()
		{
			repoPath = null;

			if (repo != null)
			{
				repo.Dispose();
				repo = null;
			}
		}

		public static void UpdateSignatureValues(string name, string email)
		{
			userSettings.signatureName = name;
			userSettings.signatureEmail = email;
			signatureName = name;
			signatureEmail = email;
		}

		public static void UpdateCredentialValues(string username, string password)
		{
			userSettings.username = username;
			userSettings.password = password;
			credentialUsername = username;
			credentialPassword = password;
			credentials = new UsernamePasswordCredentials
			{
				Username = username,
				Password = password
			};
		}

		public static void UpdateValidateGitignore(bool validateGitignore)
		{
			validateGitignoreCheckbox = validateGitignore;
			settings.validateGitignore = validateGitignore;
		}

		internal static bool IsGitLFSRepo(bool returnTrueIfValidAttributes)
		{
			bool attributesExist = File.Exists(repoPath + Path.DirectorySeparatorChar + ".gitattributes");
			if (returnTrueIfValidAttributes && attributesExist)
			{
				string lines = File.ReadAllText(repoPath + Path.DirectorySeparatorChar + ".gitattributes");
				return lines.Contains("filter=lfs diff=lfs merge=lfs");
			}

			if (attributesExist && Directory.Exists(repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar)) && File.Exists(repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar)))
			{
				string data = File.ReadAllText(repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar));
				bool isValid = data.Contains("git-lfs");
				if (isValid)
				{
					EnableGitLFSFilter();
					return true;
				}
				else
				{
					DisableGitLFSFilter();
				}
			}

			return false;
		}

		private static void EnableGitLFSFilter()
		{
			// check if filter already active
			if (lfsFilterRegistration != null || GlobalSettings.GetRegisteredFilters().Contains(lfsFilterRegistration)) return;

			// create lfs filter
            var filteredFiles = new List<FilterAttributeEntry>()
            {
                new FilterAttributeEntry("lfs")
            };
            var filter = new Filters.GitLFS("lfs", filteredFiles);
            lfsFilterRegistration = GlobalSettings.RegisterFilter(filter);
		}

		private static void DisableGitLFSFilter()
		{
           GlobalSettings.DeregisterFilter(lfsFilterRegistration);
		   lfsFilterRegistration = null;
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
				if (!Directory.Exists(repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar)))
				{
					Tools.RunExe("git-lfs", "install", null);
					if (!Directory.Exists(repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar)))
					{
						Debug.LogError("Git-LFS install failed! (Try manually)", true);
						lfsEnabled = false;
						return false;
					}
				}

				// add attr file if it doesn't exist
				if (!File.Exists(repoPath + Path.DirectorySeparatorChar + ".gitattributes"))
				{
					using (var writer = File.CreateText(repoPath + Path.DirectorySeparatorChar + ".gitattributes"))
					{
						// this will be an empty file...
					}
				}

				// add default ext to git lfs
				if (addDefaultIgnoreExts)
				{
					foreach (string ext in AppManager.settings.defaultGitLFS_Exts)
					{
						Tools.RunExe("git-lfs", string.Format("track \"*{0}\"", ext), null);
					}
				}

				// TODO: validate ext types added successfully

				// finish
				lfsEnabled = true;
			}
			catch (Exception e)
			{
				Debug.LogError("Add Git-LFS Error: " + e.Message, true);
				Environment.Exit(0);// quit for safety as application should restart
				return false;
			}
			
			EnableGitLFSFilter();
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
				if (File.Exists(repoPath + Path.DirectorySeparatorChar + ".gitattributes"))
				{
					string data = File.ReadAllText(repoPath + Path.DirectorySeparatorChar + ".gitattributes");
					var values = Regex.Matches(data, @"(\*\..*)? filter=lfs diff=lfs merge=lfs");
					foreach (Match value in values)
					{
						if (value.Groups.Count != 2) continue;
						Tools.RunExe("git-lfs", string.Format("untrack \"{0}\"", value.Groups[1].Value), null);
					}
				}

				// remove lfs repo files
				Tools.RunExe("git-lfs", "uninstall", null);
				if (File.Exists(repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar))) File.Delete(repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar));
				if (Directory.Exists(repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar))) Directory.Delete(repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar), true);
					
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

			DisableGitLFSFilter();
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

					process.StartInfo.WorkingDirectory = repoPath;
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
			size = null;

			try
			{
				string errors;
				string result = Tools.RunExeOutputErrors("git", "count-objects", null, out errors);
				if (!string.IsNullOrEmpty(errors) || string.IsNullOrEmpty(result))
				{
					Debug.LogError("git gc errors: " + errors, true);
					return -1;
				}

				var match = Regex.Match(result, @"(\d*) objects, (\d* kilobytes)");
				if (match.Groups.Count != 3)
				{
					Debug.LogError("git gc invalid result: " + result, true);
					return -1;
				}
				
				size = match.Groups[2].Value;
				return int.Parse(match.Groups[1].Value);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to optamize: " + e.Message, true);
			}

			return -1;
		}

		public static void Optimize()
		{
			try
			{
				string errors;
				string result = Tools.RunExeOutputErrors("git", "gc", null, out errors);
				if (!string.IsNullOrEmpty(result)) Debug.Log("git gc result: " + result);
				if (!string.IsNullOrEmpty(errors)) Debug.LogError("git gc errors: " + errors, true);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to optamize: " + e.Message, true);
			}
		}
	}
}
