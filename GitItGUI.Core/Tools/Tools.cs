using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace GitItGUI.Core
{
	static class Tools
	{
		public static void GetProgramFilesPath(out string programFilesx86, out string programFilesx64)
		{
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))) programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			else programFilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			programFilesx64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles).Replace(" (x86)", "");
		}

		public static bool IsBinaryFileData(Stream stream, bool disposeStream = false)
		{
			const int maxByteRead = 1024 * 1024 * 2;

			// if the file is to large consider a data file (2mb)
			if (stream.Length > maxByteRead)
			{
				if (disposeStream) stream.Dispose();
				return true;
			}

			// check for \0 characters and if they accure before the end of file, this is a data file
			int b = stream.ReadByte();
			while (b != -1)
			{
				if (b == 0 && stream.Position < maxByteRead)
				{
					if (disposeStream) stream.Dispose();
					return true;
				}

				b = stream.ReadByte();
			}

			if (disposeStream) stream.Dispose();
			return false;
		}

		public static bool IsBinaryFileData(string filename)
		{
			using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				return IsBinaryFileData(stream);
			}
		}

		public static bool IsGitLFSPtr(string data)
		{
			if (data.Length >= 1024) return false;
			var match = Regex.Match(data, @"version https://git-lfs.github.com/spec/v1.*oid sha256:.*size\s\n*", RegexOptions.Singleline);
			return match.Success;
		}

		public static void RunExe(string exe, string arguments, string input, bool hideWindow = true)
		{
			var process = new Process();
			process.StartInfo.FileName = exe;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.WorkingDirectory = RepoManager.repoPath;
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

		public static string RunExeOutput(string exe, string arguments, string input, bool hideWindow = true)
		{
			var process = new Process();
			process.StartInfo.FileName = exe;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.WorkingDirectory = RepoManager.repoPath;
			process.StartInfo.RedirectStandardInput = input != null;
			process.StartInfo.RedirectStandardOutput = true;
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

			return process.StandardOutput.ReadToEnd();
		}

		//public static bool LaunchCoreApp(string exe, string arguments, out string type, out string value)
		//{
		//	type = null;
		//	value = null;
		//	var process = new Process();
		//	process.StartInfo.FileName = exe;
		//	process.StartInfo.Arguments = arguments;
		//	process.StartInfo.WorkingDirectory = RepoManager.repoPath;
		//	process.StartInfo.RedirectStandardOutput = true;
		//	process.StartInfo.UseShellExecute = false;
		//	process.Start();
		//	process.WaitForExit();

		//	var result = process.StandardOutput.ReadToEnd();
		//	var values = result.Split(':');
		//	if (values.Length != 2)
		//	{
		//		Debug.LogWarning("Invalid merge app response: " + result, true);
		//		return false;
		//	}

		//	if (values[0] == "ERROR")
		//	{
		//		Debug.LogWarning("Response error: " + values[1], true);
		//		return false;
		//	}
		//	else if (values[0] == "SUCCEEDED")
		//	{
		//		switch (values[1])
		//		{
		//			case "Canceled": return false;
		//			case "KeepMine": File.Copy(fullPath + ".ours", fullPath, true); break;
		//			case "UseTheirs": File.Copy(fullPath + ".theirs", fullPath, true); break;
		//			default: Debug.LogWarning("Response error: " + values[1], true); return false;
		//		}

		//		RepoManager.repo.Stage(fileState.filename);
		//	}

		//	return true;
		//}

		public static bool IsSingleWord(string value)
		{
			foreach (char c in value)
			{
				switch (c)
				{
					case 'a':
					case 'b':
					case 'c':
					case 'd':
					case 'e':
					case 'f':
					case 'g':
					case 'h':
					case 'i':
					case 'j':
					case 'k':
					case 'l':
					case 'm':
					case 'n':
					case 'o':
					case 'p':
					case 'q':
					case 'r':
					case 's':
					case 't':
					case 'u':
					case 'v':
					case 'w':
					case 'x':
					case 'y':
					case 'z':

					case 'A':
					case 'B':
					case 'C':
					case 'D':
					case 'E':
					case 'F':
					case 'G':
					case 'H':
					case 'I':
					case 'J':
					case 'K':
					case 'L':
					case 'M':
					case 'N':
					case 'O':
					case 'P':
					case 'Q':
					case 'R':
					case 'S':
					case 'T':
					case 'U':
					case 'V':
					case 'W':
					case 'X':
					case 'Y':
					case 'Z':

					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':

					case '_':

						continue;
					default: return false;
				}
			}

			return true;
		}

		public static void SaveFileFromID(string filename, ObjectId id)
		{
			// get info
			var blob = RepoManager.repo.Lookup<Blob>(id);

			if (blob.Size < 1024 && IsGitLFSPtr(blob.GetContentText()))// check if lfs tracked file
			{
				if (!RepoManager.lfsEnabled)
				{
					throw new Exception("Critical error: Git-LFS is not installed but repo contains git-lfs pointers!");
				}

				// get lfs data from ptr
				using (var process = new Process())
				{
					process.StartInfo.FileName = "git-lfs";
					process.StartInfo.Arguments = "smudge";
					process.StartInfo.WorkingDirectory = RepoManager.repoPath;
					process.StartInfo.RedirectStandardInput = true;
					process.StartInfo.RedirectStandardOutput = true;
					process.StartInfo.RedirectStandardError = true;
					process.StartInfo.CreateNoWindow = true;
					process.StartInfo.UseShellExecute = false;

					process.Start();
					using (var inStream = blob.GetContentStream())
					{
						inStream.CopyTo(process.StandardInput.BaseStream);
						inStream.Flush();
					}

					process.StandardInput.Flush();
					process.StandardInput.Close();
					using (var outStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
					{
						process.StandardOutput.BaseStream.CopyTo(outStream);
						process.StandardOutput.BaseStream.Flush();
						process.StandardOutput.Close();
						outStream.Flush();
					}

					process.WaitForExit();
				}
			}
			else// if lfs fails try standard
			{
				// copy original
				using (var inStream = blob.GetContentStream())
				using (var outStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					inStream.CopyTo(outStream);
				}
			}
		}
	}
}
