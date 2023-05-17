﻿using GitCommander;
using GitItGUI.Core;
using GitItGUI.UI.Overlays;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Interactivity;

namespace GitItGUI.UI.Screens
{
    /// <summary>
    /// Interaction logic for RepoUserControl.xaml
    /// </summary>
    public partial class RepoScreen : UserControl
    {
		public static RepoScreen singleton;
		public RepoManager repoManager;

		private TabItem lastTabItem;

		public RepoScreen()
        {
			singleton = this;
            InitializeComponent();
			grid.IsVisible = false;
		}

		public void Init()
		{
			//repoManager = new RepoManager(RepoReadyCallback);
			//repoManager.RepoRefreshedCallback += repoManager_RefreshedCallback;
			//changesTab.Init();
		}

		private void RepoReadyCallback(Dispatcher dispatcher)
		{
			//Dispatcher.UIThread.InvokeAsync(delegate()
			//{
			//	dispatcher.UnhandledException += MainWindow.singleton.Dispatcher_UnhandledException;
			//	grid.IsVisible = true;
			//});
		}

		public void Dispose()
		{
			//changesTab.ClosingRepo();
			//repoManager.Dispose();
		}

		private void CheckSync()
		{
			string upToDateMsg = "ERROR";
			if (repoManager.ChangesExist()) upToDateMsg = "Out of sync";
			else upToDateMsg = repoManager.isInSync != null ? (repoManager.isInSync.Value ? "Up to date" : "Out of sync") : "Check sync error";
			string branchName = repoManager.activeBranch != null ? repoManager.activeBranch.fullname : "N/A";
			repoTitleTextBlock.Text = string.Format("Current Repo '{0}' ({1}) [{2}]", System.IO.Path.GetFileName(repoManager.repository.repoPath), branchName, upToDateMsg);
		}

		private void PrepOpen()
		{
			//changesTab.LoadCommitMessage();
			//StartScreen.singleton.Refresh();
			//CheckSync();
			//tabControl.SelectedIndex = 0;
			//MainWindow.singleton.Navigate(this);
		}

