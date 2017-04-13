using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GitCommander;

namespace GitItGUI.Core
{
	public enum MergeBinaryFileResults
	{
		Error,
		Cancel,
		UseTheirs,
		KeepMine,
		RunMergeTool
	}

	public enum MergeFileAcceptedResults
	{
		Yes,
		No
	}

	public enum SyncMergeResults
	{
		Succeeded,
		Conflicts,
		Error
	}

	public static class ChangesManager
	{
		public delegate bool AskUserToResolveConflictedFileCallbackMethod(FileState fileState, bool isBinaryFile, out MergeBinaryFileResults result);
		public static event AskUserToResolveConflictedFileCallbackMethod AskUserToResolveConflictedFileCallback;

		public delegate bool AskUserIfTheyAcceptMergedFileCallbackMethod(FileState fileState, out MergeFileAcceptedResults result);
		public static event AskUserIfTheyAcceptMergedFileCallbackMethod AskUserIfTheyAcceptMergedFileCallback;

		public static FileState[] fileStates {get; private set;}
		private static bool isSyncMode;

		internal static bool Refresh()
		{
			try
			{
				FileState[] states;
				if (!Repository.GetFileStates(out states)) throw new Exception(Repository.lastError);
				fileStates = states;
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("ChangesManager.Refresh Failed: " + e.Message, true);
				return false;
			}
		}

		public static bool FilesAreStaged()
		{
			foreach (var state in fileStates)
			{
				if (state.HasState(FileStates.DeletedFromIndex) || state.HasState(FileStates.ModifiedInIndex) ||
					state.HasState(FileStates.NewInIndex) || state.HasState(FileStates.RenamedInIndex) || state.HasState(FileStates.TypeChangeInIndex))
				{
					return true;
				}
			}

			return false;
		}

		public static bool FilesAreUnstaged()
		{
			foreach (var state in fileStates)
			{
				if (state.HasState(FileStates.DeletedFromWorkdir) || state.HasState(FileStates.ModifiedInWorkdir) ||
					state.HasState(FileStates.NewInWorkdir) || state.HasState(FileStates.RenamedInWorkdir) || state.HasState(FileStates.TypeChangeInWorkdir))
				{
					return true;
				}
			}

			return false;
		}

		public static object GetQuickViewData(FileState fileState)
		{
			try
			{
				// check if file still exists
				string fullPath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
				if (!File.Exists(fullPath))
				{
					return "<< File Doesn't Exist >>";
				}

				// check if binary file
				if (Tools.IsBinaryFileData(fullPath)) return "<< Binary File >>";

				// check for text types diffs
				if (fileState.HasState(FileStates.ModifiedInWorkdir) || fileState.HasState(FileStates.ModifiedInIndex))
				{
					string diff;
					Debug.pauseGitCommanderStdWrites = true;
					if (!Repository.GetDiff(fileState.filename, out diff)) throw new Exception(Repository.lastError);
					Debug.pauseGitCommanderStdWrites = false;

					// remove meta data stage 1
					var match = Regex.Match(diff, @"@@.*?(@@).*?\n(.*)", RegexOptions.Singleline);
					if (match.Success && match.Groups.Count == 3)
					{
						diff = match.Groups[2].Value.Replace("\\ No newline at end of file" + Environment.NewLine, "");
						diff = diff.Replace("\\ No newline at end of file", "");
					}

					// remove meta data stage 2
					bool search = true;
					while (search)
					{
						match = Regex.Match(diff, @"(@@.*?(@@).*?\n)", RegexOptions.Singleline);
						if (match.Success && match.Groups.Count == 3)
						{
							diff = diff.Replace(match.Groups[1].Value, Environment.NewLine + "<<< ----------- SECTION ----------- >>>" + Environment.NewLine);
						}
						else
						{
							search = false;
						}
					}

					return diff;
				}

				// return text
				else if (!fileState.HasState(FileStates.Unreadable))
				{
					return File.ReadAllText(fullPath);
				}
				else
				{
					Debug.LogError("Unsuported FileStatus: " + fileState.filename, true);
					return null;
				}
			}
			catch (Exception e)
			{
				Debug.pauseGitCommanderStdWrites = false;
				Debug.LogError("Failed to refresh quick view: " + e.Message, true);
				return null;
			}
		}

