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
		Sync
	}

	public class ProcessingPage : UserControl, NavigationPage
	{
		public static ProcessingPage singleton;

		public ProcessingPageModes mode = ProcessingPageModes.None;
		public string clonePath, cloneURL, cloneUsername, clonePassword;
		public bool cloneSucceeded;

		private int askToOptamizeSync;
		private Thread thread;

		public ProcessingPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}

		public void NavigatedFrom()
		{
			
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

			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
