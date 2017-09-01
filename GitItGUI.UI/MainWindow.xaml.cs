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

			repoScreen.Init();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			repoScreen.Dispose();
			AppManager.Dispose();
			base.OnClosing(e);
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
	}
}
