using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

		public static void Dispose()
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
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}
		
		public static bool Clone(string url, string path, StdInputCallbackMethod writeUsernameCallback, StdInputCallbackMethod writePasswordCallback, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
		{
			void stdCallback_CheckUserPass(string line)
			{
				// TODO: check for user / pass requests and fire callback to handle them
				if (stdCallback != null) stdCallback(line);
			}

			repoURL = url;
			repoPath = path;
			var result = Tools.RunExe("git", string.Format("clone \"{0}\"", url), stdCallback:stdCallback_CheckUserPass, stdErrorCallback:stdErrorCallback);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return isOpen = string.IsNullOrEmpty(lastError);
		}

		public static bool Open(string path)
		{
			void stdCallback(string line)
			{
				repoURL = line;
			}

			repoPath = path;
			var result = Tools.RunExe("git", "ls-remote --get-url", stdCallback:stdCallback);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;
			
			return isOpen = string.IsNullOrEmpty(lastError);
		}

		public static bool GetSignature(SignatureLocations location, out string name, out string email)
		{
			name = null;
			email = null;

			bool result = SimpleGitInvoke("git config --global user.name");
			name = lastResult;
			if (!result) return false;

			result = SimpleGitInvoke("git config --global user.email");
			email = lastResult;
			return result;
		}

		public static bool SetSignature(SignatureLocations location, string name, string email)
		{
			bool result = SimpleGitInvoke(string.Format("git config --global user.name \"{0}\"", name));
			name = lastResult;
			if (!result) return false;

			result = SimpleGitInvoke(string.Format("git config --global user.email \"{0}\"", email));
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
