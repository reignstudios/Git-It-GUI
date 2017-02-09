using GitItGUI.Core;
using System;

namespace GitItGUI.Tools
{
	static class CoreApps
	{
		private static bool LaunchCoreApp(string exe, string arguments, out string type, out string value, out string valueMessage, bool alertEnabled)
		{
			type = null;
			value = null;
			valueMessage = null;
			using (var process = new System.Diagnostics.Process())
			{
				process.StartInfo.FileName = exe;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = RepoManager.repoPath;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.UseShellExecute = false;
				process.Start();
				process.WaitForExit();

				var result = process.StandardOutput.ReadToEnd();
				var values = result.Split(':');
				if (values.Length != 2 && values.Length != 3)
				{
					Debug.LogWarning("Invalid core app response: " + result, alertEnabled);
					return false;
				}

				type = values[0];
				value = values[1];
				if (values.Length == 3) valueMessage = values[2];
				if (values[0] == "ERROR")
				{
					Debug.LogWarning("Response error: " + values[1], alertEnabled);
					return false;
				}
				else if (values[0] == "SUCCEEDED")
				{
					return true;
				}
			}

			Debug.LogWarning("Unknown Error", alertEnabled);
			return false;
		}

		public static bool LaunchNameEntry(string caption, out string result)
		{
			string exe = Environment.CurrentDirectory + "\\NameEntry.exe";
			string args = string.Format("-Caption=\"{0}\"", caption);
			string type, value;
			if (!LaunchCoreApp(exe, args, out type, out value, out result, true)) return false;
			
			return value == "Ok";
		}

		public static bool LaunchCommitEntry(string message, out string result)
		{
			string exe = Environment.CurrentDirectory + "\\CommitEntry.exe";
			string args = string.Format("-Message=\"{0}\"", message);
			string type, value;
			if (!LaunchCoreApp(exe, args, out type, out value, out result, true)) return false;
			
			return value == "Ok";
		}

		public static bool LaunchMessageBox(string title, string message, MessageBoxTypes messageBoxType, out string result)
		{
			string exe = Environment.CurrentDirectory + "\\MessageBox.exe";
			string args = string.Format("-Title=\"{0}\" -Message=\"{1}\" -Type={2}", title, message, "Ok");
			string type, value;
			if (!LaunchCoreApp(exe, args, out type, out value, out result, false)) return false;
			
			return value == "Ok";
		}

		public static bool LaunchBinaryConflicPicker(string fileInConflic, out string result)
		{
			string exe = Environment.CurrentDirectory + "\\BinaryConflicPicker.exe";
			string args = string.Format("-FileInConflic=\"{0}\"", fileInConflic);
			string type, value;
			if (!LaunchCoreApp(exe, args, out type, out value, out result, false)) return false;
			
			result = value;
			return value != "Canceled";
		}
	}
}
