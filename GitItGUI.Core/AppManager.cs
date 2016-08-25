using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	/// <summary>
	/// Handles preliminary features
	/// </summary>
	public static class AppManager
	{
		internal static XML.AppSettings settings;

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
						".psd", ".jpg", ".jpeg", ".png", ".bmp", ".tga",// image types
						".mpeg", ".mov", ".avi", ".mp4", ".wmv",// video types
						".wav", ".mp3", ".ogg", ".wma", ".acc",// audio types
						".zip", ".7z", ".rar", ".tar", ".gz",// compression types
						".fbx", ".obj", ".3ds", ".blend", ".ma", ".mb", ".dae",// 3d formats
						".pdf",// doc types
						".bin", ".data", ".raw", ".hex",// unknown binary types
					});
				}
			}
			catch (Exception e)
			{
				Debug.LogError("AppManager.Init Failed: " + e.Message);
				Dispose();
				return false;
			}

			return true;
		}

		/// <summary>
		/// Disposes all manager objects (Call before app exit)
		/// </summary>
		public static void Dispose()
		{
			RepoManager.Dispose();
			BranchManager.Dispose();
		}
	}
}
