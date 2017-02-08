using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using GitItGUI.Core;

namespace GitItGUI
{
	public enum PageTypes
	{
		CheckForUpdates,
		Start,
		AppSettings,
		MainContent
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
			if (MainContent.singleton.IsVisible) RepoManager.Refresh();
			#endif
		}

		private void Debug_debugLogCallback(object value, bool alert)
		{
			if (alert) MessageBox.Show(value.ToString());
		}

		private static NavigationPage GetActivePage()
		{
			if (CheckForUpdatesPage.singleton.IsVisible) return CheckForUpdatesPage.singleton;
			else if (StartPage.singleton.IsVisible) return StartPage.singleton;
			else if (AppSettingsPage.singleton.IsVisible) return AppSettingsPage.singleton;
			else if (MainContent.singleton.IsVisible) return MainContent.singleton;

			return null;
		}

		public static void LoadPage(PageTypes type)
		{
			NavigationPage pageFrom = GetActivePage();
			CheckForUpdatesPage.singleton.IsVisible = false;
			StartPage.singleton.IsVisible = false;
			AppSettingsPage.singleton.IsVisible = false;
			MainContent.singleton.IsVisible = false;
			switch (type)
			{
				case PageTypes.CheckForUpdates: CheckForUpdatesPage.singleton.IsVisible = true; break;
				case PageTypes.Start: StartPage.singleton.IsVisible = true; break;
				case PageTypes.AppSettings: AppSettingsPage.singleton.IsVisible = true; break;
				case PageTypes.MainContent: MainContent.singleton.IsVisible = true; break;
				default: throw new Exception("Unsuported page type: " + type);
			}

			if (pageFrom != null) pageFrom.NavigatedFrom();
			NavigationPage pageTo = GetActivePage();
			pageTo.NavigatedTo();
		}

		public static void CanInteractWithUI(bool enabled)
		{
			//singleton.tabControl.IsEnabled = enabled;
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			RepoManager.SaveSettings();
			AppManager.SaveSettings();
			AppManager.Dispose();
		}
	}
}
