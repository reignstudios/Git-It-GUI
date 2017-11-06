using GitItGUI.Core;
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
using System.ComponentModel;
using GitItGUI.UI.Overlays;
using System.Diagnostics;
using ImageMagick;

namespace GitItGUI.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static MainWindow singleton;

		public MainWindow()
		{
			// UnhandledException
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			Dispatcher.UnhandledException += Dispatcher_UnhandledException;

			// init
			singleton = this;
			InitializeComponent();

			// set pdf image processor location
			MagickNET.SetGhostscriptDirectory(Environment.CurrentDirectory);
			
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

			// RepoManager Dispatcher UnhandledException
			repoScreen.repoManager.dispatcher.UnhandledException += Dispatcher_UnhandledException;

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

			// version check
			Title += " v" + VersionInfo.versionType;
			AppManager.CheckForUpdates("http://reign-studios-services.com/GitItGUI/VersionInfo.xml", CheckForUpdatesCallback);
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

			Environment.Exit(2);
		}

		private void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
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

			Environment.Exit(2);
		}

		private void CheckForUpdatesCallback(UpdateCheckResult result)
		{
			bool shouldExit = true;
			switch (result)
			{
				case UpdateCheckResult.BadVersionError: ShowMessageOverlay("Error", "Git or (git-lfs) versions are incompatible with this app"); break;
				case UpdateCheckResult.GitVersionToLowForLFS: ShowMessageOverlay("Error", "The git version installed is incompatible with the lfs version isntalled"); break;

				case UpdateCheckResult.GitNotInstalledError: ShowMessageOverlay("Error", "Git is not installed or installed incorrectly.\nMake sure you're able to use it in cmd/term prompt"); break;
				case UpdateCheckResult.GitLFSNotInstalledError: ShowMessageOverlay("Error", "Git-LFS is not installed or installed incorrectly.\nMake sure you're able to use it in cmd/term prompt"); break;
			
				case UpdateCheckResult.GitVersionCheckError: ShowMessageOverlay("Error", "Git version parse failed.\nIts possible the git version you're using isn't supported"); break;
				case UpdateCheckResult.GitLFSVersionCheckError: ShowMessageOverlay("Error", "Git-LFS version parse failed.\nIts possible the git-lfs version you're using isn't supported"); break;

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
		}

		protected override void OnClosing(CancelEventArgs e)
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
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			
			#if !DEBUG
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
				if (AppManager.settings.autoRefreshChanges) repoScreen.Refresh();
			}
			#endif
		}

		public void Navigate(UserControl screen)
		{
			void UpdateVisibility()
			{
				// disable all screens
				startScreen.Visibility = Visibility.Hidden;
				settingsScreen.Visibility = Visibility.Hidden;
				repoScreen.Visibility = Visibility.Hidden;
				cloneScreen.Visibility = Visibility.Hidden;
				createScreen.Visibility = Visibility.Hidden;

				// enable current
				screen.Visibility = Visibility.Visible;
			}

			if (Dispatcher.CheckAccess())
			{
				UpdateVisibility();
			}
			else
			{
				Dispatcher.InvokeAsync(delegate()
				{
					UpdateVisibility();
				});
			}
		}

		public void ShowProcessingOverlay()
		{
			if (Dispatcher.CheckAccess())
			{
				processingOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					processingOverlay.Visibility = Visibility.Visible;
				});
			}
		}

		public void HideProcessingOverlay()
		{
			if (Dispatcher.CheckAccess())
			{
				processingOverlay.Visibility = Visibility.Hidden;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					processingOverlay.Visibility = Visibility.Hidden;
				});
			}
		}

		private void ShowMessageOverlayInternal(string title, string message, string option, MessageOverlayTypes type, MessageOverlay.DoneCallbackMethod doneCallback)
		{
			if (Dispatcher.CheckAccess())
			{
				messageOverlay.Setup(title, message, option, type, doneCallback);
				messageOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					messageOverlay.Setup(title, message, option, type, doneCallback);
					messageOverlay.Visibility = Visibility.Visible;
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
			if (Dispatcher.CheckAccess())
			{
				messageOverlay.Visibility = Visibility.Hidden;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					messageOverlay.Visibility = Visibility.Hidden;
				});
			}
		}

		public void ShowMergingOverlay(string filePath = null, bool isBinaryMode = false, MergeConflictOverlay.DoneCallbackMethod doneCallback = null)
		{
			if (Dispatcher.CheckAccess())
			{
				mergingOverlay.Setup(filePath, isBinaryMode, doneCallback);
				mergingOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					mergingOverlay.Setup(filePath, isBinaryMode, doneCallback);
					mergingOverlay.Visibility = Visibility.Visible;
				});
			}
		}

		public void HideMergingOverlay()
		{
			if (Dispatcher.CheckAccess())
			{
				mergingOverlay.Visibility = Visibility.Hidden;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					mergingOverlay.Visibility = Visibility.Hidden;
				});
			}
		}

		public void ShowWaitingOverlay()
		{
			if (Dispatcher.CheckAccess())
			{
				waitingOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					waitingOverlay.Visibility = Visibility.Visible;
				});
			}
		}

		public void HideWaitingOverlay()
		{
			if (Dispatcher.CheckAccess())
			{
				waitingOverlay.Visibility = Visibility.Hidden;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					waitingOverlay.Visibility = Visibility.Hidden;
				});
			}
		}

		public void ShowNameEntryOverlay(string currentName, bool showRemotesOption, NameEntryOverlay.DoneCallbackMethod doneCallback)
		{
			if (Dispatcher.CheckAccess())
			{
				nameEntryOverlay.Setup(currentName, showRemotesOption, doneCallback);
				nameEntryOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					nameEntryOverlay.Setup(currentName, showRemotesOption, doneCallback);
					nameEntryOverlay.Visibility = Visibility.Visible;
				});
			}
		}

		public void HideNameEntryOverlay()
		{
			if (Dispatcher.CheckAccess())
			{
				nameEntryOverlay.Visibility = Visibility.Hidden;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					nameEntryOverlay.Visibility = Visibility.Hidden;
				});
			}
		}
	}
}
