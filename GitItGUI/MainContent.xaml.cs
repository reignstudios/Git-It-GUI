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

		private TabControl tabControl;
		public int tabControlNavigateIndex = -1;

		public MainContent()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			tabControl = this.Find<TabControl>("tabControl");
		}

		public void NavigatedFrom()
		{
			if (MainContentPageNavigateFrom != null) MainContentPageNavigateFrom();
		}

		public void NavigatedTo()
		{
			if (tabControlNavigateIndex != -1) tabControl.SelectedIndex = tabControlNavigateIndex;
			tabControlNavigateIndex = -1;
			if (MainContentPageNavigatedTo != null) MainContentPageNavigatedTo();
		}
	}
}
