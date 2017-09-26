using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace GitItGUI.Core
{
	namespace XML
	{
		public class CustomErrorCodes
		{
			[XmlElement("ErrorCode")] public List<string> errorCodes = new List<string>();
		}

		[XmlRoot("AppSettings")]
		public class AppSettings
		{
			[XmlAttribute("WinX")] public int winX = -1;
			[XmlAttribute("WinY")] public int winY = -1;
			[XmlAttribute("WinWidth")] public int winWidth = -1;
			[XmlAttribute("WinHeight")] public int winHeight = -1;
			[XmlAttribute("MergeDiffTool")] public string mergeDiffTool = "P4Merge";
			[XmlAttribute("AutoRefreshChanges")] public bool autoRefreshChanges = true;
			[XmlElement("CustomErrorCodes")] public CustomErrorCodes customErrorCodes = new CustomErrorCodes();
			[XmlElement("Repository")] public List<string> repositories = new List<string>();
			[XmlElement("DefaultGitLFS-Ext")] public List<string> defaultGitLFS_Exts = new List<string>();
		}
	}
	
	public static class Settings
	{
		public const string appSettingsFolderName = "GitItGUI";
		public const string appSettingsFilename = "GitItGUI_Settings.xml";
		public const string repoSettingsFilename = ".gititgui";
		public const string repoUserSettingsFilename = ".gititgui-user";

		public static T Load<T>(string filename) where T : new()
		{
			if (!File.Exists(filename))
			{
				var settings = new T();
				Save<T>(filename, settings);
				return settings;
			}

			try
			{
				var xml = new XmlSerializer(typeof(T));
				using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
				{
					return (T)xml.Deserialize(stream);
				}
			}
			catch (Exception e)
			{
				DebugLog.LogError("Load Settings Error: " + e.Message, true);
				return new T();
			}
		}

		public static bool Save<T>(string filename, T settings) where T : new()
		{
			string path = Path.GetDirectoryName(filename);
			if (!Directory.Exists(path)) Directory.CreateDirectory(path);

			try
			{
				var xml = new XmlSerializer(typeof(T));
				using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					xml.Serialize(stream, settings);
				}
			}
			catch (Exception e)
			{
				DebugLog.LogError("Save Settings Error: " + e.Message, true);
				return false;
			}

			return true;
		}
	}
}
