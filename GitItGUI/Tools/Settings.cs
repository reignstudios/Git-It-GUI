using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;

namespace GitItGUI
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
			[XmlAttribute("MergeDiffTool")] public string mergeDiffTool = "Meld";
			[XmlElement("Repository")] public List<Repository> repositories = new List<Repository>();
			[XmlElement("DefaultGitLFS-Ext")] public List<string> defaultGitLFS_Exts = new List<string>();
		}

		[XmlRoot("RepoSettings")]
		public class RepoSettings
		{
			[XmlAttribute("LFSSupport")] public bool lfsSupport = true;
			[XmlAttribute("ValidateGitignore")] public bool validateGitignore = true;
		}

		[XmlRoot("RepoUserSettings")]
		public class RepoUserSettings
		{
			[XmlAttribute("SignatureName")] public string signatureName = "First Last";
			[XmlAttribute("SignatureEmail")] public string signatureEmail = "username@email.com";
			[XmlAttribute("Username")] public string username = "Username";
			[XmlAttribute("Password")] public string password = "password";
		}
	}

	static class Settings
	{
		public const string GuiFilename = "Settings.xml";
		public const string RepoFilename = ".gitgamegui";
		public const string RepoUserFilename = ".gitgamegui-user";

		public static T Load<T>(string filename) where T : new()
		{
			if (!File.Exists(filename)) return new T();

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
				MessageBox.Show("Load Settings Error: " + e.Message);
				return new T();
			}
		}

		public static bool Save<T>(string filename, T settings)
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
				MessageBox.Show("Save Settings Error: " + e.Message);
				return false;
			}

			return true;
		}
	}
}
