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

		public static Signature signature {get; private set;}
		public static UsernamePasswordCredentials credentials {get; private set;}

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
				if (!refreshMode && settings.validateGitignore)
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

				AppManager.AddActiveRepoToHistory();
				BranchManager.OpenRepo(repo);
			}
			catch (Exception e)
			{
				Debug.LogError("RepoManager.OpenRepo Failed: " + e.Message);
				Dispose();
				return false;
			}
			
			if (refreshMode) return true;
			else return RefreshInternal();
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

		/*public static bool OpenDiffTool(FileState fileState)// TODO
		{
			try
			{
				// get selected item
				var item = unstagedChangesListView.SelectedItem as FileItem;
				if (item == null) item = stagedChangesListView.SelectedItem as FileItem;
				var status = RepoUserControl.repo.RetrieveStatus(item.filename);
				if ((status & FileStatus.ModifiedInIndex) == 0 && (status & FileStatus.ModifiedInWorkdir) == 0)
				{
					MessageBox.Show("This file is not modified");
					return;
				}

				// get info and save orig file
				string fullPath = string.Format("{0}\\{1}", RepoUserControl.repoPath, item.filename);
				var changed = RepoUserControl.repo.Head.Tip[item.filename];
				Tools.SaveFileFromID(string.Format("{0}\\{1}.orig", RepoUserControl.repoPath, item.filename), changed.Target.Id);

				// open diff tool
				using (var process = new Process())
				{
					process.StartInfo.FileName = RepoUserControl.mergeToolPath;
					process.StartInfo.Arguments = string.Format("\"{0}.orig\" \"{0}\"", fullPath);
					process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
					if (!process.Start())
					{
						MessageBox.Show("Failed to start Diff tool (is it installed?)");

						// delete temp files
						if (File.Exists(fullPath + ".orig")) File.Delete(fullPath + ".orig");
						return;
					}

					process.WaitForExit();
				}

				// delete temp files
				if (File.Exists(fullPath + ".orig")) File.Delete(fullPath + ".orig");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to start Diff tool: " + ex.Message);
			}
		}*/

		/*private void openFile_Click(object sender, RoutedEventArgs e)// TODO
		{
			// check for common mistakes
			if (unstagedChangesListView.SelectedIndex < 0 && stagedChangesListView.SelectedIndex < 0)
			{
				MessageBox.Show("No file selected");
				return;
			}

			try
			{
				var item = unstagedChangesListView.SelectedItem as FileItem;
				if (item == null) item = stagedChangesListView.SelectedItem as FileItem;
				Process.Start("explorer.exe", string.Format("{0}\\{1}", RepoUserControl.repoPath, item.filename));
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to open folder location: " + ex.Message);
			}
		}

		private void openFileLocation_Click(object sender, RoutedEventArgs e)
		{
			// check for common mistakes
			if (unstagedChangesListView.SelectedIndex < 0 && stagedChangesListView.SelectedIndex < 0)
			{
				MessageBox.Show("No file selected");
				return;
			}

			try
			{
				var item = unstagedChangesListView.SelectedItem as FileItem;
				if (item == null) item = stagedChangesListView.SelectedItem as FileItem;
				Process.Start("explorer.exe", string.Format("/select, {0}\\{1}", RepoUserControl.repoPath, item.filename));
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to open folder location: " + ex.Message);
			}
		}

		private void revertFile_Click(object sender, RoutedEventArgs e)
		{
			// check for common mistakes
			if (stagedChangesListView.SelectedIndex >= 0)
			{
				MessageBox.Show("Unstage file before reverting!");
				return;
			}

			if (unstagedChangesListView.SelectedIndex < 0)
			{
				MessageBox.Show("No unstaged file selected");
				return;
			}

			var item = unstagedChangesListView.SelectedItem as FileItem;
			if (MessageBox.Show(string.Format("Are you sure you want to revert file '{0}'?", item.filename), "Revert?", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			{
				return;
			}

			try
			{
				// get selected item
				var status = RepoUserControl.repo.RetrieveStatus(item.filename);
				if ((status & FileStatus.ModifiedInIndex) == 0 && (status & FileStatus.ModifiedInWorkdir) == 0 && (status & FileStatus.DeletedFromIndex) == 0 && (status & FileStatus.DeletedFromWorkdir) == 0)
				{
					MessageBox.Show("This file is not modified or deleted");
					return;
				}
				
				var options = new CheckoutOptions();
				options.CheckoutModifiers = CheckoutModifiers.Force;
				RepoUserControl.repo.CheckoutPaths(RepoUserControl.repo.Head.FriendlyName, new string[] {item.filename}, options);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to open folder location: " + ex.Message);
			}

			RepoUserControl.Refresh();
		}*/
	}
}
