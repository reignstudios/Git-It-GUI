using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;
using System.Threading.Tasks;

namespace GitItGUI
{
	public enum ProcessingPageModes
	{
		None,
		Clone,
		Pull,
		Push,
		Sync
	}

	public class ProcessingPage : UserControl, NavigationPage
	{
		public static ProcessingPage singleton;

		public ProcessingPageModes mode = ProcessingPageModes.None;

		public ProcessingPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}

		public void NavigatedFrom()
		{
			
		}

		public async void NavigatedTo()
		{
			await Task.Delay(1000);
			if (mode == ProcessingPageModes.Pull) ChangesManager.Pull();
			else if (mode == ProcessingPageModes.Push) ChangesManager.Push();
			else if (mode == ProcessingPageModes.Sync) ChangesManager.Sync();
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
