using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;

namespace GitItGUI
{
	public enum ProcessingPageModes
	{
		None,
		Clone,
		Pull,
		Push,
		Sync,
		Merge,
		Switch
	}

	public class ProcessingPage : UserControl, NavigationPage
	{
		public static ProcessingPage singleton;

		public ProcessingPageModes mode = ProcessingPageModes.None;
		public string clonePath, cloneURL, cloneUsername, clonePassword;
		public bool cloneSucceeded;

		public BranchState mergeOtherBranch;
		public BranchState switchOtherBranch;

		private int askToOptamizeSync;
		private Thread thread;

		public ProcessingPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}

		public void NavigatedFrom()
		{
			mode = ProcessingPageModes.None;
		}

		public async void NavigatedTo()
		{
			await Task.Delay(500);
			thread = new Thread(Process);
			thread.Start();
		}

		private void Process()
		{
			if (mode == ProcessingPageModes.Pull) ChangesManager.Pull();
			else if (mode == ProcessingPageModes.Push) ChangesManager.Push();
			else if (mode == ProcessingPageModes.Sync)
			{
				if (ChangesManager.Sync())
				{
					if (askToOptamizeSync == 0 && MessageBox.Show("Would you like to run git optimizers?", MessageBoxTypes.YesNo)) RepoManager.Optimize();
					++askToOptamizeSync;
					if (askToOptamizeSync == 10) askToOptamizeSync = 0;
				}
			}
			else if (mode == ProcessingPageModes.Clone)
			{
				// clone repo
				cloneSucceeded = RepoManager.Clone(cloneURL, clonePath, cloneUsername, clonePassword, out clonePath);
				if (!cloneSucceeded)
				{
					MessageBox.Show("Failed to clone repo: " + clonePath);
					MainWindow.LoadPage(PageTypes.Clone);
					return;
				}

				// open repo
				if (!RepoManager.OpenRepo(clonePath))
				{
					MessageBox.Show("Failed to open repo: " + clonePath);
					MainWindow.LoadPage(PageTypes.Start);
					return;
				}

				// update credentials
				RepoManager.UpdateCredentialValues(cloneUsername, clonePassword);
				RepoManager.SaveSettings();
				RepoManager.Refresh();
			}
			else if (mode == ProcessingPageModes.Merge)
			{
				var result = BranchManager.MergeBranchIntoActive(mergeOtherBranch);
				if (result == MergeResults.Succeeded)
				{
					MessageBox.Show("Merge Succedded!\n(Remember to sync with the server!)");
				}
				else if (result == MergeResults.Conflicts && MessageBox.Show("Conflicts detected! Resolve now?", MessageBoxTypes.YesNo))
				{
					ChangesManager.ResolveAllConflicts();
				}
			}
			else if (mode == ProcessingPageModes.Switch)
			{
				if (!switchOtherBranch.isRemote) BranchManager.Checkout(switchOtherBranch);
				else if (MessageBox.Show("Cannot checkout to remote branch.\nDo you want to create a local one that tracks this remote instead?", MessageBoxTypes.YesNo))
				{
					string fullName = switchOtherBranch.branchName;
					if (BranchManager.AddNewBranch(fullName))
					{
						BranchManager.Checkout(fullName);
						BranchManager.AddUpdateTracking(switchOtherBranch.fullName);
					}
				}
			}
			else
			{
				MessageBox.Show("Unsuported Processing mode: " + mode);
			}

			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
