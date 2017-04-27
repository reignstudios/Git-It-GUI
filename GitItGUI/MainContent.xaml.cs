using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;
using System.IO;
using Avalonia.Interactivity;
using System.Threading;
using Avalonia.Threading;
using GitCommander;

namespace GitItGUI
{
	public delegate void MainContentPageNavigateMethod();

	public class MainContent : UserControl, NavigationPage
	{
		public static MainContent singleton;
		public event MainContentPageNavigateMethod MainContentPageNavigatedTo, MainContentPageNavigateFrom;

		private TextBlock repoName, refreshTextBlock;
		private Button closeRepoButton;
		private TabControl tabControl;
		public int tabControlNavigateIndex = -1;

		public MainContent()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			repoName = this.Find<TextBlock>("repoName");
			closeRepoButton = this.Find<Button>("closeRepoButton");
			tabControl = this.Find<TabControl>("tabControl");
			refreshTextBlock = this.Find<TextBlock>("refreshTextBlock");
			refreshTextBlock.IsVisible = false;

			closeRepoButton.Click += CloseRepoButton_Click;
			RepoManager.RepoRefreshedCallback += RepoManager_RepoRefreshedCallback;
			RepoManager.RepoRefreshingCallback += RepoManager_RepoRefreshingCallback;
		}

		private void CloseRepoButton_Click(object sender, RoutedEventArgs e)
		{
			RepoManager.Close();
			MainWindow.LoadPage(PageTypes.Start);
		}

		private void RepoManager_RepoRefreshedCallback()
		{
			// check if repo has anything commited
			if (BranchManager.isEmpty)
			{
				bool pass = false;
				if (MessageBox.Show("This is an emtpy repo, a 'README.md' file will be auto commit.\nThis is required to put the repo in a usable state.", MessageBoxTypes.OkCancel))
				{
					const string readme = "README.mb";
					string readmePath = Repository.repoPath + '\\' + readme;
					if (!File.Exists(readmePath))
					{
						using (var writer = File.CreateText(readmePath))
						{
							writer.Write("TODO");
						}
					}

					pass = Repository.Stage(readme);
					if (pass) pass = Repository.Commit("First Commit!\nAdded readme file!");
				}
				
				if (pass)
				{
					RepoManager.Refresh();
					return;
				}
				else
				{
					RepoManager.Close();
					MainWindow.LoadPage(PageTypes.Start);
					return;
				}
			}

			// get status
			string name = Repository.repoPath;
			string text;
			if (!string.IsNullOrEmpty(name))
			{
				string syncText = "";
				bool yes;
				if (ChangesManager.ChangesExist()) syncText = " - [changes exist]";
				else if (BranchManager.IsUpToDateWithRemote(out yes)) syncText = yes ? "" : " - [out of sync]";
				else syncText = " - [sync check error]";
				text = string.Format("{0} ({1}){2}", name.Substring(Path.GetDirectoryName(name).Length + 1), BranchManager.activeBranch.fullname, syncText);
			}
			else
			{
				text = "";
			}

			// set status
			if (Dispatcher.UIThread.CheckAccess())
			{
				repoName.Text = text;
			}
			else
			{
				bool isDone = false;
				Dispatcher.UIThread.InvokeAsync(delegate
				{
					repoName.Text = text;
					isDone = true;
				});

				while (!isDone) Thread.Sleep(1);
			}
		}

		private void RepoManager_RepoRefreshingCallback(bool start)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				repoName.IsVisible = !start;
				closeRepoButton.IsVisible = !start;
				tabControl.IsVisible = !start;
				refreshTextBlock.IsVisible = start;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate
				{
					repoName.IsVisible = !start;
					closeRepoButton.IsVisible = !start;
					tabControl.IsVisible = !start;
					refreshTextBlock.IsVisible = start;
				});
			}
		}

		public void NavigatedFrom()
		{
			if (MainContentPageNavigateFrom != null) MainContentPageNavigateFrom();
		}

		public void NavigatedTo()
		{
			if (tabControlNavigateIndex != -1) tabControl.SelectedIndex = tabControlNavigateIndex;
			tabControlNavigateIndex = -1;
			if (MainContentPageNavigatedTo != null) MainContentPageNavigatedTo();
		}
	}
}
