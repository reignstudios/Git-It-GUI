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

		internal static bool Refresh(bool refreshMode)
		{
			try
			{
				// gather branches
				BranchState[] bStates;
				if (!Repository.GetBrancheStates(out bStates)) throw new Exception(Repository.lastError);
				branchStates = bStates;

				// find active branch
				activeBranch = Array.Find<BranchState>(branchStates, x => x.isActive);
				if (activeBranch.isRemote)
				{
					Debug.LogError("Active repo branch cannot be a remote: " + activeBranch.fullname, true);
					if (refreshMode) Environment.Exit(0);
					else return false;
				}

				// gather remotes
				RemoteState[] rStates;
				if (!Repository.GetRemoteStates(out rStates)) throw new Exception(Repository.lastError);
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

		public static bool Checkout(BranchState branch, bool useFullname = false)
		{
			bool success = true;
			try
			{
				string name = useFullname ? branch.fullname : branch.name;
				if (activeBranch.name != name)
				{
					if (!Repository.CheckoutBranch(name)) throw new Exception(Repository.lastError);
				}
				else
				{
					Debug.LogError("Already on branch: " + name, true);
					success = false;
				}
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Checkout Failed: " + e.Message, true);
				success = false;
			}
			
			RepoManager.Refresh();
			return success;
		}

		public static MergeResults MergeBranchIntoActive(BranchState srcBranch)
		{
			MergeResults mergeResult;
			try
			{
				if (!Repository.MergeBranchIntoActive(srcBranch.fullname)) throw new Exception(Repository.lastError);

				bool yes;
				if (!Repository.ConflitedExist(out yes)) throw new Exception(Repository.lastError);
				mergeResult = yes ? MergeResults.Conflicts : MergeResults.Succeeded;
			}
			catch (Exception e)
			{
				Debug.LogError("BranchManager.Merge Failed: " + e.Message, true);
				mergeResult = MergeResults.Error;
			}
			
			RepoManager.Refresh();
			return mergeResult;
		}

		public static bool CheckoutNewBranch(string branchName, string remoteName = null)
		{
			bool success = true;
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
				success = false;
			}
			
			RepoManager.Refresh();
			return success;
		}

		public static bool DeleteNonActiveBranch(BranchState branch)
		{
			bool success = true;
			try
			{
				if (!Repository.DeleteBranch(branch.fullname, branch.isRemote)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Delete new Branch Error: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool RenameActiveBranch(string newBranchName)
		{
			bool success = true;
			try
			{
				if (!Repository.RenameActiveBranch(newBranchName)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Rename new Branch Error: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}
		
		public static bool CopyTracking(BranchState srcRemoteBranch)
		{
			bool success = true;
			try
			{
				if (!Repository.SetActiveBranchTracking(srcRemoteBranch.fullname)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Add/Update tracking Branch Error: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool RemoveTracking()
		{
			if (!activeBranch.isTracking) return true;

			bool success = true;
			try
			{
				if (!Repository.RemoveActiveBranchTracking()) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Remove Branch Error: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool IsUpToDateWithRemote(out bool yes)
		{
			if (!activeBranch.isTracking)
			{
				yes = true;
				return true;
			}

			try
			{
				if (!Repository.IsUpToDateWithRemote(activeBranch.tracking.remoteState.name, activeBranch.tracking.name, out yes)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Remove Branch Error: " + e.Message, true);
				yes = false;
				return false;
			}
			
			return true;
		}
	}
}
