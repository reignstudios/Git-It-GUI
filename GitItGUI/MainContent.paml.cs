using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class MainContent : UserControl, NavigationPage
	{
		public static MainContent singleton;

		public MainContent()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}

		public void NavigatedFrom()
		{
			
		}

		public void NavigatedTo()
		{
			
		}
	}
}
