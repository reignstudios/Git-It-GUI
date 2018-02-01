using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GitCommander;
using System.Collections.Generic;
using System.Text;

namespace GitItGUI.Core
{
	public enum MergeFileResults
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

	public class PreviewImageData : IDisposable
	{
		public bool isMergeDiff;
		public string imageExt;
		public Stream oldImage, newImage;
		public StreamReader oldImageReader, newImageReader;

		public void Dispose()
		{
			if (oldImageReader != null)
			{
				oldImageReader.Dispose();
				oldImageReader = null;
			}

			if (oldImage != null)
			{
				oldImage.Dispose();
				oldImage = null;
			}

			if (newImageReader != null)
			{
				newImageReader.Dispose();
				newImageReader = null;
			}

			if (newImage != null)
			{
				newImage.Dispose();
				newImage = null;
			}
		}
	}

	public partial class RepoManager
	{
		public delegate bool AskUserToResolveConflictedFileCallbackMethod(FileState fileState, bool isBinaryFile, out MergeFileResults result);
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
				if (!repository.GetFileStates(out states, AppManager.settings.showLFSTags)) throw new Exception(repository.lastError);
				fileStates = states;
				return true;
			}
			catch (Exception e)
			{
				DebugLog.LogError("ChangesManager.Refresh Failed: " + e.Message);
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

		private bool SaveOriginalFile(string filename, out string savedFilename)
		{
			// save data to file
			if (!repository.SaveOriginalFile(filename, out savedFilename)) return false;
			string fullPathOrig = Path.Combine(repository.repoPath, savedFilename);
			
			// check for lfs ptr
			string ptr = null;
			using (var stream = new FileStream(fullPathOrig, FileMode.Open, FileAccess.Read, FileShare.None))
			using (var reader = new StreamReader(stream))
			{
				if (!Tools.IsGitLFSPtr(reader, out ptr)) ptr = null;
			}

			// smudge lfs ptr (aka convert ptr to data)
			if (ptr != null && !repository.lfs.SmudgeFile(ptr, savedFilename))
			{
				DebugLog.LogError("Failed to smudge file: " + filename);
				return false;
			}
					
			return true;
		}

		private bool SmudgeFile(string filename, StreamReader reader)
		{
			// smudge lfs ptr (aka convert ptr to data)
			var stream = reader.BaseStream;
			if (Tools.IsGitLFSPtr(reader, out string ptr))
			{
				stream.SetLength(0);
				if (!repository.lfs.SmudgeFile(ptr, stream))
				{
					DebugLog.LogError("Failed to smudge file: " + filename);
					return false;
				}
			}
					
			return true;
		}

		private bool SaveOriginalFile(string filename, StreamReader reader)
		{
			var stream = reader.BaseStream;

			// save data to stream
			if (!repository.SaveOriginalFile(filename, stream)) return false;
			if (stream.Length == 0) return false;
			stream.Position = 0;
					
			return SmudgeFile(filename, reader);
		}

		private bool SaveConflictedFile(string filename, FileConflictSources source, StreamReader reader)
		{
			var stream = reader.BaseStream;

			// save data to stream
			if (!repository.SaveConflictedFile(filename, source, stream)) return false;
			if (stream.Length == 0) return false;
			stream.Position = 0;
				
			return SmudgeFile(filename, reader);
		}

		public object GetQuickViewData(FileState fileState, bool allowUncommonImageTypes)
		{
			lock (this)
			{
				PreviewImageData image = null;

				try
				{
					// check if file still exists
					string fullPath = Path.Combine(repository.repoPath, fileState.filename);
					if (!File.Exists(fullPath))
					{
						return "<< File Doesn't Exist >>";
					}

					// check if binary file
					if (Tools.IsBinaryFileData(fullPath)) 
					{
						// validate is supported image
						if (!Tools.IsSupportedImageFile(fullPath, allowUncommonImageTypes)) return "<< Binary File >>";

						// load new/ours image data
						image = new PreviewImageData();
						image.imageExt = Path.GetExtension(fileState.filename).ToLower();
						if (fileState.HasState(FileStates.Conflicted))
						{
							image.isMergeDiff = true;
							image.newImage = new MemoryStream();
							image.newImageReader = new StreamReader(image.newImage);
							if (SaveConflictedFile(fileState.filename, FileConflictSources.Ours, image.newImageReader))
							{
								image.newImage.Position = 0;
							}
							else
							{
								image.newImageReader.Dispose();
								image.newImage.Dispose();
								image.newImage = null;
								image.newImageReader = null;
							}
						}
						else
						{
							image.newImage = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);
						}

						// load old/theirs image data
						if (fileState.HasState(FileStates.Conflicted))
						{
							image.oldImage = new MemoryStream();
							image.oldImageReader = new StreamReader(image.oldImage);
							if (SaveConflictedFile(fileState.filename, FileConflictSources.Theirs, image.oldImageReader))
							{
								image.oldImage.Position = 0;
							}
							else
							{
								image.oldImageReader.Dispose();
								image.oldImage.Dispose();
								image.oldImage = null;
								image.oldImageReader = null;
							}
						}
						else
						{
							image.oldImage = new MemoryStream();
							image.oldImageReader = new StreamReader(image.oldImage);
							if (SaveOriginalFile(fileState.filename, image.oldImageReader))
							{
								image.oldImage.Position = 0;
							}
							else
							{
								image.oldImageReader.Dispose();
								image.oldImage.Dispose();
								image.oldImage = null;
								image.oldImageReader = null;
							}
						}
						
						return image;
					}

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
								diff = diff.Replace(match.Groups[1].Value, Environment.NewLine + "### ----------- SECTION ----------- ###" + Environment.NewLine);
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
						DebugLog.LogError("Unsuported FileStatus: " + fileState.filename);
						return null;
					}
				}
				catch (Exception e)
				{
					pauseGitCommanderStdWrites = false;
					DebugLog.LogError("Failed to refresh quick view: " + e.Message);
					if (image != null) image.Dispose();
					return e;
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
					string filePath = Path.Combine(repository.repoPath, fileState.filename);
					if (File.Exists(filePath)) File.Delete(filePath);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to delete item: " + e.Message);
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

						string filePath = Path.Combine(repository.repoPath, fileState.filename);
						if (File.Exists(filePath)) File.Delete(filePath);
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to delete item: " + e.Message);
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
						string filePath = Path.Combine(repository.repoPath, fileState.filename);
						if (File.Exists(filePath)) File.Delete(filePath);
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to delete item: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool StageFile(FileState fileState, bool refresh)
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
					DebugLog.LogError("Failed to stage item: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool StageFileList(List<FileState> fileStates, bool refresh)
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
					DebugLog.LogError("Failed to stage items: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool StageAllFiles(bool refresh)
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
					DebugLog.LogError("Failed to stage item: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool UnstageFile(FileState fileState, bool refresh)
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
					DebugLog.LogError("Failed to unstage item: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool UnstageFileList(List<FileState> fileStates, bool refresh)
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
					DebugLog.LogError("Failed to unstage items: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
				return success;
			}
		}

		public bool UnstageAllFiles(bool refresh)
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
					DebugLog.LogError("Failed to unstage item: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
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
					DebugLog.LogError("This file is not modified or deleted: " + fileState.filename);
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

		public bool CommitStagedChanges(string commitMessage, bool refresh = true)
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
					DebugLog.LogError("Failed to commit: " + e.Message);
					success = false;
				}

				if (refresh) Refresh();
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
					DebugLog.LogError("Fetch error: " + e.Message);
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
					DebugLog.LogError("Fetch error: " + e.Message);
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
						DebugLog.LogWarning("Branch is not tracking a remote!");
						return SyncMergeResults.Error;
					}
				
					// pull changes
					result = repository.Pull() ? SyncMergeResults.Succeeded : SyncMergeResults.Error;
					result = ConflictsExist() ? SyncMergeResults.Conflicts : result;
				
					if (result == SyncMergeResults.Conflicts) DebugLog.LogWarning("Merge failed, conflicts exist (please resolve)");
					else if (result == SyncMergeResults.Succeeded) DebugLog.Log("Pull Succeeded!");
					else DebugLog.Log("Pull Error!");
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to pull: " + e.Message);
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
						DebugLog.LogWarning("Branch is not tracking a remote!");
						return false;
					}
				
					if (repository.Push()) DebugLog.Log("Push Succeeded!");
					else throw new Exception(repository.lastError);
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to push: " + e.Message);
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
			
				if (result != SyncMergeResults.Succeeded || !pushPass) DebugLog.LogError("Failed to Sync changes");
				else DebugLog.Log("Sync succeeded!");
			
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
					DebugLog.LogError("Failed to get file conflicts: " + e.Message);
					return false;
				}
			}
		}

