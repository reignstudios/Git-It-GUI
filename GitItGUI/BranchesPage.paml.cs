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

		List<string> otherBranchListViewItems;

		public BranchesPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			otherBranchListView = this.Find<ListBox>("otherBranchListView");

			// apply bindings
			otherBranchListViewItems = new List<string>();
			otherBranchListView.Items = otherBranchListViewItems;
		}
	}
}
