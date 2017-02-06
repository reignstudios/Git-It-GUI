using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommander
{
    public static class Repository
    {
		public static bool isOpen {get; private set;}
		public static string lastResult {get; private set;}
		public static string lastError {get; private set;}

		internal static string repoURL, repoPath;

		public static bool Clone(string url, string path)
		{
			repoURL = url;
			repoPath = path;
			string error;
			lastResult = Tools.RunExeOutput("git", "clone " + url, null, out error, false);
			lastError = error;

			return isOpen = string.IsNullOrEmpty(lastError);
		}

		public static bool Open(string path)
		{
			repoPath = path;
			string error;
			lastResult = Tools.RunExeOutput("git", "ls-remote --get-url", null, out error, false);
			lastError = error;

			repoURL = lastResult.Replace("\n", "");
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
