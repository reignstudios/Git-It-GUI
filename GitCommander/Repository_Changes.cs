using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GitCommander
{
	[Flags]
	public enum FileStates
	{
		Unaltered = 0,
		ModifiedInWorkdir = 1,
		ModifiedInIndex = 2,
		NewInWorkdir = 4,
		NewInIndex = 8,
		DeletedFromWorkdir = 16,
		DeletedFromIndex = 32,
		RenamedInWorkdir = 64,
		RenamedInIndex = 128,
		TypeChangeInWorkdir = 256,
		TypeChangeInIndex = 512,
		Conflicted = 1024,
		Ignored = 2048,
		Unreadable = 4096,
		Copied = 8192
	}

	public enum FileConflictSources
	{
		Ours,
		Theirs
	}

	public enum FileConflictTypes
	{
		None,
		Changes,
		DeletedByUs,
		DeletedByThem,
		DeletedByBoth,
		AddedByUs,
		AddedByThem,
		AddedByBoth
	}

	public class FileState
	{
		public string filename {get; internal set;}
		public FileStates state {get; internal set;}
		public FileConflictTypes conflictType {get; internal set;}
		public bool isLFS {get; internal set;}
		public bool isSubmodule {get; internal set;}

		public static bool IsAllStates(FileStates stateFlag, FileStates[] states)
		{
			foreach (var state in states)
			{
				if ((stateFlag & state) == 0) return false;
			}

			return true;
		}

		public static bool IsAnyStates(FileStates stateFlag, FileStates[] states)
		{
			foreach (var state in states)
			{
				if ((stateFlag & state) != 0) return true;
			}

			return false;
		}

		public bool IsAllStates(FileStates[] states)
		{
			return IsAllStates(state, states);
		}

		public bool IsAnyStates(FileStates[] states)
		{
			return IsAnyStates(state, states);
		}

		public bool HasState(FileStates state)
		{
			return (this.state & state) != 0;
		}

		public bool IsUnstaged()
		{
			return
				HasState(FileStates.NewInWorkdir) ||
				HasState(FileStates.DeletedFromWorkdir) ||
				HasState(FileStates.ModifiedInWorkdir) ||
				HasState(FileStates.RenamedInWorkdir) ||
				HasState(FileStates.TypeChangeInWorkdir) ||
				HasState(FileStates.Conflicted);
		}

		public bool IsStaged()
		{
			return
				HasState(FileStates.NewInIndex) ||
				HasState(FileStates.DeletedFromIndex) ||
				HasState(FileStates.ModifiedInIndex) ||
				HasState(FileStates.RenamedInIndex) ||
				HasState(FileStates.TypeChangeInIndex);
		}

		public override string ToString()
		{
			return filename;
		}

		public string ToStateString()
		{
			string result = string.Empty;
			var values = (FileStates[])Enum.GetValues(typeof(FileStates));
			bool firstFound = false;
			foreach (var value in values)
			{
				if (((int)state & (int)value) != 0)
				{
					if (firstFound) result += " : ";
					result += string.Join(" ", Regex.Split(value.ToString(), @"(?=[A-Z](?![A-Z]|$))"));
					firstFound = true;
				}
			}

			return result;
		}
	}

	public partial class Repository
	{
		public bool Stage(string filename)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("add \"{0}\"", filename));
			}
		}

		public bool StageAll()
		{
			lock (this)
			{
				return SimpleGitInvoke("add -A");
			}
		}

		public bool Unstage(string filename)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("reset \"{0}\"", filename));
			}
		}

		public bool UnstageAll()
		{
			lock (this)
			{
				return SimpleGitInvoke("reset");
			}
		}

		public bool RevertFile(string activeBranch, string filename)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("checkout {0} -- \"{1}\"", activeBranch, filename));
			}
		}

		public bool RevertAllChanges()
		{
			lock (this)
			{
				return SimpleGitInvoke("reset --hard");
			}
		}
		
		private bool ParseFileState(string line, ref int mode, List<FileState> states, List<string> lfsExts)
		{
			bool addState(string type, FileStates stateType, FileConflictTypes conflictType = FileConflictTypes.None)
			{
				if (line.Contains(type))
				{
					var match = Regex.Match(line, type + @"\s*(.*)");
					if (match.Success)
					{
						// get common filename
						string filePath = match.Groups[1].Value;

						// parse submodule filename
						bool isSubmodule = false;
						match = Regex.Match(filePath, @"(.*)\s\((.*)\)");
						if (match.Success)
						{
							filePath = match.Groups[1].Value;
							isSubmodule = match.Groups[2].Value.Contains("content");
						}

						// parse extended filename
						if ((stateType & FileStates.Copied) != 0 || (stateType & FileStates.RenamedInIndex) != 0 || (stateType & FileStates.RenamedInWorkdir) != 0)
						{
							match = Regex.Match(filePath, @"(.*)\s->\s(.*)");
							if (match.Success) filePath = match.Groups[2].Value;
							else throw new Exception("Failed to parse copied or renamed status type");
						}
						
						if (states != null && states.Exists(x => x.filename == filePath))
						{
							var state = states.Find(x => x.filename == filePath);
							state.state |= stateType;
							return true;
						}
						else
						{
							var ext = Path.GetExtension(filePath);
							var state = new FileState()
							{
								filename = filePath,
								state = stateType,
								conflictType = conflictType,
								isSubmodule = isSubmodule,
								isLFS = lfsExts.Contains(ext)
							};
							
							states.Add(state);
							return true;
						}
					}
				}
				
				return false;
			}
			
			// gather normal files
			switch (line)
			{
				case "Changes to be committed:": mode = 0; return true;
				case "Changes not staged for commit:": mode = 1; return true;
				case "Unmerged paths:": mode = 2; return true;
				case "Untracked files:": mode = 3; return true;
			}
			
			bool pass = false;
			if (mode == 0)
			{
				pass = addState("\tnew file:", FileStates.NewInIndex);
				if (!pass) pass = addState("\tmodified:", FileStates.ModifiedInIndex);
				if (!pass) pass = addState("\tdeleted:", FileStates.DeletedFromIndex);
				if (!pass) pass = addState("\ttypechange:", FileStates.TypeChangeInIndex);
				if (!pass) pass = addState("\trenamed:", FileStates.RenamedInIndex);
				if (!pass) pass = addState("\tcopied:", FileStates.Copied | FileStates.NewInIndex);
			}
			else if (mode == 1)
			{
				pass = addState("\tmodified:", FileStates.ModifiedInWorkdir);
				if (!pass) pass = addState("\tdeleted:", FileStates.DeletedFromWorkdir);
				if (!pass) pass = addState("\ttypechange:", FileStates.TypeChangeInWorkdir);
				if (!pass) pass = addState("\trenamed:", FileStates.RenamedInWorkdir);
				if (!pass) pass = addState("\tcopied:", FileStates.Copied | FileStates.NewInWorkdir);
				if (!pass) pass = addState("\tnew file:", FileStates.NewInWorkdir);// call this just in case (should be done in untracked)
			}
			else if (mode == 2)
			{
				pass = addState("\tboth modified:", FileStates.Conflicted, FileConflictTypes.Changes);
				if (!pass) pass = addState("\tdeleted by us:", FileStates.Conflicted, FileConflictTypes.DeletedByUs);
				if (!pass) pass = addState("\tdeleted by them:", FileStates.Conflicted, FileConflictTypes.DeletedByThem);
				if (!pass) pass = addState("\tboth deleted:", FileStates.Conflicted, FileConflictTypes.DeletedByBoth);
				if (!pass) pass = addState("\tadded by us:", FileStates.Conflicted, FileConflictTypes.AddedByUs);
				if (!pass) pass = addState("\tadded by them:", FileStates.Conflicted, FileConflictTypes.AddedByThem);
				if (!pass) pass = addState("\tboth added:", FileStates.Conflicted, FileConflictTypes.AddedByBoth);
			}
			else if (mode == 3)
			{
				pass = addState("\t", FileStates.NewInWorkdir);
			}

			if (!pass)
			{
				var match = Regex.Match(line, @"\t(.*):");
				if (match.Success)
				{
					RunExeDebugLineCallback?.Invoke("ERROR: Failed to parse status for state: " + line);
					return false;
				}
			}

			return true;
		}

		public bool GetFileState(string filename, out FileState fileState, bool getLFSState)
		{
			var states = new List<FileState>();
			var lfsExts = new List<string>();
			int mode = -1;
			bool failedToParse = false;
			void stdCallback(string line)
			{
				if (!ParseFileState(line, ref mode, states, lfsExts)) failedToParse = true;
			}

			lock (this)
			{
				if (getLFSState && lfs.isEnabled)
				{
					if (!lfs.GetTrackedExts(out lfsExts))
					{
						fileState = null;
						return false;
					}
				}

				var result = RunExe("git", string.Format("status -u \"{0}\"", filename), stdCallback:stdCallback);
				lastResult = result.output;
				lastError = result.errors;
				if (!string.IsNullOrEmpty(lastError))
				{
					fileState = null;
					return false;
				}

				if (failedToParse)
				{
					fileState = null;
					return false;
				}
			
				if (states.Count != 0)
				{
					fileState = states[0];
					return true;
				}
				else
				{
					fileState = null;
					return false;
				}
			}
		}

		public bool GetFileStates(out FileState[] fileStates, bool getLFSState)
		{
			var states = new List<FileState>();
			var lfsExts = new List<string>();
			int mode = -1;
			bool failedToParse = false;
			void stdCallback(string line)
			{
				if (!ParseFileState(line, ref mode, states, lfsExts)) failedToParse = true;
			}

			lock (this)
			{
				if (getLFSState && lfs.isEnabled)
				{
					if (!lfs.GetTrackedExts(out lfsExts))
					{
						fileStates = null;
						return false;
					}
				}

				var result = RunExe("git", "status -u", stdCallback:stdCallback);
				lastResult = result.output;
				lastError = result.errors;
				if (!string.IsNullOrEmpty(lastError))
				{
					fileStates = null;
					return false;
				}

				if (failedToParse)
				{
					fileStates = null;
					return false;
				}

				fileStates = states.ToArray();
				return true;
			}
		}

		public bool ConflitedExist(out bool yes)
		{
			bool conflictExist = false;
			void stdCallback(string line)
			{
				if (line.StartsWith("warning:")) return;
				if (line.StartsWith("The file will have its original line endings in your working directory.")) return;
				conflictExist = true;
			}

			lock (this)
			{
				var result = RunExe("git", "diff --name-only --diff-filter=U", null, stdCallback:stdCallback);
				lastResult = result.output;
				lastError = result.errors;
			
				yes = conflictExist;
				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool SaveOriginalFile(string filename, out string savedFilename)
		{
			lock (this)
			{
				savedFilename = filename + ".orig";
				var result = RunExe("git", string.Format("show HEAD:\"{0}\"", filename), stdOutToFilePath:savedFilename);
				lastResult = result.output;
				lastError = result.errors;

				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool SaveOriginalFile(string filename, Stream stream)
		{
			lock (this)
			{
				var result = RunExe("git", string.Format("show HEAD:\"{0}\"", filename), stdOutToStream:stream);
				lastResult = result.output;
				lastError = result.errors;

				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool SaveConflictedFile(string filename, FileConflictSources source, out string savedFilename)
		{
			lock (this)
			{
				string sourceName = source == FileConflictSources.Ours ? "ORIG_HEAD" : "MERGE_HEAD";
				savedFilename = filename + (source == FileConflictSources.Ours ? ".ours" : ".theirs");
				var result = RunExe("git", string.Format("show {1}:\"{0}\"", filename, sourceName), stdOutToFilePath:savedFilename);
				lastResult = result.output;
				lastError = result.errors;

				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool SaveConflictedFile(string filename, FileConflictSources source, Stream stream)
		{
			lock (this)
			{
				string sourceName = source == FileConflictSources.Ours ? "ORIG_HEAD" : "MERGE_HEAD";
				var result = RunExe("git", string.Format("show {1}:\"{0}\"", filename, sourceName), stdOutToStream:stream);
				lastResult = result.output;
				lastError = result.errors;

				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool CheckoutConflictedFile(string filename, FileConflictSources source)
		{
			lock (this)
			{
				string sourceName = source == FileConflictSources.Ours ? "--ours" : "--theirs";
				return SimpleGitInvoke(string.Format("checkout {1} \"{0}\"", filename, sourceName));
			}
		}

		public bool RemoveFile(string filename)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("rm \"{0}\"", filename));
			}
		}

		public bool CompletedMergeCommitPending(out bool yes)
		{
			bool mergeCommitPending = false;
			void stdCallback(string line)
			{
				if (line == "All conflicts fixed but you are still merging.") mergeCommitPending = true;
			}

			lock (this)
			{
				var result = RunExe("git", "status", null, stdCallback:stdCallback);
				lastResult = result.output;
				lastError = result.errors;
			
				yes = mergeCommitPending;
				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool InitPullSubmodules()
		{
			lock (this)
			{
				return SimpleGitInvoke("submodule update --init --recursive");
			}
		}

		public bool PullSubmodules()
		{
			lock (this)
			{
				return SimpleGitInvoke("submodule update --recursive");
			}
		}

		public bool Fetch()
		{
			lock (this)
			{
				return SimpleGitInvoke("fetch");
			}
		}

		public bool Fetch(string remote, string branch)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("fetch {0} {1}", remote, branch));
			}
		}

		public bool Pull()
		{
			lock (this)
			{
				return SimpleGitInvoke("pull");
			}
		}

		public bool Push()
		{
			lock (this)
			{
				return SimpleGitInvoke("push");
			}
		}
		
		public bool Commit(string message)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("commit -m \"{0}\"", message));
			}
		}

		public bool GetDiff(string filename, out string diff)
		{
			lock (this)
			{
				bool result = SimpleGitInvoke(string.Format("diff HEAD \"{0}\"", filename));
				diff = lastResult;
				return result;
			}
		}
	}
}
