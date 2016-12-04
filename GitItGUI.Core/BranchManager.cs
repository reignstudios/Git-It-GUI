using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	public class BranchItem
	{
		public Branch branch;
		public string name, trackedBranchName;
		public bool isRemote, isTracking;
	}

	public static class BranchManager
	{
		public static Branch activeBranch;
		private static List<BranchItem> allBranches;

		internal static void OpenRepo(Repository repo)
		{
			activeBranch = repo.Head;
		}

		public static bool IsRemote()
		{
			return activeBranch.IsRemote && activeBranch.Remote != null && !string.IsNullOrEmpty(activeBranch.Remote.Url);
		}

		public static string GetRemoteURL()
		{
			if (activeBranch.Remote == null) return "";
			return activeBranch.Remote.Url;
		}

		public static string GetTrackedBranchName()
		{
			if (activeBranch.IsTracking || activeBranch.TrackedBranch == null) return "";
			return activeBranch.TrackedBranch.FriendlyName;
		}

		internal static bool Refresh()
		{
			if (allBranches == null) allBranches = new List<BranchItem>();
			else allBranches.Clear();

			var branches = RepoManager.repo.Branches;
			foreach (var branch in branches)
			{
				var b = new BranchItem();
				b.branch = branch;
				b.isRemote = branch.IsRemote;
				b.isTracking = branch.IsTracking;
				b.name = branch.FriendlyName;
				if (branch.IsTracking) b.trackedBranchName = branch.TrackedBranch.FriendlyName;
				allBranches.Add(b);
			}

			return true;
		}

		public static BranchItem[] GetAllBranches()
		{
			return allBranches.ToArray();
		}

		public static BranchItem[] GetOtherBranches()
		{
			var otherBranches = new List<BranchItem>();
			foreach (var branch in allBranches)
			{
				if (activeBranch != branch.branch) otherBranches.Add(branch);
			}

			return otherBranches.ToArray();
		}

		public static bool Checkout(string branchName)
		{
			try
			{
				var selectedBranch = RepoManager.repo.Branches[branchName];
				if (activeBranch != selectedBranch)
				{
					var newBranch = RepoManager.repo.Checkout(selectedBranch);
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

		public static bool MergeBranchIntoActive(string srcBranchName)
		{
			try
			{
				var srcBround = RepoManager.repo.Branches[srcBranchName];
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
				RepoManager.repo.Checkout(branch);
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

		public static bool DeleteNonActiveBranch(string branchName)//, bool deleteRemote)
		{
			try
			{
				RepoManager.repo.Branches.Remove(branchName);
				//if (deleteRemote)
				//{
				//	var remoteBranch = RepoManager.repo.Branches["origin/" + branchName];
				//	if (remoteBranch != null)
				//	{
				//		RepoManager.repo.Branches.Remove(remoteBranch);
				//		Tools.RunExe("git", string.Format("push origin --delete {0}", branchName), null);
				//	}
				//}
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
	}
}
