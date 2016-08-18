using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GitItGUI.Filters
{
	public class GitLFS : Filter
	{
		private Process process;
		private FilterMode mode;

		public GitLFS(string name, IEnumerable<FilterAttributeEntry> attributes) : base(name, attributes)
		{
		}

		protected override void Clean(string path, string root, Stream input, Stream output)
		{
			if (!RepoPage.repoSettings.lfsSupport)
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
				MessageBox.Show("LFS Clean Error: " + e.Message);
			}
		}

		protected override void Complete(string path, string root, Stream output)
		{
			if (!RepoPage.repoSettings.lfsSupport)
			{
				base.Complete(path, root, output);
				return;
			}

			try
			{
				// finalize stdin and wait for git-lfs to finish
				process.StandardInput.Flush();
				process.StandardInput.Close();
				if (mode == FilterMode.Clean)
				{
					process.WaitForExit();

					// write git-lfs pointer for 'clean' to git or file data for 'smudge' to working copy
					process.StandardOutput.BaseStream.CopyTo(output);
					process.StandardOutput.BaseStream.Flush();
					process.StandardOutput.Close();
					output.Flush();
					output.Close();
				}
				else if (mode == FilterMode.Smudge)
				{
					// write git-lfs pointer for 'clean' to git or file data for 'smudge' to working copy
					process.StandardOutput.BaseStream.CopyTo(output);
					process.StandardOutput.BaseStream.Flush();
					process.StandardOutput.Close();
					output.Flush();
					output.Close();

					process.WaitForExit();
				}

				process.Dispose();
			}
			catch (Exception e)
			{
				MessageBox.Show("LFS Complete Error: " + e.Message);
			}
		}

		protected override void Create(string path, string root, FilterMode mode)
		{
			if (!RepoPage.repoSettings.lfsSupport)
			{
				base.Create(path, root, mode);
				return;
			}

			this.mode = mode;
			try
			{
				// launch git-lfs
				process = new Process();
				process.StartInfo.FileName = "git-lfs";
				process.StartInfo.Arguments = mode == FilterMode.Clean ? "clean" : "smudge";
				process.StartInfo.WorkingDirectory = RepoPage.repoPath;
				process.StartInfo.RedirectStandardInput = true;
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;

				process.Start();
			}
			catch (Exception e)
			{
				MessageBox.Show("LFS Create Error: " + e.Message);
			}
		}

		protected override void Initialize()
		{
			base.Initialize();
		}

		protected override void Smudge(string path, string root, Stream input, Stream output)
		{
			if (!RepoPage.repoSettings.lfsSupport)
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
				MessageBox.Show("LFS Smudge Error: " + e.Message);
			}
		}
	}
}
