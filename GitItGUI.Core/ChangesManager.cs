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
				if (!Repository.GetFileStates(out var states)) throw new Exception(Repository.lastError);
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
				if (state.IsState(FileStates.DeletedFromIndex) || state.IsState(FileStates.ModifiedInIndex) ||
					state.IsState(FileStates.NewInIndex) || state.IsState(FileStates.RenamedInIndex) || state.IsState(FileStates.TypeChangeInIndex))
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
				if (state.IsState(FileStates.DeletedFromWorkdir) || state.IsState(FileStates.ModifiedInWorkdir) ||
					state.IsState(FileStates.NewInWorkdir) || state.IsState(FileStates.RenamedInWorkdir) || state.IsState(FileStates.TypeChangeInWorkdir))
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
				if (fileState.IsState(FileStates.ModifiedInWorkdir) || fileState.IsState(FileStates.ModifiedInIndex))
				{
					string diff;
					if (!Repository.GetDiff(fileState.filename, out diff)) throw new Exception(Repository.lastError);

					// remove meta data stage 1
					var match = Regex.Match(diff, @"@@.*?(@@).*?\n(.*)", RegexOptions.Singleline);
					if (match.Success && match.Groups.Count == 3) diff = match.Groups[2].Value.Replace("\\ No newline at end of file\n", "");

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
				else if (!fileState.IsState(FileStates.Unreadable))
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
				Debug.LogError("Failed to refresh quick view: " + e.Message, true);
				return null;
			}
		}

		public static bool DeleteUntrackedUnstagedFile(FileState fileState, bool refresh)
		{
			try
			{
				if (fileState.state != FileStates.NewInWorkdir) return false;
				string filePath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
				if (File.Exists(filePath)) File.Delete(filePath);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to delete item: " + e.Message, true);
				return false;
			}

			if (refresh) RepoManager.Refresh();
			return true;
		}

		public static bool DeleteUntrackedUnstagedFiles(bool refresh)
		{
			try
			{
				foreach (var fileState in fileStates)
				{
					if (!fileState.IsState(FileStates.NewInWorkdir)) continue;
					string filePath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
					if (File.Exists(filePath)) File.Delete(filePath);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to delete item: " + e.Message, true);
				return false;
			}

			if (refresh) RepoManager.Refresh();
			return true;
		}

		public static bool StageFile(FileState fileState, bool refresh)
		{
			try
			{
				if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to stage item: " + e.Message, true);
				return false;
			}

			if (refresh) RepoManager.Refresh();
			return true;
		}

		public static bool UnstageFile(FileState fileState, bool refresh)
		{
			try
			{
				if (!Repository.Unstage(fileState.filename)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to unstage item: " + e.Message, true);
				return false;
			}

			if (refresh) RepoManager.Refresh();
			return true;
		}

		public static bool RevertAll()
		{
			try
			{
				if (!Repository.RevertAllChanges()) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to reset: " + e.Message);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool RevertFile(FileState fileState)
		{
			if (fileState.state != FileStates.ModifiedInIndex && fileState.state != FileStates.ModifiedInWorkdir &&
				fileState.state != FileStates.DeletedFromIndex && fileState.state != FileStates.DeletedFromWorkdir)
			{
				Debug.LogError("This file is not modified or deleted", true);
				return false;
			}

			try
			{
				if (!Repository.RevertFile(BranchManager.activeBranch.fullname, fileState.filename)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to reset file: " + e.Message);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool CommitStagedChanges(string commitMessage)
		{
			try
			{
				if (!Repository.Commit(commitMessage)) throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to commit: " + e.Message);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static SyncMergeResults Pull(StatusUpdateCallbackMethod statusCallback)
		{
			var result = SyncMergeResults.Error;

			try
			{
				if (!BranchManager.activeBranch.isTracking)
				{
					Debug.LogWarning("Branch is not tracking a remote!", true);
					return SyncMergeResults.Error;
				}

				// check for git settings file not in repo history
				RepoManager.DeleteRepoSettingsIfUnCommit();
				
				// pull changes
				void stdCallback(string line)
				{
					if (statusCallback != null) statusCallback(line);
				}

				void stdErrorCallback(string line)
				{
					if (statusCallback != null) statusCallback(line);
				}

				result = Repository.Pull(stdCallback, stdErrorCallback) ? SyncMergeResults.Succeeded : SyncMergeResults.Error;
				result = ConflictsExist() ? SyncMergeResults.Conflicts : result;

				if (result == SyncMergeResults.Conflicts) Debug.LogWarning("Merge failed, conflicts exist (please resolve)", true);
				else if (result == SyncMergeResults.Succeeded) Debug.Log("Pull Succeeded!", !isSyncMode);
				else Debug.Log("Pull Error!", !isSyncMode);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to pull: " + e.Message, true);
				return SyncMergeResults.Error;
			}
			
			if (!isSyncMode) RepoManager.Refresh();
			return result;
		}

		public static bool Push(StatusUpdateCallbackMethod statusCallback)
		{
			try
			{
				if (!BranchManager.activeBranch.isTracking)
				{
					Debug.LogWarning("Branch is not tracking a remote!", true);
					return false;
				}

				void stdCallback(string line)
				{
					if (statusCallback != null) statusCallback(line);
				}

				if (Repository.Push(stdCallback, stdCallback)) Debug.Log("Push Succeeded!", !isSyncMode);
				else throw new Exception(Repository.lastError);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to push: " + e.Message, true);
				return false;
			}
			
			if (!isSyncMode) RepoManager.Refresh();
			return true;
		}

		public static SyncMergeResults Sync(StatusUpdateCallbackMethod statusCallback)
		{
			if (statusCallback != null) statusCallback("Syncing Started...");
			isSyncMode = true;
			var result = Pull(statusCallback);
			bool pushPass = false;
			if (result == SyncMergeResults.Succeeded) pushPass = Push(statusCallback);
			isSyncMode = false;
			
			if (result != SyncMergeResults.Succeeded || !pushPass)
			{
				Debug.LogError("Failed to Sync changes", true);
				return result;
			}
			else
			{
				Debug.Log("Sync succeeded!", true);
			}
			
			RepoManager.Refresh();
			return result;
		}

		public static bool ConflictsExist()
		{
			try
			{
				if (!Repository.ConflitedExist(out bool yes)) throw new Exception(Repository.lastError);
				return yes;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to get file conflicts: " + e.Message, true);
				return false;
			}
		}

		public static bool ResolveAllConflicts(bool refresh = true)
		{
			foreach (var fileState in fileStates)
			{
				if (fileState.IsState(FileStates.Conflicted) && !ResolveConflict(fileState, false))
				{
					Debug.LogError("Resolve conflict failed (aborting pending)", true);
					if (refresh) RepoManager.Refresh();
					return false;
				}
			}

			if (refresh) RepoManager.Refresh();
			return true;
		}
		
		public static bool ResolveConflict(FileState fileState, bool refresh)
		{
			bool wasModified = false;
			string fullPath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
			string fullPathBase = fullPath+".base", fullPathOurs = null, fullPathTheirs = null;
			void DeleteTempMergeFiles()
			{
				if (File.Exists(fullPathBase)) File.Delete(fullPathBase);
				if (File.Exists(fullPathOurs)) File.Delete(fullPathOurs);
				if (File.Exists(fullPathTheirs)) File.Delete(fullPathTheirs);
			}

			try
			{
				// make sure file needs to be resolved
				if (fileState.state != FileStates.Conflicted)
				{
					Debug.LogError("File not in conflicted state: " + fileState.filename, true);
					return false;
				}

				// save local temp files
				if (!Repository.SaveConflictedFile(fileState.filename, FileConflictSources.Ours, out fullPathOurs)) throw new Exception(Repository.lastError);
				if (!Repository.SaveConflictedFile(fileState.filename, FileConflictSources.Theirs, out fullPathTheirs)) throw new Exception(Repository.lastError);
				fullPathOurs = Repository.repoPath + Path.DirectorySeparatorChar + fullPathOurs;
				fullPathTheirs = Repository.repoPath + Path.DirectorySeparatorChar + fullPathTheirs;

				// check if files are binary (if so open select binary file tool)
				if (Tools.IsBinaryFileData(fullPathOurs) || Tools.IsBinaryFileData(fullPathTheirs))
				{
					// open merge tool
					MergeBinaryFileResults mergeBinaryResult;
					if (AskUserToResolveConflictedFileCallback != null && AskUserToResolveConflictedFileCallback(fileState, true, out mergeBinaryResult))
					{
						switch (mergeBinaryResult)
						{
							case MergeBinaryFileResults.Error: Debug.LogWarning("Error trying to resolve file: " + fileState.filename, true);
								DeleteTempMergeFiles();
								return false;

							case MergeBinaryFileResults.Cancel:
								DeleteTempMergeFiles();
								return false;

							case MergeBinaryFileResults.KeepMine: File.Copy(fullPathOurs, fullPath, true); break;
							case MergeBinaryFileResults.UseTheirs: File.Copy(fullPathTheirs, fullPath, true); break;
							default: Debug.LogWarning("Unsuported Response: " + mergeBinaryResult, true);
								DeleteTempMergeFiles();
								return false;
						}
					}
					else
					{
						Debug.LogError("Failed to resolve file: " + fileState.filename, true);
						return false;
					}

					// delete temp files
					DeleteTempMergeFiles();

					// stage and finish
					if (!Repository.Stage(fileState.filename)) throw new Exception(Repository.lastError);
					if (refresh) RepoManager.Refresh();
					return true;
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
							DeleteTempMergeFiles();
							return false;

						case MergeBinaryFileResults.Cancel:
							DeleteTempMergeFiles();
							return false;

						case MergeBinaryFileResults.KeepMine: File.Copy(fullPathOurs, fullPathBase, true); break;
						case MergeBinaryFileResults.UseTheirs: File.Copy(fullPathTheirs, fullPathBase, true); break;

						case MergeBinaryFileResults.RunMergeTool:
							using (var process = new Process())
							{
								process.StartInfo.FileName = AppManager.mergeToolPath;
								if (AppManager.mergeDiffTool == MergeDiffTools.Meld) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
								else if (AppManager.mergeDiffTool == MergeDiffTools.kDiff3) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
								else if (AppManager.mergeDiffTool == MergeDiffTools.P4Merge) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\" \"{0}\"", fullPathBase, fullPathOurs, fullPathTheirs);
								else if (AppManager.mergeDiffTool == MergeDiffTools.DiffMerge) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
								process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
								if (!process.Start())
								{
									Debug.LogError("Failed to start Merge tool (is it installed?)", true);
									DeleteTempMergeFiles();
									return false;
								}

								process.WaitForExit();
							}
							break;

						default: Debug.LogWarning("Unsuported Response: " + mergeFileResult, true);
							DeleteTempMergeFiles();
							return false;
					}
				}
				else
				{
					Debug.LogError("Failed to resolve file: " + fileState.filename, true);
					DeleteTempMergeFiles();
					return false;
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

							default: Debug.LogWarning("Unsuported Response: " + result, true); return false;
						}
					}
					else
					{
						Debug.LogError("Failed to ask user if file was resolved: " + fileState.filename, true);
						DeleteTempMergeFiles();
						return false;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to resolve file: " + e.Message, true);
				DeleteTempMergeFiles();
				return false;
			}
			
			// finish
			DeleteTempMergeFiles();
			if (refresh) RepoManager.Refresh();
			return wasModified;
		}

		public static bool OpenDiffTool(FileState fileState)
		{
			string fullPath = Repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
			string fullPathOrig = null;
			void DeleteTempDiffFiles()
			{
				if (File.Exists(fullPathOrig)) File.Delete(fullPathOrig);
			}

			try
			{
				// get selected item
				if (fileState.state != FileStates.ModifiedInIndex && fileState.state != FileStates.ModifiedInWorkdir)
				{
					Debug.LogError("This file is not modified", true);
					return false;
				}

				// get info and save orig file
				if (!Repository.SaveOriginalFile(fileState.filename, out fullPathOrig)) throw new Exception(Repository.lastError);
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