		public void OpenRepo(string folderPath)
		{
			//MainWindow.singleton.ShowProcessingOverlay();
			//repoManager.dispatcher.InvokeAsync(delegate()
			//{
			//	if (repoManager.Open(folderPath))
			//	{
			//		Dispatcher.UIThread.InvokeAsync(delegate()
			//		{
			//			// prep
			//			PrepOpen();

			//			// check repo fragmentation
			//			if (!repoManager.ChangesExist())
			//			{
			//				int count = repoManager.UnpackedObjectCount(out string size);
			//				if (count >= 1000)
			//				{
			//					string msg = string.Format("Your repo is fragmented, would you like to optimize?\nThere are '{0}' loose objects totalling '{1}' in size.", count, size);

			//					int lfsCount = -1;
			//					string lfsSize = null, option = null;
			//					if (repoManager.lfsEnabled)
			//					{
			//						lfsCount = repoManager.UnusedLFSFiles(out lfsSize);
			//						if (lfsCount >= 1000)
			//						{
			//							option = "CleanUp LFS file";
			//							msg += string.Format("\n\nYou also have '{0}' unused lfs files totalling '{1}' in size.", lfsCount, lfsSize);
			//						}
			//					}

			//					MainWindow.singleton.ShowMessageOverlay("Optimize", msg, option, MessageOverlayTypes.OkCancel, delegate(MessageOverlayResults result)
			//					{
			//						if (result == MessageOverlayResults.Ok)
			//						{
			//							bool pruneLFS = MessageOverlay.optionChecked;
			//							MainWindow.singleton.ShowProcessingOverlay();
			//							repoManager.dispatcher.InvokeAsync(delegate()
			//							{
			//								repoManager.Optimize();
			//								if (pruneLFS && repoManager.lfsEnabled) repoManager.PruneLFSFiles();
			//								MainWindow.singleton.HideProcessingOverlay();
			//							});
			//						}
			//					});
			//				}
			//			}
			//		});
			//	}
			//	else
			//	{
			//		Dispatcher.UIThread.InvokeAsync(delegate()
			//		{
			//			AppManager.RemoveRepoFromHistory(folderPath);
			//			StartScreen.singleton.RefreshHistory();
			//		});

			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to open repo");
			//	}
				
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private bool WriteUserNameCallback(StreamWriter writer)
		{
			DebugLog.LogError("WriteUserNameCallback Failed: Not implemented!");
			return false;
		}

		private bool WritePasswordCallback(StreamWriter writer)
		{
			DebugLog.LogError("WriteUserNameCallback Failed: Not implemented!");
			return false;
		}

		public void CloneRepo(string clonePath, string repoURL)
		{
			//MainWindow.singleton.ShowProcessingOverlay();
			//repoManager.dispatcher.InvokeAsync(delegate()
			//{
			//	if (repoManager.Clone(repoURL, clonePath, out string repoPath, WriteUserNameCallback, WritePasswordCallback))
			//	{
			//		if (repoManager.Open(repoPath))
			//		{
			//			Dispatcher.UIThread.InvokeAsync(delegate()
			//			{
			//				PrepOpen();
			//			});
			//		}
			//		else
			//		{
			//			AppManager.RemoveRepoFromHistory(clonePath);
			//			MainWindow.singleton.ShowMessageOverlay("Error", "Failed to open cloned repo");
			//		}
			//	}
			//	else
			//	{
			//		AppManager.RemoveRepoFromHistory(clonePath);
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to clone repo");
			//	}
				
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		public void CreateRepo(string createPath, bool isLFSEnabled, bool addLFSDefaultExts)
		{
			//MainWindow.singleton.ShowProcessingOverlay();
			//repoManager.dispatcher.InvokeAsync(delegate()
			//{
			//	if (repoManager.Create(createPath))
			//	{
			//		repoManager.disableRepoRefreshedCallback = true;
			//		bool lfsPassed = true;
			//		if (repoManager.Open(createPath))
			//		{
			//			if (isLFSEnabled)
			//			{
			//				if (!repoManager.AddGitLFSSupport(addLFSDefaultExts))
			//				{
			//					MainWindow.singleton.ShowMessageOverlay("Error", "Failed to add LFS support");
			//					MainWindow.singleton.Navigate(StartScreen.singleton);
			//					lfsPassed = false;
			//				}
			//			}

			//			if (lfsPassed)
			//			{
			//				Dispatcher.UIThread.InvokeAsync(delegate()
			//				{
			//					PrepOpen();
			//				});
			//			}
			//		}
			//		else
			//		{
			//			AppManager.RemoveRepoFromHistory(createPath);
			//			MainWindow.singleton.ShowMessageOverlay("Error", "Failed to open created repo");
			//		}

			//		repoManager.disableRepoRefreshedCallback = false;
			//		if (lfsPassed) repoManager_RefreshedCallback(false);
			//	}
			//	else
			//	{
			//		AppManager.RemoveRepoFromHistory(createPath);
			//		MainWindow.singleton.ShowMessageOverlay("Error", "Failed to create repo");
			//	}
				
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}
		
		private void repoManager_RefreshedCallback(bool isQuickRefresh)
		{
			//void RefreshInternal()
			//{
			//	if (!repoManager.isOpen) return;

			//	if (!isQuickRefresh && repoManager.isEmpty)
			//	{
			//		MainWindow.singleton.ShowMessageOverlay("Empty Repo", "Nothing has been commit to this repo, a first commit must be made to open it.", MessageOverlayTypes.OkCancel, delegate(MessageOverlayResults result)
			//		{
			//			if (result == MessageOverlayResults.Ok)
			//			{
			//				MainWindow.singleton.ShowProcessingOverlay();
			//				repoManager.dispatcher.InvokeAsync(delegate()
			//				{
			//					if (!repoManager.AddFirstAutoCommit())
			//					{
			//						MainWindow.singleton.ShowMessageOverlay("Error", "Failed to auto generate a 'first commit'");
			//						MainWindow.singleton.Navigate(StartScreen.singleton);
			//					}

			//					MainWindow.singleton.HideProcessingOverlay();
			//				});
			//			}
			//			else
			//			{
			//				MainWindow.singleton.Navigate(StartScreen.singleton);
			//			}
			//		});

			//		return;
			//	}

			//	changesTab.Refresh();
			//	if (!isQuickRefresh)
			//	{
			//		branchesTab.Refresh();
			//		settingsTab.Refresh();
			//		terminalTab.Refresh();
			//	}

			//	CheckSync();
			//}

			//if (Dispatcher.UIThread.CheckAccess())
			//{
			//	RefreshInternal();
			//}
			//else
			//{
			//	Dispatcher.UIThread.InvokeAsync(delegate()
			//	{
			//		RefreshInternal();
			//	});
			//}
		}

		public void Refresh()
		{
			//void Invoke()
			//{
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!repoManager.Refresh())
			//		{
			//			MainWindow.singleton.ShowMessageOverlay("Error", "Failed to refresh repo");
			//			MainWindow.singleton.Navigate(StartScreen.singleton);
			//		}

			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//}

			//if (repoManager != null && repoManager.isOpen)
			//{
			//	if (repoManager.dispatcher.CheckAccess())
			//	{
			//		Invoke();
			//	}
			//	else
			//	{
			//		repoManager.dispatcher.InvokeAsync(delegate()
			//		{
			//			Invoke();
			//		});
			//	}
			//}
		}

		public void QuickRefresh()
		{
			//void Invoke()
			//{
			//	MainWindow.singleton.ShowProcessingOverlay();
			//	repoManager.dispatcher.InvokeAsync(delegate()
			//	{
			//		if (!repoManager.QuickRefresh())
			//		{
			//			MainWindow.singleton.ShowMessageOverlay("Error", "Failed to quick refresh repo");
			//			MainWindow.singleton.Navigate(StartScreen.singleton);
			//		}

			//		MainWindow.singleton.HideProcessingOverlay();
			//	});
			//}

			//if (repoManager != null && repoManager.isOpen)
			//{
			//	if (repoManager.dispatcher.CheckAccess())
			//	{
			//		Invoke();
			//	}
			//	else
			//	{
			//		repoManager.dispatcher.InvokeAsync(delegate()
			//		{
			//			Invoke();
			//		});
			//	}
			//}
		}

		private void backButton_Click(object sender, RoutedEventArgs e)
		{
			//changesTab.ClosingRepo();
			//repoManager.Close();
			//MainWindow.singleton.Navigate(StartScreen.singleton);
		}

		private void refreshButton_Click(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//var selected = (TabItem)tabControl.SelectedItem;
			//if (selected == terminalTabItem) terminalTab.ScrollToEnd();
			//else if (lastTabItem == terminalTabItem) terminalTab.CheckRefreshPending();
			//lastTabItem = selected;
		}
	}
}
