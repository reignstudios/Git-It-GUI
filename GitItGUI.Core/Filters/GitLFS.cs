using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GitItGUI.Core.Filters
{
	class GitLFS : Filter
	{
		public static StatusUpdateCallbackMethod statusCallback;
		private Process process;
		private FilterMode mode;

		public GitLFS(string name, IEnumerable<FilterAttributeEntry> attributes) : base(name, attributes)
		{
		}

		protected override void Clean(string path, string root, Stream input, Stream output)
		{
			if (!RepoManager.lfsEnabled)
			{
				base.Clean(path, root, input, output);
				return;
			}

			try
			{
				// write file data to stdin
				input.CopyTo(process.StandardInput.BaseStream);
				input.Flush();
			}
			catch (Exception e)
			{
				Debug.LogError("LFS Clean Error: " + e.Message, true);
			}
		}

		protected override void Complete(string path, string root, Stream output)
		{
			if (!RepoManager.lfsEnabled)
			{
				base.Complete(path, root, output);
				return;
			}

			try
			{
				// finalize stdin and wait for git-lfs to finish
				process.StandardInput.Flush();
				process.StandardInput.Close();
				bool dataWriten = false;
				while (!process.WaitForExit(100) || !dataWriten)
				{
					process.StandardOutput.BaseStream.CopyTo(output);
					process.StandardOutput.BaseStream.Flush();
					dataWriten = true;
				}

				// write git-lfs pointer for 'clean' to git or file data for 'smudge' to working copy
				process.StandardOutput.Close();
				output.Flush();
				output.Close();

				// finish
				process.WaitForExit();
				process.Dispose();
			}
			catch (Exception e)
			{
				Debug.LogError("LFS Complete Error: " + e.Message, true);
			}
		}

		protected override void Create(string path, string root, FilterMode mode)
		{
			if (!RepoManager.lfsEnabled)
			{
				base.Create(path, root, mode);
				return;
			}

			if (statusCallback != null) statusCallback(string.Format("Processing file '{0}' with filter '{1}'", path, mode));
			this.mode = mode;
			try
			{
				// launch git-lfs
				process = new Process();
				process.StartInfo.FileName = "git-lfs";
				process.StartInfo.Arguments = string.Format("{0} \"{1}\"", mode == FilterMode.Clean ? "clean" : "smudge", path);
				process.StartInfo.WorkingDirectory = RepoManager.repoPath;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.ErrorDataReceived += Process_ErrorDataReceived;
				process.Start();

			}
			catch (Exception e)
			{
				Debug.LogError("LFS Create Error: " + e.Message, true);
			}
		}

		private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data)) Debug.LogError(string.Format("LFS {0} Error: {1}", mode, e.Data), true);
		}

		protected override void Initialize()
		{
			base.Initialize();
		}

		protected override void Smudge(string path, string root, Stream input, Stream output)
		{
			if (!RepoManager.lfsEnabled)
			{
				base.Smudge(path, root, input, output);
				return;
			}

			try
			{
				// write git-lfs pointer to stdin
				input.CopyTo(process.StandardInput.BaseStream);
				input.Flush();
			}
			catch (Exception e)
			{
				Debug.LogError("LFS Smudge Error: " + e.Message, true);
			}
		}
	}
}
