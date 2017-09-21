using GitCommander;
using GitItGUI.Core;
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

		public RepoScreen()
        {
			singleton = this;
            InitializeComponent();
			grid.Visibility = Visibility.Hidden;
		}

		public void Init()
		{
			repoManager = new RepoManager(MainWindow.singleton.Dispatcher, RepoReadyCallback);
			repoManager.RepoRefreshedCallback += Refresh;
			changesTab.Init();
		}

		private void RepoReadyCallback(Dispatcher dispatcher)
		{
			repoDispatcher = dispatcher;
			grid.Visibility = Visibility.Visible;
		}

		public void Dispose()
		{
			repoManager.Dispose();
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
						string upToDateMsg = "ERROR";
						if (repoManager.ChangesExist())
						{
							upToDateMsg = "Out of date";
						}
						else
						{
							bool upToDatePass = repoManager.IsUpToDateWithRemote(out bool isUpToDate);
							upToDateMsg = upToDatePass ? (isUpToDate ? "Up to date" : "Out of date") : "In sync check error";
						}

						repoTitleLabel.Content = string.Format("Current Repo ({0}) [{1}]", repoManager.activeBranch.fullname, upToDateMsg);
						MainWindow.singleton.Navigate(this);
					});
				}
				else
				{
					AppManager.RemoveRepoFromHistory(folderPath);
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to open repo");
				}

				StartScreen.singleton.Refresh();
				MainWindow.singleton.HideProcessingOverlay();
			});
		}
		
		public void Refresh()
		{
			changesTab.Refresh();
			branchesTab.Refresh();
		}

		private void backButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.Navigate(StartScreen.singleton);
		}

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			repoManager.dispatcher.InvokeAsync(delegate()
			{
				repoManager.Refresh();
				MainWindow.singleton.HideProcessingOverlay();
			});
		}
	}
}