		public static bool DeleteUntrackedUnstagedFile(FileState fileState, bool refresh)
		{
			bool success = true;
			try
			{
				if (!fileState.HasState(FileStates.NewInWorkdir)) return false;
				string filePath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
				if (File.Exists(filePath)) File.Delete(filePath);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to delete item: " + e.Message, true);
				success = false;
			}

			if (refresh) RepoManager.Refresh();
			return success;
		}

		public static bool DeleteUntrackedUnstagedFiles(bool refresh)
		{
			bool success = true;
			try
			{
				foreach (var fileState in fileStates)
				{
					if (!fileState.HasState(FileStates.NewInWorkdir)) continue;
					string filePath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
					if (File.Exists(filePath)) File.Delete(filePath);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to delete item: " + e.Message, true);
				success = false;
			}

			if (refresh) RepoManager.Refresh();
			return success;
		}

		public static bool StageFile(FileState fileState)
		{
			bool success = true;
			try
			{
				if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to stage item: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool StageAllFiles()
		{
			bool success = true;
			try
			{
				if (!Repository.StageAll()) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to stage item: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool UnstageFile(FileState fileState)
		{
			bool success = true;
			try
			{
				if (!Repository.Unstage(fileState.filename)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to unstage item: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool UnstageAllFiles()
		{
			bool success = true;
			try
			{
				if (!Repository.UnstageAll()) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to unstage item: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool RevertAll()
		{
			bool success = true;
			try
			{
				if (!Repository.RevertAllChanges()) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to reset: " + e.Message);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool RevertFile(FileState fileState)
		{
			if (!fileState.HasState(FileStates.ModifiedInIndex) && !fileState.HasState(FileStates.ModifiedInWorkdir) &&
				!fileState.HasState(FileStates.DeletedFromIndex) && !fileState.HasState(FileStates.DeletedFromWorkdir))
			{
				Debug.LogError("This file is not modified or deleted", true);
				return false;
			}

			bool success = true;
			try
			{
				if (!Repository.RevertFile(BranchManager.activeBranch.fullname, fileState.filename)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to reset file: " + e.Message);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool CommitStagedChanges(string commitMessage)
		{
			bool success = true;
			try
			{
				if (!Repository.Commit(commitMessage)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to commit: " + e.Message, true);
				success = false;
			}

			RepoManager.Refresh();
			return success;
		}

		public static bool Fetch()
		{
			try
			{
				if (BranchManager.activeBranch.isTracking)
				{
					if (!Repository.Fetch()) throw new Exception(Repository.lastError);
				}
				else if (BranchManager.activeBranch.isRemote)
				{
					if (!Repository.Fetch(BranchManager.activeBranch.remoteState.name, BranchManager.activeBranch.name)) throw new Exception(Repository.lastError);
				}
				else
				{
					Debug.LogError("Cannot fetch local only branch");
					return false;
				}

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("Fetch error: " + e.Message, true);
				return false;
			}
		}

		public static bool Fetch(BranchState branch)
		{
			try
			{
				if (branch.fullname == BranchManager.activeBranch.fullname)
				{
					if (!Repository.Fetch()) throw new Exception(Repository.lastError);
				}
				else if (branch.isRemote)
				{
					if (!Repository.Fetch(branch.remoteState.name, branch.name)) throw new Exception(Repository.lastError);
				}
				else
				{
					Debug.LogError("Cannot fetch local only branch");
					return false;
				}

				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("Fetch error: " + e.Message, true);
				return false;
			}
		}

		public static SyncMergeResults Pull()
		{
			var result = SyncMergeResults.Error;

			try
			{
				if (!BranchManager.activeBranch.isTracking)
				{
					Debug.LogWarning("Branch is not tracking a remote!", true);
					return SyncMergeResults.Error;
				}
				
				// pull changes
				result = Repository.Pull() ? SyncMergeResults.Succeeded : SyncMergeResults.Error;
				result = ConflictsExist() ? SyncMergeResults.Conflicts : result;
				
				if (result == SyncMergeResults.Conflicts) Debug.LogWarning("Merge failed, conflicts exist (please resolve)", true);
				else if (result == SyncMergeResults.Succeeded) Debug.Log("Pull Succeeded!", !isSyncMode);
				else Debug.Log("Pull Error!", !isSyncMode);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to pull: " + e.Message, true);
				result = SyncMergeResults.Error;
			}
			
			if (!isSyncMode) RepoManager.Refresh();
			return result;
		}

		public static bool Push()
		{
			bool success = true;
			try
			{
				if (!BranchManager.activeBranch.isTracking)
				{
					Debug.LogWarning("Branch is not tracking a remote!", true);
					return false;
				}
				
				if (Repository.Push()) Debug.Log("Push Succeeded!", !isSyncMode);
				else throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to push: " + e.Message, true);
				success = false;
			}
			
			if (!isSyncMode) RepoManager.Refresh();
			return success;
		}

		public static SyncMergeResults Sync()
		{
			isSyncMode = true;
			var result = Pull();
			bool pushPass = false;
			if (result == SyncMergeResults.Succeeded) pushPass = Push();
			isSyncMode = false;
			
			if (result != SyncMergeResults.Succeeded || !pushPass) Debug.LogError("Failed to Sync changes", true);
			else Debug.Log("Sync succeeded!", true);
			
			RepoManager.Refresh();
			return result;
		}

		public static bool ConflictsExist()
		{
			try
			{
				bool yes;
				if (!Repository.ConflitedExist(out yes)) throw new Exception(Repository.lastError);
				return yes;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to get file conflicts: " + e.Message, true);
				return false;
			}
		}

		public static bool ChangesExist()
		{
			return fileStates.Length != 0;
		}

		public static bool CompletedMergeCommitPending()
		{
			try
			{
				bool yes;
				if (!Repository.CompletedMergeCommitPending(out yes)) throw new Exception(Repository.lastError);
				return yes;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to check for pending merge commit: " + e.Message, true);
				return false;
			}
		}

		public static bool ResolveAllConflicts(bool refresh = true)
		{
			foreach (var fileState in fileStates)
			{
				if (fileState.HasState(FileStates.Conflicted) && !ResolveConflict(fileState, false))
				{
					Debug.LogError("Resolve conflict failed (aborting pending)", true);
					if (refresh) RepoManager.Refresh();
					return false;
				}
			}

			if (refresh) RepoManager.Refresh();
			return true;
		}

		private delegate void DeleteTempMergeFilesMethod();
		public static bool ResolveConflict(FileState fileState, bool refresh)
		{
			bool wasModified = false, success = true;
			string fullPath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
			string fullPathBase = fullPath+".base", fullPathOurs = null, fullPathTheirs = null;
			var DeleteTempMergeFiles = new DeleteTempMergeFilesMethod(delegate ()
			{
				if (File.Exists(fullPathBase)) File.Delete(fullPathBase);
				if (File.Exists(fullPathOurs)) File.Delete(fullPathOurs);
				if (File.Exists(fullPathTheirs)) File.Delete(fullPathTheirs);
			});

			try
			{
				// make sure file needs to be resolved
				if (!fileState.HasState(FileStates.Conflicted))
				{
					Debug.LogError("File not in conflicted state: " + fileState.filename, true);
					return false;
				}
				
				// save local temp files
				if (fileState.conflictType == FileConflictTypes.DeletedByBoth)
				{
					Debug.Log("Auto resolving file that was deleted by both branches: " + fileState.filename, true);
					if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
					goto FINISH;
				}
				else
				{
					Debug.pauseGitCommanderStdWrites = true;
					if (fileState.conflictType != FileConflictTypes.DeletedByUs)
					{
						bool fileCreated = Repository.SaveConflictedFile(fileState.filename, FileConflictSources.Ours, out fullPathOurs);
						fullPathOurs = Repository.repoPath + Path.DirectorySeparatorChar + fullPathOurs;
						if (!fileCreated) throw new Exception(Repository.lastError);
					}

					if (fileState.conflictType != FileConflictTypes.DeletedByThem)
					{
						bool fileCreated = Repository.SaveConflictedFile(fileState.filename, FileConflictSources.Theirs, out fullPathTheirs);
						fullPathTheirs = Repository.repoPath + Path.DirectorySeparatorChar + fullPathTheirs;
						if (!fileCreated) throw new Exception(Repository.lastError);
					}
					Debug.pauseGitCommanderStdWrites = false;
				}

				// check if files are binary (if so open select binary file tool) [if file conflict is because of deletion this method is also used]
				if (fileState.conflictType != FileConflictTypes.Changes || Tools.IsBinaryFileData(fullPathOurs) || Tools.IsBinaryFileData(fullPathTheirs))
				{
					// open merge tool
					MergeBinaryFileResults mergeBinaryResult;
					if (AskUserToResolveConflictedFileCallback != null && AskUserToResolveConflictedFileCallback(fileState, true, out mergeBinaryResult))
					{
						switch (mergeBinaryResult)
						{
							case MergeBinaryFileResults.Error: Debug.LogWarning("Error trying to resolve file: " + fileState.filename, true);
								goto FINISH;

							case MergeBinaryFileResults.Cancel:
								goto FINISH;

							case MergeBinaryFileResults.KeepMine:
								if (fileState.conflictType == FileConflictTypes.Changes)
								{
									if (!Repository.AcceptConflictedFile(fullPath, FileConflictSources.Ours)) throw new Exception(Repository.lastError);
								}
								else if (fileState.conflictType == FileConflictTypes.DeletedByThem)
								{
									File.Copy(fullPathOurs, fullPath, true);
									if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
								}
								else if (fileState.conflictType == FileConflictTypes.DeletedByUs)
								{
									if (!Repository.RemoveFile(fileState.filename)) throw new Exception(Repository.lastError);
								}
								break;

							case MergeBinaryFileResults.UseTheirs:
								if (fileState.conflictType == FileConflictTypes.Changes)
								{
									if (!Repository.AcceptConflictedFile(fullPath, FileConflictSources.Theirs)) throw new Exception(Repository.lastError);
								}
								else if (fileState.conflictType == FileConflictTypes.DeletedByThem)
								{
									if (!Repository.RemoveFile(fileState.filename)) throw new Exception(Repository.lastError);
								}
								else if (fileState.conflictType == FileConflictTypes.DeletedByUs)
								{
									File.Copy(fullPathTheirs, fullPath, true);
									if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
								}
								break;

							default: Debug.LogWarning("Unsuported Response: " + mergeBinaryResult, true); goto FINISH;
						}
					}
					else
					{
						Debug.LogError("Failed to resolve file: " + fileState.filename, true);
						goto FINISH;
					}

					// finish
					goto FINISH;
				}
			
				// copy base and parse
				File.Copy(fullPath, fullPathBase, true);
				string baseFile = File.ReadAllText(fullPath);
				var match = Regex.Match(baseFile, @"(<<<<<<<\s*\w*[\r\n]*).*(=======[\r\n]*).*(>>>>>>>\s*\w*[\r\n]*)", RegexOptions.Singleline);
				if (match.Success && match.Groups.Count == 4)
				{
					baseFile = baseFile.Replace(match.Groups[1].Value, "").Replace(match.Groups[2].Value, "").Replace(match.Groups[3].Value, "");
					File.WriteAllText(fullPathBase, baseFile);
				}

				// hash base file
				byte[] baseHash = null;
				using (var md5 = MD5.Create())
				{
					using (var stream = File.OpenRead(fullPathBase))
					{
						baseHash = md5.ComputeHash(stream);
					}
				}

				// start external merge tool
				MergeBinaryFileResults mergeFileResult;
				if (AskUserToResolveConflictedFileCallback != null && AskUserToResolveConflictedFileCallback(fileState, false, out mergeFileResult))
				{
					switch (mergeFileResult)
					{
						case MergeBinaryFileResults.Error: Debug.LogWarning("Error trying to resolve file: " + fileState.filename, true);
							goto FINISH;

						case MergeBinaryFileResults.Cancel:
							goto FINISH;

						case MergeBinaryFileResults.KeepMine:
							//File.Copy(fullPathOurs, fullPathBase, true);
							if (!Repository.AcceptConflictedFile(fullPath, FileConflictSources.Ours)) throw new Exception(Repository.lastError);
							goto FINISH;

						case MergeBinaryFileResults.UseTheirs:
							//File.Copy(fullPathTheirs, fullPathBase, true);
							if (!Repository.AcceptConflictedFile(fullPath, FileConflictSources.Theirs)) throw new Exception(Repository.lastError);
							goto FINISH;

						case MergeBinaryFileResults.RunMergeTool:
							using (var process = new Process())
							{
								process.StartInfo.FileName = AppManager.mergeToolPath;
								if (AppManager.mergeDiffTool == MergeDiffTools.Meld) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
								else if (AppManager.mergeDiffTool == MergeDiffTools.kDiff3) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
								else if (AppManager.mergeDiffTool == MergeDiffTools.P4Merge) process.StartInfo.Arguments = string.Format("\"{1}\" \"{0}\" \"{2}\" \"{1}\"", fullPathOurs, fullPathBase, fullPathTheirs);
								else if (AppManager.mergeDiffTool == MergeDiffTools.DiffMerge) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
								process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
								if (!process.Start())
								{
									Debug.LogError("Failed to start Merge tool (is it installed?)", true);
									goto FINISH;
								}

								process.WaitForExit();
							}
							break;

						default: Debug.LogWarning("Unsuported Response: " + mergeFileResult, true);
							goto FINISH;
					}
				}
				else
				{
					Debug.LogError("Failed to resolve file: " + fileState.filename, true);
					goto FINISH;
				}

				// get new base hash
				byte[] baseHashChange = null;
				using (var md5 = MD5.Create())
				{
					using (var stream = File.OpenRead(fullPathBase))
					{
						baseHashChange = md5.ComputeHash(stream);
					}
				}

				// check if file was modified
				if (!baseHashChange.SequenceEqual(baseHash))
				{
					wasModified = true;
					File.Copy(fullPathBase, fullPath, true);
					if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
				}

				// check if user accepts the current state of the merge
				if (!wasModified)
				{
					MergeFileAcceptedResults result;
					if (AskUserIfTheyAcceptMergedFileCallback != null && AskUserIfTheyAcceptMergedFileCallback(fileState, out result))
					{
						switch (result)
						{
							case MergeFileAcceptedResults.Yes:
								File.Copy(fullPathBase, fullPath, true);
								if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
								wasModified = true;
								break;

							case MergeFileAcceptedResults.No:
								break;

							default: Debug.LogWarning("Unsuported Response: " + result, true); goto FINISH;
						}
					}
					else
					{
						Debug.LogError("Failed to ask user if file was resolved: " + fileState.filename, true);
						goto FINISH;
					}
				}

				success = true;
			}
			catch (Exception e)
			{
				Debug.pauseGitCommanderStdWrites = false;
				Debug.LogError("Failed to resolve file: " + e.Message, true);
				DeleteTempMergeFiles();
				success = false;
			}
			
			// finish
			FINISH:;
			DeleteTempMergeFiles();
			if (refresh) RepoManager.Refresh();
			if (!success) return false;
			return wasModified;
		}

		private delegate void DeleteTempDiffFilesMethod();
		public static bool OpenDiffTool(FileState fileState)
		{
			string fullPath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
			string fullPathOrig = null;
			var DeleteTempDiffFiles = new DeleteTempDiffFilesMethod(delegate ()
			{
				if (File.Exists(fullPathOrig)) File.Delete(fullPathOrig);
			});

			try
			{
				// get selected item
				if (!fileState.HasState(FileStates.ModifiedInIndex) && !fileState.HasState(FileStates.ModifiedInWorkdir))
				{
					Debug.LogError("This file is not modified", true);
					return false;
				}

				// get info and save orig file
				Debug.pauseGitCommanderStdWrites = true;
				if (!Repository.SaveOriginalFile(fileState.filename, out fullPathOrig)) throw new Exception(Repository.lastError);
				Debug.pauseGitCommanderStdWrites = false;
				fullPathOrig = Repository.repoPath + Path.DirectorySeparatorChar + fullPathOrig;

				// open diff tool
				using (var process = new Process())
				{
					process.StartInfo.FileName = AppManager.mergeToolPath;
					process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", fullPathOrig, fullPath);
					process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
					if (!process.Start())
					{
						Debug.LogError("Failed to start Diff tool (is it installed?)", true);
						DeleteTempDiffFiles();
						return false;
					}

					process.WaitForExit();
				}
			}
			catch (Exception ex)
			{
				Debug.pauseGitCommanderStdWrites = false;
				Debug.LogError("Failed to start Diff tool: " + ex.Message, true);
				DeleteTempDiffFiles();
				return false;
			}

			// finish
			DeleteTempDiffFiles();
			return true;
		}
	}
}
