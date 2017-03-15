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
		public delegate void RunExeCallbackMethod(string stdLine);

		public static void RunExe(string exe, string arguments, string input, RunExeCallbackMethod stdCallback = null)
		{
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
					}
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						if (stdCallback != null) stdCallback(e.Data);
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
			}
		}

		public static string RunExeOutput(string exe, string arguments, string input, RunExeCallbackMethod stdCallback = null)
		{
			string output = "";
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
						output += e.Data + Environment.NewLine;
						if (stdCallback != null) stdCallback(e.Data);
					}
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						if (stdCallback != null) stdCallback(e.Data);
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
			}

			return output;
		}

		public static string RunExeOutputErrors(string exe, string arguments, string input, out string errors, RunExeCallbackMethod stdCallback = null)
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
						output += e.Data + Environment.NewLine;
						if (stdCallback != null) stdCallback(e.Data);
					}
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						outputErr += e.Data + Environment.NewLine;
						if (stdCallback != null) stdCallback(e.Data);
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
