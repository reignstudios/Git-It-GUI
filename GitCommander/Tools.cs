using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommander
{
	public delegate void StdCallbackMethod(string line);
	public delegate bool StdInputCallbackMethod(StreamWriter writer);

	public class StdInput
	{
		public StdInputCallbackMethod GetStdInputStreamCallback;
		public string input;
		public bool autoCloseInputAfterDone = true;
	}

	public static class Tools
	{
		public static (string stdResult, string stdErrorResult) RunExe(string exe, string arguments, StdInput stdInput = null, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null, bool stdResultOn = true)
		{
			if (stdCallback != null) stdResultOn = false;

			string outputErr = "", output = "", errors = "";
			using (var process = new Process())
			{
				// setup start info
				process.StartInfo.FileName = exe;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = Repository.repoPath;
				process.StartInfo.RedirectStandardInput = stdInput != null;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;

				process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						if (stdCallback != null) stdCallback(e.Data);
						if (stdResultOn)
						{
							if (output.Length != 0) output += Environment.NewLine;
							output += e.Data;
						}
					}
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (!string.IsNullOrEmpty(e.Data))
					{
						if (stdErrorCallback != null) stdErrorCallback(e.Data);
						if (outputErr.Length != 0) outputErr += Environment.NewLine;
						outputErr += e.Data;
					}
				};

				// start process
				process.Start();
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				// write input
				if (stdInput != null)
				{
					while (stdInput.GetStdInputStreamCallback != null)
					{
						if (stdInput.GetStdInputStreamCallback(process.StandardInput)) break;
					}

					if (stdInput.input != null)
					{
						process.StandardInput.WriteLine(stdInput.input);
						process.StandardInput.Flush();
						if (stdInput.autoCloseInputAfterDone) process.StandardInput.Close();
					}
				}

				// wait for process to finish
				process.WaitForExit();
				errors = outputErr;
			}

			return (output, errors);
		}
	}
}
