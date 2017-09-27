using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GitCommander
{
	public partial class Repository
	{
		public class LFS
		{
			public bool isEnabled {get; private set;}
			private Repository repository;

			internal LFS(Repository repository)
			{
				this.repository = repository;
			}
			
			internal void Open()
			{
				isEnabled = false;
				string gitattributesPath = repository.repoPath + Path.DirectorySeparatorChar + ".gitattributes";
				string lfsFolder = repository.repoPath + string.Format("{0}.git{0}lfs", Path.DirectorySeparatorChar);
				string lfsHook = repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar);
				if (File.Exists(gitattributesPath) && Directory.Exists(lfsFolder) && File.Exists(lfsHook))
				{
					// check attributes
					string lines = File.ReadAllText(gitattributesPath);
					if (!lines.Contains("filter=lfs diff=lfs merge=lfs")) return;

					// check hook
					string data = File.ReadAllText(repository.repoPath + string.Format("{0}.git{0}hooks{0}pre-push", Path.DirectorySeparatorChar));
					isEnabled = data.Contains("git-lfs");
				}
			}

			private bool SimpleLFSInvoke(string args, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
			{
				var result = repository.RunExe("git", "lfs " + args, stdCallback: stdCallback, stdErrorCallback: stdErrorCallback);
				repository.lastResult = result.output;
				repository.lastError = result.errors;

				return string.IsNullOrEmpty(repository.lastError);
			}

			public bool Install()
			{
				lock (repository)
				{
					return SimpleLFSInvoke("install");
				}
			}

			public bool Uninstall()
			{
				lock (repository)
				{
					return SimpleLFSInvoke("uninstall");
				}
			}

			public bool Track(string ext)
			{
				lock (repository)
				{
					return SimpleLFSInvoke(string.Format("track \"*{0}\"", ext));
				}
			}

			public bool Untrack(string ext)
			{
				lock (repository)
				{
					return SimpleLFSInvoke(string.Format("untrack \"*{0}\"", ext));
				}
			}

			public bool GetVersion(out string version)
			{
				lock (repository)
				{
					bool result = SimpleLFSInvoke("version");
					version = repository.lastResult;
					return result;
				}
			}

			public bool GetTrackedExts(out List<string> exts)
			{
				var extList = new List<string>();
				bool copy = false;
				void stdCallback(string line)
				{
					if (line == "Listing tracked patterns")
					{
						copy = true;
						return;
					}

					if (copy)
					{
						var match = Regex.Match(line, @"\s*\*(.*) .*");
						if (match.Success) extList.Add(match.Groups[1].Value);
						else copy = false;
					}
				}

				lock (repository)
				{
					bool result = SimpleLFSInvoke("track", stdCallback:stdCallback);
					exts = extList;
					return result;
				}
			}
		}
	}
}
