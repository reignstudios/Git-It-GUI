using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	public class BranchState
	{
		public Branch branch;
		public string name, trackedBranchName;
		public bool isRemote, isTracking;
	}

	public static class BranchManager
	{
		public static Branch activeBranch;
		private static List<BranchState> allBranches;

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
			var b = RepoManager.repo.Branches[branch.name];
			return b.IsRemote;
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

		internal static bool Refresh()
		{
			if (allBranches == null) allBranches = new List<BranchState>();
			else allBranches.Clear();

			var branches = RepoManager.repo.Branches;
			foreach (var branch in branches)
			{
				var b = new BranchState();
				b.branch = branch;
				b.isRemote = branch.IsRemote;
				b.isTracking = branch.IsTracking;
				b.name = branch.FriendlyName;
				if (branch.IsTracking) b.trackedBranchName = branch.TrackedBranch.FriendlyName;
				allBranches.Add(b);
			}

			return true;
		}

		public static BranchState[] GetAllBranches()
		{
			return allBranches.ToArray();
		}

		public static BranchState[] GetOtherBranches()
		{
			var otherBranches = new List<BranchState>();
			foreach (var branch in allBranches)
			{
				if (activeBranch != branch.branch) otherBranches.Add(branch);
			}

			return otherBranches.ToArray();
		}

		public static bool Checkout(BranchState branch)
		{
			try
			{
				var selectedBranch = RepoManager.repo.Branches[branch.name];
				if (activeBranch != selectedBranch)
				{
					var newBranch = Commands.Checkout(RepoManager.repo, selectedBranch);
					if (newBranch != selectedBranch)
					{
						Debug.LogError("Error checking out branch (do you have pending changes?)", true);
						return false;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Checkout Failed: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool MergeBranchIntoActive(BranchState srcBranch)
		{
			try
			{
				var srcBround = RepoManager.repo.Branches[srcBranch.name];
				RepoManager.repo.Merge(srcBround, RepoManager.signature);
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Merge Failed: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool AddNewBranch(string branchName)
		{
			try
			{
				var branch = RepoManager.repo.CreateBranch(branchName);
				Commands.Checkout(RepoManager.repo, branch);
				activeBranch = branch;
			}
			catch (Exception e)
			{
				Debug.LogError("Add new Branch Error: " + e.Message, true);
				return false;
			}
			
			RepoManager.Refresh();
			return true;
		}

		public static bool AddTrackingToActiveBranch(string remote)
		{
			try
			{
				RepoManager.repo.Branches.Update(activeBranch, b =>
				{
					b.Remote = remote;// normally this will be: "origin"
					b.UpstreamBranch = activeBranch.CanonicalName;
				});
			}
			catch (Exception e)
			{
				Debug.LogError("Add tracking Error: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool DeleteNonActiveBranch(BranchState branch)
		{
			try
			{
				RepoManager.repo.Branches.Remove(branch.name);
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
					b.Remote = "origin";
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

		public static bool AddUpdateTracking(BranchState srcRemoteBranch)
		{
			try
			{
				var srcBranch = RepoManager.repo.Branches[srcRemoteBranch.name];
				RepoManager.repo.Branches.Update(activeBranch, b =>
				{
					b.Remote = RepoManager.repo.Network.Remotes[srcBranch.RemoteName].Name;
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
