using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitCommander
{
	public class BranchInfo
	{
		public string name {get; internal set;}
		public string fullname {get; internal set;}
		public RemoteState remoteState {get; internal set;}
	}

	public class BranchState
	{
		public string name {get; internal set;}
		public string fullname {get; internal set;}
		public RemoteState remoteState {get; internal set;}

		public bool isActive {get; internal set;}
		public bool isRemote {get; internal set;}
		public bool isTracking {get; internal set;}
		public bool isHead {get; internal set;}
		public bool isHeadDetached {get; internal set;}
		public BranchInfo headPtr {get; internal set;}
		public BranchInfo tracking {get; internal set;}

		public override string ToString()
		{
			return fullname;
		}
	}

	public partial class Repository
	{
		public bool DeleteBranch(string branch, bool isRemote)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("branch {1} {0}", branch, isRemote ? "-dr" : "-d"));
			}
		}

		public bool DeleteRemoteBranch(string branch, string remote)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("push {1} --delete {0}", branch, remote));
			}
		}

		public bool RenameActiveBranch(string newBranchName)
		{
			lock (this)
			{
				return SimpleGitInvoke("branch -m " + newBranchName);
			}
		}

		public bool RenameNonActiveBranch(string currentBranchName, string newBranchName)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("branch -m {0} {1}", currentBranchName, newBranchName));
			}
		}

		public bool SetActiveBranchTracking(string fullBranchName)
		{
			lock (this)
			{
				return SimpleGitInvoke("branch -u " + fullBranchName);
			}
		}

		public bool RemoveActiveBranchTracking()
		{
			lock (this)
			{
				return SimpleGitInvoke("branch --unset-upstream");
			}
		}

		public bool PruneRemoteBranches()
		{
			lock (this)
			{
				return SimpleGitInvoke("remote prune origin");
			}
		}

		public bool GetRemotePrunableBrancheNames(out string[] branchName)
		{
			lock (this)
			{
				var branchNameList = new List<string>();
				void stdCallback(string line)
				{
					branchNameList.Add(line);
				}
			
				var result = RunExe("git", "remote prune origin --dry-run", stdCallback:stdCallback);
				lastResult = result.output;
				lastError = result.errors;

				if (!string.IsNullOrEmpty(lastError))
				{
					branchName = null;
					return false;
				}
			
				branchName = branchNameList.ToArray();
				return true;
			}
		}
		
		public bool GetBrancheStates(out BranchState[] brancheStates)
		{
			var states = new List<BranchState>();
			void stdCallback(string line)
			{
				line = line.TrimStart();
				if (string.IsNullOrEmpty(line)) return;

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
				bool isRemote = false, isHead = false, headPtrExists = false, isHeadDetached = false, isTracking = false;

				// get name and tracking info
				var match = Regex.Match(line, @"remotes/(.*)/HEAD\s*->\s*(\S*)");
				if (match.Success)
				{
					isRemote = true;
					isHead = true;
					headPtrExists = true;
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
					match = Regex.Match(line, @"\(HEAD detached at (.*)/(.*)\)");
					if (match.Success)
					{
						isHead = true;
						isHeadDetached = true;
						isRemote = true;
						name = "HEAD";
						remoteName = match.Groups[1].Value;
						fullname = remoteName + '/' + name;

						headPtrRemoteName = remoteName;
						headPtrName = match.Groups[2].Value;
					}
					else
					{
						match = Regex.Match(line, @"(\S*).*\[(.*)\]");
						if (match.Success)
						{
							isTracking = true;
							fullname = match.Groups[1].Value;
							name = fullname;
						
							string trackedBranch = match.Groups[2].Value;
							trackingBranchFullName = trackedBranch.Contains(":") ? trackedBranch.Split(':')[0] : trackedBranch;
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
				}

				// check if branch is remote
				match = Regex.Match(fullname, @"remotes/(.*)/(.*)");
				if (match.Success)
				{
					isRemote = true;
					remoteName = match.Groups[1].Value;
					name = match.Groups[2].Value;
					fullname = remoteName + '/' + name;
					if (name == "HEAD") isHead = true;
				}

				// create branch object
				var branch = new BranchState()
				{
					name = name,
					fullname = fullname,
					isActive = isActive,
					isRemote = isRemote,
					isHead = isHead,
					isHeadDetached = isHeadDetached,
					isTracking = isTracking,
				};

				if (!string.IsNullOrEmpty(remoteName))
				{
					branch.remoteState = new RemoteState() {name = remoteName};
				}

				// fill head info
				if (headPtrExists)
				{
					branch.headPtr = new BranchInfo()
					{
						name = headPtrName,
						fullname = headPtrFullName
					};

					if (!string.IsNullOrEmpty(headPtrRemoteName))
					{
						branch.headPtr.remoteState = new RemoteState() {name = headPtrRemoteName};
					}
				}

				// fill tracking info
				if (isTracking)
				{
					branch.tracking = new BranchInfo()
					{
						name = trackingBranchName,
						fullname = trackingBranchFullName
					};

					if (!string.IsNullOrEmpty(trackingBranchRemoteName))
					{
						branch.tracking.remoteState = new RemoteState() {name = trackingBranchRemoteName};
					}
				}

				states.Add(branch);
			}

			lock (this)
			{
				var result = RunExe("git", "branch -a -vv", stdCallback:stdCallback);
				lastResult = result.output;
				lastError = result.errors;

				if (!string.IsNullOrEmpty(lastError))
				{
					brancheStates = null;
					return false;
				}

				// get remote urls
				foreach (var state in states)
				{
					string url;
					if (state.remoteState != null && GetRemoteURL(state.remoteState.name, out url)) state.remoteState.url = url;
					if (state.headPtr != null && state.headPtr.remoteState != null && GetRemoteURL(state.headPtr.remoteState.name, out url)) state.headPtr.remoteState.url = url;
					if (state.tracking != null && state.tracking.remoteState != null && GetRemoteURL(state.tracking.remoteState.name, out url)) state.tracking.remoteState.url = url;
				}

				brancheStates = states.ToArray();
				return true;
			}
		}

		public bool CheckoutBranch(string branch)
		{
			lock (this)
			{
				var result = RunExe("git", "checkout " + branch);
				lastResult = result.output;
				lastError = result.errors;
			
				return string.IsNullOrEmpty(lastError);
			}
		}

		public bool CheckoutNewBranch(string branch)
		{
			lock (this)
			{
				return SimpleGitInvoke("checkout -b " + branch);
			}
		}

		public bool PushLocalBranchToRemote(string branch, string remote)
		{
			lock (this)
			{
				return SimpleGitInvoke(string.Format("push -u {1} {0}", branch, remote));
			}
		}

		public bool MergeBranchIntoActive(string branch)
		{
			lock (this)
			{
				return SimpleGitInvoke("merge " + branch);
			}
		}

		public bool IsUpToDateWithRemote(string remote, string branch, out bool yes)
		{
			bool isUpToDate = true;
			void stdCallback_log(string line)
			{
				var match = Regex.Match(line, @"commit (.*)");
				if (match.Success) isUpToDate = false;
			}

			void stdCallback_fetch(string line)
			{
				var match = Regex.Match(line, string.Format(@"\s*(.*)\.\.(.*)\s*{1}\s*->\s*{0}/{1}", remote, branch));
				if (match.Success && match.Groups[1].Value != match.Groups[2].Value) isUpToDate = false;
			}

			lock (this)
			{
				var result = RunExe("git", string.Format("log {0}/{1}..{1}", remote, branch), stdCallback: stdCallback_log);
				lastResult = result.output;
				lastError = result.errors;
				bool remoteDoesntHaveBranch = lastError.Contains("unknown revision or path not in the working tree");
				yes = isUpToDate && !remoteDoesntHaveBranch;
				if (!isUpToDate && !remoteDoesntHaveBranch) return string.IsNullOrEmpty(lastError);
				if (remoteDoesntHaveBranch) return true;

				isUpToDate = true;
				result = RunExe("git", string.Format("fetch {0} {1} --dry-run", remote, branch), stdCallback: stdCallback_fetch);
				lastResult = result.output;
				lastError = result.errors;
				if (!isUpToDate) yes = false;

				return string.IsNullOrEmpty(lastError);
			}
		}
	}
}
