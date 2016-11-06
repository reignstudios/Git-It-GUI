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
		Commit
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
			if (Content == MainContent.singleton) RepoManager.Refresh();
		}

		private void Debug_debugLogCallback(object value, bool alert)
		{
			if (alert) MessageBox.Show(value.ToString());
		}

		public static void LoadPage(PageTypes type)
		{
			CheckForUpdatesPage.singleton.IsVisible = false;
			StartPage.singleton.IsVisible = false;
			MainContent.singleton.IsVisible = false;
			CommitPage.singleton.IsVisible = false;
			switch (type)
			{
				case PageTypes.CheckForUpdates: CheckForUpdatesPage.singleton.IsVisible = true; break;
				case PageTypes.Start: StartPage.singleton.IsVisible = true; break;
				case PageTypes.MainContent: MainContent.singleton.IsVisible = true; break;
				case PageTypes.Commit: CommitPage.singleton.IsVisible = true; break;
			}
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
