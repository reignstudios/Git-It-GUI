using GitCommander;
using GitItGUI.Core;
using GitItGUI.UI.Images;
using GitItGUI.UI.Overlays;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
		public static ChangesTab singleton;

        public ChangesTab()
        {
			singleton = this;
            InitializeComponent();

			// setup rich text box layout
			var p = previewTextBox.Document.Blocks.FirstBlock as Paragraph;
			p.LineHeight = 1;
			previewTextBox.Document.PageWidth = 1920;
		}

		public void Init()
		{
			// bind events
			RepoScreen.singleton.repoManager.AskUserToResolveConflictedFileCallback += RepoManager_AskUserToResolveConflictedFileCallback;
			RepoScreen.singleton.repoManager.AskUserIfTheyAcceptMergedFileCallback += RepoManager_AskUserIfTheyAcceptMergedFileCallback;
		}

		public void Refresh()
		{
			// update settings
			if (AppManager.settings.simpleMode) simpleModeMenuItem_Click(null, null);
			resolveAllMenuItem.IsEnabled = RepoScreen.singleton.repoManager.ConflictsExistQuick();

			// update changes
			stagedChangesListBox.Items.Clear();
			unstagedChangesListBox.Items.Clear();
			foreach (var fileState in RepoScreen.singleton.repoManager.GetFileStates())
			{
				var item = new ListBoxItem();
				item.Tag = fileState;

				// item button
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

				// item label
				var label = new Label();
				label.Margin = new Thickness(20, 0, 0, 0);
				label.Content = fileState.filename + (fileState.isLFS ? " [LFS]" : string.Empty);
				label.ContextMenu = new ContextMenu();
				var openFileMenu = new MenuItem();
				openFileMenu.Header = "Open file";
				openFileMenu.ToolTip = fileState.filename;
				openFileMenu.Click += OpenFileMenu_Click;
				var openFileLocationMenu = new MenuItem();
				openFileLocationMenu.Header = "Open file location";
				openFileLocationMenu.ToolTip = fileState.filename;
				openFileLocationMenu.Click += OpenFileLocationMenu_Click;
				if (fileState.HasState(FileStates.Conflicted))
				{
					var resolveMenu = new MenuItem();
					resolveMenu.Header = "Resolve file";
					resolveMenu.ToolTip = fileState.filename;
					resolveMenu.Click += ResolveFileMenu_Click;
					label.ContextMenu.Items.Add(openFileLocationMenu);
				}
				label.ContextMenu.Items.Add(openFileMenu);
				label.ContextMenu.Items.Add(openFileLocationMenu);

				// item grid
				var grid = new Grid();
				grid.Children.Add(button);
				grid.Children.Add(label);
				item.Content = grid;
				if (fileState.IsStaged())
				{
					button.Click += StagedFileButton_Click;
					stagedChangesListBox.Items.Add(item);
				}
				else
				{
					button.Click += UnstagedFileButton_Click;
					unstagedChangesListBox.Items.Add(item);
				}
			}
		}

		public void LoadCommitMessage()
		{
			if (RepoScreen.singleton.repoManager.isOpen && RepoScreen.singleton.repoManager.LoadCommitMessage(out string msg)) commitMessageTextBox.Text = msg;
		}

		public void ClosingRepo()
		{
			if (RepoScreen.singleton.repoManager.isOpen) RepoScreen.singleton.repoManager.SaveCommitMessage(commitMessageTextBox.Text);
			stagedChangesListBox.Items.Clear();
			unstagedChangesListBox.Items.Clear();
			previewTextBox.Document.Blocks.Clear();
			commitMessageTextBox.Text = string.Empty;
		}

		private bool RepoManager_AskUserToResolveConflictedFileCallback(FileState fileState, bool isBinaryFile, out MergeBinaryFileResults result)
		{
			bool waiting = true, succeeded = true;
			MergeBinaryFileResults binaryResult = MergeBinaryFileResults.Error;
			MainWindow.singleton.ShowMergingOverlay(fileState.filename, delegate(MergeConflictOverlayResults mergeResult)
			{
				switch (mergeResult)
				{
					case MergeConflictOverlayResults.UseTheirs: binaryResult = MergeBinaryFileResults.UseTheirs; break;
					case MergeConflictOverlayResults.UseOurs: binaryResult = MergeBinaryFileResults.KeepMine; break;
					case MergeConflictOverlayResults.RunMergeTool: binaryResult = MergeBinaryFileResults.RunMergeTool; break;

					case MergeConflictOverlayResults.Cancel:
						binaryResult = MergeBinaryFileResults.Cancel;
						succeeded = false;
						break;

					default: throw new Exception("Unsuported merge result: " + mergeResult);
				}

				waiting = false;
			});

			// wait for ui
			while (waiting) Thread.Sleep(1);

			// return result
			result = binaryResult;
			return succeeded;
		}

		private bool RepoManager_AskUserIfTheyAcceptMergedFileCallback(FileState fileState, out MergeFileAcceptedResults result)
		{
			bool waiting = true, succeeded = false;
			MergeFileAcceptedResults mergeResult = MergeFileAcceptedResults.No;
			MainWindow.singleton.ShowMessageOverlay("Accept Changes?", "Do you want to stage the file: " + fileState.filename, MessageOverlayTypes.YesNo, delegate(MessageOverlayResults msgResult)
			{
				succeeded = msgResult == MessageOverlayResults.Ok;
				mergeResult = (msgResult == MessageOverlayResults.Ok) ? MergeFileAcceptedResults.Yes : MergeFileAcceptedResults.No;
				waiting = false;
			});

			// wait for ui
			while (waiting) Thread.Sleep(1);

			// return result
			result = mergeResult;
			return succeeded;
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

		private void ResolveFileMenu_Click(object sender, RoutedEventArgs e)
		{
			var fileState = (FileState)((MenuItem)sender).Tag;

			// check conflicts
			if (!fileState.HasState(FileStates.Conflicted))
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "File not in conflicted state");
				return;
			}

			// process
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.ResolveConflict(fileState)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to resolve conflict");
			});
		}

		private void UnstagedFileButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var fileState = (FileState)button.Tag;

			void stageFile()
			{
				// stage
				MainWindow.singleton.ShowProcessingOverlay();
				RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
				{
					if (!RepoScreen.singleton.repoManager.StageFile(fileState, false))
					{
						MainWindow.singleton.ShowMessageOverlay("Error", "Failed to stage file");
					}
					else
					{
						Dispatcher.InvokeAsync(delegate()
						{
							var item = (ListBoxItem)((Grid)button.Parent).Parent;
							unstagedChangesListBox.Items.Remove(item);
							stagedChangesListBox.Items.Add(item);
							button.Click -= UnstagedFileButton_Click;
							button.Click += StagedFileButton_Click;
						});
					}

					MainWindow.singleton.HideProcessingOverlay();
				});
			}

			// check conflicts
			if (fileState.HasState(FileStates.Conflicted))
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "File not is in a conflicted state!\nAre you sure you want to accept as resolved and stage?", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
				{
					if (result == MessageOverlayResults.Ok) stageFile();
				});

				return;
			}

			stageFile();
		}

		private void StagedFileButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var fileState = (FileState)button.Tag;
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.UnstageFile(fileState, false))
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage file");
				}
				else
				{
					Dispatcher.InvokeAsync(delegate()
					{
						var item = (ListBoxItem)((Grid)button.Parent).Parent;
						stagedChangesListBox.Items.Remove(item);
						unstagedChangesListBox.Items.Add(item);
						button.Click -= StagedFileButton_Click;
						button.Click += UnstagedFileButton_Click;
					});
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void stageAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// check conflicts
			if (RepoScreen.singleton.repoManager.ConflictsExistQuick())
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "Some files are in a conflicted state!\nPlease resolve files instead.");
				return;
			}

			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.StageAllFiles(false))
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to stage files");
				}
				else
				{
					Dispatcher.InvokeAsync(delegate()
					{
						var items = new ListBoxItem[unstagedChangesListBox.Items.Count];
						unstagedChangesListBox.Items.CopyTo(items, 0);
						unstagedChangesListBox.Items.Clear();
						foreach (var item in items) stagedChangesListBox.Items.Add(item);
						unstagedChangesListBox.Items.Clear();
					});
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void unstageAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.UnstageAllFiles(false))
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
				}
				else
				{
					Dispatcher.InvokeAsync(delegate()
					{
						var items = new ListBoxItem[stagedChangesListBox.Items.Count];
						stagedChangesListBox.Items.CopyTo(items, 0);
						stagedChangesListBox.Items.Clear();
						foreach (var item in items) unstagedChangesListBox.Items.Add(item);
						stagedChangesListBox.Items.Clear();
					});
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void stageSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// create list of selected files
			var fileStates = new List<FileState>();
			var items = new List<ListBoxItem>();
			var fileButtons = new List<Button>();
			foreach (ListBoxItem item in unstagedChangesListBox.Items)
			{
				var fileState = (FileState)item.Tag;
				if (item.IsSelected)
				{
					if (fileState.HasState(FileStates.Conflicted))
					{
						MainWindow.singleton.ShowMessageOverlay("Alert", "Some files are in a conflicted state!\nPlease resolve files instead.");
						return;
					}

					fileStates.Add(fileState);
					items.Add(item);
					fileButtons.Add((Button)((Grid)item.Content).Children[0]);
				}
			}

			// process selection
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.StageFileList(fileStates, false))
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
				}
				else
				{
					Dispatcher.InvokeAsync(delegate()
					{
						foreach (var item in items)
						{
							unstagedChangesListBox.Items.Remove(item);
							stagedChangesListBox.Items.Add(item);
						}

						foreach (var button in fileButtons)
						{
							button.Click -= UnstagedFileButton_Click;
							button.Click += StagedFileButton_Click;
						}
					});
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void unstageSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// create list of selected files
			var fileStates = new List<FileState>();
			var items = new List<ListBoxItem>();
			var fileButtons = new List<Button>();
			foreach (ListBoxItem item in stagedChangesListBox.Items)
			{
				var fileState = (FileState)item.Tag;
				if (item.IsSelected)
				{
					fileStates.Add(fileState);
					items.Add(item);
					fileButtons.Add((Button)((Grid)item.Content).Children[0]);
				}
			}

			// process selection
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.UnstageFileList(fileStates, false))
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
				}
				else
				{
					Dispatcher.InvokeAsync(delegate()
					{
						foreach (var item in items)
						{
							stagedChangesListBox.Items.Remove(item);
							unstagedChangesListBox.Items.Add(item);
						}

						foreach (var button in fileButtons)
						{
							button.Click -= StagedFileButton_Click;
							button.Click += UnstagedFileButton_Click;
						}
					});
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void resolveAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// check conflicts
			if (!RepoScreen.singleton.repoManager.ConflictsExistQuick())
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "No conflicts exist");
				return;
			}

			// process
			MainWindow.singleton.ShowWaitingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.ResolveAllConflicts()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to resolve all conflicts");
				MainWindow.singleton.HideWaitingOverlay();
			});
		}

		private void revertAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to revert all files?\nNOTE: This includes staged and un-staged.", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			{
				if (result == MessageOverlayResults.Ok)
				{
					MainWindow.singleton.ShowProcessingOverlay();
					RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
					{
						if (!RepoScreen.singleton.repoManager.RevertAll()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to revert files");
						MainWindow.singleton.HideProcessingOverlay();
					});
				}
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
			MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to revert selected files?", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			{
				if (result == MessageOverlayResults.Ok)
				{
					MainWindow.singleton.ShowProcessingOverlay();
					RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
					{
						if (!RepoScreen.singleton.repoManager.RevertFileList(fileStates)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to revert files");
						MainWindow.singleton.HideProcessingOverlay();
					});
				}
			});
		}

		private void cleanupAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to remove all untracked files?", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			{
				if (result == MessageOverlayResults.Ok)
				{
					MainWindow.singleton.ShowProcessingOverlay();
					RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
					{
						if (!RepoScreen.singleton.repoManager.DeleteUntrackedUnstagedFiles(true)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to cleanup files");
						MainWindow.singleton.HideProcessingOverlay();
					});
				}
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
			MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to remove selected untracked files?", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults result)
			{
				if (result == MessageOverlayResults.Ok)
				{
					MainWindow.singleton.ShowProcessingOverlay();
					RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
					{
						if (!RepoScreen.singleton.repoManager.DeleteUntrackedUnstagedFiles(fileStates, true)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to cleanup files");
						MainWindow.singleton.HideProcessingOverlay();
					});
				}
			});
		}

		private void ProcessPreview(ListBoxItem item)
		{
			previewTextBox.Document.Blocks.Clear();
			var fileState = (FileState)item.Tag;
			var delta = RepoScreen.singleton.repoManager.GetQuickViewData(fileState);
			if (delta == null)
			{
				var range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
				range.Text = "<<< Unsuported Preview Type >>>";
			}
			else if (delta.GetType() == typeof(string))
			{
				using (var stream = new MemoryStream())
				using (var writer = new StreamWriter(stream))
				using (var reader = new StreamReader(stream))
				{
					// write all data into stream
					writer.Write((string)delta);
					writer.Flush();
					writer.Flush();
					stream.Position = 0;

					// read lines and write formatted blocks
					void WritePreviewText(string text, SolidColorBrush blockColor)
					{
						var end = previewTextBox.Document.ContentEnd;
						var range = new TextRange(end, end);
						range.Text = text;
						range.ApplyPropertyValue(TextElement.ForegroundProperty, blockColor);
					}
						
					int blockMode = 0;
					string line = null, normalBlock = null, addBlock = null, subBlock = null, secBlock = null;
					void CheckBlocks(bool isFinishMode)
					{
						if ((blockMode != 0 || (isFinishMode && blockMode == 0)) && !string.IsNullOrEmpty(normalBlock))
						{
							WritePreviewText(normalBlock, Brushes.Black);
							normalBlock = "";
						}
						else if ((blockMode != 1 || (isFinishMode && blockMode == 1)) && !string.IsNullOrEmpty(addBlock))
						{
							WritePreviewText(addBlock, Brushes.Green);
							addBlock = "";
						}
						else if ((blockMode != 2 || (isFinishMode && blockMode == 2)) && !string.IsNullOrEmpty(subBlock))
						{
							WritePreviewText(subBlock, Brushes.Red);
							subBlock = "";
						}
						else if ((blockMode != 3 || (isFinishMode && blockMode == 3)) && !string.IsNullOrEmpty(secBlock))
						{
							WritePreviewText(secBlock, Brushes.DarkOrange);
							secBlock = "";
						}
					}

					do
					{
						line = reader.ReadLine();
						if (string.IsNullOrEmpty(line))
						{
							CheckBlocks(true);
							continue;
						}

						if (line[0] == '+')
						{
							CheckBlocks(false);
							blockMode = 1;
							addBlock += line + '\r';
						}
						else if (line[0] == '-')
						{
							CheckBlocks(false);
							blockMode = 2;
							subBlock += line + '\r';
						}
						else if (line[0] == '#')
						{
							CheckBlocks(false);
							blockMode = 3;
							secBlock += "\r\r" + line + '\r';
						}
						else
						{
							CheckBlocks(false);
							blockMode = 0;
							normalBlock += line + '\r';
						}
					}
					while (line != null);
				}
			}
		}

		private void unstagedChangesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = unstagedChangesListBox.SelectedItem;
			if (item != null)
			{
				stagedChangesListBox.SelectedItem = null;
				ProcessPreview((ListBoxItem)item);
			}
			else
			{
				if (stagedChangesListBox.SelectedItem == null) previewTextBox.Document.Blocks.Clear();
			}
		}

		private void stagedChangesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = stagedChangesListBox.SelectedItem;
			if (item != null)
			{
				unstagedChangesListBox.SelectedItem = null;
				ProcessPreview((ListBoxItem)item);
			}
			else
			{
				if (unstagedChangesListBox.SelectedItem == null) previewTextBox.Document.Blocks.Clear();
			}
		}

		private void simpleModeMenuItem_Click(object sender, RoutedEventArgs e)
		{
			AppManager.settings.simpleMode = true;
			syncButton.Visibility = Visibility.Visible;
			commitButton.Visibility = Visibility.Hidden;
			pullButton.Visibility = Visibility.Hidden;
			pushButton.Visibility = Visibility.Hidden;
		}

		private void advancedModeMenuItem_Click(object sender, RoutedEventArgs e)
		{
			AppManager.settings.simpleMode = false;
			syncButton.Visibility = Visibility.Hidden;
			commitButton.Visibility = Visibility.Visible;
			pullButton.Visibility = Visibility.Visible;
			pushButton.Visibility = Visibility.Visible;
		}

		private bool PrepCommitMessage(out string message)
		{
			// prep commit message
			message = commitMessageTextBox.Text;
			if (string.IsNullOrEmpty(message) || message.Length < 3)
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "You must enter a valid commit message!");
				return false;
			}

			message = message.Replace(Environment.NewLine, "\n");
			return true;
		}
		
		public void HandleConflics()
		{
			MainWindow.singleton.ShowMessageOverlay("Error", "Conflicts exist, resolve them now?", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults msgBoxResult)
			{
				if (msgBoxResult == MessageOverlayResults.Ok)
				{
					MainWindow.singleton.ShowMergingOverlay(null, null);
					RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
					{
						RepoScreen.singleton.repoManager.ResolveAllConflicts();
						MainWindow.singleton.HideMergingOverlay();
					});
				}
			});
		}

		private void syncButton_Click(object sender, RoutedEventArgs e)
		{
			// make sure all changes are staged
			if (unstagedChangesListBox.Items.Count != 0)
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "You must stage all files first!\nOr use Advanced mode.");
				return;
			}

			// prep commit message
			bool changesExist = RepoScreen.singleton.repoManager.ChangesExist();
			string msg = null;
			if (changesExist && !PrepCommitMessage(out msg)) return;

			// process
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (changesExist && !RepoScreen.singleton.repoManager.CommitStagedChanges(msg, false))
				{
					RepoScreen.singleton.Refresh();
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to commit changes");
					return;
				}

				var result = RepoScreen.singleton.repoManager.Sync();
				if (result != SyncMergeResults.Succeeded)
				{
					if (result == SyncMergeResults.Conflicts) HandleConflics();
					else if (result == SyncMergeResults.Error) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to sync changes");
				}
				else
				{
					Dispatcher.InvokeAsync(delegate()
					{
						commitMessageTextBox.Text = string.Empty;
					});
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void commitButton_Click(object sender, RoutedEventArgs e)
		{
			// validate changes exist
			if (stagedChangesListBox.Items.Count == 0)
			{
				MainWindow.singleton.ShowMessageOverlay("Error", "There are not staged file to commit");
				return;
			}

			// prep commit message
			if (!PrepCommitMessage(out string msg)) return;

			// process
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.CommitStagedChanges(msg))
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to commit changes");
				}
				else
				{
					Dispatcher.InvokeAsync(delegate()
					{
						commitMessageTextBox.Text = string.Empty;
					});
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		internal void pullButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				var result = RepoScreen.singleton.repoManager.Pull();
				if (result == SyncMergeResults.Conflicts)
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Conflicts exist after pull, resolve them now?", MessageOverlayTypes.YesNo, delegate(MessageOverlayResults msgBoxresult)
					{
						if (msgBoxresult == MessageOverlayResults.Ok)
						{
							MainWindow.singleton.ShowMergingOverlay(null, null);
							RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
							{
								RepoScreen.singleton.repoManager.ResolveAllConflicts();
								MainWindow.singleton.HideMergingOverlay();
							});
						}
					});
				}
				else if (result == SyncMergeResults.Error)
				{
					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to pull changes");
				}

				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		internal void pushButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.Push()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to push changes");
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void preivewDiffMenuItem_Click(object sender, RoutedEventArgs e)
		{
			// check for selection
			var stagedItem = stagedChangesListBox.SelectedItem as ListBoxItem;
			var unstagedItem = unstagedChangesListBox.SelectedItem as ListBoxItem;
			if (stagedItem == null && unstagedItem == null)
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "No file selected to preview");
				return;
			}

			var fileState = (stagedItem != null) ? (FileState)stagedItem.Tag : null;
			if (fileState == null) fileState = (unstagedItem != null) ? (FileState)unstagedItem.Tag : null;

			// process
			MainWindow.singleton.ShowWaitingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				if (!RepoScreen.singleton.repoManager.OpenDiffTool(fileState)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to show diff");
				MainWindow.singleton.HideWaitingOverlay();
			});
		}
	}
}
