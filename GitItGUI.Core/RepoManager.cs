using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	public delegate void RepoRefreshedCallbackMethod();

	/// <summary>
	/// Primary git manager
	/// </summary>
	public static class RepoManager
	{
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
		
		private static XML.RepoSettings settings;
		private static XML.RepoUserSettings userSettings;

		internal static Signature signature;
		internal static UsernamePasswordCredentials credentials;

		/// <summary>
		/// Use to open an existing repo
		/// </summary>
		/// <param name="path">Path to git repo</param>
		/// <returns>True if succeeded</returns>
		public static bool OpenRepo(string path)
		{
			try
			{
				// load repo
				RepoManager.repoPath = path;
				repo = new Repository(path);

				// check for git lfs
				lfsEnabled = IsGitLFSRepo();

				// load settings
				settings = Settings.Load<XML.RepoSettings>(path + "\\" + Settings.repoSettingsFilename);
				userSettings = Settings.Load<XML.RepoUserSettings>(path + "\\" + Settings.repoUserSettingsFilename);

				// check for .gitignore file
				if (settings.validateGitignore)
				{
					if (!File.Exists(path + "\\.gitignore"))
					{
						Debug.LogWarning("No '.gitignore' file exists.\nMake sure you add one!", true);
					}
				}

				// create user objects
				signature = new Signature(userSettings.signatureName, userSettings.signatureEmail, DateTimeOffset.UtcNow);
				credentials = new UsernamePasswordCredentials
				{
					Username = userSettings.username,
					Password = userSettings.password
				};
			}
			catch (Exception e)
			{
				Debug.LogError("RepoManager.OpenRepo Failed: " + e.Message);
				Dispose();
				return false;
			}

			//ChangesManager.Refresh();
			return true;
		}

		public static void Refresh()
		{
			OpenRepo(repoPath);
			if (RepoRefreshedCallback != null) RepoRefreshedCallback();
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
			return Directory.Exists(repoPath + "\\.git\\lfs") && File.Exists(repoPath + "\\.gitattributes") && File.Exists(repoPath + "\\.git\\hooks\\pre-push");
		}
		
		public static bool AddGitLFSSupport(bool addDefaultIgnoreExts)
		{
			// check if already init
			if (lfsEnabled)
			{
				Debug.LogWarning("Git LFS already enabled on repo");
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
						Debug.LogError("Git-LFS install failed! (Try manually)");
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
				Debug.LogError("Add Git-LFS Error: " + e.Message);
				return false;
			}
			
			return true;
		}

		public static bool RemoveGitLFSSupport(bool rebase)
		{
			// check if not init
			if (!lfsEnabled)
			{
				Debug.LogWarning("Git LFS is not enabled on repo");
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
				Debug.LogError("Remove Git-LFS Error: " + e.Message);
				return false;
			}

			return true;
		}
	}
}
