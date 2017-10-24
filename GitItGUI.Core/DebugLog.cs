using System;
using System.IO;
using GitCommander.System;

namespace GitItGUI.Core
{
	public static class DebugLog
	{
		public delegate void WriteCallbackMethod(string value);
		public static event WriteCallbackMethod WriteCallback;

		private static Stream stream;
		private static StreamWriter writer;

		static DebugLog()
		{
			try
			{
				string logDir = Path.Combine(PlatformInfo.appDataPath, Settings.appSettingsFolderName);
				if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
				string logFileName = Path.Combine(logDir, "logs.txt");
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

		private static void Write(string value)
		{
			if (stream == null) return;
			lock (stream)
			{
				if (stream == null) return;

				#if DEBUG
				Console.WriteLine(value);
				#endif

				if (writer != null) writer.WriteLine(value);
				if (WriteCallback != null) WriteCallback(value);
			}
		}

		public static void Log(object value)
		{
			Write(value.ToString());
		}

		public static void LogWarning(object value)
		{
			Write("WARNING: " + value.ToString());
		}

		public static void LogError(object value)
		{
			Write("ERROR: " + value.ToString());
		}
	}
}
