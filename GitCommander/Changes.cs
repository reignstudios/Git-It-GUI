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

	public class FileState
	{
		public string filePath;
		public FileStates state;
	}

	public static partial class Repository
	{
		public static bool Stage(string filename)
		{
			var result = Tools.RunExe("git", string.Format("add \"{0}\"", filename));
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool Unstage(string filename)
		{
			var result = Tools.RunExe("git", string.Format("reset \"{0}\"", filename));
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool RevertFile(string activeBranch, string filename)
		{
			var result = Tools.RunExe("git", string.Format("checkout {0} -- \"{1}\"", activeBranch, filename));
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool RevertAllChanges()
		{
			var result = Tools.RunExe("git", "reset --hard");
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
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

		public static bool Fetch()
		{
			var result = Tools.RunExe("git", "fetch");
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool Pull()
		{
			var result = Tools.RunExe("git", "pull");
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool Push()
		{
			var result = Tools.RunExe("git", "push");
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}
	}
}
