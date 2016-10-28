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
		#pragma warning disable CS0067
		public event EventHandler CanExecuteChanged;
		#pragma warning restore CS0067

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
			switch (sender.State)
			{
				case FileStates.NewInWorkdir:
				case FileStates.TypeChangeInWorkdir:
				case FileStates.RenamedInWorkdir:
				case FileStates.ModifiedInWorkdir:
				case FileStates.DeletedFromWorkdir:
					sender.Stage(true);
					break;

				case FileStates.NewInIndex:
				case FileStates.TypeChangeInIndex:
				case FileStates.RenamedInIndex:
				case FileStates.ModifiedInIndex:
				case FileStates.DeletedFromIndex:
					sender.Unstage(true);
					break;
			}
		}
	}

	public class FileItem
	{
		private ChangesPage page;

		private Bitmap icon;
		public Bitmap Icon {get {return icon;}}

		public FileState fileState;

		//private string filename;
		public string Filename {get {return fileState.filename;}}

		//private FileStates state;
		public FileStates State {get {return fileState.state;}}

		public FileItem()
		{
			fileState.filename = "ERROR";
		}

		public FileItem(Bitmap icon, FileState fileState)
		{
			this.icon = icon;
			this.fileState = fileState;
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

		public void Stage(bool refresh)
		{
			ChangesManager.StageFile(Filename);
			if (refresh) RepoManager.Refresh();
		}

		public void Unstage(bool refresh)
		{
			ChangesManager.UnstageFile(Filename);
			if (refresh) RepoManager.Refresh();
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

			diffTextBox = this.Find<TextBox>("diffTextBox");
			unstagedChangesListView = this.Find<ListBox>("unstagedChangesListView");
			stagedChangesListView = this.Find<ListBox>("stagedChangesListView");
			stageAllButton = this.Find<Button>("stageAllButton");
			unstageAllButton = this.Find<Button>("unstageAllButton");
			refreshChangedButton = this.Find<Button>("refreshChangedButton");
			revertAllButton = this.Find<Button>("revertAllButton");
			resolveSelectedButton = this.Find<Button>("resolveSelectedButton");
			resolveAllButton = this.Find<Button>("resolveAllButton");

			// apply bindings
			unstagedChangesListViewItems = new List<FileItem>();
			stagedChangesListViewItems = new List<FileItem>();
			unstagedChangesListView.Items = unstagedChangesListViewItems;
			stagedChangesListView.Items = stagedChangesListViewItems;
			
			unstagedChangesListView.SelectionChanged += UnstagedChangesListView_SelectionChanged;
			stagedChangesListView.SelectionChanged += StagedChangesListView_SelectionChanged;
			stageAllButton.Click += StageAllButton_Click;
			unstageAllButton.Click += UnstageAllButton_Click;
			refreshChangedButton.Click += RefreshChangedButton_Click;
			revertAllButton.Click += RevertAllButton_Click;
			resolveSelectedButton.Click += ResolveSelectedButton_Click;
			resolveAllButton.Click += ResolveAllButton_Click;

			RepoManager.RepoRefreshedCallback += RepoManager_RepoRefreshedCallback;
		}

		private void ResolveAllButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in unstagedChangesListViewItems)
			{
				ChangesManager.ResolveConflict(item.Filename);
			}

			RepoManager.Refresh();
		}

		private void ResolveSelectedButton_Click(object sender, RoutedEventArgs e)
		{
			var item = unstagedChangesListView.SelectedItem as FileItem;
			if (item == null)
			{
				Debug.Log("File must be selected", true);
				return;
			}

			ChangesManager.ResolveConflict(item.Filename);
			RepoManager.Refresh();
		}

		private void RevertAllButton_Click(object sender, RoutedEventArgs e)
		{
			ChangesManager.RevertAll();
		}

		private void RefreshChangedButton_Click(object sender, RoutedEventArgs e)
		{
			RepoManager.Refresh();
		}

		private void StageAllButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in unstagedChangesListViewItems)
			{
				item.Stage(false);
			}

			RepoManager.Refresh();
		}

		private void UnstageAllButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (var item in unstagedChangesListViewItems)
			{
				item.Unstage(false);
			}

			RepoManager.Refresh();
		}

		private void RepoManager_RepoRefreshedCallback()
		{
			unstagedChangesListViewItems.Clear();
			stagedChangesListViewItems.Clear();
			unstagedChangesListView.Items = null;
			stagedChangesListView.Items = null;
			foreach (var fileState in ChangesManager.GetFileChanges())
			{
				var item = new FileItem(ResourceManager.GetResource(fileState.state), fileState);
				if (!fileState.IsStaged()) unstagedChangesListViewItems.Add(item);
				else stagedChangesListViewItems.Add(item);
			}
			unstagedChangesListView.Items = unstagedChangesListViewItems;
			stagedChangesListView.Items = stagedChangesListViewItems;
		}

		private void UpdateDiffPanel(FileItem item)
		{
			var data = ChangesManager.GetQuickViewData(item.fileState);
			if (data == null)
			{
				diffTextBox.Text = "<<< ERROR >>>";
			}
			else if (data.GetType() == typeof(string))
			{
				diffTextBox.Text = data.ToString();
			}
			else
			{
				diffTextBox.Text = "<<< Unsported Binary Format >>>";
			}
		}

		private void UnstagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = unstagedChangesListView.SelectedItem as FileItem;
			if (item != null)
			{
				UpdateDiffPanel(item);
				stagedChangesListView.SelectedIndex = -1;
			}
		}

		private void StagedChangesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = stagedChangesListView.SelectedItem as FileItem;
			if (item != null)
			{
				UpdateDiffPanel(item);
				unstagedChangesListView.SelectedIndex = -1;
			}
		}
	}
}
