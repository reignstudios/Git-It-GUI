using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommander
{
	static class Tools
	{
		public static void RunExe(string exe, string arguments, string input, bool hideWindow = true)
		{
			using (var process = new Process())
			{
				process.StartInfo.FileName = exe;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = Repository.repoPath;
				process.StartInfo.RedirectStandardInput = input != null;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = hideWindow;
				process.Start();
				if (input != null)
				{
					process.StandardInput.WriteLine(input);
					process.StandardInput.Flush();
					process.StandardInput.Close();
				}
				process.WaitForExit();
			}
		}

		public static string RunExeOutput(string exe, string arguments, string input, out string errors, bool hideWindow = true)
		{
			string outputErr = "", output = "";
			using (var process = new Process())
			{
				process.StartInfo.FileName = exe;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = Repository.repoPath;
				process.StartInfo.RedirectStandardInput = input != null;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = hideWindow;

				process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (e.Data != null) output += e.Data + Environment.NewLine;
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (e.Data != null) outputErr += e.Data + Environment.NewLine;
				};

				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				if (input != null)
				{
					process.StandardInput.WriteLine(input);
					process.StandardInput.Flush();
					process.StandardInput.Close();
				}

				process.WaitForExit();
				errors = outputErr;
			}

			return output;
		}
	}
}
