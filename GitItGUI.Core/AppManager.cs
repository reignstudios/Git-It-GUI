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
		private static string checkForUpdatesURL, checkForUpdatesOutOfDateURL;
		internal static XML.AppSettings settings;

		public static string mergeToolPath {get; private set;}
		public static MergeDiffTools mergeDiffTool {get; private set;}
		public static bool autoRefreshChanges;
		public static IReadOnlyList<XML.Repository> repositories {get{return settings.repositories;}}

		public static int MaxRepoHistoryCount = 20;

		private static WebClient client;
		#if WINDOWS
		private const string platform = "Windows";
		#elif MAC
		private const string platform = "Mac";
		#elif LINUX
		private const string platform = "Linux";
		#endif

		/// <summary>
		/// Must be called before using any other API feature
		/// </summary>
		/// <returns>True if succeeded</returns>
		public static bool Init()
		{
			try
			{
				// load settings
				string rootAppSettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
				settings = Settings.Load<XML.AppSettings>(rootAppSettingsPath + "\\" + Settings.appSettingsFolderName + "\\" + Settings.appSettingsFilename);

				// apply default lfs ignore types
				if (settings.defaultGitLFS_Exts.Count == 0)
				{
					settings.defaultGitLFS_Exts.AddRange(new List<string>()
					{
						".jpg", ".jpeg", ".png", ".bmp", ".tga", ".tif",// image types
						".psd",// image binary types
						".ai", ".svg", ".dwg",// vector binary types
						".ae",// video binary types
						".mpeg", ".mov", ".avi", ".mp4", ".wmv",// video types
						".wav", ".mp3", ".ogg", ".wma", ".acc",// audio types
						".zip", ".7z", ".rar", ".tar", ".gz",// compression types
						".fbx", ".obj", ".3ds", ".FBX", ".OBJ", ".3DS", ".blend", ".ma", ".mb", ".dae", ".daz", ".stl", ".wrl", ".spp", ".sbs", ".sppr", ".sbsar", ".ztl", ".zpr", ".obg",// 3d formats
						".pdf", ".doc", ".docx",// doc types
						".unity", ".unitypackage", ".uasset",// known binary types
						".bin", ".data", ".raw", ".hex",// unknown binary types
					});
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

		public static void SetMergeDiffTool(MergeDiffTools tool)
		{
			mergeDiffTool = tool;
			settings.mergeDiffTool = tool.ToString();
			LoadMergeDiffTool();
		}

		private static void LoadMergeDiffTool()
		{
			string programFilesx86, programFilesx64;
			Tools.GetProgramFilesPath(out programFilesx86, out programFilesx64);
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

		internal static void AddActiveRepoToHistory()
		{
			// add if doesn't exist
			var item = new XML.Repository()
			{
				path = RepoManager.repoPath
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
				settings.repositories.Add(item);
				var buff = settings.repositories[0];
				settings.repositories[0] = settings.repositories[settings.repositories.Count-1];
				settings.repositories[settings.repositories.Count-1] = buff;
			}
			else
			{
				var buff = settings.repositories[0];
				settings.repositories[0] = found;
				settings.repositories[index] = buff;
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
			string rootAppSettingsPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			Settings.Save<XML.AppSettings>(rootAppSettingsPath + "\\" + Settings.appSettingsFolderName + "\\" + Settings.appSettingsFilename, settings);
		}

		/// <summary>
		/// Disposes all manager objects (Call before app exit)
		/// </summary>
		public static void Dispose()
		{
			RepoManager.Dispose();
		}

		public static bool CheckForUpdates(string url, string outOfDateURL, CheckForUpdatesCallbackMethod checkForUpdatesCallback)
		{
			try
			{
				AppManager.checkForUpdatesCallback = checkForUpdatesCallback;
				checkForUpdatesURL = url;
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
				// get git and git-lfs version
				string gitVersion = null, gitlfsVersion = null;
				string gitlfsRequiredGitVersion = "0.0.0.0";

				try
				{
					gitVersion = Tools.RunExeOutput("git", "version", null);
				}
				catch
				{
					Debug.LogError("git is not installed correctly. (Make sure git is usable in the cmd/terminal)", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					return;
				}

				try
				{
					gitlfsVersion = Tools.RunExeOutput("git-lfs", "version", null);
				}
				catch
				{
					Debug.LogError("git-lfs is not installed correctly. (Make sure git-lfs is usable in the cmd/terminal)", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					return;
				}

				var match = Regex.Match(gitVersion, @"git version (.*)\.windows");
				if (match.Success && match.Groups.Count == 2) gitVersion = match.Groups[1].Value;
				else
				{
					Debug.LogError("Failed to grab git version!", true);
					client.Dispose();
					if (checkForUpdatesCallback != null) checkForUpdatesCallback(false, true);
					return;
				}
				
				match = Regex.Match(gitlfsVersion, @"git-lfs/(.*) \(GitHub; windows amd64; go (.*); git ");
				if (match.Success && match.Groups.Count == 3)
				{
					gitlfsVersion = match.Groups[1].Value;
					gitlfsRequiredGitVersion = match.Groups[2].Value;
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
					return;
				}

				// check versions
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
						else if (xmlReader.Name == "GitVersion")
						{
							while (xmlReader.Read())
							{
								if (xmlReader.Name == platform)
								{
									if (!IsValidVersion(gitVersion, xmlReader.ReadInnerXml()))
									{
										Debug.LogError("Your 'git' version is out of date.", true);
										using (var process = Process.Start("https://git-scm.com/downloads"))
										{
											process.WaitForExit();
										}
									}
								}

								if (xmlReader.Name == "GitVersion") break;
							}
						}
						else if (xmlReader.Name == "Git_LFS_Version")
						{
							while (xmlReader.Read())
							{
								if (xmlReader.Name == platform)
								{
									if (!IsValidVersion(gitlfsVersion, xmlReader.ReadInnerXml()))
									{
										Debug.LogError("Your 'git-lfs' version is out of date.", true);
										using (var process = Process.Start("https://git-lfs.github.com/"))
										{
											process.WaitForExit();
										}
									}
								}

								if (xmlReader.Name == "GitVersion") break;
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
