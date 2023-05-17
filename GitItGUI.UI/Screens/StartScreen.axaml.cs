using GitItGUI.Core;
using GitItGUI.UI.Utils;
using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using System.Collections.Generic;

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
		private Bitmap outOfDateImage, outOfDateImageFlash;

		public StartScreen()
		{
			singleton = this;
			InitializeComponent();

			outOfDateImage = new Bitmap("Images/Update.png");
			outOfDateImageFlash = new Bitmap("Images/UpdateFlash.png");

			updateImage.IsVisible = false;
			//updateImage.MouseUp += UpdateImage_MouseUp;
			//timer = new DispatcherTimer(TimeSpan.FromSeconds(.25), DispatcherPriority.Background, DispatcherCallback, Dispatcher);
		}

		private void DispatcherCallback(object sender, EventArgs e)
		{
			if (outOfDate && IsVisible)
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
			updateImage.IsVisible = true;
			outOfDate = true;
		}

		internal void RefreshHistory()
		{
			historyListBox.Items.Clear();
			foreach (var repo in AppManager.repositories)
			{
				var item = new ListBoxItem();
				item.Content = Path.GetFileName(repo);
				//item.ToolTip = repo;
				item.FontSize = 24;

				//item.HorizontalContentAlignment = HorizontalAlignment.Center;
				item.DoubleTapped += Item_MouseDoubleClick;
				item.ContextMenu = new ContextMenu();

				// open folder path
				var menuItem = new MenuItem();
				menuItem.Header = "Open folder path";
				//menuItem.ToolTip = repo;
				menuItem.Click += OpenRepoMenuItem_Click;
				historyListBox.Items.Add(menuItem);

				// remove repo from history
				menuItem = new MenuItem();
				menuItem.Header = "Remove from history";
				//menuItem.ToolTip = repo;
				menuItem.Click += RemoveHistoryMenuItem_Click;
				historyListBox.Items.Add(menuItem);

				historyListBox.Items.Add(item);
			}
		}

		public void Refresh()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				RefreshHistory();
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate()
				{
					RefreshHistory();
				});
			}
		}

		private void OpenRepoMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//var item = (MenuItem)sender;
			//string repo = (string)item.ToolTip;
			//if (!Tools.OpenFolderLocation(repo))
			//{
			//	AppManager.RemoveRepoFromHistory(repo);
			//	RefreshHistory();
			//}
		}

		private void RemoveHistoryMenuItem_Click(object sender, RoutedEventArgs e)
		{
			//var item = (MenuItem)sender;
			//string repo = (string)item.ToolTip;
			//AppManager.RemoveRepoFromHistory(repo);
			//RefreshHistory();
		}

		private void Item_MouseDoubleClick(object sender, Avalonia.Input.TappedEventArgs e)
		{
			//var item = (ListBoxItem)sender;
			//string repo = (string)item.ToolTip;
			//RepoScreen.singleton.OpenRepo(repo);
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
			//MainWindow.singleton.Navigate(CloneScreen.singleton);
		}

		private void createButton_Click(object sender, RoutedEventArgs e)
		{
			CreateScreen.singleton.Setup();
			//MainWindow.singleton.Navigate(CreateScreen.singleton);
		}

		private void settingsButton_Click(object sender, RoutedEventArgs e)
		{
			SettingsScreen.singleton.Setup();
			//MainWindow.singleton.Navigate(SettingsScreen.singleton);
		}

		private void UpdateImage_MouseUp(object sender, Avalonia.Input.TappedEventArgs e)
		{
			using var process = Process.Start("https://github.com/reignstudios/Git-It-GUI/releases");
		}
	}
}
