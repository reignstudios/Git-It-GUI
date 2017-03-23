using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	public class Remote
	{
		public string name, url;

		public override string ToString()
		{
			return name;
		}
	}

	public class BranchState
	{
		public Branch branch;
		public string fullName, branchName, trackedBranchName;
		public bool isRemote, isTracking;
	}

	public enum MergeResults
	{
		Succeeded,
		Conflicts,
		Error
	}

	public static class BranchManager
	{
		public static Branch activeBranch;
		public static GitCommander.Branch activeBranchCommander;
		private static List<BranchState> allBranches;
		private static List<Remote> remotes;

		internal static void OpenRepo(Repository repo)
		{
			activeBranch = repo.Head;
		}

		public static bool IsRemote()
		{
			return activeBranch.IsRemote;
		}

		public static bool IsRemote(BranchState branch)
		{
			var b = RepoManager.repo.Branches[branch.fullName];
			return b.IsRemote;
		}

		public static bool IsTracking()
		{
			return activeBranch.IsTracking;
		}

		public static bool IsTracking(BranchState branch)
		{
			var b = RepoManager.repo.Branches[branch.fullName];
			return b.IsTracking;
		}

		public static string GetRemoteURL()
		{
			if (!activeBranch.IsTracking || activeBranch.TrackedBranch == null) return "";
			return RepoManager.repo.Network.Remotes[activeBranch.TrackedBranch.RemoteName].Url;
		}

		public static string GetTrackedBranchName()
		{
			if (!activeBranch.IsTracking || activeBranch.TrackedBranch == null) return "";
			return activeBranch.TrackedBranch.FriendlyName;
		}

		public static Remote[] GetRemotes()
		{
			return remotes.ToArray();
		}

		internal static bool Refresh()
		{
			// gather branches
			if (allBranches == null) allBranches = new List<BranchState>();
			else allBranches.Clear();

			var branches = RepoManager.repo.Branches;
			foreach (var branch in branches)
			{
				var b = new BranchState();
				b.branch = branch;
				b.isRemote = branch.IsRemote;
				b.isTracking = branch.IsTracking;
				b.fullName = branch.FriendlyName;
				b.branchName = branch.FriendlyName.Replace(branch.RemoteName+"/", "");
				if (branch.IsTracking) b.trackedBranchName = branch.TrackedBranch.FriendlyName;
				allBranches.Add(b);
			}

			// gather remotes
			if (remotes == null) remotes = new List<Remote>();
			else remotes.Clear();

			var allRemotes = RepoManager.repo.Network.Remotes;
			foreach (var remote in allRemotes)
			{
				var newRemote = new Remote()
				{
					name = remote.Name,
					url = remote.Url
				};

				remotes.Add(newRemote);
			}

			return true;
		}

		public static BranchState[] GetAllBranches()
		{
			return allBranches.ToArray();
		}

		public static BranchState[] GetOtherBranches(bool getRemotes)
		{
			var otherBranches = new List<BranchState>();
			foreach (var branch in allBranches)
			{
				if (activeBranch.FriendlyName != branch.branch.FriendlyName)
				{
					if (getRemotes)
					{
						otherBranches.Add(branch);
					}
					else
					{
						if (!branch.isRemote) otherBranches.Add(branch);
					}
				}
			}

			return otherBranches.ToArray();
		}

		public static bool Checkout(BranchState branch, StatusUpdateCallbackMethod statusCallback)
		{
			return Checkout(branch.fullName, statusCallback);
		}

		public static bool Checkout(string name, StatusUpdateCallbackMethod statusCallback)
		{
			try
			{
				// check for git settings file not in repo history
				RepoManager.DeleteRepoSettingsIfUnCommit();

				// checkout
				var selectedBranch = RepoManager.repo.Branches[name];
				if (activeBranch.FriendlyName != selectedBranch.FriendlyName)
				{
					var options = new CheckoutOptions();
					options.OnCheckoutProgress = delegate(string path, int completedSteps, int totalSteps)
					{
						if (statusCallback != null) statusCallback(string.Format("Checking out: {0}%", (int)((completedSteps / (decimal)(totalSteps+1)) * 100)));
					};

					Filters.GitLFS.statusCallback = statusCallback;
					var newBranch = Commands.Checkout(RepoManager.repo, selectedBranch, options);
					Filters.GitLFS.statusCallback = null;

					if (newBranch.FriendlyName != selectedBranch.FriendlyName)
					{
						Debug.LogError("Error checking out branch (do you have pending changes?)", true);
						Filters.GitLFS.statusCallback = null;
						return false;
					}
				}
				else
				{
					Debug.LogError("Already on branch: " + name, true);
					Filters.GitLFS.statusCallback = null;
					return false;
				}
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Checkout Failed: " + e.Message, true);
				Filters.GitLFS.statusCallback = null;
				return false;
			}

			Filters.GitLFS.statusCallback = null;
			RepoManager.Refresh();
			return true;
		}

		public static MergeResults MergeBranchIntoActive(BranchState srcBranch, StatusUpdateCallbackMethod statusCallback)
		{
			MergeResults mergeResult;
			try
			{
				// check for git settings file not in repo history
				RepoManager.DeleteRepoSettingsIfUnCommit();

				// merge
				var options = new MergeOptions();
				options.OnCheckoutProgress = delegate(string path, int completedSteps, int totalSteps)
				{
					if (statusCallback != null) statusCallback(string.Format("Checking out: {0}%", (int)((completedSteps / (decimal)(totalSteps+1)) * 100)));
				};

				var srcBround = RepoManager.repo.Branches[srcBranch.fullName];

				Filters.GitLFS.statusCallback = statusCallback;
				var result = RepoManager.repo.Merge(srcBround, RepoManager.signature, options);
				Filters.GitLFS.statusCallback = null;

				if (result.Status == MergeStatus.Conflicts) mergeResult = MergeResults.Conflicts;
				else mergeResult = MergeResults.Succeeded;
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Merge Failed: " + e.Message, true);
				Filters.GitLFS.statusCallback = null;
				return MergeResults.Error;
			}

			Filters.GitLFS.statusCallback = null;
			RepoManager.Refresh();
			return mergeResult;
		}

		public static bool AddNewBranch(string branchName, string remoteName = null)
		{
			bool success = true;

			try
			{
				// create branch
				var branch = RepoManager.repo.CreateBranch(branchName);
				Commands.Checkout(RepoManager.repo, branch);
				activeBranch = branch;

				// push branch to remote
				if (!string.IsNullOrEmpty(remoteName))
				{
					// add remote
					RepoManager.repo.Branches.Update(activeBranch, b =>
					{
						b.Remote = remoteName;
						b.TrackedBranch = string.Format("refs/remotes/{0}/{1}", remoteName, branchName);
						b.UpstreamBranch = "refs/heads/" + branchName;
					});

					// push remote
					var result = GitCommander.Tools.RunExe("git", string.Format("push -u {0} {1}", remoteName, branch.FriendlyName));
					if (!string.IsNullOrEmpty(result.stdErrorResult) && !result.stdErrorResult.Contains("To create a merge request for"))//NOTE: this ignores false positive noise errors that come from GitLab
					{
						Debug.LogError("Push remote failed: " + result.stdErrorResult, true);
						success = false;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Add new Branch Error: " + e.Message, true);
				return false;
			}
			
			RepoManager.Refresh();
			return success;
		}

		public static bool DeleteNonActiveBranch(BranchState branch)
		{
			try
			{
				RepoManager.repo.Branches.Remove(branch.fullName);
			}
			catch (Exception e)
			{
				Debug.LogError("Delete new Branch Error: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool RenameActiveBranch(string newBranchName)
		{
			try
			{
				var branch = RepoManager.repo.Branches.Rename(activeBranch.FriendlyName, newBranchName);
				RepoManager.repo.Branches.Update(branch, b =>
				{
					b.Remote = activeBranch.RemoteName;
					b.UpstreamBranch = branch.CanonicalName;
				});
			}
			catch (Exception e)
			{
				Debug.LogError("Rename new Branch Error: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool CopyTracking(BranchState srcRemoteBranch)
		{
			return CopyTracking(srcRemoteBranch.fullName);
		}

		public static bool CopyTracking(string srcRemoteBranch)
		{
			try
			{
				var srcBranch = RepoManager.repo.Branches[srcRemoteBranch];
				RepoManager.repo.Branches.Update(activeBranch, b =>
				{
					b.Remote = srcBranch.RemoteName;
					b.TrackedBranch = srcBranch.CanonicalName;
					b.UpstreamBranch = srcBranch.UpstreamBranchCanonicalName;
				});
			}
			catch (Exception e)
			{
				Debug.LogError("Add/Update tracking Branch Error: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool RemoveTracking()
		{
			if (!activeBranch.IsTracking) return true;

			try
			{
				RepoManager.repo.Branches.Update(activeBranch, b =>
				{
					b.Remote = null;
					b.TrackedBranch = null;
					b.UpstreamBranch = null;
				});
			}
			catch (Exception e)
			{
				Debug.LogError("Remove Branch Error: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}
	}
}
