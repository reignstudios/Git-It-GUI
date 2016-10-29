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
				case FileStates.Conflicted:
					return true;

				
				case FileStates.NewInWorkdir:
				case FileStates.DeletedFromWorkdir:
				case FileStates.ModifiedInWorkdir:
				case FileStates.RenamedInWorkdir:
				case FileStates.TypeChangeInWorkdir:
					return false;
			}

			throw new Exception("Unsuported state: " + state);
		}
	}

	public static class ChangesManager
	{
		private static List<FileState> fileStates;

		public static FileState[] GetFileChanges()
		{
			return fileStates.ToArray();
		}

		internal static bool Refresh()
		{
			try
			{
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
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.ModifiedInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.ModifiedInIndex) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.ModifiedInIndex));
						stateHandled = true;
					}

					if ((state & FileStatus.NewInWorkdir) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.NewInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.NewInIndex) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.NewInIndex));
						stateHandled = true;
					}

					if ((state & FileStatus.DeletedFromWorkdir) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.DeletedFromWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.DeletedFromIndex) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.DeletedFromIndex));
						stateHandled = true;
					}

					if ((state & FileStatus.RenamedInWorkdir) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.RenamedInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.RenamedInIndex) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.RenamedInIndex));
						stateHandled = true;
					}

					if ((state & FileStatus.TypeChangeInWorkdir) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.TypeChangeInWorkdir));
						stateHandled = true;
					}

					if ((state & FileStatus.TypeChangeInIndex) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.TypeChangeInIndex));
						stateHandled = true;
					}

					if ((state & FileStatus.Conflicted) != 0)
					{
						fileStates.Add(new FileState(fileStatus.FilePath, FileStates.Conflicted));
						stateHandled = true;
					}

					if ((state & FileStatus.Ignored) != 0)
					{
						stateHandled = true;
					}

					if ((state & FileStatus.Unreadable) != 0)
					{
						string fullpath = RepoManager.repoPath + "\\" + fileStatus.FilePath;
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

		public static object GetQuickViewData(FileState fileState)
		{
			try
			{
				// check if file still exists
				string fullPath = RepoManager.repoPath + "\\" + fileState.filename;
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

		public static bool StageFile(FileState fileState, bool refresh)
		{
			try
			{
				RepoManager.repo.Stage(fileState.filename);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to stage item: " + e.Message, true);
				return false;
			}

			if (refresh) RepoManager.Refresh();
		}

		public static bool UnstageFile(FileState fileState, bool refresh)
		{
			try
			{
				RepoManager.repo.Unstage(fileState.filename);
				return true;
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to unstage item: " + e.Message, true);
				return false;
			}

			if (refresh) RepoManager.Refresh();
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

		public static bool CommitStagedChanged(string commitMessage)
		{
			try
			{
				RepoManager.repo.Commit(commitMessage, RepoManager.signature, RepoManager.signature);
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to commit: " + e.Message);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool Pull()
		{
			try
			{
				if (!BranchManager.IsRemote())
				{
					Debug.LogWarning("Branch is not remote!");
					return false;
				}

				var options = new PullOptions();
				options.FetchOptions = new FetchOptions();
				options.FetchOptions.CredentialsProvider = (_url, _user, _cred) => RepoManager.credentials;
				options.FetchOptions.TagFetchMode = TagFetchMode.All;
				RepoManager.repo.Network.Pull(RepoManager.signature, options);
				//ResolveConflicts();// TODO
			}
			catch (Exception e)
			{
				Debug.LogError(string.Format("Failed to pull: {0}\n\nIf this is from a merge conflict.\nYou either need to stage and commit conflicting files\nor delete conflicting files.", e.Message, true));
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		public static bool Push()
		{
			try
			{
				if (!BranchManager.IsRemote())
				{
					Debug.LogWarning("Branch is not remote!");
					return false;
				}
				
				var options = new PushOptions();

				// pre push git lfs file data
				if (RepoManager.lfsEnabled)
				{
					options.OnNegotiationCompletedBeforePush = delegate(IEnumerable<PushUpdate> updates)
					{
						using (var process = new Process())
						{
							process.StartInfo.FileName = "git-lfs";
							process.StartInfo.Arguments = "pre-push origin";
							process.StartInfo.WorkingDirectory = RepoManager.repoPath;
							process.StartInfo.CreateNoWindow = true;
							process.StartInfo.UseShellExecute = false;
							process.StartInfo.RedirectStandardInput = true;
							process.StartInfo.RedirectStandardOutput = true;
							process.StartInfo.RedirectStandardError = true;
							process.Start();
				
							foreach (var update in updates)
							{
								string value = string.Format("{0} {1} {2} {3}\n", update.SourceRefName, update.SourceObjectId.Sha, update.DestinationRefName, update.DestinationObjectId.Sha);
								process.StandardInput.Write(value);
							}

							process.StandardInput.Write("\0");
							process.StandardInput.Flush();
							process.StandardInput.Close();
							process.WaitForExit();

							string output = process.StandardOutput.ReadToEnd();
							string outputErr = process.StandardError.ReadToEnd();
							if (!string.IsNullOrEmpty(output)) Console.WriteLine("git-lfs pre-push results: " + output);
							if (!string.IsNullOrEmpty(outputErr))
							{
								Console.WriteLine("git-lfs pre-push error results: " + outputErr);
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
					Debug.LogError("Failed to push (do you have valid permisions?): " + ex.Message);
					pushError = true;
				};
				RepoManager.repo.Network.Push(BranchManager.activeBranch, options);

				if (!pushError) Debug.Log("Push Succeeded!");
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to push: " + e.Message, true);
				return false;
			}

			RepoManager.Refresh();
			return true;
		}

		//private static async Task<bool> ResolveChange(FileState item)
		public static bool ResolveConflict(FileState fileState, bool refresh)
		{
			// make sure file needs to be resolved
			if (fileState.state != FileStates.Conflicted)
			{
				Debug.LogError("File not in conflicted state: " + fileState.filename, true);
				return false;
			}

			// get info
			string fullPath = string.Format("{0}\\{1}", RepoManager.repoPath, fileState.filename);
			var conflict = RepoManager.repo.Index.Conflicts[fileState.filename];
			var ours = RepoManager.repo.Lookup<Blob>(conflict.Ours.Id);
			var theirs = RepoManager.repo.Lookup<Blob>(conflict.Theirs.Id);

			// save local temp files
			Tools.SaveFileFromID(fullPath + ".ours", ours.Id);
			Tools.SaveFileFromID(fullPath + ".theirs", theirs.Id);

			// check if files are binary (if so open select binary file tool)
			if (ours.IsBinary || theirs.IsBinary || Tools.IsBinaryFileData(fullPath + ".ours") || Tools.IsBinaryFileData(fullPath + ".theirs"))// TODO
			{
				// open merge tool
				/*MainWindow.CanInteractWithUI(false);
				var mergeBinaryFileWindow = new MergeBinaryFileWindow(fileState.filename);
				mergeBinaryFileWindow.Owner = MainWindow.singleton;
				mergeBinaryFileWindow.Show();
				await mergeBinaryFileWindow.WaitForClose();
				MainWindow.CanInteractWithUI(true);
				if (mergeBinaryFileWindow.result == MergeBinaryResults.Cancel) return false;

				// copy selected
				if (mergeBinaryFileWindow.result == MergeBinaryResults.KeepMine) File.Copy(fullPath + ".ours", fullPath, true);
				else if (mergeBinaryFileWindow.result == MergeBinaryResults.UserTheirs) File.Copy(fullPath + ".theirs", fullPath, true);

				RepoUserControl.repo.Stage(fileState.filename);*/

				// delete temp files
				if (File.Exists(fullPath + ".base")) File.Delete(fullPath + ".base");
				if (File.Exists(fullPath + ".ours")) File.Delete(fullPath + ".ours");
				if (File.Exists(fullPath + ".theirs")) File.Delete(fullPath + ".theirs");

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

					// delete temp files
					if (File.Exists(fullPath + ".base")) File.Delete(fullPath + ".base");
					if (File.Exists(fullPath + ".ours")) File.Delete(fullPath + ".ours");
					if (File.Exists(fullPath + ".theirs")) File.Delete(fullPath + ".theirs");

					return false;
				}

				process.WaitForExit();
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
			bool wasModified = false;
			if (!baseHashChange.SequenceEqual(baseHash))
			{
				wasModified = true;
				File.Copy(fullPath + ".base", fullPath, true);
				RepoManager.repo.Stage(fileState.filename);
			}

			// delete temp files
			if (File.Exists(fullPath + ".base")) File.Delete(fullPath + ".base");
			if (File.Exists(fullPath + ".ours")) File.Delete(fullPath + ".ours");
			if (File.Exists(fullPath + ".theirs")) File.Delete(fullPath + ".theirs");

			// check if user accepts the current state of the merge
			/*if (!wasModified && MessageBox.Show("No changes detected. Accept as merged?", "Accept Merge?", MessageBoxButton.YesNo) == MessageBoxResult.Yes)// TODO
			{
				RepoManager.repo.Stage(fileState.filename);
				wasModified = true;
			}*/

			// finish
			if (refresh) RepoManager.Refresh();
			return wasModified;
		}

		public static bool OpenDiffTool(FileState fileState)
		{
			string fullPath = string.Format("{0}\\{1}", RepoManager.repoPath, fileState.filename);

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
				Tools.SaveFileFromID(string.Format("{0}\\{1}.orig", RepoManager.repoPath, fileState.filename), changed.Target.Id);

				// open diff tool
				using (var process = new Process())
				{
					process.StartInfo.FileName = AppManager.mergeToolPath;
					process.StartInfo.Arguments = string.Format("\"{0}.orig\" \"{0}\"", fullPath);
					process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
					if (!process.Start())
					{
						Debug.LogError("Failed to start Diff tool (is it installed?)", true);

						// delete temp files
						if (File.Exists(fullPath + ".orig")) File.Delete(fullPath + ".orig");
						return false;
					}

					process.WaitForExit();
				}

				// delete temp files
				if (File.Exists(fullPath + ".orig")) File.Delete(fullPath + ".orig");
			}
			catch (Exception ex)
			{
				if (File.Exists(fullPath + ".orig")) File.Delete(fullPath + ".orig");
				Debug.LogError("Failed to start Diff tool: " + ex.Message, true);
			}

			return true;
		}
	}
}
