using System;
using System.IO;
using GitCommander.System;

namespace GitItGUI.Core
{
	public static class DebugLog
	{
		public delegate void WriteCallbackMethod(string value, bool alert);
		public static event WriteCallbackMethod WriteCallback;

		private static Stream stream;
		private static StreamWriter writer;

		static DebugLog()
		{
			try
			{
				string logDir = PlatformInfo.appDataPath + Path.DirectorySeparatorChar + Settings.appSettingsFolderName;
				if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
				string logFileName = logDir + Path.DirectorySeparatorChar + "logs.txt";
				stream = new FileStream(logFileName, FileMode.Create, FileAccess.Write, FileShare.None);
				writer = new StreamWriter(stream);
			}
			catch (Exception e)
			{
				LogError("Failed to init debug log file: " + e.Message);
			}
		}
		
		public static void Dispose()
		{
			if (stream != null)
			{
				lock (stream)
				{
					writer.Flush();
					stream.Flush();
					writer.Dispose();
					stream.Dispose();
					stream = null;
				}
			}
		}

		private static void Write(string value, bool alert)
		{
			if (stream == null) return;
			lock (stream)
			{
				if (stream == null) return;

				#if DEBUG
				Console.WriteLine(value);
				#endif

				if (writer != null) writer.WriteLine(value);
				if (WriteCallback != null) WriteCallback(value, alert);
			}
		}

		public static void Log(object value, bool alert = false)
		{
			Write(value.ToString(), alert);
		}

		public static void LogWarning(object value, bool alert = false)
		{
			Write("WARNING: " + value.ToString(), alert);
		}

		public static void LogError(object value, bool alert = false)
		{
			Write("ERROR: " + value.ToString(), alert);
		}
	}
}
