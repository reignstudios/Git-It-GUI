using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using GitItGUI.Core;
using GitItGUI.Tools;
using Avalonia.Threading;
using System.Threading;

namespace GitItGUI
{
	public class BranchesPage : UserControl
	{
		public static BranchesPage singleton;

		// ui objects
		TextBlock trackingLabel, trackedBranchLabel, remoteURLLabel;
		TextBox activeBranchTextBox, trackingOriginTextBox, remoteURLTextBox;
		ListBox otherBranchListView;
		Button addBranchButton, renameBranchButton, addTrackingButton, removeTrackingButton, switchBranchButton, mergeBranchButton, deleteBranchButton;
		CheckBox advancedModeCheckBox;

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
			addTrackingButton = this.Find<Button>("addTrackingButton");
			removeTrackingButton = this.Find<Button>("removeTrackingButton");
			switchBranchButton = this.Find<Button>("switchBranchButton");
			mergeBranchButton = this.Find<Button>("mergeBranchButton");
			deleteBranchButton = this.Find<Button>("deleteBranchButton");
			advancedModeCheckBox = this.Find<CheckBox>("advancedModeCheckBox");
			trackingLabel = this.Find<TextBlock>("trackingLabel");
			trackedBranchLabel = this.Find<TextBlock>("trackedBranchLabel");
			remoteURLLabel = this.Find<TextBlock>("remoteURLLabel");

			// apply bindings
			otherBranchListViewItems = new List<string>();
			otherBranchListView.Items = otherBranchListViewItems;
			addBranchButton.Click += AddBranchButton_Click;
			renameBranchButton.Click += RenameBranchButton_Click;
			addTrackingButton.Click += AddTrackingButton_Click;
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
			RepoManager_RepoRefreshedCallback();
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
			addTrackingButton.IsVisible = isAdvancedMode;
			removeTrackingButton.IsVisible = isAdvancedMode;
			trackingLabel.IsVisible = isAdvancedMode;
			trackedBranchLabel.IsVisible = isAdvancedMode;
			remoteURLLabel.IsVisible = isAdvancedMode;

			// fill other branches list
			var branches = BranchManager.GetOtherBranches(isAdvancedMode);
			var items = new List<string>();
			foreach (var branch in branches)
			{
				string detailedName = branch.fullName;
				if (!isAdvancedMode)
				{
					if (!branch.isRemote) items.Add(detailedName);
				}
				else
				{
					if (branch.isRemote) detailedName += " <Remote Branch>";
					else if (branch.isTracking) detailedName += string.Format(" <Local Branch> [tracking remote: {0}]", branch.trackedBranchName);
					else detailedName += " <Local Branch>";
					
					items.Add(detailedName);
				}
			}

			otherBranchListView.Items = items;
			activeBranchTextBox.Text = BranchManager.activeBranch.FriendlyName;
			trackingOriginTextBox.Text = BranchManager.GetTrackedBranchName();
			remoteURLTextBox.Text = BranchManager.GetRemoteURL();
		}

		private void AddBranchButton_Click(object sender, RoutedEventArgs e)
		{
			string result;
			if (CoreApps.LaunchNameEntry("Enter branch name", out result)) BranchManager.AddNewBranch(result);
		}

		private void RenameBranchButton_Click(object sender, RoutedEventArgs e)
		{
			string result;
			if (CoreApps.LaunchNameEntry("Enter branch name", out result)) BranchManager.RenameActiveBranch(result);
		}

		private void AddTrackingButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchListView.SelectedIndex == -1)
			{
				Debug.Log("Must select a 'Remote' branch!", true);
				return;
			}

			var branch = BranchManager.GetOtherBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
			if (!BranchManager.IsRemote(branch))
			{
				Debug.Log("Branch selected is not a 'Remote'", true);
				return;
			}

			BranchManager.AddUpdateTracking(branch);
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

			var branch = BranchManager.GetOtherBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
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

			var branch = BranchManager.GetOtherBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
			if (branch.branch.FriendlyName == BranchManager.activeBranch.FriendlyName)
			{
				Debug.LogError("You must select a non active branch", true);
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to merge branch '{0}' into '{1}'?", branch.fullName, BranchManager.activeBranch.FriendlyName), MessageBoxTypes.YesNo)) return;

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

			var branch = BranchManager.GetOtherBranches(advancedModeCheckBox.IsChecked)[otherBranchListView.SelectedIndex];
			if (branch.branch.FriendlyName == BranchManager.activeBranch.FriendlyName)
			{
				Debug.LogError("You must select a non active branch", true);
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to delete branch '{0}'?", branch.fullName), MessageBoxTypes.YesNo)) return;
			BranchManager.DeleteNonActiveBranch(branch);
		}
	}
}
