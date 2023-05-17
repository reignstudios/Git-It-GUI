using GitCommander;
using GitItGUI.Core;
using GitItGUI.UI.Overlays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;

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
				remoteURLTextBox.Text = repoManager.activeBranch.tracking.remoteState.url;
			}
			else if (repoManager.activeBranch.isRemote)
			{
				trackedRemoteBranchTextBox.Text = string.Empty;
				remoteURLTextBox.Text = repoManager.activeBranch.remoteState.url;
			}
			else
			{
				trackedRemoteBranchTextBox.Text = string.Empty;
				remoteURLTextBox.Text = string.Empty;
			}

			// update non-active branch list
			nonActiveBranchesListBox.Items.Clear();
			foreach (var branch in repoManager.branchStates)
			{
				if (branch.isActive) continue;

				var item = new ListBoxItem();
				item.Tag = branch;
				//if (branch.isTracking) item.ToolTip = "Tracking: " + branch.tracking.fullname;
				item.Content = string.Format("{0} [{1}]", branch.fullname, branch.isRemote ? "REMOTE" : "LOCAL");
				nonActiveBranchesListBox.Items.Add(item);
				item.ContextMenu = new ContextMenu();

				// switch context menu
				var switchFileMenu = new MenuItem();
				switchFileMenu.Tag = branch;
				switchFileMenu.Header = string.Format("Switch to '{0}'", branch.fullname);
				switchFileMenu.Click += SwitchFileMenu_Click;
				nonActiveBranchesListBox.Items.Add(switchFileMenu);

				// merge context menu
				var mergeFileMenu = new MenuItem();
				mergeFileMenu.Tag = branch;
				mergeFileMenu.Header = string.Format("Merge '{0}' into '{1}'", branch.fullname, repoManager.activeBranch.name);
				mergeFileMenu.Click += MergeFileMenu_Click;
				nonActiveBranchesListBox.Items.Add(mergeFileMenu);
				nonActiveBranchesListBox.Items.Add(new Separator());

				// rename context menu
				if (!branch.isRemote)
				{
					var renameFileMenu = new MenuItem();
					renameFileMenu.Tag = branch;
					renameFileMenu.Header = string.Format("Rename '{0}'", branch.fullname);
					renameFileMenu.Click += RenameFileMenu_Click;
					nonActiveBranchesListBox.Items.Add(renameFileMenu);
				}

				// delete context menu
				var deleteFileMenu = new MenuItem();
				deleteFileMenu.Tag = branch;
				deleteFileMenu.Header = string.Format("Delete '{0}'", branch.fullname);
				deleteFileMenu.Click += DeleteFileMenu_Click;
				nonActiveBranchesListBox.Items.Add(deleteFileMenu);
			}
		}

		private void ToolButton_Click(object sender, RoutedEventArgs e)
		{
			//var button = (Button)sender;
			//button.ContextMenu.IsOpen = true;
		}

		private void renameMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowNameEntryOverlay(RepoScreen.singleton.repoManager.activeBranch.name, false, delegate(string name, string remoteName, bool succeeded)
			//{
			//	if (!succeeded) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!RepoScreen.singleton.repoManager.RenameActiveBranch(name)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to rename branch");
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void newBranchMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowNameEntryOverlay(RepoScreen.singleton.repoManager.activeBranch.name, true, delegate(string name, string remoteName, bool succeeded)
			//{
			//	if (!succeeded) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!RepoScreen.singleton.repoManager.CheckoutNewBranch(name, remoteName)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to create new branch");
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void copyTrackingMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//if (nonActiveBranchesListBox.SelectedIndex == -1)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "You must select a non-active branch");
			//	return;
			//}

			//var branch = ((BranchState)((ListBoxItem)nonActiveBranchesListBox.SelectedItem).Tag);
			//if (!branch.isRemote)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "You must select a non-active 'REMOTE' branch");
			//	return;
			//}

			//MainWindow.singleton.ShowMessageOverlay("Copy Tracking", string.Format("Are you sure you want to copy tracking from branch '{1}' into '{0}'", RepoScreen.singleton.repoManager.activeBranch.fullname, branch.fullname), MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			//{
			//	if (result != MessageOverlayResults.Ok) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!RepoScreen.singleton.repoManager.CopyTracking(branch)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to copy tracking");
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void removeTrackingMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowMessageOverlay("Remove Tracking", string.Format("Are you sure you want to copy tracking from branch '{0}'", RepoScreen.singleton.repoManager.activeBranch.fullname), MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			//{
			//	if (result != MessageOverlayResults.Ok) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!RepoScreen.singleton.repoManager.RemoveTracking()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to remove tracking");
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void SwitchFileMenu_Click(object sender, RoutedEventArgs e)
		{
			//var branch = (BranchState)((MenuItem)sender).Tag;
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//{
			//	if (!RepoScreen.singleton.repoManager.Checkout(branch)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to switch branch");
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void MergeFileMenu_Click(object sender, RoutedEventArgs e)
		{
			//var branch = (BranchState)((MenuItem)sender).Tag;
			//MainWindow.singleton.ShowMessageOverlay("Merge Branches", string.Format("Are you sure you want to merge branch '{1}' into '{0}'", RepoScreen.singleton.repoManager.activeBranch.fullname, branch.fullname), MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			//{
			//	if (result != MessageOverlayResults.Ok) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		var mergeResult = RepoScreen.singleton.repoManager.MergeBranchIntoActive(branch);
			//		if (mergeResult == MergeResults.Conflicts) ChangesTab.singleton.HandleConflics();
			//		else if (mergeResult == MergeResults.Error) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to merge branches");

			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void RenameFileMenu_Click(object sender, RoutedEventArgs e)
		{
			//var branch = (BranchState)((MenuItem)sender).Tag;
			//MainWindow.singleton.ShowNameEntryOverlay(branch.name, false, delegate(string name, string remoteName, bool succeeded)
			//{
			//	if (!succeeded) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!RepoScreen.singleton.repoManager.RenameNonActiveBranch(branch, name)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to rename branch");
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void DeleteFileMenu_Click(object sender, RoutedEventArgs e)
		{
			//var branch = (BranchState)((MenuItem)sender).Tag;
			//if (branch.isRemote)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "You must select a non-active 'LOCAL' branch\nOr run cleanup to remove invalid remotes.");
			//	return;
			//}

			//MainWindow.singleton.ShowMessageOverlay("Delete", string.Format("Are you sure you want to delete branch '{0}'", branch.fullname), MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			//{
			//	if (result != MessageOverlayResults.Ok) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!RepoScreen.singleton.repoManager.DeleteNonActiveBranch(branch)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to delete branch");
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void cleanupMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowMessageOverlay("Cleanup", "Would you like to remove remote branches untracked on your server?", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			//{
			//	if (result != MessageOverlayResults.Ok) return;
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!RepoScreen.singleton.repoManager.PruneRemoteBranches()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to prune remote branches");
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//});
		}

		private void pullMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//ChangesTab.singleton.pullButton_Click(null, null);
		}

		private void pushMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//ChangesTab.singleton.pushButton_Click(null, null);
		}
	}
}
