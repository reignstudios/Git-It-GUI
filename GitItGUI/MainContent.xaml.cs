using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public delegate void MainContentPageNavigateMethod();

	public class MainContent : UserControl, NavigationPage
	{
		public static MainContent singleton;
		public event MainContentPageNavigateMethod MainContentPageNavigatedTo, MainContentPageNavigateFrom;

		public MainContent()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}

		public void NavigatedFrom()
		{
			if (MainContentPageNavigateFrom != null) MainContentPageNavigateFrom();
		}

		public void NavigatedTo()
		{
			if (MainContentPageNavigatedTo != null) MainContentPageNavigatedTo();
		}
	}
}
