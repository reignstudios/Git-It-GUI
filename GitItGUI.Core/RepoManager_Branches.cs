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

	public partial class RepoManager
	{
		public bool isEmpty {get; private set;}
		public BranchState activeBranch {get; private set;}
		public BranchState[] branchStates {get; private set;}
		public RemoteState[] remoteStates {get; private set;}

		private bool RefreshBranches(bool isRefreshMode)
		{
			isEmpty = false;

			try
			{
				// gather branches
				BranchState[] bStates;
				if (!Repository.GetBrancheStates(out bStates)) throw new Exception(Repository.lastError);
				branchStates = bStates;

				// check for new repo state
				if (branchStates.Length == 0)
				{
					isEmpty = true;
					activeBranch = null;
					branchStates = null;
					remoteStates = null;
					DebugLog.LogWarning("New branch, nothing commit!");
					return true;
				}

				// find active branch
				activeBranch = Array.Find<BranchState>(branchStates, x => x.isActive);
				if (activeBranch.isRemote)
				{
					DebugLog.LogError("Active repo branch cannot be a remote: " + activeBranch.fullname, true);
					if (isRefreshMode) Environment.Exit(0);
					else return false;
				}

				// gather remotes
				RemoteState[] rStates;
				if (!Repository.GetRemoteStates(out rStates)) throw new Exception(Repository.lastError);
				remoteStates = rStates;
			}
			catch (Exception e)
			{
				DebugLog.LogError("BranchManager.Refresh Failed: " + e.Message, true);
				return false;
			}

			return true;
		}

		public BranchState[] GetNonActiveBranches(bool getRemotes)
		{
			lock(this)
			{
				if (branchStates == null) return new BranchState[0];

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
		}

		public bool Checkout(BranchState branch, bool useFullname = false)
		{
			lock (this)
			{
				if (activeBranch == null) return false;

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
						DebugLog.LogError("Already on branch: " + name, true);
						success = false;
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("BranchManager.Checkout Failed: " + e.Message, true);
					success = false;
				}
			
				Refresh();
				return success;
			}
		}

		public MergeResults MergeBranchIntoActive(BranchState srcBranch)
		{
			lock (this)
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
					DebugLog.LogError("BranchManager.Merge Failed: " + e.Message, true);
					mergeResult = MergeResults.Error;
				}
			
				Refresh();
				return mergeResult;
			}
		}

		public bool CheckoutNewBranch(string branchName, string remoteName = null)
		{
			lock (this)
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
					DebugLog.LogError("Add new Branch Error: " + e.Message, true);
					success = false;
				}
			
				Refresh();
				return success;
			}
		}

		public bool DeleteNonActiveBranch(BranchState branch)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!Repository.DeleteBranch(branch.fullname, branch.isRemote)) throw new Exception(Repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Delete new Branch Error: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool RenameActiveBranch(string newBranchName)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!Repository.RenameActiveBranch(newBranchName)) throw new Exception(Repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Rename new Branch Error: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}
		
		public bool CopyTracking(BranchState srcRemoteBranch)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!Repository.SetActiveBranchTracking(srcRemoteBranch.fullname)) throw new Exception(Repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Add/Update tracking Branch Error: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool RemoveTracking()
		{
			lock (this)
			{
				if (activeBranch == null) return false;
				if (!activeBranch.isTracking) return true;

				bool success = true;
				try
				{
					if (!Repository.RemoveActiveBranchTracking()) throw new Exception(Repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Remove Branch Error: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool IsUpToDateWithRemote(out bool yes)
		{
			lock (this)
			{
				if (activeBranch == null)
				{
					yes = false;
					return false;
				}

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
					DebugLog.LogError("Remove Branch Error: " + e.Message, true);
					yes = false;
					return false;
				}
			
				return true;
			}
		}

		public bool PruneRemoteBranches()
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!Repository.PruneRemoteBranches()) throw new Exception(Repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to prune branches: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}
	}
}
