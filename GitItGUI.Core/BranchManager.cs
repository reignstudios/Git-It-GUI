using GitCommander;
using System;
using System.Collections.Generic;

namespace GitItGUI.Core
{
	public enum MergeResults
	{
		Succeeded,
		Conflicts,
		Error
	}

	public static class BranchManager
	{
		public static BranchState activeBranch {get; private set;}
		public static BranchState[] branchStates {get; private set;}
		public static RemoteState[] remoteStates {get; private set;}

		internal static bool Refresh()
		{
			try
			{
				// gather branches
				if (!Repository.GetBrancheStates(out var bStates)) throw new Exception(Repository.lastError);
				branchStates = bStates;

				// find active branch
				activeBranch = Array.Find<BranchState>(branchStates, x => x.isActive);

				// gather remotes
				if (!Repository.GetRemoteStates(out var rStates)) throw new Exception(Repository.lastError);
				remoteStates = rStates;
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Refresh Failed: " + e.Message, true);
				return false;
			}

			return true;
		}

		public static BranchState[] GetNonActiveBranches(bool getRemotes)
		{
			var nonActiveBranches = new List<BranchState>();
			foreach (var branch in branchStates)
			{
				if (activeBranch.fullname != branch.fullname)
				{
					if (getRemotes)
					{
						nonActiveBranches.Add(branch);
					}
					else
					{
						if (!branch.isRemote) nonActiveBranches.Add(branch);
					}
				}
			}

			return nonActiveBranches.ToArray();
		}

		public static bool Checkout(BranchState branch, StatusUpdateCallbackMethod statusCallback)
		{
			try
			{
				// check for git settings file not in repo history
				RepoManager.DeleteRepoSettingsIfUnCommit();

				// checkout
				if (activeBranch.name != branch.name)
				{
					if (!Repository.CheckoutExistingBranch(branch.name)) throw new Exception(Repository.lastError);
				}
				else
				{
					Debug.LogError("Already on branch: " + branch.name, true);
					return false;
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

		public static MergeResults MergeBranchIntoActive(BranchState srcBranch, StatusUpdateCallbackMethod statusCallback)
		{
			MergeResults mergeResult;
			try
			{
				// check for git settings file not in repo history
				RepoManager.DeleteRepoSettingsIfUnCommit();

				// merge
				if (!Repository.MergeBranchIntoActive(srcBranch.name)) throw new Exception(Repository.lastError);
				if (!Repository.ConflitedExist(out bool yes)) throw new Exception(Repository.lastError);
				mergeResult = yes ? MergeResults.Conflicts : MergeResults.Succeeded;
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Merge Failed: " + e.Message, true);
				return MergeResults.Error;
			}
			
			RepoManager.Refresh();
			return mergeResult;
		}

		public static bool CheckoutNewBranch(string branchName, string remoteName = null)
		{
			try
			{
				// create branch
				if (!Repository.CheckoutNewBranch(branchName)) throw new Exception(Repository.lastError);

				// push branch to remote
				if (!string.IsNullOrEmpty(remoteName))
				{
					if (!Repository.PushLocalBranchToRemote(branchName, remoteName))
					{
						//NOTE: this ignores false positive noise/errors that come from GitLab
						if (!string.IsNullOrEmpty(Repository.lastError) && !Repository.lastError.Contains("To create a merge request for"))
						{
							throw new Exception(Repository.lastError);
						}
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Add new Branch Error: " + e.Message, true);
				return false;
			}
			
			RepoManager.Refresh();
			return true;
		}

		public static bool DeleteNonActiveBranch(BranchState branch)
		{
			try
			{
				if (!Repository.DeleteBranch(branch.name)) throw new Exception(Repository.lastError);
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
				if (!Repository.RenameActiveBranch(newBranchName)) throw new Exception(Repository.lastError);
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
			try
			{
				if (!Repository.SetActiveBranchTracking(srcRemoteBranch.fullname)) throw new Exception(Repository.lastError);
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
			if (!activeBranch.isTracking) return true;

			try
			{
				if (!Repository.RemoveActiveBranchTracking()) throw new Exception(Repository.lastError);
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
