﻿using GitCommander;
using GitItGUI.Core;
using GitItGUI.UI.Images;
using GitItGUI.UI.Overlays;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Layout;

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
			//var p = previewTextBox.Document.Blocks.FirstBlock as Paragraph;
			//p.LineHeight = 1;
			//previewTextBox.Document.PageWidth = 1920;
		}

		public void Init()
		{
			// bind events
			RepoScreen.singleton.repoManager.AskUserToResolveConflictedFileCallback += RepoManager_AskUserToResolveConflictedFileCallback;
			RepoScreen.singleton.repoManager.AskUserIfTheyAcceptMergedFileCallback += RepoManager_AskUserIfTheyAcceptMergedFileCallback;

			// apply grid offsets
			//if (AppManager.settings.changesPanelHL != -1) columDefHL.Width = new GridLength(AppManager.settings.changesPanelHL, columDefHL.Width.GridUnitType);
			//if (AppManager.settings.changesPanelHR != -1) columDefHR.Width = new GridLength(AppManager.settings.changesPanelHR, columDefHR.Width.GridUnitType);
			//if (AppManager.settings.changesPanelStagingVU != -1) rowStagingDefVU.Height = new GridLength(AppManager.settings.changesPanelStagingVU, rowStagingDefVU.Height.GridUnitType);
			//if (AppManager.settings.changesPanelStagingVD != -1) rowStagingDefVD.Height = new GridLength(AppManager.settings.changesPanelStagingVD, rowStagingDefVD.Height.GridUnitType);
			//if (AppManager.settings.changesPanelCommitDiffVU != -1) rowCommitDiffDefVU.Height = new GridLength(AppManager.settings.changesPanelCommitDiffVU, rowCommitDiffDefVU.Height.GridUnitType);
			//if (AppManager.settings.changesPanelCommitDiffVD != -1) rowCommitDiffDefVD.Height = new GridLength(AppManager.settings.changesPanelCommitDiffVD, rowCommitDiffDefVD.Height.GridUnitType);
		}

		public void Refresh()
		{
			// update settings
			if (AppManager.settings.simpleMode) simpleModeMenuItem_Click(null, null);
			resolveAllMenuItem.IsEnabled = RepoScreen.singleton.repoManager.ConflictsExistQuick();

			// clear preview
			//previewTextBox.Document.Blocks.Clear();
			//previewTextBox.Visibility = Visibility.Visible;
			previewGrid.IsVisible = false;
			previewSingleGrid.IsVisible = false;

			// update changes
			stagedChangesListBox.Items.Clear();
			unstagedChangesListBox.Items.Clear();
			foreach (var fileState in RepoScreen.singleton.repoManager.GetFileStates())
			{
				if (fileState.isSubmodule) continue;

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
				//button.ToolTip = fileState.ToStateString();

				// item label
				var textBlock = new TextBlock();
				textBlock.Margin = new Thickness(24, 0, 0, 0);
				textBlock.Text = fileState.filename + (fileState.isLFS ? " [LFS]" : string.Empty);
				textBlock.ContextMenu = new ContextMenu();
				var openFileMenu = new MenuItem();
				openFileMenu.Tag = fileState;
				openFileMenu.Header = "Open file";
				//openFileMenu.ToolTip = fileState.filename;
				openFileMenu.Click += OpenFileMenu_Click;
				var openFileLocationMenu = new MenuItem();
				openFileLocationMenu.Header = "Open file location";
				//openFileLocationMenu.ToolTip = fileState.filename;
				openFileLocationMenu.Click += OpenFileLocationMenu_Click;
				if (fileState.HasState(FileStates.Conflicted))
				{
					var resolveMenu = new MenuItem();
					resolveMenu.Tag = fileState;
					resolveMenu.Header = "Resolve file";
					//resolveMenu.ToolTip = fileState.filename;
					resolveMenu.Click += ResolveFileMenu_Click;
					textBlock.ContextMenu.Items.Add(resolveMenu);
				}
				if (!fileState.HasState(FileStates.NewInWorkdir) && !fileState.HasState(FileStates.NewInIndex))
				{
					var historyMenu = new MenuItem();
					historyMenu.Tag = fileState;
					historyMenu.Header = "History";
					//historyMenu.ToolTip = fileState.filename;
					historyMenu.Click += HistoryFileMenu_Click;
					textBlock.ContextMenu.Items.Add(historyMenu);
				}
				textBlock.ContextMenu.Items.Add(openFileMenu);
				textBlock.ContextMenu.Items.Add(openFileLocationMenu);

				// item grid
				var grid = new Grid();
				grid.Children.Add(button);
				grid.Children.Add(textBlock);
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
			if (RepoScreen.singleton.repoManager.isOpen && RepoScreen.singleton.repoManager.ChangesExist() && RepoScreen.singleton.repoManager.LoadCommitMessage(out string msg))
			{
				commitMessageTextBox.Text = msg;
			}
		}

		public void ClosingRepo()
		{
			//// save commit message
			//if (RepoScreen.singleton.repoManager.isOpen)
			//{
			//	RepoScreen.singleton.repoManager.SaveCommitMessage(commitMessageTextBox.Text);
			//}

			//// save grid offset
			//AppManager.settings.changesPanelHL = columDefHL.Width.Value;
			//AppManager.settings.changesPanelHR = columDefHR.Width.Value;
			//AppManager.settings.changesPanelStagingVU = rowStagingDefVU.Height.Value;
			//AppManager.settings.changesPanelStagingVD = rowStagingDefVD.Height.Value;
			//AppManager.settings.changesPanelCommitDiffVU = rowCommitDiffDefVU.Height.Value;
			//AppManager.settings.changesPanelCommitDiffVD = rowCommitDiffDefVD.Height.Value;

			//// clear states
			//stagedChangesListBox.Items.Clear();
			//unstagedChangesListBox.Items.Clear();
			////previewTextBox.Document.Blocks.Clear();
			//commitMessageTextBox.Text = string.Empty;

			////previewTextBox.Visibility = Visibility.Visible;
			//previewGrid.IsVisible = false;
			//previewSingleGrid.IsVisible = false;
		}

		private bool RepoManager_AskUserToResolveConflictedFileCallback(FileState fileState, bool isBinaryFile, out MergeFileResults result)
		{
			bool waiting = true, succeeded = true;
			MergeFileResults binaryResult = MergeFileResults.Error;
			//MainWindow.singleton.ShowMergingOverlay(fileState.filename, isBinaryFile, delegate (MergeConflictOverlayResults mergeResult)
			//{
			//	switch (mergeResult)
			//	{
			//		case MergeConflictOverlayResults.UseTheirs: binaryResult = MergeFileResults.UseTheirs; break;
			//		case MergeConflictOverlayResults.UseOurs: binaryResult = MergeFileResults.KeepMine; break;
			//		case MergeConflictOverlayResults.RunMergeTool: binaryResult = MergeFileResults.RunMergeTool; break;

			//		case MergeConflictOverlayResults.Cancel:
			//			binaryResult = MergeFileResults.Cancel;
			//			succeeded = false;
			//			break;

			//		default: throw new Exception("Unsuported merge result: " + mergeResult);
			//	}

			//	waiting = false;
			//});

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
			//MainWindow.singleton.ShowMessageOverlay("Accept Changes?", "No changes detected.\nDo you want to stage the file as is: " + fileState.filename, MessageOverlayTypes.YesNo, delegate (MessageOverlayResults msgResult)
			//{
			//	succeeded = msgResult == MessageOverlayResults.Ok;
			//	mergeResult = (msgResult == MessageOverlayResults.Ok) ? MergeFileAcceptedResults.Yes : MergeFileAcceptedResults.No;
			//	waiting = false;
			//});

			// wait for ui
			while (waiting) Thread.Sleep(1);

			// return result
			result = mergeResult;
			return succeeded;
		}

		private void ToolButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			//button.ContextMenu.IsOpen = true;
		}

		private void OpenFileMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			//RepoScreen.singleton.repoManager.OpenFile((string)item.ToolTip);
		}

		private void OpenFileLocationMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			//RepoScreen.singleton.repoManager.OpenFileLocation((string)item.ToolTip);
		}

		private void ResolveFileMenu_Click(object sender, RoutedEventArgs e)
		{
			//var fileState = (FileState)((MenuItem)sender).Tag;

			//// check conflicts
			//if (!fileState.HasState(FileStates.Conflicted))
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "File not in conflicted state");
			//	return;
			//}

			//// process
			//MainWindow.singleton.ShowMergingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.ResolveConflict(fileState)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to resolve conflict");
			//	MainWindow.singleton.HideMergingOverlay();
			//});
		}

		private void HistoryFileMenu_Click(object sender, RoutedEventArgs e)
		{
			//var fileState = (FileState)((MenuItem)sender).Tag;
			//HistoryTab.singleton.OpenHistory(fileState.filename);
		}

		private void UnstagedFileButton_Click(object sender, RoutedEventArgs e)
		{
			//var button = (Button)sender;
			//var fileState = (FileState)button.Tag;

			//void stageFile()
			//{
			//	// stage
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//	{
			//		if (!RepoScreen.singleton.repoManager.StageFile(fileState, false))
			//		{
			//			MainWindow.singleton.ShowMessageOverlay("Error", "Failed to stage file");
			//		}
			//		else
			//		{
			//			Dispatcher.InvokeAsync(delegate ()
			//			{
			//				var item = (ListBoxItem)((Grid)button.Parent).Parent;
			//				unstagedChangesListBox.Items.Remove(item);
			//				stagedChangesListBox.Items.Add(item);
			//				button.Click -= UnstagedFileButton_Click;
			//				button.Click += StagedFileButton_Click;
			//			});
			//		}

			//		RepoScreen.singleton.repoManager.QuickRefresh();
			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//}

			//// check conflicts
			//if (fileState.HasState(FileStates.Conflicted))
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "File is in a conflicted state!\nAre you sure you want to accept as resolved and stage?", MessageOverlayTypes.YesNo, delegate (MessageOverlayResults result)
			//	{
			//		if (result == MessageOverlayResults.Ok) stageFile();
			//	});

			//	return;
			//}

			//stageFile();
		}

		private void StagedFileButton_Click(object sender, RoutedEventArgs e)
		{
			//var button = (Button)sender;
			//var fileState = (FileState)button.Tag;
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.UnstageFile(fileState, false))
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage file");
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			var item = (ListBoxItem)((Grid)button.Parent).Parent;
			//			stagedChangesListBox.Items.Remove(item);
			//			unstagedChangesListBox.Items.Add(item);
			//			button.Click -= StagedFileButton_Click;
			//			button.Click += UnstagedFileButton_Click;
			//		});
			//	}

			//	RepoScreen.singleton.repoManager.QuickRefresh();
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void stageAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//// check conflicts
			//if (RepoScreen.singleton.repoManager.ConflictsExistQuick())
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "Some files are in a conflicted state!\nPlease resolve files instead.");
			//	return;
			//}

			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.StageAllFiles(false))
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to stage files");
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			var items = new ListBoxItem[unstagedChangesListBox.Items.Count];
			//			unstagedChangesListBox.Items.CopyTo(items, 0);
			//			unstagedChangesListBox.Items.Clear();
			//			foreach (var item in items) stagedChangesListBox.Items.Add(item);
			//			unstagedChangesListBox.Items.Clear();
			//		});
			//	}

			//	RepoScreen.singleton.repoManager.QuickRefresh();
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void unstageAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.UnstageAllFiles(false))
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			var items = new ListBoxItem[stagedChangesListBox.Items.Count];
			//			stagedChangesListBox.Items.CopyTo(items, 0);
			//			stagedChangesListBox.Items.Clear();
			//			foreach (var item in items) unstagedChangesListBox.Items.Add(item);
			//			stagedChangesListBox.Items.Clear();
			//		});
			//	}

			//	RepoScreen.singleton.repoManager.QuickRefresh();
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void stageSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//// create list of selected files
			//var fileStates = new List<FileState>();
			//var items = new List<ListBoxItem>();
			//var fileButtons = new List<Button>();
			//foreach (ListBoxItem item in unstagedChangesListBox.Items)
			//{
			//	var fileState = (FileState)item.Tag;
			//	if (item.IsSelected)
			//	{
			//		if (fileState.HasState(FileStates.Conflicted))
			//		{
			//			MainWindow.singleton.ShowMessageOverlay("Alert", "Some files are in a conflicted state!\nPlease resolve files instead.");
			//			return;
			//		}

			//		fileStates.Add(fileState);
			//		items.Add(item);
			//		fileButtons.Add((Button)((Grid)item.Content).Children[0]);
			//	}
			//}

			//// process selection
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.StageFileList(fileStates, false))
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			foreach (var item in items)
			//			{
			//				unstagedChangesListBox.Items.Remove(item);
			//				stagedChangesListBox.Items.Add(item);
			//			}

			//			foreach (var button in fileButtons)
			//			{
			//				button.Click -= UnstagedFileButton_Click;
			//				button.Click += StagedFileButton_Click;
			//			}
			//		});
			//	}

			//	RepoScreen.singleton.repoManager.QuickRefresh();
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void unstageSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//// create list of selected files
			//var fileStates = new List<FileState>();
			//var items = new List<ListBoxItem>();
			//var fileButtons = new List<Button>();
			//foreach (ListBoxItem item in stagedChangesListBox.Items)
			//{
			//	var fileState = (FileState)item.Tag;
			//	if (item.IsSelected)
			//	{
			//		fileStates.Add(fileState);
			//		items.Add(item);
			//		fileButtons.Add((Button)((Grid)item.Content).Children[0]);
			//	}
			//}

			//// process selection
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.UnstageFileList(fileStates, false))
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to un-stage files");
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			foreach (var item in items)
			//			{
			//				stagedChangesListBox.Items.Remove(item);
			//				unstagedChangesListBox.Items.Add(item);
			//			}

			//			foreach (var button in fileButtons)
			//			{
			//				button.Click -= StagedFileButton_Click;
			//				button.Click += UnstagedFileButton_Click;
			//			}
			//		});
			//	}

			//	RepoScreen.singleton.repoManager.QuickRefresh();
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void ResolveAllConflicts()
		{
			//MainWindow.singleton.ShowMergingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.ResolveAllConflicts()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to resolve all conflicts");
			//	MainWindow.singleton.HideMergingOverlay();
			//});
		}

		private void resolveAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//// check conflicts
			//if (!RepoScreen.singleton.repoManager.ConflictsExistQuick())
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "No conflicts exist");
			//	return;
			//}

			//// process
			//ResolveAllConflicts();
		}

		private void revertAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to revert all files?\nNOTE: This includes staged and un-staged.", MessageOverlayTypes.YesNo, delegate (MessageOverlayResults result)
			//{
			//	if (result == MessageOverlayResults.Ok)
			//	{
			//		MainWindow.singleton.ShowProcessingOverlay();
			//		RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//		{
			//			if (!RepoScreen.singleton.repoManager.RevertAll()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to revert files");
			//			MainWindow.singleton.HideProcessingOverlay();
			//		});
			//	}
			//});
		}

		private void revertSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//// create list of selected files
			//var fileStates = new List<FileState>();
			//foreach (var item in unstagedChangesListBox.Items)
			//{
			//	var i = (ListBoxItem)item;
			//	var fileState = (FileState)i.Tag;
			//	if (i.IsSelected) fileStates.Add(fileState);
			//}

			//// process selection
			//MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to revert selected files?", MessageOverlayTypes.YesNo, delegate (MessageOverlayResults result)
			//{
			//	if (result == MessageOverlayResults.Ok)
			//	{
			//		MainWindow.singleton.ShowProcessingOverlay();
			//		RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//		{
			//			if (!RepoScreen.singleton.repoManager.RevertFileList(fileStates)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to revert files");
			//			MainWindow.singleton.HideProcessingOverlay();
			//		});
			//	}
			//});
		}

		private void cleanupAllMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to remove all untracked files?", MessageOverlayTypes.YesNo, delegate (MessageOverlayResults result)
			//{
			//	if (result == MessageOverlayResults.Ok)
			//	{
			//		MainWindow.singleton.ShowProcessingOverlay();
			//		RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//		{
			//			if (!RepoScreen.singleton.repoManager.DeleteUntrackedUnstagedFiles(true)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to cleanup files");
			//			MainWindow.singleton.HideProcessingOverlay();
			//		});
			//	}
			//});
		}

		private void cleanupSelectedMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//// create list of selected files
			//var fileStates = new List<FileState>();
			//foreach (var item in unstagedChangesListBox.Items)
			//{
			//	var i = (ListBoxItem)item;
			//	var fileState = (FileState)i.Tag;
			//	if (i.IsSelected) fileStates.Add(fileState);
			//}

			//// process selection
			//MainWindow.singleton.ShowMessageOverlay("Warning", "Are you sure you want to remove selected untracked files?", MessageOverlayTypes.YesNo, delegate (MessageOverlayResults result)
			//{
			//	if (result == MessageOverlayResults.Ok)
			//	{
			//		MainWindow.singleton.ShowProcessingOverlay();
			//		RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//		{
			//			if (!RepoScreen.singleton.repoManager.DeleteUntrackedUnstagedFiles(fileStates, true)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to cleanup files");
			//			MainWindow.singleton.HideProcessingOverlay();
			//		});
			//	}
			//});
		}

		private void ProcessPreview(ListBoxItem item)
		{
			/*var fileState = (FileState)item.Tag;
			var delta = RepoScreen.singleton.repoManager.GetQuickViewData(fileState, true);
			if (delta == null)
			{
				previewTextBox.Visibility = Visibility.Visible;
				previewGrid.Visibility = Visibility.Hidden;
				previewSingleGrid.Visibility = Visibility.Hidden;
				previewTextBox.Document.Blocks.Clear();
				var range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
				range.Text = "<<< Invalid Preview Type >>>";
			}
			else if (delta.GetType().IsSubclassOf(typeof(Exception)))
			{
				previewTextBox.Visibility = Visibility.Visible;
				previewGrid.Visibility = Visibility.Hidden;
				previewSingleGrid.Visibility = Visibility.Hidden;
				previewTextBox.Document.Blocks.Clear();
				var range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
				var e = (Exception)delta;
				range.Text = "ERROR: " + e.Message;
				range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
			}
			else if (delta is string)
			{
				previewTextBox.Visibility = Visibility.Visible;
				previewGrid.Visibility = Visibility.Hidden;
				previewSingleGrid.Visibility = Visibility.Hidden;
				previewTextBox.Document.Blocks.Clear();

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
					void WritePreviewText(string text, SolidColorBrush fBlockColor, SolidColorBrush bBlockColor)
					{
						var end = previewTextBox.Document.ContentEnd;
						var range = new TextRange(end, end);
						range.Text = text;
						range.ApplyPropertyValue(TextElement.ForegroundProperty, fBlockColor);
						if (bBlockColor != null) range.ApplyPropertyValue(TextElement.BackgroundProperty, bBlockColor);
					}
						
					int blockMode = 0;
					SolidColorBrush fBlockBrush = Brushes.Black, bBlockBrush = null;
					string blockText = string.Empty;
					string line = null;
					void CheckBlocks(bool isFinishMode, int currentBlockMode)
					{
						bool switched = currentBlockMode != blockMode;
						if ((switched || isFinishMode) && !string.IsNullOrEmpty(blockText))
						{
							WritePreviewText(blockText, fBlockBrush, bBlockBrush);
							blockText = string.Empty;
						}
					}

					bool ScanBlockPattern(ref string text, int length, char character)
					{
						if (text == null || text.Length < length) return false;
						for (int i = 0; i != length; ++i)
						{
							char c = text[i];
							if (c != character) return false;
						}

						return true;
					}

					bool inConflictDiffMode = false;
					do
					{
						long streamPos = stream.Position;
						line = reader.ReadLine();

						// check for end of file
						if (string.IsNullOrEmpty(line))
						{
							CheckBlocks(true, blockMode);
							continue;
						}
						
						// check for conflicted file changes
						if (inConflictDiffMode || fileState.HasState(FileStates.Conflicted))
						{
							if (ScanBlockPattern(ref line, 7, '<'))
							{
								CheckBlocks(false, 5);
								blockMode = 5;
								fBlockBrush = Brushes.DeepPink;
								bBlockBrush = Brushes.Transparent;
								if (streamPos == 0) blockText += line + '\r';
								else blockText += "\r\r" + line + '\r';

								CheckBlocks(false, 0);
								blockMode = 0;
								fBlockBrush = Brushes.Black;
								bBlockBrush = Brushes.LightGreen;
								inConflictDiffMode = true;
								continue;
							}
							else if (ScanBlockPattern(ref line, 7, '='))
							{
								CheckBlocks(false, 6);
								blockMode = 6;
								fBlockBrush = Brushes.DeepPink;
								bBlockBrush = Brushes.Transparent;
								blockText += line + '\r';

								CheckBlocks(false, 0);
								blockMode = 0;
								fBlockBrush = Brushes.Black;
								bBlockBrush = Brushes.LightBlue;
								continue;
							}
							else if (ScanBlockPattern(ref line, 7, '>'))
							{
								CheckBlocks(false, 7);
								blockMode = 7;
								fBlockBrush = Brushes.DeepPink;
								bBlockBrush = Brushes.Transparent;
								blockText += line + "\r\r";
								inConflictDiffMode = false;
								continue;
							}
							else if (inConflictDiffMode)
							{
								blockText += line + '\r';
								continue;
							}
						}

						// standard changes
						if (line[0] == '+')
						{
							CheckBlocks(false, 1);
							blockMode = 1;
							fBlockBrush = Brushes.Green;
							blockText += line + '\r';
						}
						else if (line[0] == '-')
						{
							CheckBlocks(false, 2);
							blockMode = 2;
							fBlockBrush = Brushes.Red;
							blockText += line + '\r';
						}
						else if (ScanBlockPattern(ref line, 3, '#'))
						{
							CheckBlocks(false, 3);
							blockMode = 3;
							fBlockBrush = Brushes.DarkOrange;
							blockText += "\r\r" + line + '\r';
						}
						else
						{
							CheckBlocks(false, 0);
							blockMode = 0;
							fBlockBrush = Brushes.Black;
							blockText += line + '\r';
						}
					}
					while (line != null);
				}
			}
			else if (delta is PreviewImageData)
			{
				previewTextBox.Visibility = Visibility.Hidden;

				var imageDelta = (PreviewImageData)delta;
				BitmapSource LoadImage(Stream stream, string ext)
				{
					if (stream == null) return new BitmapImage();

					try
					{
						stream.Position = 0;
						bool commonExt = Tools.IsSupportedImageExt(ext, false);
						if (commonExt)
						{
							// load common image types
							var bitmap = new BitmapImage();
							bitmap.BeginInit();
							bitmap.CacheOption = BitmapCacheOption.OnLoad;
							bitmap.UriSource = null;
							bitmap.StreamSource = stream;
							bitmap.EndInit();

							bitmap.Freeze();
							return bitmap;
						}
						else
						{
							// convert ext to enum
							var format = MagickFormat.Unknown;
							bool allowTransparency = true;
							switch (ext)
							{
								case ".tga": format = MagickFormat.Tga; break;
								case ".svg": format = MagickFormat.Svg; break;
								case ".psd": format = MagickFormat.Psd; break;
								case ".pdf": format = MagickFormat.Pdf; allowTransparency = false; break;
							}

							// load uncommon image types
							var settings = new MagickReadSettings();
							settings.Format = format;
							using (var image = new MagickImage(stream, settings))
							{
								if (!allowTransparency) image.HasAlpha = false;
								return image.ToBitmapSource();
							}
						}
					}
					catch (Exception e)
					{
						DebugLog.LogError("Failed to load image: " + e.Message);
						return new BitmapImage();
					}
				}
				
				if (fileState.HasState(FileStates.NewInIndex) || fileState.HasState(FileStates.NewInWorkdir))
				{
					previewSingleGrid.Visibility = Visibility.Visible;
					previewGrid.Visibility = Visibility.Hidden;
					previewImage.Source = LoadImage(imageDelta.newImage, imageDelta.imageExt);
				}
				else
				{
					if (!imageDelta.isMergeDiff)
					{
						oldImageLabel.Content = imageDelta.oldImage != null ? "Old" : "N/A";
						newImageLabel.Content = imageDelta.newImage != null ? "New" : "N/A";
					}
					else
					{
						oldImageLabel.Content = imageDelta.oldImage != null ? "Theirs" : "N/A";
						newImageLabel.Content = imageDelta.newImage != null ? "Ours" : "N/A";
					}

					previewSingleGrid.Visibility = Visibility.Hidden;
					previewGrid.Visibility = Visibility.Visible;
					newImage.Source = LoadImage(imageDelta.newImage, imageDelta.imageExt);
					oldImage.Source = LoadImage(imageDelta.oldImage, imageDelta.imageExt);
				}

				imageDelta.Dispose();
			}
			else
			{
				previewTextBox.Visibility = Visibility.Visible;
				previewGrid.Visibility = Visibility.Hidden;
				previewSingleGrid.Visibility = Visibility.Hidden;
				var range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
				range.Text = "<<< Unsuported Preview Type >>>";
			}*/
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
				//if (stagedChangesListBox.SelectedItem == null) previewTextBox.Document.Blocks.Clear();
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
				//if (unstagedChangesListBox.SelectedItem == null) previewTextBox.Document.Blocks.Clear();
			}
		}

		private void simpleModeMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//AppManager.settings.simpleMode = true;
			//commitAndPushButton.Visibility = Visibility.Hidden;
			//commitButton.Visibility = Visibility.Hidden;
			//pullButton.Visibility = Visibility.Hidden;
			//pushButton.Visibility = Visibility.Hidden;
		}

		private void advancedModeMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//AppManager.settings.simpleMode = false;
			//commitAndPushButton.Visibility = Visibility.Visible;
			//commitButton.Visibility = Visibility.Visible;
			//pullButton.Visibility = Visibility.Visible;
			//pushButton.Visibility = Visibility.Visible;
		}

		private bool PrepCommitMessage(out string message)
		{
			// prep commit message
			message = commitMessageTextBox.Text;
			if (string.IsNullOrEmpty(message) || message.Length < 3)
			{
				//MainWindow.singleton.ShowMessageOverlay("Alert", "You must enter a valid commit message!");
				return false;
			}

			message = message.Replace(Environment.NewLine, "\n");
			return true;
		}

		public void HandleConflics()
		{
			//MainWindow.singleton.ShowMessageOverlay("Error", "Conflicts exist, resolve them now?", MessageOverlayTypes.YesNo, delegate (MessageOverlayResults msgBoxResult)
			//{
			//	if (msgBoxResult == MessageOverlayResults.Ok) ResolveAllConflicts();
			//});
		}

		private void syncButton_Click(object sender, RoutedEventArgs e)
		{
			//// make sure all changes are staged
			//if (AppManager.settings.simpleMode && unstagedChangesListBox.Items.Count != 0)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "You must stage all files first!\nOr use Advanced mode.");
			//	return;
			//}

			//// validate changes exist
			//string msg = null;
			//bool needCommit = false;
			//bool changesExist = RepoScreen.singleton.repoManager.ChangesExist();
			//if (changesExist && stagedChangesListBox.Items.Count != 0)
			//{
			//	// prep commit message
			//	if (!PrepCommitMessage(out msg)) return;
			//	needCommit = true;
			//}

			//// process
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (needCommit && !RepoScreen.singleton.repoManager.CommitStagedChanges(msg, false))
			//	{
			//		RepoScreen.singleton.Refresh();
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to commit changes");
			//		return;
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			commitMessageTextBox.Text = string.Empty;
			//		});
			//	}

			//	var result = RepoScreen.singleton.repoManager.Sync();
			//	if (result != SyncMergeResults.Succeeded)
			//	{
			//		if (result == SyncMergeResults.Conflicts) HandleConflics();
			//		else if (result == SyncMergeResults.Error) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to sync changes");
			//	}

			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void commitAndPushButton_Click(object sender, RoutedEventArgs e)
		{
			//// validate changes exist
			//bool changesExist = RepoScreen.singleton.repoManager.ChangesExist();
			//if (changesExist && stagedChangesListBox.Items.Count == 0)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Error", "There are no staged files to commit");
			//	return;
			//}

			//// prep commit message
			//string msg = null;
			//if (changesExist && !PrepCommitMessage(out msg)) return;

			//// process
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (changesExist && !RepoScreen.singleton.repoManager.CommitStagedChanges(msg, false))
			//	{
			//		RepoScreen.singleton.Refresh();
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to commit changes");
			//		return;
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			commitMessageTextBox.Text = string.Empty;
			//		});
			//	}

			//	if (!RepoScreen.singleton.repoManager.Push()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to push changes");
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void commitButton_Click(object sender, RoutedEventArgs e)
		{
			//// validate changes exist
			//bool changesExist = RepoScreen.singleton.repoManager.ChangesExist();
			//if (changesExist && stagedChangesListBox.Items.Count == 0)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Error", "There are no staged files to commit");
			//	return;
			//}

			//// prep commit message
			//if (!PrepCommitMessage(out string msg)) return;

			//// process
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.CommitStagedChanges(msg))
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to commit changes");
			//	}
			//	else
			//	{
			//		Dispatcher.InvokeAsync(delegate ()
			//		{
			//			commitMessageTextBox.Text = string.Empty;
			//		});
			//	}

			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		internal void pullButton_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	var result = RepoScreen.singleton.repoManager.Pull();
			//	if (result == SyncMergeResults.Conflicts)
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Conflicts exist after pull, resolve them now?", MessageOverlayTypes.YesNo, delegate (MessageOverlayResults msgBoxresult)
			//		{
			//			if (msgBoxresult == MessageOverlayResults.Ok) ResolveAllConflicts();
			//		});
			//	}
			//	else if (result == SyncMergeResults.Error)
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to pull changes");
			//	}

			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		internal void pushButton_Click(object sender, RoutedEventArgs e)
		{
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.Push()) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to push changes");
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void preivewDiffMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//// check for selection
			//var stagedItem = stagedChangesListBox.SelectedItem as ListBoxItem;
			//var unstagedItem = unstagedChangesListBox.SelectedItem as ListBoxItem;
			//if (stagedItem == null && unstagedItem == null)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "No file selected to preview");
			//	return;
			//}

			//var fileState = (stagedItem != null) ? (FileState)stagedItem.Tag : null;
			//if (fileState == null) fileState = (unstagedItem != null) ? (FileState)unstagedItem.Tag : null;

			//// validate isn't new file
			//if (fileState.HasState(FileStates.NewInIndex) || fileState.HasState(FileStates.NewInWorkdir))
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Note", "File has no history.");
			//	return;
			//}

			//// process
			//MainWindow.singleton.ShowWaitingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate ()
			//{
			//	if (!RepoScreen.singleton.repoManager.OpenDiffTool(fileState)) MainWindow.singleton.ShowMessageOverlay("Error", "Failed to show diff");
			//	MainWindow.singleton.HideWaitingOverlay();
			//});
		}

		private void quickRefreshButton_Click(object sender, RoutedEventArgs e)
		{
			RepoScreen.singleton.QuickRefresh();
		}
	}
}
