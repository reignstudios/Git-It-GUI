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
		public static bool pauseGitCommanderStdWrites;

		static Debug()
		{
			try
			{
				string logFileName = PlatformSettings.appDataPath;
				logFileName += Path.DirectorySeparatorChar + Settings.appSettingsFolderName + Path.DirectorySeparatorChar + "logs.txt";
				stream = new FileStream(logFileName, FileMode.Create, FileAccess.Write, FileShare.None);
				writer = new StreamWriter(stream);

				// bind events
				GitCommander.Tools.RunExeDebugLineCallback += Tools_RunExeDebugLineCallback;
				GitCommander.Tools.StdCallback += Tools_StdCallback;
				GitCommander.Tools.StdWarningCallback += Tools_StdWarningCallback;
				GitCommander.Tools.StdErrorCallback += Tools_StdErrorCallback;
			}
			catch (Exception e)
			{
				LogError("Failed to init debug log file: " + e.Message);
			}
		}

		private static void Tools_RunExeDebugLineCallback(string line)
		{
			Log(line);
		}

		private static void Tools_StdCallback(string line)
		{
			if (!pauseGitCommanderStdWrites) Log(line);
		}

		private static void Tools_StdWarningCallback(string line)
		{
			LogWarning(line);
		}

		private static void Tools_StdErrorCallback(string line)
		{
			LogError(line);
		}
		
		public static void Dispose()
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
			lock (stream)
			{
				string msg = value.ToString();

				#if DEBUG
				Console.WriteLine(msg);
				#endif

				if (writer != null) writer.WriteLine(msg);
				if (debugLogCallback != null) debugLogCallback(value, alert);
			}
		}

		public static void LogWarning(object value, bool alert = false)
		{
			lock (stream)
			{
				string msg = "WARNING: " + value.ToString();

				#if DEBUG
				Console.WriteLine(msg);
				#endif

				if (writer != null) writer.WriteLine(msg);
				if (debugLogWarningCallback != null) debugLogWarningCallback(value, alert);
			}
		}

		public static void LogError(object value, bool alert = false)
		{
			lock (stream)
			{
				string msg = "ERROR: " + value.ToString();

				#if DEBUG
				Console.WriteLine(msg);
				#endif

				if (writer != null) writer.WriteLine(msg);
				if (debugLogErrorCallback != null) debugLogErrorCallback(value, alert);
			}
		}
	}
}
