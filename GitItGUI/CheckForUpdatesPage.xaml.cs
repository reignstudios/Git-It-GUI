﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using GitItGUI.Core;

namespace GitItGUI
{
	public class CheckForUpdatesPage : UserControl, NavigationPage
	{
		public static CheckForUpdatesPage singleton;
		
		public CheckForUpdatesPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}

		public void Check(string url)
		{
			if (!AppManager.CheckForUpdates("http://reign-studios-services.com/GitGameGUI/VersionInfo.xml", "http://reign-studios-services.com/GitGameGUI/index.html", checkForUpdatesCallback))
			{
				StartPage.singleton.RefreshUI();
				MainWindow.LoadPage(PageTypes.Start);
			}
		}

		public void NavigatedFrom()
		{
			
		}

		public void NavigatedTo()
		{
			
		}

		private void checkForUpdatesCallback(bool succeeded)
		{
			StartPage.singleton.RefreshUI();
			MainWindow.LoadPage(PageTypes.Start);
		}
	}
}