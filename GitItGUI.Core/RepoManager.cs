using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
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

		internal static Signature signature {get {return new Signature(userSettings.signatureName, userSettings.signatureEmail, DateTimeOffset.UtcNow);}}
		internal static UsernamePasswordCredentials credentials {get; private set;}

		/// <summary>
		/// Use to open an existing repo
		/// </summary>
		/// <param name="path">Path to git repo</param>
		/// <returns>True if succeeded</returns>
		public static bool OpenRepo(string path)
		{
			// unload repo
			if (string.IsNullOrEmpty(path))
			{
				Dispose();
				return true;
			}

			bool refreshMode = path == repoPath;
			
			try
			{
				// load repo
				repoPath = path;
				repo = new Repository(path);

				// check for git lfs
				lfsEnabled = IsGitLFSRepo();

				// load settings
				settings = Settings.Load<XML.RepoSettings>(path + "\\" + Settings.repoSettingsFilename);
				userSettings = Settings.Load<XML.RepoUserSettings>(path + "\\" + Settings.repoUserSettingsFilename);

				// check for .gitignore file
				validateGitignoreCheckbox = settings.validateGitignore;
				if (!refreshMode && settings.validateGitignore)
				{
					if (!File.Exists(path + "\\.gitignore"))
					{
						Debug.LogWarning("No '.gitignore' file exists.\nMake sure you add one!", true);
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

		/// <summary>
		/// Saves open repo's settings
		/// </summary>
		public static void SaveSettings()
		{
			if (!string.IsNullOrEmpty(repoPath) && repo != null)
			{
				Settings.Save<XML.RepoSettings>(repoPath + "\\" + Settings.repoSettingsFilename, settings);
				Settings.Save<XML.RepoUserSettings>(repoPath + "\\" + Settings.repoUserSettingsFilename, userSettings);
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

		private static bool IsGitLFSRepo()
		{
			if (Directory.Exists(repoPath + "\\.git\\lfs") && File.Exists(repoPath + "\\.gitattributes") && File.Exists(repoPath + "\\.git\\hooks\\pre-push"))
			{
				string data = File.ReadAllText(repoPath + "\\.git\\hooks\\pre-push");
				return data.Contains("git-lfs");
			}

			return false;
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
				if (!Directory.Exists(repoPath + "\\.git\\lfs"))
				{
					Tools.RunExe("git-lfs", "install", null);
					if (!Directory.Exists(repoPath + "\\.git\\lfs"))
					{
						Debug.LogError("Git-LFS install failed! (Try manually)", true);
						lfsEnabled = false;
						return false;
					}
				}

				// add attr file if it doesn't exist
				if (!File.Exists(repoPath + "\\.gitattributes"))
				{
					using (var writer = File.CreateText(repoPath + "\\.gitattributes"))
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
				if (File.Exists(repoPath + "\\.gitattributes"))
				{
					string data = File.ReadAllText(repoPath + "\\.gitattributes");
					var values = Regex.Matches(data, @"(\*\..*)? filter=lfs diff=lfs merge=lfs");
					foreach (Match value in values)
					{
						if (value.Groups.Count != 2) continue;
						Tools.RunExe("git-lfs", string.Format("untrack \"{0}\"", value.Groups[1].Value), null);
					}
				}

				// remove lfs repo files
				Tools.RunExe("git-lfs", "uninstall", null);
				if (File.Exists(repoPath + "\\.git\\hooks\\pre-push")) File.Delete(repoPath + "\\.git\\hooks\\pre-push");
				if (Directory.Exists(repoPath + "\\.git\\lfs")) Directory.Delete(repoPath + "\\.git\\lfs", true);
					
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
				return false;
			}

			return true;
		}

		public static void OpenGitk()
		{
			// get gitk path
			string programFilesx86, programFilesx64;
			Tools.GetProgramFilesPath(out programFilesx86, out programFilesx64);

			// open gitk
			var process = new Process();
			process.StartInfo.FileName = programFilesx64 + "\\Git\\cmd\\gitk.exe";
			process.StartInfo.WorkingDirectory = repoPath;
			process.StartInfo.Arguments = "";
			process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
			if (!process.Start())
			{
				Debug.LogError("Failed to start history tool (is it installed?)", true);
				return;
			}

			process.WaitForExit();
			Refresh();
		}
	}
}
