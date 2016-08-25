using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitItGUI
{
	public class BranchesPage : UserControl
	{
		public static BranchesPage singleton;

		// ui objects
		ListBox activeBranchListView, otherBranchListView;
		Button addButton, renameButton, mergeButton, deleteButton;

		List<string> activeBranchListViewItems, otherBranchListViewItems;

		public BranchesPage()
		{
			singleton = this;
			LoadUI();
		}

		private void LoadUI()
		{
			AvaloniaXamlLoader.Load(this);

			activeBranchListView = this.Find<ListBox>("activeBranchListView");
			otherBranchListView = this.Find<ListBox>("otherBranchListView");

			// apply bindings
			activeBranchListViewItems = new List<string>();
			otherBranchListViewItems = new List<string>();
			activeBranchListView.Items = activeBranchListViewItems;
			otherBranchListView.Items = otherBranchListViewItems;
		}
	}
}
