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

			internal void Close()
			{
				isEnabled = false;
			}
			
			internal void Open()
			{
				isEnabled = false;
				string gitattributesPath = Path.Combine(repository.repoPath, ".gitattributes");
				string lfsFolder = Path.Combine(repository.repoPath, ".git", "lfs");
				string lfsHook = Path.Combine(repository.repoPath, ".git", "hooks", "pre-push");
				if (File.Exists(gitattributesPath) && Directory.Exists(lfsFolder) && File.Exists(lfsHook))
				{
					// check attributes
					string lines = File.ReadAllText(gitattributesPath);
					if (!lines.Contains("filter=lfs diff=lfs merge=lfs")) return;

					// check hook
					string data = File.ReadAllText(lfsHook);
					isEnabled = data.Contains("git-lfs");
				}
			}

			private bool SimpleLFSInvoke(string args, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
			{
				var result = repository.RunExe("git", "lfs " + args, stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
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

			public bool Track(List<string> exts)
			{
				lock (repository)
				{
					if (exts.Count == 0) return true;

					string args = string.Empty;
					foreach (string ext in exts)
					{
						args += string.Format("\"*{0}\" ", ext);
					}

					return SimpleLFSInvoke("track " + args.TrimEnd(' '));
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

			public bool SmudgeFile(string ptr, string dstFileName)
			{
				bool stdInputStreamCallback(StreamWriter writer)
				{
					writer.Write(ptr);
					writer.Flush();
					writer.BaseStream.Flush();
					
					return true;
				}

				lock (repository)
				{
					var result = repository.RunExe("git", "lfs smudge", stdInputStreamCallback:stdInputStreamCallback, stdOutToFilePath:dstFileName);
					repository.lastResult = result.output;
					repository.lastError = result.errors;

					return string.IsNullOrEmpty(repository.lastError);
				}
			}

			public bool SmudgeFile(string ptr, Stream stream)
			{
				bool stdInputStreamCallback(StreamWriter writer)
				{
					writer.Write(ptr);
					writer.Flush();
					writer.BaseStream.Flush();
					
					return true;
				}

				lock (repository)
				{
					var result = repository.RunExe("git", "lfs smudge", stdInputStreamCallback:stdInputStreamCallback, stdOutToStream:stream);
					repository.lastResult = result.output;
					repository.lastError = result.errors;

					return string.IsNullOrEmpty(repository.lastError);
				}
			}

			public bool PruneObjectCount(out int count, out string size)
			{
				bool resultFound = false;
				int _count = -1;
				string _size = "N/A";
				void stdCallback(string line)
				{
					if (resultFound) return;

					var match = Regex.Match(line, @"(\d*) files would be pruned \((.*)\)");
					if (match.Success)
					{
						resultFound = true;
						int.TryParse(match.Groups[1].Value, out _count);
						_size = match.Groups[2].Value;
					}
				}

				lock (repository)
				{
					bool result = SimpleLFSInvoke("prune --dry-run", stdCallback:stdCallback);
					count = _count;
					size = _size;
					return result;
				}
			}

			public bool Prune()
			{
				lock (repository)
				{
					return SimpleLFSInvoke("prune");
				}
			}
		}
	}
}
