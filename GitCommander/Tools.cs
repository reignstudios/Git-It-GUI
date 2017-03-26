using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitCommander.System;

namespace GitCommander
{
	public delegate void StdCallbackMethod(string line);
	public delegate bool StdInputStreamCallbackMethod(StreamWriter writer);
	public delegate void GetStdInputStreamCallbackMethod(StreamWriter writer);

	public static class Tools
	{
		public static event StdCallbackMethod StdCallback, StdErrorCallback;
		
		public static Tuple<string, string> RunExe
		(
			string exe, string arguments, string workingDirectory = null,
			StdInputStreamCallbackMethod stdInputStreamCallback = null, GetStdInputStreamCallbackMethod getStdInputStreamCallback = null,
			StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null,
			bool stdResultOn = true, bool stdErrorResultOn = true,
			string stdOutToFilePath = null
		)
		{
			if (stdCallback != null) stdResultOn = false;

			string output = "", errors = "";
			using (var process = new Process())
			{
				// setup start info
				process.StartInfo.FileName = exe;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = workingDirectory == null ? Repository.repoPath : workingDirectory;
				process.StartInfo.RedirectStandardInput = stdInputStreamCallback != null || getStdInputStreamCallback != null;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;

				if (PlatformSettings.platform == Platforms.Mac)
				{
					process.StartInfo.EnvironmentVariables["PATH"] = "/usr/local/bin";
				}

				FileStream stdOutStream = null;
				StreamWriter stdOutStreamWriter = null;
				if (stdOutToFilePath != null)
				{
					stdOutToFilePath = Repository.repoPath + Path.DirectorySeparatorChar + stdOutToFilePath;
					stdOutStream = new FileStream(stdOutToFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
					stdOutStreamWriter = new StreamWriter(stdOutStream);
				}
				
				process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (e.Data == null) return;

					if (stdOutToFilePath != null)
					{
						stdOutStreamWriter.WriteLine(e.Data);
						stdOutStreamWriter.Flush();
						stdOutStream.Flush();
					}
					
					if (stdCallback != null) stdCallback(e.Data);
					if (stdResultOn)
					{
						if (output.Length != 0) output += Environment.NewLine;
						output += e.Data;
					}

					if (StdCallback != null) StdCallback(e.Data);
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					if (e.Data == null) return;

					if (stdErrorCallback != null) stdErrorCallback(e.Data);
					if (stdErrorResultOn)
					{
						if (errors.Length != 0) errors += Environment.NewLine;
						errors += e.Data;
					}

					if (StdErrorCallback != null) StdErrorCallback(e.Data);
				};

				// start process
				process.Start();
				if (getStdInputStreamCallback != null) getStdInputStreamCallback(process.StandardInput);
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				// write input
				if (stdInputStreamCallback != null)
				{
					while (!stdInputStreamCallback(process.StandardInput)) Thread.Sleep(1);
					process.StandardInput.Flush();
					process.StandardInput.Close();
				}

				// wait for process to finish
				process.WaitForExit();

				// close stdOut file
				if (stdOutStreamWriter != null) stdOutStreamWriter.Dispose();
				if (stdOutStream != null)
				{
					stdOutStream.Close();
					stdOutStream.Dispose();
				}
			}

			return new Tuple<string, string>(output, errors);
		}
	}
}
