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
			
			if (!AppManager.Init())
			{
				MessageBox.Show(this, "Failed to start AppManager");
				Environment.Exit(0);
				return;
			}

			startScreen.Init();
			repoScreen.Init();

			if (AppManager.settings.winX != -1)
			{
				Left = AppManager.settings.winX;
				Top = AppManager.settings.winY;
				Width = AppManager.settings.winWidth;
				Height = AppManager.settings.winHeight;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
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

			AppManager.settings.winX = (int)Left;
			AppManager.settings.winY = (int)Top;
			AppManager.settings.winWidth = (int)Width;
			AppManager.settings.winHeight = (int)Height;

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
				repoScreen.Refresh();
			}
			#endif
		}

		public void Navigate(UserControl screen)
		{
			void UpdateVisibility()
			{
				startScreen.Visibility = Visibility.Hidden;
				settingsScreen.Visibility = Visibility.Hidden;
				repoScreen.Visibility = Visibility.Hidden;
				if (screen == startScreen) startScreen.Visibility = Visibility.Visible;
				else if (screen == settingsScreen) settingsScreen.Visibility = Visibility.Visible;
				else if (screen == repoScreen) repoScreen.Visibility = Visibility.Visible;
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
