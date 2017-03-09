using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	public enum FileStates
	{
		ModifiedInWorkdir,
		ModifiedInIndex,
		NewInWorkdir,
		NewInIndex,
		DeletedFromWorkdir,
		DeletedFromIndex,
		RenamedInWorkdir,
		RenamedInIndex,
		TypeChangeInWorkdir,
		TypeChangeInIndex,
		Conflicted
	}

	public class FileState
	{
		public string filename;
		public FileStates state;

		public FileState(string filename, FileStates state)
		{
			this.filename = filename;
			this.state = state;
		}

		public bool IsStaged()
		{
			switch (state)
			{
				case FileStates.NewInIndex:
				case FileStates.DeletedFromIndex:
				case FileStates.ModifiedInIndex:
				case FileStates.RenamedInIndex:
				case FileStates.TypeChangeInIndex:
					return true;

				
				case FileStates.NewInWorkdir:
				case FileStates.DeletedFromWorkdir:
				case FileStates.ModifiedInWorkdir:
				case FileStates.RenamedInWorkdir:
				case FileStates.TypeChangeInWorkdir:
				case FileStates.Conflicted:
					return false;
			}

			throw new Exception("Unsuported state: " + state);
		}
	}

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
		public delegate bool AskUserToResolveBinaryFileCallbackMethod(FileState fileState, out MergeBinaryFileResults result);
		public static event AskUserToResolveBinaryFileCallbackMethod AskUserToResolveBinaryFileCallback;

		public delegate bool AskUserIfTheyAcceptMergedFileCallbackMethod(FileState fileState, out MergeFileAcceptedResults result);
		public static event AskUserIfTheyAcceptMergedFileCallbackMethod AskUserIfTheyAcceptMergedFileCallback;

		private static List<FileState> fileStates;
		public static bool changesExist {get; private set;}
		public static bool changesStaged {get; private set;}

		private static bool isSyncMode;

		public static FileState[] GetFileChanges()
		{
			return fileStates.ToArray();
		}

		private static bool FileStateExists(string filename)
		{
			return fileStates.Exists(x => x.filename == filename);
		}

		internal static bool Refresh()
		{
			try
			{
				changesExist = false;
				changesStaged = false;
				fileStates = new List<FileState>();
				bool changesFound = false;
				var repoStatus = RepoManager.repo.RetrieveStatus();
				foreach (var fileStatus in repoStatus)
				{
					if (fileStatus.FilePath == Settings.repoUserSettingsFilename) continue;

					changesFound = true;
					bool stateHandled = false;
					var state = fileStatus.State;
					if ((state & FileStatus.ModifiedInWorkdir) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.ModifiedInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.ModifiedInIndex) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.ModifiedInIndex));
						stateHandled = true;
						changesStaged = true;
					}

					if ((state & FileStatus.NewInWorkdir) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.NewInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.NewInIndex) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.NewInIndex));
						stateHandled = true;
						changesStaged = true;
					}

					if ((state & FileStatus.DeletedFromWorkdir) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.DeletedFromWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.DeletedFromIndex) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.DeletedFromIndex));
						stateHandled = true;
						changesStaged = true;
					}

					if ((state & FileStatus.RenamedInWorkdir) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.RenamedInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.RenamedInIndex) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.RenamedInIndex));
						stateHandled = true;
						changesStaged = true;
					}

					if ((state & FileStatus.TypeChangeInWorkdir) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.TypeChangeInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.TypeChangeInIndex) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.TypeChangeInIndex));
						stateHandled = true;
						changesStaged = true;
					}

					if ((state & FileStatus.Conflicted) != 0)
					{
						if (!FileStateExists(fileStatus.FilePath)) fileStates.Add(new FileState(fileStatus.FilePath, FileStates.Conflicted));
						stateHandled = true;
					}

					if ((state & FileStatus.Ignored) != 0)
					{
						stateHandled = true;
					}

					if ((state & FileStatus.Unreadable) != 0)
					{
						string fullpath = RepoManager.repoPath + Path.DirectorySeparatorChar + fileStatus.FilePath;
						if (File.Exists(fullpath))
						{
							// disable readonly if this is the cause
							var attributes = File.GetAttributes(fullpath);
							if ((attributes & FileAttributes.ReadOnly) != 0) File.SetAttributes(fullpath, FileAttributes.Normal);
							else
							{
								Debug.LogError("Problem will file read (please fix and refresh)\nCause: " + fileStatus.FilePath);
								continue;
							}

							// check to make sure file is now readable
							attributes = File.GetAttributes(fullpath);
							if ((attributes & FileAttributes.ReadOnly) != 0) Debug.LogError("File is not readable (you will need to fix the issue and refresh\nCause: " + fileStatus.FilePath);
							else Debug.LogError("Problem will file read (please fix and refresh)\nCause: " + fileStatus.FilePath);
						}
						else
						{
							Debug.LogError("Expected file doesn't exist: " + fileStatus.FilePath);
						}

						stateHandled = true;
					}

					if (!stateHandled)
					{
						Debug.LogError("Unsuported File State: " + state);
					}
				}

				if (!changesFound) Debug.Log("No Changes, now do some stuff!");
				else changesExist = true;
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to update file status: " + e.Message, true);
				return false;
			}
		}

		public static FileState[] GetFileStatuses()
		{
			return fileStates.ToArray();
		}

		public static bool FilesAreStaged()
		{
			foreach (var state in fileStates)
			{
				if (state.state == FileStates.DeletedFromIndex || state.state == FileStates.ModifiedInIndex ||
					state.state == FileStates.NewInIndex || state.state == FileStates.RenamedInIndex || state.state == FileStates.TypeChangeInIndex)
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
				if (state.state == FileStates.DeletedFromWorkdir || state.state == FileStates.ModifiedInWorkdir ||
					state.state == FileStates.NewInWorkdir|| state.state == FileStates.RenamedInWorkdir || state.state == FileStates.TypeChangeInWorkdir)
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
				string fullPath = RepoManager.repoPath + Path.DirectorySeparatorChar + fileState.filename;
				if (!File.Exists(fullPath))
				{
					return "<< File Doesn't Exist >>";
				}

				// if new file just grab local data
				if (fileState.state == FileStates.NewInWorkdir || fileState.state == FileStates.NewInIndex || fileState.state == FileStates.Conflicted)
				{
					string value;
					if (!Tools.IsBinaryFileData(fullPath))
					{
						using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.None))
						using (var reader = new StreamReader(stream))
						{
							value= reader.ReadToEnd();
						}
					}
					else
					{
						value = "<< Binary File >>";
					}

					return value;
				}

				// check if binary file
				var file = RepoManager.repo.Index[fileState.filename];
				var blob = RepoManager.repo.Lookup<Blob>(file.Id);
				if (blob.IsBinary || Tools.IsBinaryFileData(fullPath))
				{
					return "<< Binary File >>";
				}

				// check for text types
				if (fileState.state == FileStates.ModifiedInWorkdir)
				{
					var patch = RepoManager.repo.Diff.Compare<Patch>(new List<string>(){fileState.filename});// use this for details about change

					string content = patch.Content;

					var match = Regex.Match(content, @"@@.*?(@@).*?\n(.*)", RegexOptions.Singleline);
					if (match.Success && match.Groups.Count == 3) content = match.Groups[2].Value.Replace("\\ No newline at end of file\n", "");

					// remove meta data stage 2
					bool search = true;
					while (search)
					{
						patch = RepoManager.repo.Diff.Compare<Patch>(new List<string>() {fileState.filename});
						match = Regex.Match(content, @"(@@.*?(@@).*?\n)", RegexOptions.Singleline);
						if (match.Success && match.Groups.Count == 3)
						{
							content = content.Replace(match.Groups[1].Value, Environment.NewLine + "<<< ----------- SECTION ----------- >>>" + Environment.NewLine);
						}
						else
						{
							search = false;
						}
					}

					return content;
				}
				else if (fileState.state == FileStates.ModifiedInIndex ||
					fileState.state == FileStates.DeletedFromWorkdir || fileState.state == FileStates.DeletedFromIndex ||
					fileState.state == FileStates.RenamedInWorkdir || fileState.state == FileStates.RenamedInIndex ||
					fileState.state == FileStates.TypeChangeInWorkdir || fileState.state == FileStates.TypeChangeInIndex)
				{
					return blob.GetContentText();
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
				string filePath = RepoManager.repoPath + Path.DirectorySeparatorChar + fileState.filename;
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
					if (fileState.state != FileStates.NewInWorkdir) continue;
					string filePath = RepoManager.repoPath + Path.DirectorySeparatorChar + fileState.filename;
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
				Commands.Stage(RepoManager.repo, fileState.filename);
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
				//Commands.Unstage(RepoManager.repo, fileState.filename);// libgit2sharp has some bug on this method
				Tools.RunExe("git", string.Format("reset \"{0}\"", fileState.filename), null);// use this hack for now
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
				RepoManager.repo.Reset(ResetMode.Hard);
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
			if (fileState.state == FileStates.ModifiedInIndex && fileState.state == FileStates.ModifiedInWorkdir &&
				fileState.state == FileStates.DeletedFromIndex && fileState.state == FileStates.DeletedFromWorkdir)
			{
				Debug.LogError("This file is not modified or deleted", true);
				return false;
			}

			try
			{
				var options = new CheckoutOptions();
				options.CheckoutModifiers = CheckoutModifiers.Force;
				RepoManager.repo.CheckoutPaths(RepoManager.repo.Head.FriendlyName, new string[] {fileState.filename}, options);
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
				var sig = RepoManager.signature;
				RepoManager.repo.Commit(commitMessage, sig, sig);
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
				if (!BranchManager.IsTracking())
				{
					Debug.LogWarning("Branch is not tracking a remote!", true);
					return SyncMergeResults.Error;
				}

				// check for git settings file not in repo history
				RepoManager.DeleteRepoSettingsIfUnCommit();

				// check if git-lfs repo before pull
				bool isGitLFS = RepoManager.IsGitLFSRepo(false);

				// pull
				var options = new PullOptions();
				options.FetchOptions = new FetchOptions();
				options.FetchOptions.CredentialsProvider = (_url, _user, _cred) => RepoManager.credentials;
				options.FetchOptions.TagFetchMode = TagFetchMode.All;
				options.FetchOptions.OnProgress = delegate(string serverProgressOutput)
				{
					if (statusCallback != null) statusCallback(serverProgressOutput);
					return true;
				};

				options.FetchOptions.OnTransferProgress = delegate(TransferProgress progress)
				{
					if (statusCallback != null) statusCallback(string.Format("Downloading: {0}%", (int)((progress.ReceivedObjects / (decimal)(progress.TotalObjects+1)) * 100)));
					return true;
				};

				options.MergeOptions = new MergeOptions();
				options.MergeOptions.OnCheckoutProgress = delegate(string path, int completedSteps, int totalSteps)
				{
					if (statusCallback != null) statusCallback(string.Format("Checking out: {0}%", (int)((completedSteps / (decimal)(totalSteps+1)) * 100)));
				};
				
				Filters.GitLFS.statusCallback = statusCallback;
				Commands.Pull(RepoManager.repo, RepoManager.signature, options);
				Filters.GitLFS.statusCallback = null;

				// check if repo git-lfs has changed
				if (!isGitLFS && RepoManager.IsGitLFSRepo(true))
				{
					Debug.LogWarning("Repo seems to now support Git-LFS.\nCritical: You will need to re-pull for these changes!", true);
					RepoManager.AddGitLFSSupport(false);
				}
				else if (isGitLFS && !RepoManager.IsGitLFSRepo(true))
				{
					Debug.LogWarning("Repo seems to have removed Git-LFS support.\nYou will need to re-pull for these changes!", true);
					RepoManager.RemoveGitLFSSupport(false);
				}

				result = ConflictsExist() ? SyncMergeResults.Conflicts : SyncMergeResults.Succeeded;
				if (result == SyncMergeResults.Conflicts) Debug.LogWarning("Merge failed, conflicts exist (please resolve)", true);
				else Debug.Log("Pull Succeeded!", !isSyncMode);
			}
			catch (Exception e)
			{
				if (e.Message == "Too many redirects or authentication replays") Debug.LogError("Invalid Username or Password", true);
				else Debug.LogError("Failed to pull: " + e.Message, true);
				Filters.GitLFS.statusCallback = null;
				return SyncMergeResults.Error;
			}

			Filters.GitLFS.statusCallback = null;
			if (!isSyncMode) RepoManager.Refresh();
			return result;
		}

		public static bool Push(StatusUpdateCallbackMethod statusCallback)
		{
			try
			{
				if (!BranchManager.IsTracking())
				{
					Debug.LogWarning("Branch is not tracking a remote!", true);
					return false;
				}

				// pre push git lfs file data
				var options = new PushOptions();
				if (RepoManager.lfsEnabled)
				{
					if (statusCallback != null) statusCallback("Starting Git-LFS pre-push...");
					options.OnNegotiationCompletedBeforePush = delegate(IEnumerable<PushUpdate> updates)
					{
						if (updates.Count() == 0) return true;

						string outputErr = "", output = "";
						using (var process = new Process())
						{
							process.StartInfo.FileName = "git-lfs";
							process.StartInfo.Arguments = "pre-push " + RepoManager.repo.Network.Remotes[BranchManager.activeBranch.RemoteName].Name;
							process.StartInfo.WorkingDirectory = RepoManager.repoPath;
							process.StartInfo.CreateNoWindow = true;
							process.StartInfo.UseShellExecute = false;
							process.StartInfo.RedirectStandardInput = true;
							process.StartInfo.RedirectStandardOutput = true;
							process.StartInfo.RedirectStandardError = true;

							process.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e)
							{
								if (!string.IsNullOrEmpty(e.Data))
								{
									output += e.Data + Environment.NewLine;
									if (statusCallback != null) statusCallback(e.Data);
								}
							};

							process.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e)
							{
								if (!string.IsNullOrEmpty(e.Data))
								{
									outputErr += e.Data + Environment.NewLine;
									if (statusCallback != null) statusCallback("ERROR: " + e.Data);
								}
							};

							process.Start();
							process.BeginOutputReadLine();
							process.BeginErrorReadLine();
				
							foreach (var update in updates)
							{
								string value = string.Format("{0} {1} {2} {3}\n", update.SourceRefName, update.SourceObjectId.Sha, update.DestinationRefName, update.DestinationObjectId.Sha);
								process.StandardInput.Write(value);
							}

							process.StandardInput.Write("\0");
							process.StandardInput.Flush();
							process.StandardInput.Close();
							process.WaitForExit();
							
							if (!string.IsNullOrEmpty(output)) Debug.Log("git-lfs pre-push results: " + output);
							if (!string.IsNullOrEmpty(outputErr))
							{
								Debug.LogError("git-lfs pre-push error results: " + outputErr, true);
								return false;
							}
						}

						return true;
					};
				}
				
				// post git push
				options.CredentialsProvider = (_url, _user, _cred) => RepoManager.credentials;
				bool pushError = false;
				options.OnPushStatusError = delegate(PushStatusError ex)
				{
					Debug.LogError("Failed to push (do you have valid permisions?): " + ex.Message, true);
					pushError = true;
				};

				options.OnPushTransferProgress = delegate(int current, int total, long bytes)
				{
					if (statusCallback != null) statusCallback(string.Format("Uploading: {0}%", (int)((current / (decimal)(total+1)) * 100)));
					return true;
				};

				Filters.GitLFS.statusCallback = statusCallback;
				RepoManager.repo.Network.Push(BranchManager.activeBranch, options);
				Filters.GitLFS.statusCallback = null;
				
				if (!pushError)
				{
					Debug.Log("Push Succeeded!", !isSyncMode);
				}
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to push: " + e.Message, true);
				Filters.GitLFS.statusCallback = null;
				return false;
			}

			Filters.GitLFS.statusCallback = null;
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
			foreach (var fileState in fileStates)
			{
				if (fileState.state == FileStates.Conflicted) return true;
			}

			return false;
		}

		public static bool ResolveAllConflicts(bool refresh = true)
		{
			foreach (var fileState in fileStates)
			{
				if (fileState.state == FileStates.Conflicted && !ResolveConflict(fileState, false))
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

			try
			{
				// make sure file needs to be resolved
				if (fileState.state != FileStates.Conflicted)
				{
					Debug.LogError("File not in conflicted state: " + fileState.filename, true);
					return false;
				}

				// get info
				string fullPath = RepoManager.repoPath + Path.DirectorySeparatorChar + fileState.filename;
				var conflict = RepoManager.repo.Index.Conflicts[fileState.filename];
				var ours = RepoManager.repo.Lookup<Blob>(conflict.Ours.Id);
				var theirs = RepoManager.repo.Lookup<Blob>(conflict.Theirs.Id);

				// save local temp files
				Tools.SaveFileFromID(fullPath + ".ours", ours.Id);
				Tools.SaveFileFromID(fullPath + ".theirs", theirs.Id);

				// check if files are binary (if so open select binary file tool)
				if (ours.IsBinary || theirs.IsBinary || Tools.IsBinaryFileData(fullPath + ".ours") || Tools.IsBinaryFileData(fullPath + ".theirs"))
				{
					// open merge tool
					MergeBinaryFileResults mergeBinaryResult;
					if (AskUserToResolveBinaryFileCallback != null && AskUserToResolveBinaryFileCallback(fileState, out mergeBinaryResult))
					{
						switch (mergeBinaryResult)
						{
							case MergeBinaryFileResults.Error: Debug.LogWarning("Error trying to resolve file: " + fileState.filename, true);
								DeleteTempMergeFiles(fullPath);
								return false;

							case MergeBinaryFileResults.Cancel:
								DeleteTempMergeFiles(fullPath);
								return false;

							case MergeBinaryFileResults.KeepMine: File.Copy(fullPath + ".ours", fullPath, true); break;
							case MergeBinaryFileResults.UseTheirs: File.Copy(fullPath + ".theirs", fullPath, true); break;
							default: Debug.LogWarning("Unsuported Response: " + mergeBinaryResult, true);
								DeleteTempMergeFiles(fullPath);
								return false;
						}
					}
					else
					{
						Debug.LogError("Failed to resolve file: " + fileState.filename, true);
						return false;
					}

					// delete temp files
					DeleteTempMergeFiles(fullPath);

					// stage and finish
					Commands.Stage(RepoManager.repo, fileState.filename);
					if (refresh) RepoManager.Refresh();
					return true;
				}
			
				// copy base and parse
				File.Copy(fullPath, fullPath + ".base", true);
				string baseFile = File.ReadAllText(fullPath);
				var match = Regex.Match(baseFile, @"(<<<<<<<\s*\w*[\r\n]*).*(=======[\r\n]*).*(>>>>>>>\s*\w*[\r\n]*)", RegexOptions.Singleline);
				if (match.Success && match.Groups.Count == 4)
				{
					baseFile = baseFile.Replace(match.Groups[1].Value, "").Replace(match.Groups[2].Value, "").Replace(match.Groups[3].Value, "");
					File.WriteAllText(fullPath + ".base", baseFile);
				}

				// hash base file
				byte[] baseHash = null;
				using (var md5 = MD5.Create())
				{
					using (var stream = File.OpenRead(fullPath + ".base"))
					{
						baseHash = md5.ComputeHash(stream);
					}
				}

				// start external merge tool
				MergeBinaryFileResults mergeFileResult;
				if (AskUserToResolveBinaryFileCallback != null && AskUserToResolveBinaryFileCallback(fileState, out mergeFileResult))
				{
					switch (mergeFileResult)
					{
						case MergeBinaryFileResults.Error: Debug.LogWarning("Error trying to resolve file: " + fileState.filename, true);
							DeleteTempMergeFiles(fullPath);
							return false;

						case MergeBinaryFileResults.Cancel:
							DeleteTempMergeFiles(fullPath);
							return false;

						case MergeBinaryFileResults.KeepMine: File.Copy(fullPath + ".ours", fullPath + ".base", true); break;
						case MergeBinaryFileResults.UseTheirs: File.Copy(fullPath + ".theirs", fullPath + ".base", true); break;

						case MergeBinaryFileResults.RunMergeTool:
							using (var process = new Process())
							{
								process.StartInfo.FileName = AppManager.mergeToolPath;
								if (AppManager.mergeDiffTool == MergeDiffTools.Meld) process.StartInfo.Arguments = string.Format("\"{0}.ours\" \"{0}.base\" \"{0}.theirs\"", fullPath);
								else if (AppManager.mergeDiffTool == MergeDiffTools.kDiff3) process.StartInfo.Arguments = string.Format("\"{0}.ours\" \"{0}.base\" \"{0}.theirs\"", fullPath);
								else if (AppManager.mergeDiffTool == MergeDiffTools.P4Merge) process.StartInfo.Arguments = string.Format("\"{0}.base\" \"{0}.ours\" \"{0}.theirs\" \"{0}.base\"", fullPath);
								else if (AppManager.mergeDiffTool == MergeDiffTools.DiffMerge) process.StartInfo.Arguments = string.Format("\"{0}.ours\" \"{0}.base\" \"{0}.theirs\"", fullPath);
								process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
								if (!process.Start())
								{
									Debug.LogError("Failed to start Merge tool (is it installed?)", true);
									DeleteTempMergeFiles(fullPath);
									return false;
								}

								process.WaitForExit();
							}
							break;

						default: Debug.LogWarning("Unsuported Response: " + mergeFileResult, true);
							DeleteTempMergeFiles(fullPath);
							return false;
					}
				}
				else
				{
					Debug.LogError("Failed to resolve file: " + fileState.filename, true);
					DeleteTempMergeFiles(fullPath);
					return false;
				}

				// get new base hash
				byte[] baseHashChange = null;
				using (var md5 = MD5.Create())
				{
					using (var stream = File.OpenRead(fullPath + ".base"))
					{
						baseHashChange = md5.ComputeHash(stream);
					}
				}

				// check if file was modified
				if (!baseHashChange.SequenceEqual(baseHash))
				{
					wasModified = true;
					File.Copy(fullPath + ".base", fullPath, true);
					Commands.Stage(RepoManager.repo, fileState.filename);
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
								File.Copy(fullPath + ".base", fullPath, true);
								Commands.Stage(RepoManager.repo, fileState.filename);
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
						DeleteTempMergeFiles(fullPath);
						return false;
					}
				}

				// delete temp files
				DeleteTempMergeFiles(fullPath);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to resolve file: " + e.Message, true);
				return false;
			}

			// finish
			if (refresh) RepoManager.Refresh();
			return wasModified;
		}

		private static void DeleteTempMergeFiles(string fullPath)
		{
			if (File.Exists(fullPath + ".base")) File.Delete(fullPath + ".base");
			if (File.Exists(fullPath + ".ours")) File.Delete(fullPath + ".ours");
			if (File.Exists(fullPath + ".theirs")) File.Delete(fullPath + ".theirs");
		}

		private static void DeleteTempDiffFiles(string fullPath)
		{
			if (File.Exists(fullPath + ".orig")) File.Delete(fullPath + ".orig");
		}

		public static bool OpenDiffTool(FileState fileState)
		{
			string fullPath = RepoManager.repoPath + Path.DirectorySeparatorChar + fileState.filename;

			try
			{
				// get selected item
				if (fileState.state != FileStates.ModifiedInIndex && fileState.state != FileStates.ModifiedInWorkdir)
				{
					Debug.LogError("This file is not modified", true);
					return false;
				}

				// get info and save orig file
				var changed = RepoManager.repo.Head.Tip[fileState.filename];
				Tools.SaveFileFromID(string.Format("{0}{1}{2}.orig", RepoManager.repoPath, Path.DirectorySeparatorChar, fileState.filename), changed.Target.Id);

				// open diff tool
				using (var process = new Process())
				{
					process.StartInfo.FileName = AppManager.mergeToolPath;
					process.StartInfo.Arguments = string.Format("\"{0}.orig\" \"{0}\"", fullPath);
					process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
					if (!process.Start())
					{
						Debug.LogError("Failed to start Diff tool (is it installed?)", true);
						DeleteTempDiffFiles(fullPath);
						return false;
					}

					process.WaitForExit();
				}

				// delete temp files
				DeleteTempDiffFiles(fullPath);
			}
			catch (Exception ex)
			{
				DeleteTempDiffFiles(fullPath);
				Debug.LogError("Failed to start Diff tool: " + ex.Message, true);
			}

			return true;
		}
	}
}
