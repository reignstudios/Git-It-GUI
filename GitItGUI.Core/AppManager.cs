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
	class VersionNumber
	{
		public int major, minor, patch, build;
	}

	public enum UpdateCheckResult
	{
		CommonError,
		Success,
		AppVersionOutOfDate,
		AppVersionParseError,
		GitNotInstalledError,
		GitLFSNotInstalledError,
		BadVersionError,
		GitVersionCheckError,
		GitLFSVersionCheckError,
		GitVersionToLowForLFS
	}

	public delegate void CheckForUpdatesCallbackMethod(UpdateCheckResult result);

	/// <summary>
	/// Handles preliminary features
	/// </summary>
	public static class AppManager
	{
		public static XML.AppSettings settings {get; private set;}
		internal static List<string> defaultGitLFS_Exts;

		public static bool isMergeToolInstalled {get; private set;}
		public static string mergeToolPath {get; private set;}
		public static IReadOnlyList<string> repositories {get{return settings.repositories;}}

		public static int MaxRepoHistoryCount = 20;

		private static WebClient client;
		private static string platformName = "Unknown";

		static AppManager()
		{
			switch (PlatformInfo.platform)
			{
				case Platforms.Windows: platformName = "Windows"; break;
				case Platforms.Mac: platformName = "Mac"; break;
				case Platforms.Linux: platformName = "Linux"; break;
				default: throw new Exception("Unsupported platform: " + PlatformInfo.platform);
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
				settings = Settings.Load<XML.AppSettings>(PlatformInfo.appDataPath + seperator + Settings.appSettingsFolderName + seperator + Settings.appSettingsFilename);

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
					".unity", ".unitypackage", ".uasset", ".asset", ".exr",// known binary types
					".bin", ".data", ".raw", ".hex",// unknown binary types
				};

				defaultGitLFS_Exts = new List<string>();
				defaultGitLFS_Exts.AddRange(lowerCase);
				for (int i = 0; i != lowerCase.Count; ++i) defaultGitLFS_Exts.Add(lowerCase[i].ToUpper());

				// load
				LoadMergeDiffTool();
			}
			catch (Exception e)
			{
				DebugLog.LogError("AppManager.Init Failed: " + e.Message);
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
			settings.mergeDiffTool = tool;
			LoadMergeDiffTool();
		}

		private static void LoadMergeDiffTool()
		{
			if (PlatformInfo.platform == Platforms.Windows)
			{
				string programFilesx86, programFilesx64;
				PlatformInfo.GetWindowsProgramFilesPath(out programFilesx86, out programFilesx64);
				switch (settings.mergeDiffTool)
				{
					case MergeDiffTools.Meld: mergeToolPath = programFilesx86 + "\\Meld\\Meld.exe"; break;
					case MergeDiffTools.kDiff3: mergeToolPath = programFilesx64 + "\\KDiff3\\kdiff3.exe"; break;
					case MergeDiffTools.P4Merge: mergeToolPath = programFilesx64 + "\\Perforce\\p4merge.exe"; break;
					case MergeDiffTools.DiffMerge: mergeToolPath = programFilesx64 + "\\SourceGear\\Common\\\\DiffMerge\\sgdm.exe"; break;
				}
			}
			else if (PlatformInfo.platform == Platforms.Mac)
			{
				mergeToolPath = "";
			}
			else if (PlatformInfo.platform == Platforms.Linux)
			{
				mergeToolPath = "";
			}
			else
			{
				throw new Exception("Unsported platform: " + PlatformInfo.platform);
			}

			isMergeToolInstalled = File.Exists(mergeToolPath);
			if (!isMergeToolInstalled) DebugLog.LogWarning("Diff/Merge tool not installed: " + mergeToolPath);
		}

		public static void RemoveRepoFromHistory(string repoPath)
		{
			foreach (var repo in settings.repositories)
			{
				if (repo == repoPath)
				{
					settings.repositories.Remove(repo);
					return;
				}
			}
		}

		internal static void AddRepoToHistory(string repoPath)
		{
			// add if doesn't exist
			string found = null;
			int index = 0;
			foreach (var repo in settings.repositories)
			{
				if (repo == repoPath)
				{
					found = repo;
					break;
				}

				++index;
			}

			if (found == null)
			{
				settings.repositories.Insert(0, repoPath);
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
			Settings.Save<XML.AppSettings>(PlatformInfo.appDataPath + Path.DirectorySeparatorChar + Settings.appSettingsFolderName + Path.DirectorySeparatorChar + Settings.appSettingsFilename, settings);
		}

		/// <summary>
		/// Call before app exit after everything else
		/// </summary>
		public static void Dispose()
		{
			DebugLog.Dispose();
		}

		public static void CheckForUpdates(string url, CheckForUpdatesCallbackMethod checkForUpdatesCallback)
		{
			try
			{
				// validate git install
				var repository = new Repository();

				// git and git-lfs versions
				string gitVersion = null, gitlfsVersion = null;
				string gitlfsRequiredGitVersion = "0.0.0.0";
				const string minGitVersion = "2.13.0", minGitLFSVersion = "2.2.1";
				
				// get git version string
				try
				{
					if (!repository.GetVersion(out gitVersion)) throw new Exception(repository.lastError);
				}
				catch
				{
					DebugLog.LogError("git is not installed correctly. (Make sure git is usable in the cmd/terminal)");
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.GitNotInstalledError);
					DownloadGit();
					return;
				}

				// get git-lfs version string
				try
				{
					if (!repository.lfs.GetVersion(out gitlfsVersion)) throw new Exception(repository.lastError);
				}
				catch
				{
					DebugLog.LogError("git-lfs is not installed correctly. (Make sure git-lfs is usable in the cmd/terminal)");
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.GitLFSNotInstalledError);
					DownloadGitLFS();
					return;
				}

				// grab git version value
				string appendix = "";
				if (PlatformInfo.platform == Platforms.Windows) appendix = @"\.windows";
				var match = Regex.Match(gitVersion, @"git version (.*)" + appendix);
				if (match.Success && match.Groups.Count == 2) gitVersion = match.Groups[1].Value;
				else
				{
					DebugLog.LogError("Failed to grab git version!");
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.GitVersionCheckError);
					DownloadGit();
					return;
				}
				
				// grab lfs and required git version value
				if (PlatformInfo.platform == Platforms.Windows) appendix = @"; git .*\)";
				else appendix = @"\)";
				match = Regex.Match(gitlfsVersion, @"git-lfs/(.*) \(GitHub; (\w*) (\w*); go (.*)" + appendix);
				if (match.Success && match.Groups.Count == 5)
				{
					gitlfsVersion = match.Groups[1].Value;
					gitlfsRequiredGitVersion = match.Groups[4].Value;
				}
				else
				{
					DebugLog.LogError("Failed to grab git-lfs version!");
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.GitLFSVersionCheckError);
					DownloadGitLFS();
					return;
				}

				// make sure the git version installed is supporeted by lfs
				if (!IsValidVersion(gitVersion, gitlfsRequiredGitVersion))
				{
					DebugLog.LogError(string.Format("'git-lfs' version is not compatible with 'git' version installed!"));
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.GitVersionToLowForLFS);
					DownloadGit();
					DownloadGitLFS();
					return;
				}

				// check min git versions
				bool gitValid = true, gitlfsValid = true;
				if (!IsValidVersion(gitVersion, minGitVersion))
				{
					DebugLog.LogError("Your 'git' version is out of date.\nDownload and install with defaults!");
					gitValid = false;
				}

				if (!IsValidVersion(gitlfsVersion, minGitLFSVersion))
				{
					DebugLog.LogError("Your 'git-lfs' version is out of date.\nDownload and install with defaults!");
					gitlfsValid = false;
				}

				if (!gitValid || !gitlfsValid)
				{
					if (!gitValid) DownloadGit();
					if (!gitlfsValid) DownloadGitLFS();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.BadVersionError);
					return;
				}

				// check app version
				client = new WebClient();
				client.DownloadStringCompleted += Client_DownloadStringCompleted;
				client.DownloadStringAsync(new Uri(url), checkForUpdatesCallback);
			}
			catch (Exception e)
			{
				DebugLog.LogError("Failed to check for updates: " + e.Message);
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.CommonError);
			}
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
			var checkForUpdatesCallback = (CheckForUpdatesCallbackMethod)e.UserState;

			if (e.Error != null)
			{
				DebugLog.LogError("Failed to check for updates: " + e.Error.Message);
				client.Dispose();
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.CommonError);
				return;
			}

			if (e.Cancelled)
			{
				DebugLog.LogError("Update check canceled!");
				client.Dispose();
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.CommonError);
				return;
			}

			try
			{
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
								DebugLog.LogError("Your 'Git-It-GUI' version is out of date.");
								if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.AppVersionOutOfDate);
							}
						}
					}
				}

				if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.Success);
			}
			catch (Exception ex)
			{
				DebugLog.LogError("Failed to get version info!\nMake sure git and git-lfs are installed\nAlso make sure you're connected to the internet: \n\n" + ex.Message);
				if (checkForUpdatesCallback != null) checkForUpdatesCallback(UpdateCheckResult.AppVersionParseError);
			}
			
			client.Dispose();
		}
	}
}
