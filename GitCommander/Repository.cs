using System.IO;
using System.Text.RegularExpressions;

namespace GitCommander
{
	public enum SignatureLocations
	{
		Local,
		Global
	}

    public partial class Repository
    {
		public bool isOpen {get; private set;}
		public string lastResult {get; private set;}
		public string lastError {get; private set;}

		public string repoURL {get; private set;}
		public string repoPath {get; private set;}
		public LFS lfs {get; private set;}

		public Repository()
		{
			lfs = new LFS(this);
			InitTools();
		}

		public void Close()
		{
			lock (this)
			{
				isOpen = false;
				lastResult = null;
				lastError = null;
				repoURL = null;
				repoPath = null;
			}
		}

		private bool SimpleGitInvoke(string args, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
		{
			lock (this)
			{
				var result = RunExe("git", args, stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
				lastResult = result.output;
				lastError = result.errors;

				return string.IsNullOrEmpty(lastError);
			}
		}
		
		public bool Clone(string url, string path, out string repoClonedPath, StdInputStreamCallbackMethod writeUsernameCallback, StdInputStreamCallbackMethod writePasswordCallback)
		{
			lock (this)
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
			
				var result = RunExe("git", string.Format("clone \"{0}\"", url), workingDirectory:path, getStdInputStreamCallback:getStdInputStreamCallback, stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
				lastResult = result.output;
				lastError = result.errors;
			
				repoClonedPath = repoClonedPathTemp;
				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool Open(string path)
		{
			lock (this)
			{
				Close();

				void stdCallback(string line)
				{
					repoURL = line;
				}
			
				var result = RunExe("git", "rev-parse --git-dir", workingDirectory:path);
				lastResult = result.output;
				lastError = result.errors;
				if (!string.IsNullOrEmpty(lastError)) return false;
			
				// get repo url
				repoURL = "";
				result = RunExe("git", "ls-remote --get-url", stdCallback:stdCallback, workingDirectory:path);
				lastResult = result.output;
				lastError = result.errors;
			
				repoPath = path;
				return isOpen = true;
			}
		}

		public bool GetSignature(SignatureLocations location, out string name, out string email)
		{
			lock (this)
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
		}

		public bool SetSignature(SignatureLocations location, string name, string email)
		{
			lock (this)
			{
				string globalValue = (location == SignatureLocations.Global) ? " --global" : "";
				bool result = SimpleGitInvoke(string.Format("config{1} user.name \"{0}\"", name, globalValue));
				name = lastResult;
				if (!result) return false;

				result = SimpleGitInvoke(string.Format("config{1} user.email \"{0}\"", email, globalValue));
				email = lastResult;
				return result;
			}
		}

		public bool UnpackedObjectCount(out int count, out string size)
		{
			lock (this)
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
		}

		public bool GarbageCollect()
		{
			lock (this)
			{
				return SimpleGitInvoke("gc");
			}
		}

		public bool GetVersion(out string version)
		{
			lock (this)
			{
				bool result = SimpleGitInvoke("version");
				version = lastResult;
				return result;
			}
		}
    }
}
