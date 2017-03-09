using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
					break;

				case PlatformID.MacOSX:
					platform = Platforms.Mac;
					break;

				case PlatformID.Unix:
					platform = Platforms.Linux;
					break;
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
