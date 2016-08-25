using System;

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

		public static void Log(object value, bool alert = false)
		{
			Console.WriteLine("GitItGUI.Core Log: " + value.ToString());
			if (debugLogCallback != null) debugLogCallback(value, alert);
		}

		public static void LogWarning(object value, bool alert = false)
		{
			Console.WriteLine("GitItGUI.Core Log WARNING: " + value.ToString());
			if (debugLogWarningCallback != null) debugLogWarningCallback(value, alert);
		}

		public static void LogError(object value, bool alert = false)
		{
			Console.WriteLine("GitItGUI.Core Log ERROR: " + value.ToString());
			if (debugLogErrorCallback != null) debugLogErrorCallback(value, alert);
		}
	}
}
