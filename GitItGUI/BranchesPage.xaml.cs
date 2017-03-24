using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using GitItGUI.Core;
using GitItGUI.Tools;
using Avalonia.Threading;
using System.Threading;
using GitCommander;

namespace GitItGUI
{
	public class BranchesPage : UserControl
	{
		public static BranchesPage singleton;

		// ui objects
		TextBlock trackingLabel, trackedBranchLabel, remoteURLLabel, newBranchLabel;
		TextBox activeBranchTextBox, trackingOriginTextBox, remoteURLTextBox;
		ListBox otherBranchListView;
		Button addBranchButton, renameBranchButton, copyTrackingButton, removeTrackingButton, switchBranchButton, mergeBranchButton, deleteBranchButton;
		CheckBox advancedModeCheckBox;
		DropDown remotesDropDown;

		List<string> otherBranchListViewItems;

		public BranchesPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			activeBranchTextBox = this.Find<TextBox>("activeBranchTextBox");
			trackingOriginTextBox = this.Find<TextBox>("trackingOriginTextBox");
			remoteURLTextBox = this.Find<TextBox>("remoteURLTextBox");
			otherBranchListView = this.Find<ListBox>("otherBranchListView");
			addBranchButton = this.Find<Button>("addBranchButton");
			renameBranchButton = this.Find<Button>("renameBranchButton");
			copyTrackingButton = this.Find<Button>("copyTrackingButton");
			removeTrackingButton = this.Find<Button>("removeTrackingButton");
			switchBranchButton = this.Find<Button>("switchBranchButton");
			mergeBranchButton = this.Find<Button>("mergeBranchButton");
			deleteBranchButton = this.Find<Button>("deleteBranchButton");
			advancedModeCheckBox = this.Find<CheckBox>("advancedModeCheckBox");
			trackingLabel = this.Find<TextBlock>("trackingLabel");
			trackedBranchLabel = this.Find<TextBlock>("trackedBranchLabel");
			remoteURLLabel = this.Find<TextBlock>("remoteURLLabel");
			newBranchLabel = this.Find<TextBlock>("newBranchLabel");
			remotesDropDown = this.Find<DropDown>("remotesDropDown");

			// apply bindings
			otherBranchListViewItems = new List<string>();
			otherBranchListView.Items = otherBranchListViewItems;
			addBranchButton.Click += AddBranchButton_Click;
			renameBranchButton.Click += RenameBranchButton_Click;
			copyTrackingButton.Click += CopyTrackingButton_Click;
			removeTrackingButton.Click += RemoveTrackingButton_Click;
			switchBranchButton.Click += SwitchBranchButton_Click;
			mergeBranchButton.Click += MergeBranchButton_Click;
			deleteBranchButton.Click += DeleteBranchButton_Click;
			advancedModeCheckBox.Click += AdvancedModeCheckBox_Click;

			// bind events
			RepoManager.RepoRefreshedCallback += RepoManager_RepoRefreshedCallback;
		}

		private void AdvancedModeCheckBox_Click(object sender, RoutedEventArgs e)
		{
			RepoManager_RepoRefreshedCallback_UIThread();
		}

