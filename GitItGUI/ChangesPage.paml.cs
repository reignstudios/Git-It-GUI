using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GitItGUI.Core;
using LibGit2Sharp;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GitItGUI
{
	public class ClickCommand : ICommand
	{
		public event EventHandler CanExecuteChanged;
		private FileItem sender;

		public ClickCommand(FileItem sender)
		{
			this.sender = sender;
		}

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			ChangesManager.StageFile(sender.Filename);
		}
	}

	public class FileItem
	{
		private ChangesPage page;

		private Bitmap icon;
		public Bitmap Icon {get {return icon;}}

		private string filename;
		public string Filename {get {return filename;}}

		public FileItem()
		{
			filename = "ERROR";
		}

		public FileItem(Bitmap icon, string filename)
		{
			this.icon = icon;
			this.filename = filename;
		}

		private ClickCommand clickCommand;
		public ClickCommand ClickCommand
		{
			get
			{
				clickCommand = new ClickCommand(this);
				return clickCommand;
			}
		}
	}

	public class ChangesPage : UserControl
	{
		public static ChangesPage singleton;

		// ui objects
		Button refreshChangedButton, revertAllButton, stageAllButton, unstageAllButton, resolveSelectedButton, resolveAllButton;
		Button openDiffToolButton, commitStagedButton, syncChangesButton;
		ListBox unstagedChangesListView, stagedChangesListView;
		ScrollViewer diffTextBoxScrollViewer;
		TextBox diffTextBox;

		List<FileItem> unstagedChangesListViewItems, stagedChangesListViewItems;

		public ChangesPage()
		{
			singleton = this;
			LoadUI();
		}

		private void LoadUI()
		{
			AvaloniaXamlLoader.Load(this);

			unstagedChangesListView = this.Find<ListBox>("unstagedChangesListView");
			stagedChangesListView = this.Find<ListBox>("stagedChangesListView");

			// apply bindings
			unstagedChangesListViewItems = new List<FileItem>();
			stagedChangesListViewItems = new List<FileItem>();
			unstagedChangesListView.Items = unstagedChangesListViewItems;
			stagedChangesListView.Items = stagedChangesListViewItems;
			
			unstagedChangesListView.SelectionChanged += UnstagedChangesListView_SelectionChanged;
			stagedChangesListView.SelectionChanged += StagedChangesListView_SelectionChanged;
			RepoManager.RepoRefreshedCallback += RepoManager_RepoRefreshedCallback;
		}

		private void RepoManager_RepoRefreshedCallback()
		{
			unstagedChangesListViewItems.Clear();
			stagedChangesListViewItems.Clear();
			foreach (var fileState in ChangesManager.GetFileChanges())
			{
				var item = new FileItem(ResourceManager.GetResource(fileState.state), fileState.filePath);
				if (!fileState.IsStaged()) unstagedChangesListViewItems.Add(item);
				else stagedChangesListViewItems.Add(item);
			}
			unstagedChangesListView.Items = unstagedChangesListViewItems;
			stagedChangesListView.Items = stagedChangesListViewItems;

			//var i = unstagedChangesListView.ItemContainerGenerator.Containers.First().ContainerControl;
			//foreach (var child in i.LogicalChildren.First().LogicalChildren)
			//{
			//	if (child.GetType() == typeof(Image))
			//	{
			//		var image = child as Image;
			//		image.PointerReleased += Image_PointerReleased;
			//	}
			//}
		}

		private void UnstagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//var i = unstagedChangesListView.ItemContainerGenerator.Containers.First().ContainerControl;
			//foreach (var child in i.LogicalChildren.First().LogicalChildren)
			//{
			//	if (child.GetType() == typeof(Image))
			//	{
			//		var image = child as Image;
			//		string value = image.Name;
			//	}
			//}
			//var visualItems = unstagedChangesListView.F.FindControl<Image>("testImage");
			//foreach (FileItem item in unstagedChangesListView.Items)
			//{
			//	//var l = new ListBoxItem();l.Content
			//	var image = item.FindControl<Image>("testImage");
			//}
		}

		private void StagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			
		}
	}
}
