using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public string filePath;
		public FileStates status;

		public FileState(string filename, FileStates status)
		{
			this.filePath = filename;
			this.status = status;
		}
	}

	public static class ChangesManager
	{
		private static List<FileState> fileStates;

		internal static void Refresh()
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
			}
			catch (Exception e)
			{
				Debug.LogError("Failed to update file status: " + e.Message, true);
				fileStates = null;
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
				//foreach (var item in RepoManager.repo.RetrieveStatus())
				{
					//if (item.FilePath != filename) continue;
					//var state = item.State;

					// check if file still exists
					string fullPath = RepoManager.repoPath + "\\" + fileState.filePath;
					if (!File.Exists(fullPath))
					{
						return "<< File Doesn't Exist >>";
					}

					// if new file just grab local data
					//if ((state & FileStatus.NewInWorkdir) != 0 || (state & FileStatus.NewInIndex) != 0 || (state & FileStatus.Conflicted) != 0)
					if (fileState.status == FileStates.NewInWorkdir || fileState.status == FileStates.NewInIndex || fileState.status == FileStates.Conflicted)
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
					var file = RepoManager.repo.Index[fileState.filePath];
					var blob = RepoManager.repo.Lookup<Blob>(file.Id);
					if (blob.IsBinary || Tools.IsBinaryFileData(fullPath))
					{
						return "<< Binary File >>";
					}

					// check for text types
					//if ((state & FileStatus.ModifiedInWorkdir) != 0)
					if (fileState.status == FileStates.ModifiedInWorkdir)
					{
						//var patch = RepoUserControl.repo.Diff.Compare<TreeChanges>(new List<string>(){item.FilePath});// use this for details about change
						var patch = RepoManager.repo.Diff.Compare<Patch>(new List<string>(){fileState.filePath});

						string content = patch.Content;

						var match = Regex.Match(content, @"@@.*?(@@).*?\n(.*)", RegexOptions.Singleline);
						if (match.Success && match.Groups.Count == 3) content = match.Groups[2].Value.Replace("\\ No newline at end of file\n", "");

						// remove meta data stage 2
						bool search = true;
						while (search)
						{
							patch = RepoManager.repo.Diff.Compare<Patch>(new List<string>() {fileState.filePath});
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
					//else if ((state & FileStatus.ModifiedInIndex) != 0 ||
					//	(state & FileStatus.DeletedFromWorkdir) != 0 || (state & FileStatus.DeletedFromIndex) != 0 ||
					//	(state & FileStatus.RenamedInWorkdir) != 0 || (state & FileStatus.RenamedInIndex) != 0 ||
					//	(state & FileStatus.TypeChangeInWorkdir) != 0 || (state & FileStatus.TypeChangeInIndex) != 0)
					else if (fileState.status == FileStates.ModifiedInIndex ||
						fileState.status == FileStates.DeletedFromWorkdir || fileState.status == FileStates.DeletedFromIndex ||
						fileState.status == FileStates.RenamedInWorkdir || fileState.status == FileStates.RenamedInIndex ||
						fileState.status == FileStates.TypeChangeInWorkdir || fileState.status == FileStates.TypeChangeInIndex)
					{
						return blob.GetContentText();
					}
					else
					{
						Debug.LogError("Unsuported FileStatus: " + fileState.filePath, true);
						return null;
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Failed to refresh quick view: " + ex.Message, true);
			}

			return null;
		}
	}
}
