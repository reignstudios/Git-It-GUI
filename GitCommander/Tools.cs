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
		public delegate void RunExeCallbackMethod(string line);
		
		public static string RunExe(string exe, string arguments, string input, out string errors, RunExeCallbackMethod stdCallback = null, RunExeCallbackMethod stdErrorCallback = null)
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
				process.StartInfo.CreateNoWindow = true;

				process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						if (stdCallback != null) stdCallback(e.Data);
						else output += e.Data + Environment.NewLine;
					}
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						if (stdErrorCallback != null) stdErrorCallback(e.Data);
						else outputErr += e.Data + Environment.NewLine;
					}
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
