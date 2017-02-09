using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;
using System.Threading.Tasks;

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
			await Task.Delay(1000);

			if (mode == ProcessingPageModes.Pull) ChangesManager.Pull();
			else if (mode == ProcessingPageModes.Push) ChangesManager.Push();
			else if (mode == ProcessingPageModes.Sync) ChangesManager.Sync();
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

				// load main repo page
				MainWindow.LoadPage(PageTypes.MainContent);
				return;
			}

			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
