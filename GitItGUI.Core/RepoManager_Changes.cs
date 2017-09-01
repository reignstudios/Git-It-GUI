using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GitCommander;
using System.Collections.Generic;

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

	public partial class RepoManager
	{
		public delegate bool AskUserToResolveConflictedFileCallbackMethod(FileState fileState, bool isBinaryFile, out MergeBinaryFileResults result);
		public event AskUserToResolveConflictedFileCallbackMethod AskUserToResolveConflictedFileCallback;

		public delegate bool AskUserIfTheyAcceptMergedFileCallbackMethod(FileState fileState, out MergeFileAcceptedResults result);
		public event AskUserIfTheyAcceptMergedFileCallbackMethod AskUserIfTheyAcceptMergedFileCallback;

		private FileState[] fileStates;
		private bool isSyncMode;

		private bool RefreshChanges()
		{
			try
			{
				FileState[] states;
				if (!repository.GetFileStates(out states)) throw new Exception(repository.lastError);
				fileStates = states;
				return true;
			}
			catch (Exception e)
			{
				DebugLog.LogError("ChangesManager.Refresh Failed: " + e.Message, true);
				return false;
			}
		}

		public FileState[] GetFileStates()
		{
			lock (this)
			{
				var copy = new FileState[fileStates.Length];
				Array.Copy(fileStates, copy, fileStates.Length);
				return copy;
			}
		}

		public bool FilesAreStaged()
		{
			lock (this)
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
		}

		public bool FilesAreUnstaged()
		{
			lock (this)
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
		}

		public object GetQuickViewData(FileState fileState)
		{
			lock (this)
			{
				try
				{
					// check if file still exists
					string fullPath = repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
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
						pauseGitCommanderStdWrites = true;
						if (!repository.GetDiff(fileState.filename, out diff)) throw new Exception(repository.lastError);
						pauseGitCommanderStdWrites = false;

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
								diff = diff.Replace(match.Groups[1].Value, Environment.NewLine + "*<<< ----------- SECTION ----------- >>>" + Environment.NewLine);
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
						DebugLog.LogError("Unsuported FileStatus: " + fileState.filename, true);
						return null;
					}
				}
				catch (Exception e)
				{
					pauseGitCommanderStdWrites = false;
					DebugLog.LogError("Failed to refresh quick view: " + e.Message, true);
					return null;
				}
			}
		}

		public bool DeleteUntrackedUnstagedFile(FileState fileState, bool refresh)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!fileState.HasState(FileStates.NewInWorkdir)) return false;
					string filePath = repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
					if (File.Exists(filePath)) File.Delete(filePath);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to delete item: " + e.Message, true);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool DeleteUntrackedUnstagedFiles(List<FileState> fileStates, bool refresh)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					foreach (var fileState in fileStates)
					{
						if (!fileState.HasState(FileStates.NewInWorkdir))
						{
							DebugLog.LogWarning("This file is tracked (skipping): " + fileState.filename);
							continue;
						}

						string filePath = repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
						if (File.Exists(filePath)) File.Delete(filePath);
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to delete item: " + e.Message, true);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool DeleteUntrackedUnstagedFiles(bool refresh)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					foreach (var fileState in fileStates)
					{
						if (!fileState.HasState(FileStates.NewInWorkdir)) continue;
						string filePath = repository.repoPath + Path.DirectorySeparatorChar + fileState.filename;
						if (File.Exists(filePath)) File.Delete(filePath);
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to delete item: " + e.Message, true);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool StageFile(FileState fileState)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to stage item: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool StageFileList(List<FileState> fileStates)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					foreach (var fileState in fileStates)
					{
						if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to stage items: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool StageAllFiles()
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!repository.StageAll()) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to stage item: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool UnstageFile(FileState fileState)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!repository.Unstage(fileState.filename)) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to unstage item: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool UnstageFileList(List<FileState> fileStates)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					foreach (var fileState in fileStates)
					{
						if (!repository.Unstage(fileState.filename)) throw new Exception(repository.lastError);
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to unstage items: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool UnstageAllFiles()
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!repository.UnstageAll()) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to unstage item: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool RevertAll()
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!repository.RevertAllChanges()) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to reset: " + e.Message);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool RevertFile(FileState fileState)
		{
			lock (this)
			{
				if (!fileState.HasState(FileStates.ModifiedInIndex) && !fileState.HasState(FileStates.ModifiedInWorkdir) &&
				!fileState.HasState(FileStates.DeletedFromIndex) && !fileState.HasState(FileStates.DeletedFromWorkdir))
				{
					DebugLog.LogError("This file is not modified or deleted: " + fileState.filename, true);
					return false;
				}

				bool success = true;
				try
				{
					if (!repository.RevertFile(activeBranch.fullname, fileState.filename)) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to reset file: " + e.Message);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool RevertFileList(List<FileState> fileStates)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					foreach (var fileState in fileStates)
					{
						if (!fileState.HasState(FileStates.ModifiedInIndex) && !fileState.HasState(FileStates.ModifiedInWorkdir) &&
						!fileState.HasState(FileStates.DeletedFromIndex) && !fileState.HasState(FileStates.DeletedFromWorkdir))
						{
							DebugLog.LogWarning("This file is not modified or deleted (skipping): " + fileState.filename);
							continue;
						}

						if (!repository.RevertFile(activeBranch.fullname, fileState.filename)) throw new Exception(repository.lastError);
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to reset file: " + e.Message);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool CommitStagedChanges(string commitMessage)
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!repository.Commit(commitMessage)) throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to commit: " + e.Message, true);
					success = false;
				}

				Refresh();
				return success;
			}
		}

		public bool Fetch()
		{
			lock (this)
			{
				try
				{
					if (activeBranch.isTracking)
					{
						if (!repository.Fetch()) throw new Exception(repository.lastError);
					}
					else if (activeBranch.isRemote)
					{
						if (!repository.Fetch(activeBranch.remoteState.name, activeBranch.name)) throw new Exception(repository.lastError);
					}
					else
					{
						DebugLog.LogError("Cannot fetch local only branch");
						return false;
					}

					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Fetch error: " + e.Message, true);
					return false;
				}
			}
		}

		public bool Fetch(BranchState branch)
		{
			lock (this)
			{
				try
				{
					if (branch.fullname == activeBranch.fullname)
					{
						if (!repository.Fetch()) throw new Exception(repository.lastError);
					}
					else if (branch.isRemote)
					{
						if (!repository.Fetch(branch.remoteState.name, branch.name)) throw new Exception(repository.lastError);
					}
					else
					{
						DebugLog.LogError("Cannot fetch local only branch");
						return false;
					}

					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Fetch error: " + e.Message, true);
					return false;
				}
			}
		}

		public SyncMergeResults Pull()
		{
			lock (this)
			{
				var result = SyncMergeResults.Error;

				try
				{
					if (!activeBranch.isTracking)
					{
						DebugLog.LogWarning("Branch is not tracking a remote!", true);
						return SyncMergeResults.Error;
					}
				
					// pull changes
					result = repository.Pull() ? SyncMergeResults.Succeeded : SyncMergeResults.Error;
					result = ConflictsExist() ? SyncMergeResults.Conflicts : result;
				
					if (result == SyncMergeResults.Conflicts) DebugLog.LogWarning("Merge failed, conflicts exist (please resolve)", true);
					else if (result == SyncMergeResults.Succeeded) DebugLog.Log("Pull Succeeded!", !isSyncMode);
					else DebugLog.Log("Pull Error!", !isSyncMode);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to pull: " + e.Message, true);
					result = SyncMergeResults.Error;
				}
			
				if (!isSyncMode) Refresh();
				return result;
			}
		}

		public bool Push()
		{
			lock (this)
			{
				bool success = true;
				try
				{
					if (!activeBranch.isTracking)
					{
						DebugLog.LogWarning("Branch is not tracking a remote!", true);
						return false;
					}
				
					if (repository.Push()) DebugLog.Log("Push Succeeded!", !isSyncMode);
					else throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to push: " + e.Message, true);
					success = false;
				}
			
				if (!isSyncMode) Refresh();
				return success;
			}
		}

		public SyncMergeResults Sync()
		{
			lock (this)
			{
				isSyncMode = true;
				var result = Pull();
				bool pushPass = false;
				if (result == SyncMergeResults.Succeeded) pushPass = Push();
				isSyncMode = false;
			
				if (result != SyncMergeResults.Succeeded || !pushPass) DebugLog.LogError("Failed to Sync changes", true);
				else DebugLog.Log("Sync succeeded!", true);
			
				Refresh();
				return result;
			}
		}

		public bool ConflictsExist()
		{
			lock (this)
			{
				try
				{
					bool yes;
					if (!repository.ConflitedExist(out yes)) throw new Exception(repository.lastError);
					return yes;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to get file conflicts: " + e.Message, true);
					return false;
				}
			}
		}

		public bool ChangesExist()
		{
			lock (this)
			{
				return fileStates.Length != 0;
			}
		}

		public bool CompletedMergeCommitPending()
		{
			lock (this)
			{
				try
				{
					bool yes;
					if (!repository.CompletedMergeCommitPending(out yes)) throw new Exception(repository.lastError);
					return yes;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to check for pending merge commit: " + e.Message, true);
					return false;
				}
			}
		}

		public bool ResolveAllConflicts(bool refresh = true)
		{
			lock (this)
			{
				foreach (var fileState in fileStates)
				{
					if (fileState.HasState(FileStates.Conflicted) && !ResolveConflict(fileState, false))
					{
						DebugLog.LogError("Resolve conflict failed (aborting pending)", true);
						if (refresh) Refresh();
						return false;
					}
				}

				if (refresh) Refresh();
				return true;
			}
		}
		
		public bool ResolveConflict(FileState fileState, bool refresh)
		{
			lock (this)
			{
				bool success = true;
				string fullPath = repository.repoPath + Path.DirectorySeparatorChar + fileState.filename.Replace('/', Path.DirectorySeparatorChar);
				string fullPathBase = fullPath+".base", fullPathOurs = null, fullPathTheirs = null;
				void DeleteTempMergeFiles()
				{
					if (File.Exists(fullPathBase)) File.Delete(fullPathBase);
					if (File.Exists(fullPathOurs)) File.Delete(fullPathOurs);
					if (File.Exists(fullPathTheirs)) File.Delete(fullPathTheirs);
				}

				try
				{
					// make sure file is in a normal state
					File.SetAttributes(fullPath, FileAttributes.Normal);

					// make sure file needs to be resolved
					if (!fileState.HasState(FileStates.Conflicted))
					{
						DebugLog.LogError("File not in conflicted state: " + fileState.filename, true);
						return false;
					}
				
					// save local temp files
					if (fileState.conflictType == FileConflictTypes.DeletedByBoth)
					{
						DebugLog.Log("Auto resolving file that was deleted by both branches: " + fileState.filename, true);
						if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
						goto FINISH;
					}
					else
					{
						pauseGitCommanderStdWrites = true;
						if (fileState.conflictType != FileConflictTypes.DeletedByUs)
						{
							bool fileCreated = repository.SaveConflictedFile(fileState.filename, FileConflictSources.Ours, out fullPathOurs);
							fullPathOurs = repository.repoPath + Path.DirectorySeparatorChar + fullPathOurs.Replace('/', Path.DirectorySeparatorChar);
							if (!fileCreated) throw new Exception(repository.lastError);
						}

						if (fileState.conflictType != FileConflictTypes.DeletedByThem)
						{
							bool fileCreated = repository.SaveConflictedFile(fileState.filename, FileConflictSources.Theirs, out fullPathTheirs);
							fullPathTheirs = repository.repoPath + Path.DirectorySeparatorChar + fullPathTheirs.Replace('/', Path.DirectorySeparatorChar);
							if (!fileCreated) throw new Exception(repository.lastError);
						}
						pauseGitCommanderStdWrites = false;
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
								case MergeBinaryFileResults.Error:
									throw new Exception("Error trying to resolve file: " + fileState.filename);

								case MergeBinaryFileResults.Cancel:
									success = false;
									goto FINISH;

								case MergeBinaryFileResults.KeepMine:
									if (fileState.conflictType == FileConflictTypes.Changes)
									{
										if (!repository.CheckoutConflictedFile(fileState.filename, FileConflictSources.Ours)) throw new Exception(repository.lastError);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByThem)
									{
										File.Copy(fullPathOurs, fullPath, true);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByUs)
									{
										if (!repository.RemoveFile(fileState.filename)) throw new Exception(repository.lastError);
									}
									break;

								case MergeBinaryFileResults.UseTheirs:
									if (fileState.conflictType == FileConflictTypes.Changes)
									{
										if (!repository.CheckoutConflictedFile(fileState.filename, FileConflictSources.Theirs)) throw new Exception(repository.lastError);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByThem)
									{
										if (!repository.RemoveFile(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByUs)
									{
										File.Copy(fullPathTheirs, fullPath, true);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									}
									break;

								default: throw new Exception("Unsuported Response: " + mergeBinaryResult);
							}
						}
						else
						{
							throw new Exception("Failed to resolve file: " + fileState.filename);
						}

						// finish
						goto FINISH;
					}
			
					// copy base and parse
					string baseFile = File.ReadAllText(fullPath);
					var match = Regex.Match(baseFile, @"(<<<<<<<\s*\w*[\r\n]*).*(=======[\r\n]*).*(>>>>>>>\s*\w*[\r\n]*)", RegexOptions.Singleline);
					if (match.Success && match.Groups.Count == 4)
					{
						baseFile = baseFile.Replace(match.Groups[1].Value, "").Replace(match.Groups[2].Value, "").Replace(match.Groups[3].Value, "");
						File.WriteAllText(fullPathBase, baseFile);
					}
					else
					{
						File.Copy(fullPath, fullPathBase, true);
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
							case MergeBinaryFileResults.Error:
								throw new Exception("Error trying to resolve file: " + fileState.filename);

							case MergeBinaryFileResults.Cancel:
								success = false;
								goto FINISH;

							case MergeBinaryFileResults.KeepMine:
								File.Copy(fullPathOurs, fullPathBase, true);
								break;

							case MergeBinaryFileResults.UseTheirs:
								File.Copy(fullPathTheirs, fullPathBase, true);
								break;

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
										throw new Exception("Failed to start Merge tool (is it installed?)");
									}

									process.WaitForExit();
								}
								break;

							default: throw new Exception("Unsuported Response: " + mergeFileResult);
						}
					}
					else
					{
						throw new Exception("Failed to resolve file: " + fileState.filename);
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
					bool wasModified = false;
					if (!baseHashChange.SequenceEqual(baseHash))
					{
						wasModified = true;
						File.Copy(fullPathBase, fullPath, true);
						if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
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
									if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									wasModified = true;
									break;

								case MergeFileAcceptedResults.No:
									success = false;
									break;

								default: throw new Exception("Unsuported Response: " + result);
							}
						}
						else
						{
							throw new Exception("Failed to ask user if file was resolved: " + fileState.filename);
						}
					}
				}
				catch (Exception e)
				{
					pauseGitCommanderStdWrites = false;
					DebugLog.LogError("Failed to resolve file: " + e.Message, true);
					DeleteTempMergeFiles();
					success = false;
				}
			
				// finish
				FINISH:;
				DeleteTempMergeFiles();
				if (refresh) Refresh();
				return success;
			}
		}
		
		public bool OpenDiffTool(FileState fileState)
		{
			lock (this)
			{
				string fullPath = repository.repoPath + Path.DirectorySeparatorChar + fileState.filename.Replace('/', Path.DirectorySeparatorChar);
				string fullPathOrig = null;
				void DeleteTempDiffFiles()
				{
					if (File.Exists(fullPathOrig)) File.Delete(fullPathOrig);
				}

				try
				{
					// get selected item
					if (!fileState.HasState(FileStates.ModifiedInIndex) && !fileState.HasState(FileStates.ModifiedInWorkdir))
					{
						DebugLog.LogError("This file is not modified", true);
						return false;
					}

					// get info and save orig file
					pauseGitCommanderStdWrites = true;
					if (!repository.SaveOriginalFile(fileState.filename, out fullPathOrig)) throw new Exception(repository.lastError);
					pauseGitCommanderStdWrites = false;
					fullPathOrig = repository.repoPath + Path.DirectorySeparatorChar + fullPathOrig;

					// open diff tool
					using (var process = new Process())
					{
						process.StartInfo.FileName = AppManager.mergeToolPath;
						process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", fullPathOrig, fullPath);
						process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
						if (!process.Start())
						{
							DebugLog.LogError("Failed to start Diff tool (is it installed?)", true);
							DeleteTempDiffFiles();
							return false;
						}

						process.WaitForExit();
					}
				}
				catch (Exception ex)
				{
					pauseGitCommanderStdWrites = false;
					DebugLog.LogError("Failed to start Diff tool: " + ex.Message, true);
					DeleteTempDiffFiles();
					return false;
				}

				// finish
				DeleteTempDiffFiles();
				return true;
			}
		}
	}
}
