using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using GitItGUI.Core;

namespace GitItGUI
{
	enum BranchModes
	{
		None,
		AddingBranch,
		RenameBranch
	}

	public class BranchesPage : UserControl
	{
		public static BranchesPage singleton;
		private BranchModes mode = BranchModes.None;

		// ui objects
		TextBox activeBranchTextBox, trackingOriginTextBox, remoteURLTextBox;
		ListBox otherBranchListView;
		Button addBranchButton, renameBranchButton, addTrackingButton, removeTrackingButton, switchBranchButton, mergeBranchButton, deleteBranchButton;

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

			// bind events
			MainContent.singleton.MainContentPageNavigatedTo += NavigatedTo;
			RepoManager.RepoRefreshedCallback += RepoManager_RepoRefreshedCallback;
		}

		private void RepoManager_RepoRefreshedCallback()
		{
			var branches = BranchManager.GetOtherBranches();
			var items = new List<string>();
			foreach (var branch in branches)
			{
				if (branch.isRemote) items.Add(branch.name + " <Remote Branch>");
				else if (branch.isTracking) items.Add(branch.name + string.Format(" <Local Branch> [tracking remote: {0}]", branch.trackedBranchName));
				else items.Add(branch.name + " <Local Branch>");
			}

			otherBranchListView.Items = items;
			activeBranchTextBox.Text = BranchManager.activeBranch.FriendlyName;
			trackingOriginTextBox.Text = BranchManager.GetTrackedBranchName();
			remoteURLTextBox.Text = BranchManager.GetRemoteURL();
		}

		private void NavigatedTo()
		{
			if (mode == BranchModes.AddingBranch && NamePage.succeeded)
			{
				BranchManager.AddNewBranch(NamePage.value);
			}
			else if (mode == BranchModes.RenameBranch && NamePage.succeeded)
			{
				BranchManager.RenameActiveBranch(NamePage.value);
			}
		}

		private void AddBranchButton_Click(object sender, RoutedEventArgs e)
		{
			mode = BranchModes.AddingBranch;
			MainWindow.LoadPage(PageTypes.Name);
		}

		private void RenameBranchButton_Click(object sender, RoutedEventArgs e)
		{
			mode = BranchModes.RenameBranch;
			MainWindow.LoadPage(PageTypes.Name);
		}

		private void AddTrackingButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchListView.SelectedIndex == -1)
			{
				Debug.Log("Must select a 'Remote' branch!", true);
				return;
			}

			// TODO: check if selected branch is remote
		}

		private void RemoveTrackingButton_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private void SwitchBranchButton_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private void MergeBranchButton_Click(object sender, RoutedEventArgs e)
		{
			
		}

		private void DeleteBranchButton_Click(object sender, RoutedEventArgs e)
		{
			
		}
	}
}
