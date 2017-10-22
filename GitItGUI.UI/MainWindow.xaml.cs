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
			singleton = this;
			InitializeComponent();
			
			// init app manager
			if (!AppManager.Init())
			{
				MessageBox.Show(this, "Failed to start AppManager");
				Environment.Exit(0);
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
			}

			// version check
			Title += " v" + VersionInfo.versionType;
			AppManager.CheckForUpdates("http://reign-studios-services.com/GitItGUI/VersionInfo.xml", CheckForUpdatesCallback);
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

		public void ShowMessageOverlay(string title, string message, MessageOverlayTypes type = MessageOverlayTypes.Ok, MessageOverlay.DoneCallbackMethod doneCallback = null)
		{
			if (Dispatcher.CheckAccess())
			{
				messageOverlay.Setup(title, message, type, doneCallback);
				messageOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					messageOverlay.Setup(title, message, type, doneCallback);
					messageOverlay.Visibility = Visibility.Visible;
				});
			}
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

		public void ShowMergingOverlay(string filePath, MergeConflictOverlay.DoneCallbackMethod doneCallback)
		{
			if (Dispatcher.CheckAccess())
			{
				mergingOverlay.Setup(filePath, doneCallback);
				mergingOverlay.Visibility = Visibility.Visible;
			}
			else
			{
				Dispatcher.InvokeAsync(delegate ()
				{
					mergingOverlay.Setup(filePath, doneCallback);
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
