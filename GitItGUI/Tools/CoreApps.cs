using GitItGUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Tools
{
	static class CoreApps
	{
		public static bool LaunchCoreApp(string exe, string arguments, out string type, out string value)
		{
			type = null;
			value = null;
			var process = new System.Diagnostics.Process();
			process.StartInfo.FileName = exe;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.WorkingDirectory = RepoManager.repoPath;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.UseShellExecute = false;
			process.Start();
			process.WaitForExit();

			var result = process.StandardOutput.ReadToEnd();
			var values = result.Split(':');
			if (values.Length != 2)
			{
				Debug.LogWarning("Invalid merge app response: " + result, true);
				return false;
			}

			type = values[0];
			value = values[1];
			if (values[0] == "ERROR")
			{
				Debug.LogWarning("Response error: " + values[1], true);
				return false;
			}
			else if (values[0] == "SUCCEEDED")
			{
				return true;
			}

			Debug.LogWarning("Unknown Error", true);
			return false;
		}
	}
}
