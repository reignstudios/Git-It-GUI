using System;
using System.IO;
using GitCommander.System;

namespace GitItGUI.Core
{
	/// <summary>
	/// Use to hook internal logging methods
	/// </summary>
	/// <param name="value">Log value (usally a string)</param>
	/// <param name="alert">Whether or not the UI should alert the user. (Normally a MessageBox)</param>
	public delegate void DebugLogCallbackMethod(object value, bool alert);

	public static class Debug
	{
		/// <summary>
		/// Use to hook internal logging methods
		/// </summary>
		public static event DebugLogCallbackMethod debugLogCallback, debugLogWarningCallback, debugLogErrorCallback;

		private static Stream stream;
		private static StreamWriter writer;

		static Debug()
		{
			try
			{
				string logFileName = PlatformSettings.appDataPath;
				logFileName += Path.DirectorySeparatorChar + Settings.appSettingsFolderName + Path.DirectorySeparatorChar + "logs.txt";
				stream = new FileStream(logFileName, FileMode.Create, FileAccess.Write, FileShare.None);
				writer = new StreamWriter(stream);
			}
			catch (Exception e)
			{
				LogError("Failed to init debug log file: " + e.Message);
			}
		}

		internal static void Dispose()
		{
			if (stream != null)
			{
				writer.Flush();
				stream.Flush();
				stream.Dispose();
				stream = null;
			}
		}

		public static void Log(object value, bool alert = false)
		{
			string msg = value.ToString();
			Console.WriteLine(msg);
			if (writer != null) writer.WriteLine(msg);
			if (debugLogCallback != null) debugLogCallback(value, alert);
		}

		public static void LogWarning(object value, bool alert = false)
		{
			string msg = "WARNING: " + value.ToString();
			Console.WriteLine(msg);
			if (writer != null) writer.WriteLine(msg);
			if (debugLogWarningCallback != null) debugLogWarningCallback(value, alert);
		}

		public static void LogError(object value, bool alert = false)
		{
			string msg = "ERROR: " + value.ToString();
			Console.WriteLine(msg);
			if (writer != null) writer.WriteLine(msg);
			if (debugLogErrorCallback != null) debugLogErrorCallback(value, alert);
		}
	}
}
