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

namespace GitItGUI.UI.Screens.RepoTabs
{
    /// <summary>
    /// Interaction logic for BranchesTab.xaml
    /// </summary>
    public partial class BranchesTab : UserControl
    {
        public BranchesTab()
        {
            InitializeComponent();
        }

		public void Refresh()
		{
			var repoManager = RepoScreen.singleton.repoManager;

			// update active branch
			branchNameTextBox.Text = repoManager.activeBranch.fullname;
			if (repoManager.activeBranch.isTracking)
			{
				trackedRemoteBranchTextBox.Text = repoManager.activeBranch.tracking.fullname;
				trackedRemoteBranchTextBox.Text = repoManager.activeBranch.tracking.remoteState.url;
			}
			else if (repoManager.activeBranch.isRemote)
			{
				trackedRemoteBranchTextBox.Text = repoManager.activeBranch.remoteState.url;
			}

			// update non-active branch list
			nonActiveBranchesListBox.Items.Clear();
			foreach (var branch in repoManager.branchStates)
			{
				if (branch.isActive) continue;

				var item = new ListBoxItem();
				item.Tag = branch;
				if (branch.isTracking) item.ToolTip = "Tracking: " + branch.tracking.fullname;
				item.Content = string.Format("{0} [{1}]", branch.fullname, branch.isRemote ? "REMOTE" : "LOCAL");
				nonActiveBranchesListBox.Items.Add(item);
			}
		}

		private void ToolButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			button.ContextMenu.IsOpen = true;
		}

		private void renameMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowNameEntryOverlay(RepoScreen.singleton.repoManager.activeBranch.name, delegate(string name, bool succeeded)
			{
				if (!succeeded) return;
				MainWindow.singleton.ShowProcessingOverlay();
				RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
				{
					if (!RepoScreen.singleton.repoManager.RenameActiveBranch(name)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage file");
					MainWindow.singleton.HideProcessingOverlay();
				});
			});
		}

		private void newBranchMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void copyTrackingMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void removeTrackingMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void switchBranchMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void mergeBranchMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void deleteBranchMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}

		private void cleanupMenuItem_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
