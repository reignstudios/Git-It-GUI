using GitItGUI.Core;
using System;
using System.Windows.Forms;
using System.Windows.Interop;

namespace GitItGUI.UI.Utils
{
	public enum Platforms
	{
		Windows,
		Mac,
		Linux
	}

	static class Platform
    {
		public static readonly Platforms platform;
		public static readonly string appDataPath;

		static Platform()
		{
			var osPlatform = Environment.OSVersion.Platform;
			switch (osPlatform)
			{
				case PlatformID.Win32NT:
				case PlatformID.WinCE:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
					platform = Platforms.Windows;
					appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
					break;

				case PlatformID.MacOSX:
					platform = Platforms.Mac;
					appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					break;

				case PlatformID.Unix:
					platform = IsUnixMac() ? Platforms.Mac : Platforms.Linux;
					appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					break;
			}
		}

		private static bool IsUnixMac()
		{
			try
			{
				using (var process = new System.Diagnostics.Process())
				{
					process.StartInfo.UseShellExecute = false;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.FileName = "uname";
					process.Start();
					process.WaitForExit();
					string output = process.StandardOutput.ReadToEnd();
					if (output.Contains("Darwin")) return true;
					return false;
				}
			}
			catch
			{
				return false;
			}
		}

		public static void GetWindowsProgramFilesPath(out string programFilesx86, out string programFilesx64)
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))) programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			else programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			programFilesx64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Replace(" (x86)", "");
		}

		public static string ConvertPathToPlatform(string path)
		{
			if (platform == Platforms.Windows) return path.Replace('/', '\\');
			else return path;
		}

		public static bool SelectFolder(out string folderPath)
		{
			var dlg = new FolderBrowserDialog();
			var nativeWindow = new NativeWindow();
			nativeWindow.AssignHandle(new WindowInteropHelper(MainWindow.singleton).Handle);
			if (dlg.ShowDialog(nativeWindow) == DialogResult.OK)
			{
				folderPath = dlg.SelectedPath;
				return true;
			}

			folderPath = null;
			return false;
		}

		public static void OpenFile(string filePath)
		{
			try
			{
				if (platform == Platforms.Windows)
				{
					System.Diagnostics.Process.Start("explorer.exe", filePath);
				}
				else
				{
					throw new Exception("Unsuported platform: " + platform);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to open file: " + ex.Message, true);
			}
		}

		public static void OpenFileLocation(string filePath)
		{
			try
			{
				if (platform == Platforms.Windows)
				{
					System.Diagnostics.Process.Start("explorer.exe", string.Format("/select, {0}", filePath));
				}
				else
				{
					throw new Exception("Unsuported platform: " + platform);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to open folder location: " + ex.Message, true);
			}
		}
	}
}
