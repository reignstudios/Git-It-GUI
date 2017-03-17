using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitCommander
{
    public static class Repository
    {
		public static bool isOpen {get; private set;}
		public static string lastResult {get; private set;}
		public static string lastError {get; private set;}

		internal static string repoURL, repoPath;

		#region Repo Methods
		public static bool Clone(string url, string path)
		{
			repoURL = url;
			repoPath = path;
			string error;
			lastResult = Tools.RunExe("git", string.Format("clone \"{0}\"", url), null, out error);
			lastError = error;

			return isOpen = string.IsNullOrEmpty(lastError);
		}

		public static bool Open(string path)
		{
			repoPath = path;
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
		#endregion

		#region Changes Methods
		public static bool Stage(string filename)
		{
			string error;
			lastResult = Tools.RunExe("git", string.Format("add \"{0}\"", filename), null, out error);
			lastError = error;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool Unstage(string filename)
		{
			string error;
			lastResult = Tools.RunExe("git", string.Format("reset \"{0}\"", filename), null, out error);
			lastError = error;

			return string.IsNullOrEmpty(lastError);
		}

		private static bool AddState(string line, string type, FileStates stateType, out FileState state)
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

		public static bool GetFileStates(out List<FileState> states)
		{
			string error;
			lastResult = Tools.RunExe("git", "status", null, out error);// TODO: use stdout callback method to parse results
			//NOTE TODO: will need to use "git status -u" to list all untracked files
			lastError = error;
			if (!string.IsNullOrEmpty(lastError))
			{
				states = null;
				return false;
			}
			
			states = new List<FileState>();
			if (!string.IsNullOrEmpty(lastResult))
			{
				var lines = lastResult.Split(new string[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
				int mode = -1;
				foreach (var line in lines)
				{
					switch (line)
					{
						case "Changes to be committed:": mode = 0; continue;
						case "Changes not staged for commit:": mode = 1; continue;
						case "Untracked files:": mode = 2; continue;
					}

					if (mode == 0)
					{
						FileState state;
						if (AddState(line, "\tnew file:", FileStates.NewInIndex, out state)) states.Add(state);
						else if (AddState(line, "\tmodified:", FileStates.ModifiedInIndex, out state)) states.Add(state);
					}
					else if (mode == 1)
					{
						FileState state;
						if (AddState(line, "\tmodified:", FileStates.ModifiedInWorkdir, out state)) states.Add(state);
					}
					else if (mode == 2)
					{
						FileState state;
						if (AddState(line, "\t", FileStates.NewInWorkdir, out state)) states.Add(state);
					}
				}
			}

			return true;
		}

		public static bool Fetch()
		{
			string error;
			lastResult = Tools.RunExe("git", "fetch", null, out error);
			lastError = error;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool Pull()
		{
			string error;
			lastResult = Tools.RunExe("git", "pull", null, out error);
			lastError = error;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool Push()
		{
			string error;
			lastResult = Tools.RunExe("git", "push", null, out error);
			lastError = error;

			return string.IsNullOrEmpty(lastError);
		}
		#endregion

		#region Branches Methods
		// "git remote prune origin --dry-run" lists are possilbe remotes to prune
		// "git remote prune origin" prunes all invalid remotes
		// "git branch -d <branch name>" deletes branch

		public static bool GetRemotes(out string[] remotes)
		{
			string error;
			lastResult = Tools.RunExe("git", "remote show", null, out error);
			lastError = error;

			if (!string.IsNullOrEmpty(lastError))
			{
				remotes = null;
				return false;
			}
			
			remotes = lastResult.Split(new string[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
			return true;
		}

		public static bool GetAllBranches(out string[] branches)
		{
			string error;
			lastResult = Tools.RunExe("git", "branch -a", null, out error);
			lastError = error;

			if (!string.IsNullOrEmpty(lastError))
			{
				branches = null;
				return false;
			}

			branches = lastResult.Split(new string[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
			return true;
		}
		#endregion
    }
}
