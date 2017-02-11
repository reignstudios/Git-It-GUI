using Avalonia.Controls;
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
			if (!AppManager.CheckForUpdates("http://reign-studios-services.com/GitItGUI/VersionInfo.xml", "https://github.com/reignstudios/Git-It-GUI/releases", checkForUpdatesCallback))
			{
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
