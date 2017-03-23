using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitCommander
{
    public static partial class Repository
    {
		public static bool isOpen {get; private set;}
		public static string lastResult {get; private set;}
		public static string lastError {get; private set;}

		internal static string repoURL, repoPath;

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

		public static void Dispose()
		{
			isOpen = false;
			lastResult = null;
			lastError = null;
			repoURL = null;
			repoPath = null;
		}
    }
}
