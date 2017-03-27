using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
		Unreadable = 4096
	}

	public enum FileConflictSources
	{
		Ours,
		Theirs
	}

	public class FileState
	{
		public string filename {get; internal set;}
		public FileStates state {get; internal set;}

		public bool IsState(FileStates state)
		{
			return (this.state & state) != 0;
		}

		public bool IsStaged()
		{
			return
				IsState(FileStates.NewInIndex) ||
				IsState(FileStates.DeletedFromIndex) ||
				IsState(FileStates.ModifiedInIndex) ||
				IsState(FileStates.RenamedInIndex) ||
				IsState(FileStates.TypeChangeInIndex);
		}

		public override string ToString()
		{
			return filename;
		}
	}

	public static partial class Repository
	{
		public static bool Stage(string filename)
		{
			return SimpleGitInvoke(string.Format("add \"{0}\"", filename));
		}

		public static bool StageAll()
		{
			return SimpleGitInvoke("add -A");
		}

		public static bool Unstage(string filename)
		{
			return SimpleGitInvoke(string.Format("reset \"{0}\"", filename));
		}

		public static bool UnstageAll()
		{
			return SimpleGitInvoke("reset");
		}

		public static bool RevertFile(string activeBranch, string filename)
		{
			return SimpleGitInvoke(string.Format("checkout {0} -- \"{1}\"", activeBranch, filename));
		}

		public static bool RevertAllChanges()
		{
			return SimpleGitInvoke("reset --hard");
		}

		private delegate bool AddState(string type, FileStates stateType);
		private static void ParseFileState(string line, ref int mode, List<FileState> states)
		{
			var addState = new AddState(delegate(string type, FileStates stateType)
			{
				if (line.Contains(type))
				{
					var match = Regex.Match(line, type + @"\s*(.*)");
					if (match.Groups.Count == 2)
					{
						string filePath = match.Groups[1].Value;
						if (states != null && states.Exists(x => x.filename == filePath))
						{
							var state = states.Find(x => x.filename == filePath);
							state.state |= stateType;
							return true;
						}
						else
						{
							var state = new FileState()
							{
								filename = filePath,
								state = stateType
							};
							
							states.Add(state);
							return true;
						}
					}
				}
				
				return false;
			});
		
			// gather normal files
			switch (line)
			{
				case "Changes to be committed:": mode = 0; return;
				case "Changes not staged for commit:": mode = 1; return;
				case "Unmerged paths:": mode = 2; return;
				case "Untracked files:": mode = 3; return;
			}
			
			if (mode == 0)
			{
				bool pass = addState("\tnew file:", FileStates.NewInIndex);
				if (!pass) pass = addState("\tmodified:", FileStates.ModifiedInIndex);
				if (!pass) pass = addState("\tdeleted:", FileStates.DeletedFromIndex);
				if (!pass) pass = addState("\trenamed:", FileStates.RenamedInIndex);
				// TODO: check for valid unhanled types
				//if (!pass)
				//{
				//	var match = Regex.Match(line, @"\t(.*):");
				//	if (match.Success)
				//	{
						
				//		return false;
				//	}
				//}
			}
			else if (mode == 1)
			{
				bool pass = addState("\tmodified:", FileStates.ModifiedInWorkdir);
				if (!pass) pass = addState("\tdeleted:", FileStates.DeletedFromWorkdir);
				if (!pass) pass = addState("\trenamed:", FileStates.RenamedInWorkdir);
			}
			else if (mode == 2)
			{
				addState("\tboth modified:", FileStates.Conflicted);
			}
			else if (mode == 3)
			{
				addState("\t", FileStates.NewInWorkdir);
			}
		}

		public static bool GetFileState(string filename, out FileState fileState)
		{
			var states = new List<FileState>();
			int mode = -1;
			var stdCallback = new StdCallbackMethod(delegate(string line)
			{
				ParseFileState(line, ref mode, states);
			});

			var result = Tools.RunExe("git", string.Format("status -u \"{0}\"", filename), stdCallback:stdCallback);
			lastResult = result.Item1;
			lastError = result.Item2;
			if (!string.IsNullOrEmpty(lastError))
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

		public static bool GetFileStates(out FileState[] fileStates)
		{
			var states = new List<FileState>();
			int mode = -1;
			var stdCallback = new StdCallbackMethod(delegate(string line)
			{
				ParseFileState(line, ref mode, states);
			});
			
			var result = Tools.RunExe("git", "status -u", stdCallback:stdCallback);
			lastResult = result.Item1;
			lastError = result.Item2;
			if (!string.IsNullOrEmpty(lastError))
			{
				fileStates = null;
				return false;
			}

			fileStates = states.ToArray();
			return true;
		}

		public static bool ConflitedExist(out bool yes)
		{
			bool conflictExist = false;
			var stdCallback = new StdCallbackMethod(delegate(string line)
			{
				conflictExist = true;
			});

			var result = Tools.RunExe("git", "diff --name-only --diff-filter=U", null, stdCallback:stdCallback);
			lastResult = result.Item1;
			lastError = result.Item2;
			
			yes = conflictExist;
			return string.IsNullOrEmpty(lastError);
		}

		public static bool SaveOriginalFile(string filename, out string savedFilename)
		{
			savedFilename = filename + ".orig";
			var result = Tools.RunExe("git", string.Format("show HEAD:\"{0}\"", filename), stdOutToFilePath:savedFilename);
			lastResult = result.Item1;
			lastError = result.Item2;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool SaveConflictedFile(string filename, FileConflictSources source, out string savedFilename)
		{
			string sourceName = source == FileConflictSources.Ours ? "ORIG_HEAD" : "MERGE_HEAD";
			savedFilename = filename + (source == FileConflictSources.Ours ? ".ours" : ".theirs");
			var result = Tools.RunExe("git", string.Format("show {1}:\"{0}\"", filename, sourceName), stdOutToFilePath:savedFilename);
			lastResult = result.Item1;
			lastError = result.Item2;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool AcceptConflictedFile(string filename, FileConflictSources source)
		{
			string sourceName = source == FileConflictSources.Ours ? "--ours" : "--theirs";
			return SimpleGitInvoke(string.Format("checkout {1} \"{0}\"", filename, sourceName));
		}

		public static bool Fetch()
		{
			return SimpleGitInvoke("fetch");
		}

		public static bool Fetch(string remote, string branch)
		{
			return SimpleGitInvoke(string.Format("fetch {0} {1}", remote, branch));
		}

		public static bool Pull()
		{
			return SimpleGitInvoke("pull");
		}

		public static bool Push()
		{
			return SimpleGitInvoke("push");
		}
		
		public static bool Commit(string message)
		{
			return SimpleGitInvoke(string.Format("commit -m \"{0}\"", message));
		}

		public static bool GetDiff(string filename, out string diff)
		{
			bool result = SimpleGitInvoke(string.Format("diff HEAD \"{0}\"", filename));
			diff = lastResult;
			return result;
		}
	}
}
