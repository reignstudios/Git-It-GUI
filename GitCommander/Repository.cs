using System.IO;
using System.Text.RegularExpressions;

namespace GitCommander
{
	public enum SignatureLocations
	{
		Local,
		Global
	}

    public static partial class Repository
    {
		public static bool isOpen {get; private set;}
		public static string lastResult {get; private set;}
		public static string lastError {get; private set;}

		public static string repoURL {get; private set;}
		public static string repoPath {get; private set;}

		public static void Close()
		{
			isOpen = false;
			lastResult = null;
			lastError = null;
			repoURL = null;
			repoPath = null;
		}

		private static bool SimpleGitInvoke(string args, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
		{
			var result = Tools.RunExe("git", args, stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
			lastResult = result.Item1;
			lastError = result.Item2;

			return string.IsNullOrEmpty(lastError);
		}
		
		public static bool Clone(string url, string path, out string repoClonedPath, StdInputStreamCallbackMethod writeUsernameCallback, StdInputStreamCallbackMethod writePasswordCallback)
		{
			StreamWriter stdInWriter = null;
			void getStdInputStreamCallback(StreamWriter writer)
			{
				stdInWriter = writer;
			}
			
			string repoClonedPathTemp = null;
			void stdCallback(string line)
			{
				if (line.StartsWith("Cloning into"))
				{
					var match = Regex.Match(line, @"Cloning into '(.*)'\.\.\.");
					if (match.Success) repoClonedPathTemp = match.Groups[1].Value;
				}
			}

			void stdErrorCallback(string line)
			{
				if (line.StartsWith("Username for"))
				{
					if (writeUsernameCallback == null || !writeUsernameCallback(stdInWriter)) stdInWriter.WriteLine("");
				}
				else if (line.StartsWith("Password for"))
				{
					if (writePasswordCallback == null || !writePasswordCallback(stdInWriter)) stdInWriter.WriteLine("");
				}
			}
			
			var result = Tools.RunExe("git", string.Format("clone \"{0}\"", url), workingDirectory:path, getStdInputStreamCallback:getStdInputStreamCallback, stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
			lastResult = result.Item1;
			lastError = result.Item2;
			
			repoClonedPath = repoClonedPathTemp;
			return string.IsNullOrEmpty(lastError);
		}

		public static bool Open(string path)
		{
			Close();

			void stdCallback(string line)
			{
				repoURL = line;
			}
			
			var result = Tools.RunExe("git", "rev-parse --git-dir", workingDirectory:path);
			lastResult = result.Item1;
			lastError = result.Item2;
			if (!string.IsNullOrEmpty(lastError)) return false;
			
			// get repo url
			repoURL = "";
			result = Tools.RunExe("git", "ls-remote --get-url", stdCallback:stdCallback, workingDirectory:path);
			lastResult = result.Item1;
			lastError = result.Item2;
			
			repoPath = path;
			return isOpen = true;
		}

		public static void ForceOpen(string repoPath, string repoURL)
		{
			Close();
			Repository.repoPath = repoPath;
			Repository.repoURL = repoURL;
			isOpen = true;
		}

		public static bool GetSignature(SignatureLocations location, out string name, out string email)
		{
			name = null;
			email = null;
			string globalValue = (location == SignatureLocations.Global) ? " --global" : "";

			bool result = SimpleGitInvoke(string.Format("config{0} user.name", globalValue));
			name = lastResult;
			if (!result) return false;

			result = SimpleGitInvoke(string.Format("config{0} user.email", globalValue));
			email = lastResult;
			return result;
		}

		public static bool SetSignature(SignatureLocations location, string name, string email)
		{
			string globalValue = (location == SignatureLocations.Global) ? " --global" : "";
			bool result = SimpleGitInvoke(string.Format("config{1} user.name \"{0}\"", name, globalValue));
			name = lastResult;
			if (!result) return false;

			result = SimpleGitInvoke(string.Format("config{1} user.email \"{0}\"", email, globalValue));
			email = lastResult;
			return result;
		}

		public static bool UnpackedObjectCount(out int count, out string size)
		{
			bool result = SimpleGitInvoke("count-objects");
			if (!string.IsNullOrEmpty(lastError) || string.IsNullOrEmpty(lastResult))
			{
				count = -1;
				size = null;
				return false;
			}

			var match = Regex.Match(lastResult, @"(\d*) objects, (\d* kilobytes)");
			if (match.Groups.Count != 3)
			{
				count = -1;
				size = null;
				return false;
			}
			
			count = int.Parse(match.Groups[1].Value);
			size = match.Groups[2].Value;
			return true;
		}

		public static bool GarbageCollect()
		{
			return SimpleGitInvoke("gc");
		}

		public static bool GetVersion(out string version)
		{
			bool result = SimpleGitInvoke("version");
			version = lastResult;
			return result;
		}
    }
}
