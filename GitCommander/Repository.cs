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

		private static bool SimpleGitInvoke(string args)
		{
			var result = Tools.RunExe("git", args);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}
		
		public static bool Clone(string url, string path)
		{
			repoURL = url;
			repoPath = path;
			var result = Tools.RunExe("git", string.Format("clone \"{0}\"", url));
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return isOpen = string.IsNullOrEmpty(lastError);
		}

		public static bool Open(string path)
		{
			repoPath = path;
			lastResult = "";
			lastError = "";
			//string error;
			//lastResult = Tools.RunExeOutputErrors("git", "ls-remote --get-url", null, out error);
			//lastError = error;
			//
			//repoURL = lastResult.Replace("\n", "");
			//return isOpen = string.IsNullOrEmpty(lastError);

			return true;
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
