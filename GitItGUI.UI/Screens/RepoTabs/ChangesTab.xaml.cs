using GitCommander;
using GitItGUI.UI.Images;
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
    /// Interaction logic for ChangesTab.xaml
    /// </summary>
    public partial class ChangesTab : UserControl
    {
        public ChangesTab()
        {
            InitializeComponent();

			var p = previewTextBox.Document.Blocks.FirstBlock as Paragraph;
			p.LineHeight = 1;

			var range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
			range.Text = "+ Addition" + Environment.NewLine;
			range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Green);

			range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
			range.Text = "- Subtraction";
			range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
		}

		public void Refresh()
		{
			stagedChangesListBox.Items.Clear();
			unstagedChangesListBox.Items.Clear();
			foreach (var fileState in RepoScreen.singleton.repoManager.GetFileStates())
			{
				var item = new ListBoxItem();
				item.Tag = fileState;

				var button = new Button();
				button.Width = 20;
				button.Height = 20;
				button.HorizontalAlignment = HorizontalAlignment.Left;
				button.Background = new SolidColorBrush(Colors.Transparent);
				button.BorderBrush = new SolidColorBrush(Colors.LightGray);
				button.BorderThickness = new Thickness(1);
				var image = new Image();
				image.Source = ImagePool.GetImage(fileState.state);
				button.Content = image;
				button.Tag = fileState;

				var label = new Label();
				label.Margin = new Thickness(20, 0, 0, 0);
				label.Content = fileState.filename;
				label.ContextMenu = new ContextMenu();
				var openFileMenu = new MenuItem();
				openFileMenu.Header = "Open file";
				openFileMenu.ToolTip = fileState.filename;
				openFileMenu.Click += OpenFileMenu_Click;
				var openFileLocationMenu = new MenuItem();
				openFileLocationMenu.Header = "Open file location";
				openFileLocationMenu.ToolTip = fileState.filename;
				openFileLocationMenu.Click += OpenFileLocationMenu_Click;
				label.ContextMenu.Items.Add(openFileMenu);
				label.ContextMenu.Items.Add(openFileLocationMenu);

				var grid = new Grid();
				grid.Children.Add(button);
				grid.Children.Add(label);
				item.Content = grid;
				if (fileState.IsStaged())
				{
					button.Click += StagedButton_Click;
					stagedChangesListBox.Items.Add(item);
				}
				else
				{
					button.Click += UnstagedButton_Click;
					unstagedChangesListBox.Items.Add(item);
				}
			}
		}

		private void ToolButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			button.ContextMenu.IsOpen = true;
		}

		private void OpenFileMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			RepoScreen.singleton.repoManager.OpenFile((string)item.ToolTip);
		}

		private void OpenFileLocationMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			RepoScreen.singleton.repoManager.OpenFileLocation((string)item.ToolTip);
		}

		private void UnstagedButton_Click(object sender, RoutedEventArgs e)
		{
			var fileState = (FileState)((Button)sender).Tag;
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.StageFile(fileState)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage file");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void StagedButton_Click(object sender, RoutedEventArgs e)
		{
			var fileState = (FileState)((Button)sender).Tag;
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.UnstageFile(fileState)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to stage file");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void stageAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.StageAllFiles()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to stage files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void unstageAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.UnstageAllFiles()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void stageSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// create list of selected files
			var fileStates = new List<FileState>();
			foreach (var item in unstagedChangesListBox.Items)
			{
				var i = (ListBoxItem)item;
				var fileState = (FileState)i.Tag;
				if (i.IsSelected) fileStates.Add(fileState);
			}

			// process selection
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.StageFileList(fileStates)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void unstageSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// create list of selected files
			var fileStates = new List<FileState>();
			foreach (var item in stagedChangesListBox.Items)
			{
				var i = (ListBoxItem)item;
				var fileState = (FileState)i.Tag;
				if (i.IsSelected) fileStates.Add(fileState);
			}

			// process selection
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.UnstageFileList(fileStates)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void revertAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.RevertAll()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to revert files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void revertSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// create list of selected files
			var fileStates = new List<FileState>();
			foreach (var item in unstagedChangesListBox.Items)
			{
				var i = (ListBoxItem)item;
				var fileState = (FileState)i.Tag;
				if (i.IsSelected) fileStates.Add(fileState);
			}

			// process selection
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.RevertFileList(fileStates)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to revert files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void cleanupAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.DeleteUntrackedUnstagedFiles(true)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to cleanup files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void cleanupSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// create list of selected files
			var fileStates = new List<FileState>();
			foreach (var item in unstagedChangesListBox.Items)
			{
				var i = (ListBoxItem)item;
				var fileState = (FileState)i.Tag;
				if (i.IsSelected) fileStates.Add(fileState);
			}

			// process selection
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.DeleteUntrackedUnstagedFiles(fileStates, true)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to cleanup files");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}
	}
}
