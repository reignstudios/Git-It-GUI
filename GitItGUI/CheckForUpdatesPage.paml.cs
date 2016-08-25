using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using GitItGUI.Core;

namespace GitItGUI
{
	public class CheckForUpdatesPage : UserControl
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
				MainWindow.LoadPage(PageTypes.Start);
			}
		}

		private void checkForUpdatesCallback(bool succeeded)
		{
			MainWindow.LoadPage(PageTypes.Start);
		}
	}
}
