using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	public static class BranchManager
	{
		public static Branch activeBranch;

		internal static void OpenRepo(Repository repo)
		{
			activeBranch = repo.Head;
		}

		public static string GetRemote()
		{
			if (!activeBranch.IsRemote) return "";
			return activeBranch.Remote.Url;
		}

		public static string[] GetBranches()
		{
			try
			{
				var branchNames = new List<string>();
				var branches = RepoManager.repo.Branches;
				foreach (var branch in branches)
				{
					// make sure we don't show remotes that match locals
					if (branch.IsRemote)
					{
						if (branch.FriendlyName == "origin/HEAD" || branch.FriendlyName == "origin/master") continue;

						bool found = false;
						foreach (var otherBranch in branches)
						{
							if (branch.FriendlyName == otherBranch.FriendlyName) continue;
							if (branch.FriendlyName.Replace("origin/", "") == otherBranch.FriendlyName)
							{
								found = true;
								break;
							}
						}

						if (found) continue;
						branchNames.Add(branch.FriendlyName);
					}
				}

				return branchNames.ToArray();
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.GetBranches Failed: " + e.Message);
				return null;
			}
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

		public static bool AddNewBranch(string branchName, bool isRemote)
		{
			try
			{
				var branch = RepoManager.repo.CreateBranch(branchName);
				if (isRemote)
				{
					RepoManager.repo.Branches.Update(branch, b =>
					{
						b.Remote = "origin";
						b.UpstreamBranch = branch.CanonicalName;
					});
				}

				RepoManager.repo.Checkout(branch);
				activeBranch = branch;
			}
			catch (Exception ex)
			{
				Debug.LogError("Add new Branch Error: " + ex.Message, true);
				return false;
			}
			
			RepoManager.Refresh();
			return true;
		}

		public static bool DeleteNonActiveBranch(string branchName)
		{
			try
			{
				RepoManager.repo.Branches.Remove(branchName);
				var remoteBranch = RepoManager.repo.Branches["origin/" + branchName];
				if (remoteBranch != null)
				{
					RepoManager.repo.Branches.Remove(remoteBranch);
					Tools.RunExe("git", string.Format("push origin --delete {0}", branchName), null);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Delete new Branch Error: " + ex.Message, true);
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
			catch (Exception ex)
			{
				Debug.LogError("Rename new Branch Error: " + ex.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}
	}
}
