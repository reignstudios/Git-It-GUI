using GitItGUI.Core;
using GitItGUI.UI.Utils;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace GitItGUI.UI.Screens
{
	/// <summary>
	/// Interaction logic for StartUserControl.xaml
	/// </summary>
	public partial class StartScreen : UserControl
	{
		public static StartScreen singleton;
		private DispatcherTimer timer;
		private bool outOfDate, outOfDateFlash;
		private BitmapImage outOfDateImage, outOfDateImageFlash;

		public StartScreen()
		{
			singleton = this;
			InitializeComponent();

			outOfDateImage = new BitmapImage(new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/Update.png"));
			outOfDateImageFlash = new BitmapImage(new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/UpdateFlash.png"));

			updateImage.Visibility = Visibility.Hidden;
			updateImage.MouseUp += UpdateImage_MouseUp;
			timer = new DispatcherTimer(TimeSpan.FromSeconds(.25), DispatcherPriority.Background, DispatcherCallback, Dispatcher);
		}

		private void DispatcherCallback(object sender, EventArgs e)
		{
			if (outOfDate && Visibility == Visibility.Visible)
			{
				updateImage.Source = outOfDateFlash ? outOfDateImage : outOfDateImageFlash;
				outOfDateFlash = !outOfDateFlash;
			}
		}

		public void Init()
		{
			RefreshHistory();
		}

		public void EnabledOutOfDate()
		{
			updateImage.Visibility = Visibility.Visible;
			outOfDate = true;
		}

		internal void RefreshHistory()
		{
			historyListBox.Items.Clear();
			foreach (var repo in AppManager.repositories)
			{
				var item = new ListBoxItem();
				item.Content = Path.GetFileName(repo);
				item.ToolTip = repo;
				item.FontSize = 24;

				item.HorizontalContentAlignment = HorizontalAlignment.Center;
				item.MouseDoubleClick += Item_MouseDoubleClick;
				item.ContextMenu = new ContextMenu();

				// open folder path
				var menuItem = new MenuItem();
				menuItem.Header = "Open folder path";
				menuItem.ToolTip = repo;
				menuItem.Click += OpenRepoMenuItem_Click;
				item.ContextMenu.Items.Add(menuItem);

				// remove repo from history
				menuItem = new MenuItem();
				menuItem.Header = "Remove from history";
				menuItem.ToolTip = repo;
				menuItem.Click += RemoveHistoryMenuItem_Click;
				item.ContextMenu.Items.Add(menuItem);

				historyListBox.Items.Add(item);
			}
		}

		public void Refresh()
		{
			if (MainWindow.singleton.Dispatcher.CheckAccess())
			{
				RefreshHistory();
			}
			else
			{
				MainWindow.singleton.Dispatcher.InvokeAsync(delegate()
				{
					RefreshHistory();
				});
			}
		}

		private void OpenRepoMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			string repo = (string)item.ToolTip;
			if (!Tools.OpenFolderLocation(repo))
			{
				AppManager.RemoveRepoFromHistory(repo);
				RefreshHistory();
			}
		}

		private void RemoveHistoryMenuItem_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			string repo = (string)item.ToolTip;
			AppManager.RemoveRepoFromHistory(repo);
			RefreshHistory();
		}

		private void Item_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var item = (ListBoxItem)sender;
			string repo = (string)item.ToolTip;
			RepoScreen.singleton.OpenRepo(repo);
		}

		private void openButton_Click(object sender, RoutedEventArgs e)
		{
			if (PlatformUtils.SelectFolder(out string folderPath))
			{
				RepoScreen.singleton.OpenRepo(folderPath);
			}
		}

		private void cloneButton_Click(object sender, RoutedEventArgs e)
		{
			CloneScreen.singleton.Setup();
			MainWindow.singleton.Navigate(CloneScreen.singleton);
		}

		private void createButton_Click(object sender, RoutedEventArgs e)
		{
			CreateScreen.singleton.Setup();
			MainWindow.singleton.Navigate(CreateScreen.singleton);
		}

		private void settingsButton_Click(object sender, RoutedEventArgs e)
		{
			SettingsScreen.singleton.Setup();
			MainWindow.singleton.Navigate(SettingsScreen.singleton);
		}

		private void UpdateImage_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			using (var process = Process.Start("https://github.com/reignstudios/Git-It-GUI/releases"))
			{
				process.WaitForExit();
			}
		}
	}
}
