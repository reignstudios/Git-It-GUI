using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GitItGUI.Core
{
	public enum Platforms
	{
		Windows,
		Mac,
		Linux
	}

	public static class PlatformSettings
	{
		public static readonly Platforms platform;
		public static readonly string appDataPath;

		static PlatformSettings()
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
				using (var process = new Process())
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
	}
}
