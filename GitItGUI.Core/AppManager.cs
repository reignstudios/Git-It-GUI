using GitCommander;
using GitCommander.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace GitItGUI.Core
{
	public enum MergeDiffTools
	{
		Meld,
		kDiff3,
		P4Merge,
		DiffMerge
	}

	class VersionNumber
	{
		public int major, minor, patch, build;
	}

	public delegate void CheckForUpdatesCallbackMethod(bool succeeded, bool invalidFeatures);

	/// <summary>
	/// Handles preliminary features
	/// </summary>
	public static class AppManager
	{
		private static CheckForUpdatesCallbackMethod checkForUpdatesCallback;
		private static string checkForUpdatesOutOfDateURL;
		internal static XML.AppSettings settings;

		public static string mergeToolPath {get; private set;}
		public static MergeDiffTools mergeDiffTool {get; private set;}
		public static bool autoRefreshChanges;
		public static IReadOnlyList<XML.Repository> repositories {get{return settings.repositories;}}

		public static int MaxRepoHistoryCount = 20;

		private static WebClient client;
		private static string platformName = "Unknown";

		static AppManager()
		{
			switch (PlatformSettings.platform)
			{
				case Platforms.Windows: platformName = "Windows"; break;
				case Platforms.Mac: platformName = "Mac"; break;
				case Platforms.Linux: platformName = "Linux"; break;
				default: throw new Exception("Unsupported platform: " + PlatformSettings.platform);
			}
		}

		/// <summary>
		/// Must be called before using any other API feature
		/// </summary>
		/// <returns>True if succeeded</returns>
		public static bool Init()
		{
			try
			{
				// load settings
				char seperator = Path.DirectorySeparatorChar;
				settings = Settings.Load<XML.AppSettings>(PlatformSettings.appDataPath + seperator + Settings.appSettingsFolderName + seperator + Settings.appSettingsFilename);

				// apply default lfs ignore types
				var lowerCase = new List<string>()
				{
					".jpg", ".jpeg", ".png", ".bmp", ".tga", ".tif",// image types
					".psd",// image binary types
					".ai", ".svg", ".dwg",// vector binary types
					".ae",// video binary types
					".mpeg", ".mov", ".avi", ".mp4", ".wmv",// video types
					".wav", ".mp3", ".ogg", ".wma", ".acc",// audio types
					".zip", ".7z", ".rar", ".tar", ".gz",// compression types
					".fbx", ".obj", ".3ds", ".blend", ".ma", ".mb", ".dae", ".daz", ".stl", ".wrl", ".spp", ".sbs", ".sppr", ".sbsar", ".ztl", ".zpr", ".obg",// 3d formats
					".pdf", ".doc", ".docx",// doc types
					".unity", ".unitypackage", ".uasset",// known binary types
					".bin", ".data", ".raw", ".hex",// unknown binary types
				};

				var upperCase = new List<string>();
				for (int i = 0; i != lowerCase.Count; ++i) upperCase.Add(lowerCase[i].ToUpper());

				foreach (var item in lowerCase)
				{
					if (!settings.defaultGitLFS_Exts.Contains(item)) settings.defaultGitLFS_Exts.Add(item);
				}

				foreach (var item in upperCase)
				{
					if (!settings.defaultGitLFS_Exts.Contains(item)) settings.defaultGitLFS_Exts.Add(item);
				}

				// load
				LoadMergeDiffTool();
				autoRefreshChanges = settings.autoRefreshChanges;
			}
			catch (Exception e)
			{
				Debug.LogError("AppManager.Init Failed: " + e.Message);
				Dispose();
				return false;
			}

			return true;
		}

		public static bool MergeDiffToolInstalled()
		{
			return File.Exists(mergeToolPath);
		}

		public static void SetMergeDiffTool(MergeDiffTools tool)
		{
			mergeDiffTool = tool;
			settings.mergeDiffTool = tool.ToString();
			LoadMergeDiffTool();
		}

		private static void LoadMergeDiffTool()
		{
			if (PlatformSettings.platform == Platforms.Windows)
			{
				string programFilesx86, programFilesx64;
				PlatformSettings.GetWindowsProgramFilesPath(out programFilesx86, out programFilesx64);
				switch (settings.mergeDiffTool)
				{
					case "Meld":
						mergeDiffTool = MergeDiffTools.Meld;
						mergeToolPath = programFilesx86 + "\\Meld\\Meld.exe";
						break;

					case "kDiff3":
						mergeDiffTool = MergeDiffTools.kDiff3;
						mergeToolPath = programFilesx64 + "\\KDiff3\\kdiff3.exe";
						break;

					case "P4Merge":
						mergeDiffTool = MergeDiffTools.P4Merge;
						mergeToolPath = programFilesx64 + "\\Perforce\\p4merge.exe"; 
						break;

					case "DiffMerge":
						mergeDiffTool = MergeDiffTools.DiffMerge;
						mergeToolPath = programFilesx64 + "\\SourceGear\\Common\\\\DiffMerge\\sgdm.exe";
						break;
				}
			}
			else if (PlatformSettings.platform == Platforms.Mac)
			{
				mergeDiffTool = MergeDiffTools.Meld;
				mergeToolPath = "";
			}
			else if (PlatformSettings.platform == Platforms.Linux)
			{
				mergeDiffTool = MergeDiffTools.Meld;
				mergeToolPath = "";
			}
			else
			{
				throw new Exception("Unsported platform: " + PlatformSettings.platform);
			}
		}

		public static void RemoveRepoFromHistory(string repoPath)
		{
			foreach (var repo in settings.repositories)
			{
				if (repo.path == repoPath)
				{
					settings.repositories.Remove(repo);
					return;
				}
			}
		}

		internal static void AddActiveRepoToHistory()
		{
			// add if doesn't exist
			var item = new XML.Repository()
			{
				path = Repository.repoPath
			};

			XML.Repository found = null;
			int index = 0;
			foreach (var repo in settings.repositories)
			{
				if (repo.path == item.path)
				{
					found = repo;
					break;
				}

				++index;
			}

			if (found == null)
			{
				settings.repositories.Insert(0, item);
			}
			else if (index != 0) 
			{
				var buff = settings.repositories[0];
				settings.repositories.RemoveAt(index);
				settings.repositories.RemoveAt(0);
				settings.repositories.Insert(0, found);
				settings.repositories.Insert(1, buff);
			}

			// trim
			if (settings.repositories.Count > MaxRepoHistoryCount)
			{
				settings.repositories.RemoveAt(settings.repositories.Count - 1);
			}
		}

		/// <summary>
		/// Saves all app settings
		/// </summary>
		public static void SaveSettings()
		{
			settings.autoRefreshChanges = autoRefreshChanges;
			Settings.Save<XML.AppSettings>(PlatformSettings.appDataPath + Path.DirectorySeparatorChar + Settings.appSettingsFolderName + Path.DirectorySeparatorChar + Settings.appSettingsFilename, settings);
		}

		/// <summary>
		/// Disposes all manager objects (Call before app exit)
		/// </summary>
		public static void Dispose()
		{
			RepoManager.Dispose();
			Debug.Dispose();
		}

		public static bool CheckForUpdates(string url, string outOfDateURL, CheckForUpdatesCallbackMethod checkForUpdatesCallback)
		{
			try
			{
				AppManager.checkForUpdatesCallback = checkForUpdatesCallback;
				checkForUpdatesOutOfDateURL = outOfDateURL;

				client = new WebClient();
				client.DownloadStringCompleted += Client_DownloadStringCompleted;
				client.DownloadStringAsync(new Uri(url));
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to check for updates: " + e.Message, true);
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, false);
			}

			return true;
		}

		private static VersionNumber GetVersionNumber(string version)
		{
			var result = new VersionNumber();
			var values = version.Split('.');
			int i = 0;
			foreach (var value in values)
			{
				int num = 0;
				int.TryParse(value, out num);
				if (i == 0) result.major = num;
				else if (i == 1) result.minor = num;
				else if (i == 2) result.patch = num;
				else if (i == 3) result.build = num;
				else break;

				++i;
			}

			return result;
		}

		private static bool IsValidVersion(string currentVersion, string requiredVersion)
		{
			var v1 = GetVersionNumber(currentVersion);
			var v2 = GetVersionNumber(requiredVersion);
			if (v1.major > v2.major)
			{
				return true;
			}
			else if (v1.major < v2.major)
			{
				return false;
			}
			else if (v1.major == v2.major)
			{
				if (v1.minor > v2.minor)
				{
					return true;
				}
				else if (v1.minor < v2.minor)
				{
					return false;
				}
				else
				{
					if (v1.patch > v2.patch)
					{
						return true;
					}
					else if (v1.patch < v2.patch)
					{
						return false;
					}
					else
					{
						if (v1.build >= v2.build)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private static void DownloadGit()
		{
			using (var process = Process.Start("https://git-scm.com/downloads"))
			{
				process.WaitForExit();
			}
		}

		private static void DownloadGitLFS()
		{
			using (var process = Process.Start("https://git-lfs.github.com/"))
			{
				process.WaitForExit();
			}
		}

		private static void Client_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				Debug.LogError("Failed to check for updates: " + e.Error.Message, true);
				client.Dispose();
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, false);
				return;
			}

			if (e.Cancelled)
			{
				Debug.LogError("Update check canceled!", true);
				client.Dispose();
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, false);
				return;
			}

			try
			{
				// git and git-lfs versions
				string gitVersion = null, gitlfsVersion = null;
				string gitlfsRequiredGitVersion = "0.0.0.0";
				const string minGitVersion = "2.11.0", minGitLFSVersion = "1.5.5";

				// get git version string
				try
				{
					if (!Repository.GetVersion(out gitVersion)) throw new Exception(Repository.lastError);
				}
				catch
				{
					Debug.LogError("git is not installed correctly. (Make sure git is usable in the cmd/terminal)", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					DownloadGit();
					return;
				}

				// get git-lfs version string
				try
				{
					if (!Repository.LFS.GetVersion(out gitlfsVersion)) throw new Exception(Repository.lastError);
				}
				catch
				{
					Debug.LogError("git-lfs is not installed correctly. (Make sure git-lfs is usable in the cmd/terminal)", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					DownloadGitLFS();
					return;
				}

				// grab git version value
				string appendix = "";
				if (PlatformSettings.platform == Platforms.Windows) appendix = @"\.windows";
				var match = Regex.Match(gitVersion, @"git version (.*)" + appendix);
				if (match.Success && match.Groups.Count == 2) gitVersion = match.Groups[1].Value;
				else
				{
					Debug.LogError("Failed to grab git version!", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					return;
				}
				
				// grab lfs and required git version value
				if (PlatformSettings.platform == Platforms.Windows) appendix = @"; git .*\)";
				else appendix = @"\)";
				match = Regex.Match(gitlfsVersion, @"git-lfs/(.*) \(GitHub; (\w*) (\w*); go (.*)" + appendix);
				if (match.Success && match.Groups.Count == 5)
				{
					gitlfsVersion = match.Groups[1].Value;
					gitlfsRequiredGitVersion = match.Groups[4].Value;
				}
				else
				{
					Debug.LogError("Failed to grab git-lfs version!", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					return;
				}

				// make sure the git version installed is supporeted by lfs
				if (!IsValidVersion(gitVersion, gitlfsRequiredGitVersion))
				{
					Debug.LogError(string.Format("'git-lfs' version is not compatible with 'git' version installed!"), true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					DownloadGit();
					DownloadGitLFS();
					return;
				}

				// check min git versions
				bool gitValid = true, gitlfsValid = true;
				if (!IsValidVersion(gitVersion, minGitVersion))
				{
					Debug.LogError("Your 'git' version is out of date.\nDownload and install with defaults!", true);
					gitValid = false;
				}

				if (!IsValidVersion(gitlfsVersion, minGitLFSVersion))
				{
					Debug.LogError("Your 'git-lfs' version is out of date.\nDownload and install with defaults!", true);
					gitlfsValid = false;
				}

				if (!gitValid || !gitlfsValid)
				{
					if (!gitValid) DownloadGit();
					if (!gitlfsValid) DownloadGitLFS();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					return;
				}

				// check app version
				bool canCheckAppVersion = true;
				using (var reader = new StringReader(e.Result))
				using (var xmlReader = new XmlTextReader(reader))
				{
					while (xmlReader.Read())
					{
						if (canCheckAppVersion && xmlReader.Name == "AppVersion")
						{
							canCheckAppVersion = false;
							if (!IsValidVersion(VersionInfo.version, xmlReader.ReadInnerXml()))
							{
								Debug.LogError("Your 'Git-It-GUI' version is out of date.", true);
								using (var process = Process.Start(checkForUpdatesOutOfDateURL))
								{
									process.WaitForExit();
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to get version info!\nMake sure git and git-lfs are installed\nAlso make sure you're connected to the internet: \n\n" + ex.Message, true);
			}

			client.Dispose();
			if (checkForUpdatesCallback != null) checkForUpdatesCallback(true, false);
		}
	}
}