		public bool ConflictsExistQuick()
		{
			lock (this)
			{
				try
				{
					foreach (var state in fileStates)
					{
						if (state.HasState(FileStates.Conflicted)) return true;
					}
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to get file conflicts: " + e.Message);
				}

				return false;
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
					DebugLog.LogError("Failed to check for pending merge commit: " + e.Message);
					return false;
				}
			}
		}

		public bool ResolveAllConflicts()
		{
			lock (this)
			{
				foreach (var fileState in fileStates)
				{
					if (fileState.HasState(FileStates.Conflicted) && !ResolveConflict(fileState, false))
					{
						DebugLog.LogError("Resolve conflict failed (aborting pending)");
						Refresh();
						return false;
					}
				}

				Refresh();
				return true;
			}
		}
		
		public bool ResolveConflict(FileState fileState, bool refresh = true)
		{
			lock (this)
			{
				//bool success = true;
				string fullPath = Path.Combine(repository.repoPath, fileState.filename);
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
						DebugLog.LogError("File not in conflicted state: " + fileState.filename);
						return false;
					}
				
					// save local temp files
					if (fileState.conflictType == FileConflictTypes.DeletedByBoth)
					{
						DebugLog.Log("Auto resolving file that was deleted by both branches: " + fileState.filename);
						if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
						return true;
					}
					else
					{
						pauseGitCommanderStdWrites = true;
						if (fileState.conflictType != FileConflictTypes.DeletedByUs || fileState.conflictType == FileConflictTypes.AddedByUs || fileState.conflictType == FileConflictTypes.AddedByBoth)
						{
							bool result = repository.SaveConflictedFile(fileState.filename, FileConflictSources.Ours, out fullPathOurs);
							fullPathOurs = Path.Combine(repository.repoPath, fullPathOurs);
							if (!result) throw new Exception(repository.lastError);
						}

						if (fileState.conflictType != FileConflictTypes.DeletedByThem || fileState.conflictType == FileConflictTypes.AddedByThem || fileState.conflictType == FileConflictTypes.AddedByBoth)
						{
							bool result = repository.SaveConflictedFile(fileState.filename, FileConflictSources.Theirs, out fullPathTheirs);
							fullPathTheirs = Path.Combine(repository.repoPath, fullPathTheirs);
							if (!result) throw new Exception(repository.lastError);
						}
						pauseGitCommanderStdWrites = false;
					}

					// ======================================
					// Handle Binary conflicts
					// ======================================
					#region Binary
					// check if files are binary (if so open select binary file tool) [if file conflict is because of deletion this method is also used]
					if (fileState.conflictType != FileConflictTypes.Changes || Tools.IsBinaryFileData(fullPathOurs) || Tools.IsBinaryFileData(fullPathTheirs))
					{
						// validate callback
						if (AskUserToResolveConflictedFileCallback == null) return false;
						
						// process merge results
						if (AskUserToResolveConflictedFileCallback(fileState, true, out var mergeBinaryResult))
						{
							switch (mergeBinaryResult)
							{
								case MergeFileResults.Error:
									throw new Exception("Error trying to resolve file: " + fileState.filename);

								case MergeFileResults.Cancel:
									return false;

								case MergeFileResults.KeepMine:
									if (fileState.conflictType == FileConflictTypes.Changes)
									{
										if (!repository.CheckoutConflictedFile(fileState.filename, FileConflictSources.Ours)) throw new Exception(repository.lastError);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByThem || fileState.conflictType == FileConflictTypes.AddedByUs || fileState.conflictType == FileConflictTypes.AddedByBoth)
									{
										File.Copy(fullPathOurs, fullPath, true);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByUs || fileState.conflictType == FileConflictTypes.AddedByThem)
									{
										if (!repository.RemoveFile(fileState.filename)) throw new Exception(repository.lastError);
									}
									break;

								case MergeFileResults.UseTheirs:
									if (fileState.conflictType == FileConflictTypes.Changes)
									{
										if (!repository.CheckoutConflictedFile(fileState.filename, FileConflictSources.Theirs)) throw new Exception(repository.lastError);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByThem || fileState.conflictType == FileConflictTypes.AddedByUs)
									{
										if (!repository.RemoveFile(fileState.filename)) throw new Exception(repository.lastError);
									}
									else if (fileState.conflictType == FileConflictTypes.DeletedByUs || fileState.conflictType == FileConflictTypes.AddedByThem || fileState.conflictType == FileConflictTypes.AddedByBoth)
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
					}
					#endregion

					// ======================================
					// Handle Text conflicts
					// ======================================
					#region Text
					else
					{
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

						// validate callback
						if (AskUserToResolveConflictedFileCallback == null) return false;
					
						// process merge results
						if (AskUserToResolveConflictedFileCallback(fileState, false, out var mergeFileResult))
						{
							switch (mergeFileResult)
							{
								case MergeFileResults.Error:
									throw new Exception("Error trying to resolve file: " + fileState.filename);

								case MergeFileResults.Cancel:
									return false;

								case MergeFileResults.KeepMine:
									File.Copy(fullPathOurs, fullPathBase, true);
									break;

								case MergeFileResults.UseTheirs:
									File.Copy(fullPathTheirs, fullPathBase, true);
									break;

								case MergeFileResults.RunMergeTool:
									// validate diff/merge tool installed
									if (string.IsNullOrEmpty(AppManager.mergeToolPath))
									{
										DebugLog.LogError("Diff/Merge tool not selected in app settings. ResolveConflict failed");
										return false;
									}

									// run diff/merge tool
									using (var process = new Process())
									{
										process.StartInfo.FileName = AppManager.mergeToolPath;
										if (AppManager.settings.mergeDiffTool == MergeDiffTools.Meld) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
										else if (AppManager.settings.mergeDiffTool == MergeDiffTools.kDiff3) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
										else if (AppManager.settings.mergeDiffTool == MergeDiffTools.P4Merge) process.StartInfo.Arguments = string.Format("\"{1}\" \"{0}\" \"{2}\" \"{1}\"", fullPathOurs, fullPathBase, fullPathTheirs);
										else if (AppManager.settings.mergeDiffTool == MergeDiffTools.DiffMerge) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\" \"{2}\"", fullPathOurs, fullPathBase, fullPathTheirs);
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
							// validate callback
							if (AskUserIfTheyAcceptMergedFileCallback == null) return false;

							// process merge results
							if (AskUserIfTheyAcceptMergedFileCallback(fileState, out var mergeAcceptedResult))
							{
								switch (mergeAcceptedResult)
								{
									case MergeFileAcceptedResults.Yes:
										File.Copy(fullPathBase, fullPath, true);
										if (!repository.Stage(fileState.filename)) throw new Exception(repository.lastError);
										wasModified = true;
										break;

									case MergeFileAcceptedResults.No:
										return false;

									default: throw new Exception("Unsuported Response: " + mergeAcceptedResult);
								}
							}
							else
							{
								throw new Exception("Failed to ask user if file was resolved: " + fileState.filename);
							}
						}
					}
					#endregion
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to resolve file: " + e.Message);
					return false;
				}
				finally
				{
					pauseGitCommanderStdWrites = false;
					DeleteTempMergeFiles();
					if (refresh) Refresh();
				}

				return true;
			}
		}
		
		public bool OpenDiffTool(FileState fileState)
		{
			// validate diff/merge tool installed
			if (string.IsNullOrEmpty(AppManager.mergeToolPath))
			{
				DebugLog.LogError("Diff/Merge tool not selected in app settings. OpenDiffTool failed");
				return false;
			}

			lock (this)
			{
				string fullPath = Path.Combine(repository.repoPath, fileState.filename);
				string fullPathOurs = null, fullPathTheirs = null, fullPathOrig = null;
				void DeleteTempDiffFiles()
				{
					if (File.Exists(fullPathOurs)) File.Delete(fullPathOurs);
					if (File.Exists(fullPathTheirs)) File.Delete(fullPathTheirs);
					if (File.Exists(fullPathOrig)) File.Delete(fullPathOrig);
				}

				try
				{
					// validate state
					if ((!fileState.HasState(FileStates.ModifiedInIndex) && !fileState.HasState(FileStates.ModifiedInWorkdir)) && !fileState.HasState(FileStates.Conflicted))
					{
						DebugLog.LogError("This file is not modified/conflicted");
						return false;
					}

					// get info and save orig file
					pauseGitCommanderStdWrites = true;
					if (fileState.HasState(FileStates.Conflicted))
					{
						if (!repository.SaveConflictedFile(fileState.filename, FileConflictSources.Ours, out fullPathOurs)) throw new Exception(repository.lastError);
						if (!repository.SaveConflictedFile(fileState.filename, FileConflictSources.Theirs, out fullPathTheirs)) throw new Exception(repository.lastError);
						fullPathOurs = Path.Combine(repository.repoPath, fullPathOurs);
						fullPathTheirs = Path.Combine(repository.repoPath, fullPathTheirs);
					}
					else
					{
						if (!SaveOriginalFile(fileState.filename, out fullPathOrig)) throw new Exception(repository.lastError);
						fullPathOrig = Path.Combine(repository.repoPath, fullPathOrig);
					}
					pauseGitCommanderStdWrites = false;

					// run diff/merge tool
					using (var process = new Process())
					{
						process.StartInfo.FileName = AppManager.mergeToolPath;
						if (fileState.HasState(FileStates.Conflicted)) process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", fullPathTheirs, fullPathOurs);
						else process.StartInfo.Arguments = string.Format("\"{0}\" \"{1}\"", fullPathOrig, fullPath);
						process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
						if (!process.Start())
						{
							DebugLog.LogError("Failed to start Diff tool (is it installed?)");
							DeleteTempDiffFiles();
							return false;
						}

						process.WaitForExit();
					}
				}
				catch (Exception e)
				{
					pauseGitCommanderStdWrites = false;
					DebugLog.LogError("Failed to start Diff tool: " + e.Message);
					DeleteTempDiffFiles();
					return false;
				}

				// finish
				DeleteTempDiffFiles();
				return true;
			}
		}

		public bool SaveCommitMessage(string message)
		{
			lock (this)
			{
				try
				{
					using (var stream = new FileStream(Path.Combine(repository.repoPath, ".git", "GITGUI_MSG"), FileMode.Create, FileAccess.Write, FileShare.None))
					using (var writer = new StreamWriter(stream))
					{
						writer.Write(message);
					}
					
					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to save commit msg history: " + e.Message);
					return false;
				}
			}
		}

		public bool LoadCommitMessage(out string message)
		{
			lock (this)
			{
				try
				{
					string filename = Path.Combine(repository.repoPath, ".git", "GITGUI_MSG");
					if (!File.Exists(filename))
					{
						message = null;
						return false;
					}

					message = File.ReadAllText(filename);
					return true;
				}
				catch (Exception e)
				{
					DebugLog.LogError("Failed to start Diff tool: " + e.Message);
					message = null;
					return false;
				}
			}
		}
	}
}
