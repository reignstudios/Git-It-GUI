using GitItGUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.ComponentModel;
using GitItGUI.UI.Overlays;
using System.Diagnostics;
using ImageMagick;
using Avalonia.Threading;

namespace GitItGUI.UI
{
	public partial class MainWindow : Window
	{
		public static MainWindow singleton;

		public MainWindow()
		{
			InitializeComponent();

			// UnhandledException
			#if !DEBUG
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			Dispatcher.UnhandledException += Dispatcher_UnhandledException;
			#endif

			// init debug log
			DebugLog.Log("Git-It-GUI v" + VersionInfo.version + Environment.NewLine);

			// init
			singleton = this;
			InitializeComponent();

			// set pdf image processor location (just use default if you want pdf support)
			//MagickNET.SetGhostscriptDirectory(Environment.CurrentDirectory);
			
			// init app manager
			if (!AppManager.Init())
			{
				MessageBox.Show(this, "Failed to start AppManager");
				Environment.Exit(1);
				return;
			}

			// init screens
			startScreen.Init();
			repoScreen.Init();

			// validate diff/merge tool exists
			settingsScreen.ValidateDiffMergeTool();

			// position/size window from settings
			if (AppManager.settings.winX != -1)
			{
				Left = AppManager.settings.winX;
				Top = AppManager.settings.winY;
				Width = AppManager.settings.winWidth;
				Height = AppManager.settings.winHeight;
				if (AppManager.settings.winMaximized) WindowState = WindowState.Maximized;
			}

			// validate system
			Title += " v" + VersionInfo.versionType;
			AppManager.ValidateSystem("http://reign-studios-services.com/GitItGUI/VersionInfo.xml", CheckForUpdatesCallback);
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var ex = e.ExceptionObject as Exception;
			if (ex != null)
			{
				string error = "CRITICAL AppDomain: " + ex.Message;
				MessageBox.Show(error);
				DebugLog.LogError(error);
				DebugLog.LogError("STACKTRACE: " + ex.StackTrace);
			}
			else
			{
				const string error = "CRITICAL AppDomain: Unknown error";
				MessageBox.Show(error);
				DebugLog.LogError(error);
			}

			DebugLog.Dispose();
			Environment.Exit(2);
		}

		#if !DEBUG
		internal void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			var ex = e.Exception;
			if (ex != null)
			{
				string error = "CRITICAL Dispatcher: " + ex.Message;
				MessageBox.Show(error);
				DebugLog.LogError(error);
				DebugLog.LogError("STACKTRACE: " + ex.StackTrace);
			}
			else
			{
				const string error = "CRITICAL Dispatcher: Unknown error";
				MessageBox.Show(error);
				DebugLog.LogError(error);
			}

			DebugLog.Dispose();
			Environment.Exit(2);
		}
		#endif

		public void ShowSystemErrorMessageBox(string title, string message)
		{
			MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void CheckForUpdatesCallback(UpdateCheckResult result)
		{
			Dispatcher.UIThread.InvokeAsync(delegate()
			{
				bool shouldExit = true;
				switch (result)
				{
					case UpdateCheckResult.BadVersionError: ShowSystemErrorMessageBox("Error", "Git or (git-lfs) versions are incompatible with this app"); break;
					case UpdateCheckResult.GitVersionToLowForLFS: ShowSystemErrorMessageBox("Error", "The git version installed is incompatible with the lfs version isntalled"); break;

					case UpdateCheckResult.GitNotInstalledError: ShowSystemErrorMessageBox("Error", "Git is not installed or installed incorrectly.\nMake sure you're able to use it in cmd/term prompt"); break;
					case UpdateCheckResult.GitLFSNotInstalledError: ShowSystemErrorMessageBox("Error", "Git-LFS is not installed or installed incorrectly.\nMake sure you're able to use it in cmd/term prompt"); break;
			
					case UpdateCheckResult.GitVersionCheckError: ShowSystemErrorMessageBox("Error", "Git version parse failed.\nIts possible the git version you're using isn't supported"); break;
					case UpdateCheckResult.GitLFSVersionCheckError: ShowSystemErrorMessageBox("Error", "Git-LFS version parse failed.\nIts possible the git-lfs version you're using isn't supported"); break;

					case UpdateCheckResult.UnicodeSettingsFailed: ShowSystemErrorMessageBox("Error", "Git unicode support is not enabled or failed to be enabled!"); break;

					case UpdateCheckResult.AppVersionOutOfDate:
						shouldExit = false;
						startScreen.EnabledOutOfDate();
						break;

					case UpdateCheckResult.AppVersionParseError:
					case UpdateCheckResult.Success:
					case UpdateCheckResult.CommonError:
						shouldExit = false;
						break;
				}

				if (shouldExit) Environment.Exit(0);
			});
		}

		/*protected override void OnClosing(CancelEventArgs e)
		{
			// validate we are in a quitable state
			if
			(
				processingOverlay.Visibility == Visibility.Visible ||
				waitingOverlay.Visibility == Visibility.Visible ||
				messageOverlay.Visibility == Visibility.Visible ||
				nameEntryOverlay.Visibility == Visibility.Visible ||
				mergingOverlay.Visibility == Visibility.Visible
			)
			{
				e.Cancel = true;
				return;
			}

			DebugLog.Log("Closing main window...");

			// apply screen state
			AppManager.settings.winX = (int)Left;
			AppManager.settings.winY = (int)Top;
			AppManager.settings.winWidth = (int)Width;
			AppManager.settings.winHeight = (int)Height;
			AppManager.settings.winMaximized = WindowState == WindowState.Maximized;

			// apply setting if UI open
			if (settingsScreen.Visibility == Visibility.Visible) settingsScreen.Apply();

			// finish
			repoScreen.Dispose();
			AppManager.SaveSettings();
			AppManager.Dispose();
			base.OnClosing(e);
		}*/

		/*protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			
			//#if !DEBUG
			if
			(
				repoScreen != null &&
				processingOverlay.Visibility != Visibility.Visible &&
				waitingOverlay.Visibility != Visibility.Visible &&
				messageOverlay.Visibility != Visibility.Visible &&
				nameEntryOverlay.Visibility != Visibility.Visible &&
				mergingOverlay.Visibility != Visibility.Visible
			)
			{
				if (AppManager.settings.autoRefreshChanges && repoScreen.repoManager != null && repoScreen.repoManager.isOpen) repoScreen.QuickRefresh();
			}
			//#endif
		}*/

		public void Navigate(UserControl screen)
		{
			void UpdateVisibility()
			{
				// disable all screens
				startScreen.isVisible = false;
				settingsScreen.isVisible = false;
				repoScreen.isVisible = false;
				cloneScreen.isVisible = false;
				createScreen.isVisible = false;

				// enable current
				screen.IsVisible = true;
			}

			if (Dispatcher.UIThread.CheckAccess())
			{
				UpdateVisibility();
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate()
				{
					UpdateVisibility();
				});
			}
		}

