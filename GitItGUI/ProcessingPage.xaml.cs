using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;
using System.Threading.Tasks;
using System.Threading;
using Avalonia.Threading;
using System.Linq;
using GitCommander;
using System;
using System.IO;

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
		public static volatile bool isActive;

		// clone
		public ProcessingPageModes mode = ProcessingPageModes.None;
		public string clonePath, cloneURL;
		public bool cloneSucceeded;

		// branch
		public BranchState mergeOtherBranch;
		public BranchState switchOtherBranch;
		public bool fetchBeforeMerge;

		// inverface
		private Thread thread;
		private TextBox statusTextBox;

		public ProcessingPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui
			statusTextBox = this.Find<TextBox>("statusTextBox");

			// bind events
			GitCommander.Tools.StdCallback += Tools_StdCallback;
			GitCommander.Tools.StdWarningCallback += Tools_StdCallback;
			GitCommander.Tools.StdErrorCallback += Tools_StdCallback;
		}

		private void Tools_StdCallback(string line)
		{
			if (mode != ProcessingPageModes.None) StatusUpdateCallback(line);
		}

		public void NavigatedFrom()
		{
			mode = ProcessingPageModes.None;
			isActive = false;
		}

		public async void NavigatedTo()
		{
			isActive = true;
			statusTextBox.Text = "Waiting...";
			await Task.Delay(500);
			thread = new Thread(Process);
			thread.Start();
		}

		private void StatusUpdateCallback(string status)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				statusTextBox.Text = status;
			}
			else
			{
				bool isDone = false;
				Dispatcher.UIThread.InvokeAsync(delegate
				{
					statusTextBox.Text = status;
					isDone = true;
				});

				while (!isDone) Thread.Sleep(1);
			}
		}

		private void HandleMergeConflicts()
		{
			const string warning = "\nIf you notice extra files in your staged area (its OK),\nthis is common after a merge conflic.";
			const string resolveFailWarning = "Please resolve conflicts then sync your changes with the server!" + warning;
			if (MessageBox.Show("Conflicts detected! Resolve now?", MessageBoxTypes.YesNo))
			{
				if (ChangesManager.ResolveAllConflicts()) MessageBox.Show("Now sync your changes with the server!" + warning);
				else MessageBox.Show(resolveFailWarning);
			}
			else
			{
				MessageBox.Show(resolveFailWarning);
			}

			MainContent.singleton.tabControlNavigateIndex = 0;
		}

		private void CheckForFragmentation()
		{
			string size;
			int count = RepoManager.UnpackedObjectCount(out size);
			if (count >= 1000 && MessageBox.Show(string.Format("Would you like to run git optimizers?\nYou have {0} from {1} unpacked files.\nThis can take over 10 sec to complete!", size, count), MessageBoxTypes.YesNo))
			{
				RepoManager.Optimize();
			}
		}

		private void Process()
		{
			// pull
			if (mode == ProcessingPageModes.Pull)
			{
				var result = ChangesManager.Pull();
				if (result == SyncMergeResults.Succeeded)
				{
					CheckForFragmentation();
				}
				else if (result == SyncMergeResults.Conflicts)
				{
					HandleMergeConflicts();
				}
			}

			// push
			else if (mode == ProcessingPageModes.Push)
			{
				if (ChangesManager.Push())
				{
					CheckForFragmentation();
				}
			}

			// sync
			else if (mode == ProcessingPageModes.Sync)
			{
				var result = ChangesManager.Sync();
				if (result == SyncMergeResults.Succeeded)
				{
					CheckForFragmentation();
				}
				else if (result == SyncMergeResults.Conflicts)
				{
					HandleMergeConflicts();
				}
			}

			// clone
			else if (mode == ProcessingPageModes.Clone)
			{
				var writeUsernameCallback = new StdInputStreamCallbackMethod(delegate(StreamWriter writer)
				{
					string username;
					if (Tools.CoreApps.LaunchNameEntry("Enter Username", false, out username))
					{
						writer.WriteLine(username);
						return true;
					}

					return false;
				});

				var writePasswordCallback = new StdInputStreamCallbackMethod(delegate(StreamWriter writer)
				{
					string username;
					if (Tools.CoreApps.LaunchNameEntry("Enter Password", true, out username))
					{
						writer.WriteLine(username);
						return true;
					}

					return false;
				});

				// clone repo
				cloneSucceeded = RepoManager.Clone(cloneURL, clonePath, out clonePath, writeUsernameCallback, writePasswordCallback);
				if (!cloneSucceeded)
				{
					MessageBox.Show("Failed to clone repo: " + clonePath);
					MainWindow.LoadPage(PageTypes.Clone);
					return;
				}

				// open repo
				if (!RepoManager.OpenRepo(clonePath, true))
				{
					MessageBox.Show("Failed to open repo: " + clonePath);
					MainWindow.LoadPage(PageTypes.Start);
					return;
				}
			}

			// merge
			else if (mode == ProcessingPageModes.Merge)
			{
				if (fetchBeforeMerge && !ChangesManager.Fetch(mergeOtherBranch))
				{
					MessageBox.Show("Failed to fetch!");
					MainWindow.LoadPage(PageTypes.MainContent);
					return;
				}

				var result = BranchManager.MergeBranchIntoActive(mergeOtherBranch);
				if (result == MergeResults.Succeeded)
				{
					MessageBox.Show("Merge Succedded!\n(Remember to sync with the server!)");
					MainContent.singleton.tabControlNavigateIndex = 0;
					MainWindow.LoadPage(PageTypes.MainContent);
					return;
				}
				else if (result == MergeResults.Conflicts)
				{
					HandleMergeConflicts();
				}
			}

			// switch
			else if (mode == ProcessingPageModes.Switch)
			{
				if (!switchOtherBranch.isRemote) BranchManager.Checkout(switchOtherBranch);
				else if (MessageBox.Show("Cannot checkout to remote branch.\nDo you want to create a local one that tracks this remote instead?", MessageBoxTypes.YesNo))
				{
					if (Array.Exists<BranchState>(BranchManager.branchStates, x => x.fullname == switchOtherBranch.name))
					{
						MessageBox.Show(string.Format("A local branch under '{0}' already exists, checking out to it instead.", switchOtherBranch.name));
						BranchManager.Checkout(Array.Find<BranchState>(BranchManager.branchStates, x => x.fullname == switchOtherBranch.name));
					}
					else
					{
						BranchManager.Checkout(switchOtherBranch);
					}
				}
			}

			// error
			else
			{
				MessageBox.Show("Unsuported Processing mode: " + mode);
			}

			// finish
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