		private void RepoManager_RepoRefreshedCallback()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				RepoManager_RepoRefreshedCallback_UIThread();
			}
			else
			{
				bool isDone = false;
				Dispatcher.UIThread.InvokeAsync(delegate
				{
					RepoManager_RepoRefreshedCallback_UIThread();
					isDone = true;
				});

				while (!isDone) Thread.Sleep(1);
			}
		}

		private void RepoManager_RepoRefreshedCallback_UIThread()
		{
			bool isAdvancedMode = advancedModeCheckBox.IsChecked;

			// show/hide advanced options
			addBranchButton.IsVisible = isAdvancedMode;
			renameBranchButton.IsVisible = isAdvancedMode;
			trackingOriginTextBox.IsVisible = isAdvancedMode;
			remoteURLTextBox.IsVisible = isAdvancedMode;
			copyTrackingButton.IsVisible = isAdvancedMode;
			removeTrackingButton.IsVisible = isAdvancedMode;
			trackingLabel.IsVisible = isAdvancedMode;
			trackedBranchLabel.IsVisible = isAdvancedMode;
			remoteURLLabel.IsVisible = isAdvancedMode;
			newBranchLabel.IsVisible = isAdvancedMode;
			remotesDropDown.IsVisible = isAdvancedMode;

			// fill remotes drop down
			if (isAdvancedMode)
			{
				var remotes = new List<RemoteState>();
				var localRemote = new RemoteState("LOCAL", null);
				remotes.Add(localRemote);
				remotes.AddRange(BranchManager.remoteStates);
				remotesDropDown.Items = remotes;
				remotesDropDown.SelectedIndex = 0;
			}

			// fill other branches list
			var branches = BranchManager.GetNonActiveBranches(isAdvancedMode);
			var items = new List<string>();
			foreach (var branch in branches)
			{
				string detailedName = branch.fullname;
				if (!isAdvancedMode)
				{
					if (!branch.isRemote) items.Add(detailedName);
				}
				else
				{
					if (branch.isRemote) detailedName += " <Remote Branch>";
					else if (branch.isTracking) detailedName += string.Format(" <Local Branch> [tracking remote: {0}]", branch.tracking.fullname);
					else detailedName += " <Local Branch>";
					
					items.Add(detailedName);
				}
			}

			otherBranchListView.Items = items;
			activeBranchTextBox.Text = BranchManager.activeBranch.name;
			if (BranchManager.activeBranch.isTracking)
			{
				trackingOriginTextBox.Text = BranchManager.activeBranch.tracking.fullname;
				if (BranchManager.activeBranch.remoteState != null) remoteURLTextBox.Text = BranchManager.activeBranch.remoteState.url;
				else remoteURLTextBox.Text = "";
			}
			else
			{
				trackingOriginTextBox.Text = "";
				remoteURLTextBox.Text = "";
			}
		}

		private void AddBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (remotesDropDown.SelectedItem == null)
			{
				Debug.LogError("Must select remote!", true);
				return;
			}

			var remote = (RemoteState)remotesDropDown.SelectedItem;
			string remoteName = remote.name;
			if (remote.url == null) remoteName = null;

			string result;
			if (CoreApps.LaunchNameEntry("Enter branch name", out result)) BranchManager.CheckoutNewBranch(result, remoteName);
		}

		private void RenameBranchButton_Click(object sender, RoutedEventArgs e)
		{
			string result;
			if (CoreApps.LaunchNameEntry("Enter branch name", out result)) BranchManager.RenameActiveBranch(result);
		}

		private void CopyTrackingButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchListView.SelectedIndex == -1)
			{
				Debug.Log("Must select a 'Remote' branch!", true);
				return;
			}

			var branch = BranchManager.GetNonActiveBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
			if (!BranchManager.activeBranch.isRemote)
			{
				Debug.Log("Branch selected is not a 'Remote'", true);
				return;
			}

			BranchManager.CopyTracking(branch);
		}

		private void RemoveTrackingButton_Click(object sender, RoutedEventArgs e)
		{
			BranchManager.RemoveTracking();
		}

		private void SwitchBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchListView.SelectedIndex == -1)
			{
				Debug.Log("Must select a 'Other' branch!", true);
				return;
			}

			var branch = BranchManager.GetNonActiveBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
			ProcessingPage.singleton.mode = ProcessingPageModes.Switch;
			ProcessingPage.singleton.switchOtherBranch = branch;
			MainWindow.LoadPage(PageTypes.Processing);
		}

		private void MergeBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchListView.SelectedIndex == -1)
			{
				Debug.Log("Must select a 'Other' branch!", true);
				return;
			}

			var branch = BranchManager.GetNonActiveBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
			if (branch.fullname == BranchManager.activeBranch.fullname)
			{
				Debug.LogError("You must select a non active branch", true);
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to merge branch '{0}' into '{1}'?", branch.fullname, BranchManager.activeBranch.fullname), MessageBoxTypes.YesNo)) return;

			ProcessingPage.singleton.mode = ProcessingPageModes.Merge;
			ProcessingPage.singleton.mergeOtherBranch = branch;
			MainWindow.LoadPage(PageTypes.Processing);
		}

		private void DeleteBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchListView.SelectedIndex == -1)
			{
				Debug.Log("Must select a 'Other' branch!", true);
				return;
			}

			var branch = BranchManager.GetNonActiveBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
			if (branch.fullname == BranchManager.activeBranch.fullname)
			{
				Debug.LogError("You must select a non active branch", true);
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to delete branch '{0}'?", branch.fullname), MessageBoxTypes.YesNo)) return;
			BranchManager.DeleteNonActiveBranch(branch);
		}
	}
}