		public void ShowProcessingOverlay()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				processingOverlay.IsVisible = true;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					processingOverlay.IsVisible = true;
				});
			}
		}

		public void HideProcessingOverlay()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				processingOverlay.IsVisible = false;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					processingOverlay.IsVisible = false;
				});
			}
		}

		private void ShowMessageOverlayInternal(string title, string message, string option, MessageOverlayTypes type, MessageOverlay.DoneCallbackMethod doneCallback)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				messageOverlay.Setup(title, message, option, type, doneCallback);
				messageOverlay.IsVisible = true;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					messageOverlay.Setup(title, message, option, type, doneCallback);
					messageOverlay.IsVisible = true;
				});
			}
		}

		public void ShowMessageOverlay(string title, string message, MessageOverlayTypes type = MessageOverlayTypes.Ok, MessageOverlay.DoneCallbackMethod doneCallback = null)
		{
			ShowMessageOverlayInternal(title, message, null, type, doneCallback);
		}

		public void ShowMessageOverlay(string title, string message, string option, MessageOverlayTypes type = MessageOverlayTypes.Ok, MessageOverlay.DoneCallbackMethod doneCallback = null)
		{
			ShowMessageOverlayInternal(title, message, option, type, doneCallback);
		}

		public void HideMessageOverlay()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				messageOverlay.IsVisible = false;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					messageOverlay.IsVisible = false;
				});
			}
		}

		public void ShowMergingOverlay(string filePath = null, bool isBinaryMode = false, MergeConflictOverlay.DoneCallbackMethod doneCallback = null)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				mergingOverlay.Setup(filePath, isBinaryMode, doneCallback);
				mergingOverlay.IsVisible = true;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					mergingOverlay.Setup(filePath, isBinaryMode, doneCallback);
					mergingOverlay.IsVisible = true;
				});
			}
		}

		public void HideMergingOverlay()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				mergingOverlay.IsVisible = false;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					mergingOverlay.IsVisible = false;
				});
			}
		}

		public void ShowWaitingOverlay()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				waitingOverlay.IsVisible = true;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					waitingOverlay.IsVisible = true;
				});
			}
		}

		public void HideWaitingOverlay()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				waitingOverlay.IsVisible = false;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					waitingOverlay.IsVisible = false;
				});
			}
		}

		public void ShowNameEntryOverlay(string currentName, bool showRemotesOption, NameEntryOverlay.DoneCallbackMethod doneCallback)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				nameEntryOverlay.Setup(currentName, showRemotesOption, doneCallback);
				nameEntryOverlay.IsVisible = true;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					nameEntryOverlay.Setup(currentName, showRemotesOption, doneCallback);
					nameEntryOverlay.IsVisible = true;
				});
			}
		}

		public void HideNameEntryOverlay()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				nameEntryOverlay.IsVisible = false;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate ()
				{
					nameEntryOverlay.IsVisible = false;
				});
			}
		}
	}
}