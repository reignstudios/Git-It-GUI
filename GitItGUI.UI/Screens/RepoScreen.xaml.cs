using GitCommander;
using GitItGUI.Core;
using GitItGUI.UI.Overlays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GitItGUI.UI.Screens
{
    /// <summary>
    /// Interaction logic for RepoUserControl.xaml
    /// </summary>
    public partial class RepoScreen : UserControl
    {
		public static RepoScreen singleton;
		public RepoManager repoManager;
		public Dispatcher repoDispatcher;

		private TabItem lastTabItem;

		public RepoScreen()
        {
			singleton = this;
            InitializeComponent();
			grid.Visibility = Visibility.Hidden;
		}

		public void Init()
		{
			repoManager = new RepoManager(RepoReadyCallback);
			repoManager.RepoRefreshedCallback += repoManager_RefreshedCallback;
			changesTab.Init();
		}

		private void RepoReadyCallback(Dispatcher dispatcher)
		{
			repoDispatcher = dispatcher;
			Dispatcher.InvokeAsync(delegate()
			{
				grid.Visibility = Visibility.Visible;
			});
		}

		public void Dispose()
		{
			repoManager.Dispose();
		}

		private void CheckSync()
		{
			string upToDateMsg = "ERROR";
			if (repoManager.ChangesExist()) upToDateMsg = "Out of date";
			else upToDateMsg = repoManager.isInSync != null ? (repoManager.isInSync.Value ? "Up to date" : "Out of date") : "In sync check error";
			repoTitleLabel.Content = string.Format("Current Repo '{0}' ({1}) [{2}]", System.IO.Path.GetFileName(repoManager.repository.repoPath), repoManager.activeBranch.fullname, upToDateMsg);
		}

		public void OpenRepo(string folderPath)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (repoManager.OpenRepo(folderPath))
				{
					MainWindow.singleton.Dispatcher.InvokeAsync(delegate()
					{
						// prep
						CheckSync();
						tabControl.SelectedIndex = 0;
						MainWindow.singleton.Navigate(this);

						// check repo fragmentation
						if (!repoManager.ChangesExist())
						{
							int count = repoManager.UnpackedObjectCount(out string size);
							if (count >= 1000)
							{
								MainWindow.singleton.ShowMessageOverlay("Optamize", string.Format("Your repo is fragmented, would you like to optamize?\nThere are '{0}' loose objects totalling '{1}' in size", count, size), MessageOverlayTypes.OkCancel, delegate(MessageOverlayResults result)
								{
									if (result == MessageOverlayResults.Ok)
									{
										MainWindow.singleton.ShowProcessingOverlay();
										repoManager.dispatcher.InvokeAsync(delegate()
										{
											repoManager.Optimize();
											MainWindow.singleton.HideProcessingOverlay();
										});
									}
								});
							}
						}
					});
				}
				else
				{
					AppManager.RemoveRepoFromHistory(folderPath);
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to open repo");
				}
				
				MainWindow.singleton.HideProcessingOverlay();
			});
		}
		
		private void repoManager_RefreshedCallback()
		{
			void RefreshInternal()
			{
				if (!repoManager.isOpen) return;
				changesTab.Refresh();
				branchesTab.Refresh();
				settingsTab.Refresh();
				terminalTab.Refresh();
				CheckSync();
			}

			if (Dispatcher.CheckAccess())
			{
				RefreshInternal();
			}
			else
			{
				Dispatcher.InvokeAsync(delegate()
				{
					RefreshInternal();
				});
			}
		}

		public void Refresh()
		{
			void Invoke()
			{
				MainWindow.singleton.ShowProcessingOverlay();
				repoManager.dispatcher.InvokeAsync(delegate()
				{
					if (!repoManager.Refresh())
					{
						MainWindow.singleton.ShowMessageOverlay("Error", "Failed to refresh repo");
						MainWindow.singleton.Navigate(StartScreen.singleton);
					}

					MainWindow.singleton.HideProcessingOverlay();
				});
			}

			if (repoManager != null && repoManager.isOpen)
			{
				if (repoManager.dispatcher.CheckAccess())
				{
					Invoke();
				}
				else
				{
					repoManager.dispatcher.InvokeAsync(delegate()
					{
						Invoke();
					});
				}
			}
		}

		private void backButton_Click(object sender, RoutedEventArgs e)
		{
			repoManager.Close();
			MainWindow.singleton.Navigate(StartScreen.singleton);
		}

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selected = (TabItem)tabControl.SelectedItem;
			if (selected == terminalTabItem) terminalTab.ScrollToEnd();
			else if (lastTabItem == terminalTabItem) terminalTab.CheckRefreshPending();
			lastTabItem = selected;
		}
	}
}
