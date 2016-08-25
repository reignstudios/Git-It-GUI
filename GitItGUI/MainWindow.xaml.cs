using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace GitItGUI
{
	public enum PageTypes
	{
		CheckForUpdates,
		Start,
		MainContent
	}
	
	public class MainWindow : Window
	{
		public static MainWindow singleton;

		public MainWindow()
		{
			singleton = this;
			LoadUI();
			App.AttachDevTools(this);
		}

		private void LoadUI()
		{
			// load pages
			CheckForUpdatesPage.singleton = new CheckForUpdatesPage();
			StartPage.singleton = new StartPage();
			MainContent.singleton = new MainContent();

			// load main page
			AvaloniaXamlLoader.Load(this);
			this.Closed += MainWindow_Closed;

			LoadPage(PageTypes.CheckForUpdates);
			CheckForUpdatesPage.singleton.Check("http://reign-studios-services.com/GitItGUI/VersionInfo.xml");
		}

		public static void LoadPage(PageTypes type)
		{
			switch (type)
			{
				case PageTypes.CheckForUpdates: singleton.Content = CheckForUpdatesPage.singleton; break;
				case PageTypes.Start: singleton.Content = StartPage.singleton; break;
				case PageTypes.MainContent: singleton.Content = MainContent.singleton; break;
			}
		}

		public static void CanInteractWithUI(bool enabled)
		{
			//singleton.tabControl.IsEnabled = enabled;
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			// TODO: save settings and dispose
		}
	}
}
