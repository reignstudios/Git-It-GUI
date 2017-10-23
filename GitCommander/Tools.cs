using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace GitCommander
{
	public delegate void StdCallbackMethod(string line);
	public delegate bool StdInputStreamCallbackMethod(StreamWriter writer);
	public delegate void GetStdInputStreamCallbackMethod(StreamWriter writer);
	public delegate void GetStdOutputStreamCallbackMethod(StreamReader reader);

	public partial class Repository
	{
		public event StdCallbackMethod RunExeDebugLineCallback, StdCallback, StdErrorCallback, StdWarningCallback;
		private List<string> errorPrefixes;

		private void InitTools()
		{
			errorPrefixes = new List<string>()
			{
				"error:",
				"fatal:"
			};
		}

		public void AddErrorCode(string errorCode)
		{
			errorCode = errorCode.ToLower();
			if (!errorPrefixes.Contains(errorCode)) errorPrefixes.Add(errorCode);
		}
		
		private (string output, string errors) RunExe
		(
			string exe, string arguments, string workingDirectory = null,
			StdInputStreamCallbackMethod stdInputStreamCallback = null, GetStdInputStreamCallbackMethod getStdInputStreamCallback = null,
			GetStdOutputStreamCallbackMethod getStdOutputStreamCallback = null,
			StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null,
			bool stdResultOn = true, bool stdErrorResultOn = true,
			string stdOutToFilePath = null
		)
		{
			if (RunExeDebugLineCallback != null) RunExeDebugLineCallback(string.Format("GitCommander: {0} {1}", exe, arguments));
			if (stdCallback != null) stdResultOn = false;

			string output = "", errors = "";
			using (var process = new Process())
			{
				// setup start info
				process.StartInfo.FileName = exe;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.WorkingDirectory = workingDirectory == null ? repoPath : workingDirectory;
				process.StartInfo.RedirectStandardInput = stdInputStreamCallback != null || getStdInputStreamCallback != null;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
				process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
				
				FileStream stdOutStream = null;
				StreamWriter stdOutStreamWriter = null;
				if (stdOutToFilePath != null && getStdOutputStreamCallback == null)
				{
					stdOutToFilePath = repoPath + Path.DirectorySeparatorChar + stdOutToFilePath.Replace('/', Path.DirectorySeparatorChar);
					stdOutStream = new FileStream(stdOutToFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
					stdOutStreamWriter = new StreamWriter(stdOutStream, Encoding.UTF8);
				}

				var outDataReceived = new StdCallbackMethod(delegate(string line)
				{
					if (stdOutToFilePath != null) stdOutStreamWriter.WriteLine(line);
					
					if (stdResultOn)
					{
						if (output.Length != 0) output += Environment.NewLine;
						output += line;
					}

					if (stdCallback != null) stdCallback(line);
					if (line.StartsWith("warning:"))
					{
						if (StdWarningCallback != null) StdWarningCallback(line);
					}
					else
					{
						if (StdCallback != null) StdCallback(line);
					}
				});
				
				process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					string line = e.Data;
					if (line == null) return;
					outDataReceived(line);
				};

				process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
				{
					string line = e.Data;
					if (line == null) return;
					
					// valid true error
					string lineLower = line.ToLower();
					bool isError = false;
					foreach (var prefix in errorPrefixes)
					{
						if (lineLower.StartsWith(prefix))
						{
							isError = true;
							break;
						}
					}

					// if not error use normal stdout callbacks
					if (!isError)
					{
						outDataReceived(line);
						return;
					}

					// invoke error callbacks
					if (stdErrorResultOn)
					{
						if (errors.Length != 0) errors += Environment.NewLine;
						errors += line;
					}

					if (stdErrorCallback != null) stdErrorCallback(line);
					if (StdErrorCallback != null) StdErrorCallback(line);
				};

				// start process
				process.Start();
				if (getStdInputStreamCallback != null) getStdInputStreamCallback(process.StandardInput);
				if (getStdOutputStreamCallback == null) process.BeginOutputReadLine();
				else getStdOutputStreamCallback(process.StandardOutput);
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
				if (stdOutStreamWriter != null) stdOutStreamWriter.Flush();
				if (stdOutStream != null)
				{
					stdOutStream.Flush();
					stdOutStream.Dispose();
				}
			}

			return (output, errors);
		}

		public void RunGenericCmd(string cmd)
		{
			const string prefix = "git ";
			if (!cmd.StartsWith(prefix))
			{
				if (RunExeDebugLineCallback != null) RunExeDebugLineCallback("Only git commands permitted");
				return;
			}

			cmd = cmd.Remove(0, prefix.Length);
			RunExe("git", cmd);
		}
	}
}
