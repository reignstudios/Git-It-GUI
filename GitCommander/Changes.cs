using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitCommander
{
	public enum FileStates
	{
		Unknown,
		ModifiedInWorkdir,
		ModifiedInIndex,
		NewInWorkdir,
		NewInIndex,
		DeletedFromWorkdir,
		DeletedFromIndex,
		RenamedInWorkdir,
		RenamedInIndex,
		TypeChangeInWorkdir,
		TypeChangeInIndex,
		Conflicted
	}

	public enum FileConflictSources
	{
		Ours,
		Theirs
	}

	public class FileState
	{
		public string filePath;
		public FileStates state;
	}

	public static partial class Repository
	{
		public static bool Stage(string filename)
		{
			return SimpleGitInvoke(string.Format("add \"{0}\"", filename));
		}

		public static bool Unstage(string filename)
		{
			return SimpleGitInvoke(string.Format("reset \"{0}\"", filename));
		}

		public static bool RevertFile(string activeBranch, string filename)
		{
			return SimpleGitInvoke(string.Format("checkout {0} -- \"{1}\"", activeBranch, filename));
		}

		public static bool RevertAllChanges()
		{
			return SimpleGitInvoke("reset --hard");
		}

		public static bool GetFileStates(out FileState[] states)
		{
			bool AddState(string line, string type, FileStates stateType, out FileState state)
			{
				if (line.Contains(type))
				{
					var match = Regex.Match(line, type + @"\s*(.*)");
					if (match.Groups.Count == 2)
					{
						state = new FileState()
						{
							filePath = match.Groups[1].Value,
							state = stateType
						};

						return true;
					}
				}

				state = null;
				return false;
			}
		
			// gather normal files
			var statesList = new List<FileState>();
			int mode = -1;
			void stdCallback_Normal(string line)
			{
				switch (line)
				{
					case "Changes to be committed:": mode = 0; return;
					case "Changes not staged for commit:": mode = 1; return;
					case "Unmerged paths:": mode = 2; return;
				}

				if (mode == 0)
				{
					FileState state;
					if (AddState(line, "\tnew file:", FileStates.NewInIndex, out state)) statesList.Add(state);
					else if (AddState(line, "\tmodified:", FileStates.ModifiedInIndex, out state)) statesList.Add(state);
				}
				else if (mode == 1)
				{
					FileState state;
					if (AddState(line, "\tmodified:", FileStates.ModifiedInWorkdir, out state)) statesList.Add(state);
				}
				else if (mode == 2)
				{
					FileState state;
					if (AddState(line, "\tboth modified:", FileStates.Conflicted, out state)) statesList.Add(state);
				}
			}
			
			var result = Tools.RunExe("git", "status", stdCallback:stdCallback_Normal);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;
			if (!string.IsNullOrEmpty(lastError))
			{
				states = null;
				return false;
			}

			// gather untracked files
			mode = -1;
			void stdCallback_Untracked(string line)
			{
				if (line == "Untracked files:")
				{
					mode = 0;
					return;
				}

				if (mode == 0)
				{
					FileState state;
					if (AddState(line, "\t", FileStates.NewInWorkdir, out state)) statesList.Add(state);
				}
			}

			result = Tools.RunExe("git", "status -u", null, stdCallback_Untracked);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			if (!string.IsNullOrEmpty(lastError))
			{
				states = null;
				return false;
			}

			states = statesList.ToArray();
			return true;
		}

		public static bool GetConflitedFiles(out FileState[] states)
		{
			var statesList = new List<FileState>();
			void stdCallback(string line)
			{
				var state = new FileState()
				{
					filePath = line,
					state = FileStates.Conflicted
				};

				statesList.Add(state);
			}

			var result = Tools.RunExe("git", "diff --name-only --diff-filter=U", null, stdCallback);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			states = statesList.ToArray();
			return string.IsNullOrEmpty(lastError);
		}

		public static bool SaveConflictedFile(string filename, FileConflictSources source)
		{
			string sourceName = source == FileConflictSources.Ours ? "ORIG_HEAD" : "MERGE_HEAD";
			return SimpleGitInvoke(string.Format("show {1}:'{0}' >'{0}.ours'", filename, sourceName));
		}

		public static bool AcceptConflictedFile(string filename, FileConflictSources source)
		{
			string sourceName = source == FileConflictSources.Ours ? "--ours" : "--theirs";
			return SimpleGitInvoke(string.Format("git checkout {1} '{0}'", filename, sourceName));
		}

		public static bool Fetch(StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
		{
			return SimpleGitInvoke("fetch", stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
		}

		public static bool Pull(StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
		{
			return SimpleGitInvoke("pull", stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
		}

		public static bool Push(StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
		{
			return SimpleGitInvoke("push", stdCallback:stdCallback, stdErrorCallback:stdErrorCallback);
		}
		
		public static bool Commit(string message)
		{
			return SimpleGitInvoke(string.Format("commit -m \"{0}\"", message));
		}
	}
}
