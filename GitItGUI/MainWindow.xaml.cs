using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using GitItGUI.Core;
using Avalonia.Threading;
using System.Threading;
using System.IO;

namespace GitItGUI
{
	public enum PageTypes
	{
		CheckForUpdates,
		Start,
		AppSettings,
		MainContent,
		Processing,
		Clone
	}
	
	public class MainWindow : Window
	{
		public static MainWindow singleton;

		public MainWindow()
		{
			singleton = this;

			// load main page
			AvaloniaXamlLoader.Load(this);
			App.AttachDevTools(this);
			Title = "Git-It-GUI v" + VersionInfo.version;
			Debug.Log(Title);

			// load resources
			ResourceManager.Init();

			// load core
			Debug.debugLogCallback += Debug_debugLogCallback;
			Debug.debugLogWarningCallback += Debug_debugLogCallback;
			Debug.debugLogErrorCallback += Debug_debugLogCallback;
			if (!AppManager.Init())
			{
				Close();
				return;
			}

			// check for updates
			LoadPage(PageTypes.CheckForUpdates);
			CheckForUpdatesPage.singleton.Check("http://reign-studios-services.com/GitItGUI/VersionInfo.xml");

			// bind events
			Closed += MainWindow_Closed;
			Activated += MainWindow_Activated;
		}

		private void MainWindow_Activated(object sender, EventArgs e)
		{
			#if !DEBUG
			if (MainContent.singleton.IsVisible && AppManager.autoRefreshChanges) RepoManager.Refresh();
			#endif
		}

		private void Debug_debugLogCallback(object value, bool alert)
		{
			if (alert) MessageBox.Show(value.ToString());
		}

		private static NavigationPage GetActivePageType()
		{
			if (CheckForUpdatesPage.singleton.IsVisible) return CheckForUpdatesPage.singleton;
			else if (StartPage.singleton.IsVisible) return StartPage.singleton;
			else if (AppSettingsPage.singleton.IsVisible) return AppSettingsPage.singleton;
			else if (MainContent.singleton.IsVisible) return MainContent.singleton;
			else if (ProcessingPage.singleton.IsVisible) return ProcessingPage.singleton;
			else if (ClonePage.singleton.IsVisible) return ClonePage.singleton;

			return null;
		}

		public static void LoadPage(PageTypes type)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				LoadPage_UIThread(type);
			}
			else
			{
				bool isDone = false;
				Dispatcher.UIThread.InvokeAsync(delegate
				{
					LoadPage_UIThread(type);
					isDone = true;
				});

				while (!isDone) Thread.Sleep(1);
			}
		}

		private static void LoadPage_UIThread(PageTypes type)
		{
			NavigationPage pageFrom = GetActivePageType();
			CheckForUpdatesPage.singleton.IsVisible = false;
			StartPage.singleton.IsVisible = false;
			AppSettingsPage.singleton.IsVisible = false;
			MainContent.singleton.IsVisible = false;
			ProcessingPage.singleton.IsVisible = false;
			ClonePage.singleton.IsVisible = false;
			switch (type)
			{
				case PageTypes.CheckForUpdates: CheckForUpdatesPage.singleton.IsVisible = true; break;
				case PageTypes.Start: StartPage.singleton.IsVisible = true; break;
				case PageTypes.AppSettings: AppSettingsPage.singleton.IsVisible = true; break;
				case PageTypes.MainContent: MainContent.singleton.IsVisible = true; break;
				case PageTypes.Processing: ProcessingPage.singleton.IsVisible = true; break;
				case PageTypes.Clone: ClonePage.singleton.IsVisible = true; break;
				default: throw new Exception("Unsuported page type: " + type);
			}

			if (pageFrom != null) pageFrom.NavigatedFrom();
			NavigationPage pageTo = GetActivePageType();
			pageTo.NavigatedTo();
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			AppManager.SaveSettings();
			AppManager.Dispose();
		}
	}
}
