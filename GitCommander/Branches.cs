using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitCommander
{
	public class BranchInfo
	{
		public string name, fullname, remoteName;
	}

	public class Branch
	{
		public string name, fullname, remoteName;
		public bool isActive, isRemote, isTracking, isHead;
		public BranchInfo head, tracking;
	}

	public static partial class Repository
	{
		public static bool DeleteBranch(string branch)
		{
			var result = Tools.RunExe("git", "branch -d " + branch);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool PruneRemoteBranches()
		{
			var result = Tools.RunExe("git", "remote prune origin");
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			return string.IsNullOrEmpty(lastError);
		}

		public static bool GetRemotePrunableBrancheNames(out string[] branchName)
		{
			var branchNameList = new List<string>();
			void stdCallback(string line)
			{
				branchNameList.Add(line);
			}
			
			var result = Tools.RunExe("git", "remote prune origin --dry-run", stdCallback:stdCallback);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			if (!string.IsNullOrEmpty(lastError))
			{
				branchName = null;
				return false;
			}
			
			branchName = branchNameList.ToArray();
			return true;
		}
		
		public static bool GetAllBranches(out Branch[] branches)
		{
			var branchList = new List<Branch>();
			void stdCallback(string line)
			{
				line = line.TrimStart();

				// check if remote
				bool isActive = false;
				if (line[0] == '*')
				{
					isActive = true;
					line = line.Remove(0, 2);
				}

				// state vars
				string name, fullname, trackingBranchName = null, trackingBranchFullName = null, trackingBranchRemoteName = null;
				string remoteName = null, headPtrName = null, headPtrFullName = null, headPtrRemoteName = null;
				bool isRemote = false, isHead = false, isTracking = false;

				// get name and tracking info
				var match = Regex.Match(line, @"remotes/(.*)/HEAD\s*->\s*(\S*)");
				if (match.Success)
				{
					isRemote = true;
					isHead = true;
					name = "HEAD";
					fullname = "remotes/" + match.Groups[1].Value + '/' + name;
					headPtrFullName = match.Groups[2].Value;

					var values = headPtrFullName.Split('/');
					if (values.Length == 2)
					{
						headPtrRemoteName = values[0];
						headPtrName = values[1];
					}
				}
				else
				{
					match = Regex.Match(line, @"(\S*).*\[(.*)\]");
					if (match.Success)
					{
						isTracking = true;
						fullname = match.Groups[1].Value;
						name = fullname;

						trackingBranchFullName = match.Groups[2].Value;
						var values = trackingBranchFullName.Split('/');
						if (values.Length == 2)
						{
							trackingBranchRemoteName = values[0];
							trackingBranchName = values[1];
						}
					}
					else
					{
						match = Regex.Match(line, @"(\S*)");
						if (match.Success)
						{
							fullname = match.Groups[1].Value;
							name = fullname;
						}
						else
						{
							return;
						}
					}
				}

				// check if branch is remote
				match = Regex.Match(fullname, @"remotes/(.*)/(.*)");
				if (match.Success)
				{
					isRemote = true;
					remoteName = match.Groups[1].Value;
					name = match.Groups[2].Value;
					fullname = remoteName + '/' + name;
				}

				// create branch object
				var branch = new Branch()
				{
					name = name,
					fullname = fullname,
					isActive = isActive,
					isRemote = isRemote,
					remoteName = remoteName,
					isHead = isHead,
					isTracking = isTracking,
				};

				// fill head info
				if (isHead)
				{
					branch.head = new BranchInfo()
					{
						name = headPtrName,
						fullname = headPtrFullName,
						remoteName = headPtrRemoteName
					};
				}

				// fill tracking info
				if (isTracking)
				{
					branch.tracking = new BranchInfo()
					{
						name = trackingBranchName,
						fullname = trackingBranchFullName,
						remoteName = trackingBranchRemoteName
					};
				}

				branchList.Add(branch);
			}
			
			var result = Tools.RunExe("git", "branch -a -vv", stdCallback:stdCallback);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			if (!string.IsNullOrEmpty(lastError))
			{
				branches = null;
				return false;
			}

			branches = branchList.ToArray();
			return true;
		}
	}
}
