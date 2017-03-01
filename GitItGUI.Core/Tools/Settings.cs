using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace GitItGUI.Core
{
	namespace XML
	{
		public class Repository
		{
			[XmlText] public string path = "";
		}

		[XmlRoot("AppSettings")]
		public class AppSettings
		{
			[XmlAttribute("MergeDiffTool")] public string mergeDiffTool = "P4Merge";
			[XmlAttribute("AutoRefreshChanges")] public bool autoRefreshChanges = true;
			[XmlElement("Repository")] public List<Repository> repositories = new List<Repository>();
			[XmlElement("DefaultGitLFS-Ext")] public List<string> defaultGitLFS_Exts = new List<string>();
		}

		[XmlRoot("RepoSettings")]
		public class RepoSettings
		{
			[XmlAttribute("ValidateGitignore")] public bool validateGitignore = true;
		}

		[XmlRoot("RepoUserSettings")]
		public class RepoUserSettings
		{
			[XmlAttribute("SignatureName")] public string signatureName = "TODO: First Last";
			[XmlAttribute("SignatureEmail")] public string signatureEmail = "TODO: username@email.com";
			[XmlAttribute("Username")] public string username = "TODO: Username";
			[XmlAttribute("Password")] public string password = "";
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
				Debug.LogError("Load Settings Error: " + e.Message, true);
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
				Debug.LogError("Save Settings Error: " + e.Message, true);
				return false;
			}

			return true;
		}
	}
}
