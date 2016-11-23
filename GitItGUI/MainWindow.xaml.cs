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
		MainContent,
		Commit,
		Name
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
			if (MainContent.singleton.IsVisible) RepoManager.Refresh();
		}

		private void Debug_debugLogCallback(object value, bool alert)
		{
			if (alert) MessageBox.Show(value.ToString());
		}

		private static NavigationPage GetActivePage()
		{
			if (CheckForUpdatesPage.singleton.IsVisible) return CheckForUpdatesPage.singleton;
			else if (StartPage.singleton.IsVisible) return StartPage.singleton;
			else if (MainContent.singleton.IsVisible) return MainContent.singleton;
			else if (CommitPage.singleton.IsVisible) return CommitPage.singleton;
			else if (NamePage.singleton.IsVisible) return NamePage.singleton;

			return null;
		}

		public static void LoadPage(PageTypes type)
		{
			CheckForUpdatesPage.singleton.IsVisible = false;
			StartPage.singleton.IsVisible = false;
			MainContent.singleton.IsVisible = false;
			CommitPage.singleton.IsVisible = false;
			NamePage.singleton.IsVisible = false;
			NavigationPage pageFrom = GetActivePage();
			switch (type)
			{
				case PageTypes.CheckForUpdates: CheckForUpdatesPage.singleton.IsVisible = true; break;
				case PageTypes.Start: StartPage.singleton.IsVisible = true; break;
				case PageTypes.MainContent: MainContent.singleton.IsVisible = true; break;
				case PageTypes.Commit: CommitPage.singleton.IsVisible = true; break;
				case PageTypes.Name: NamePage.singleton.IsVisible = true; break;
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
