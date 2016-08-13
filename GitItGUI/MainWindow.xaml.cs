using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public enum PageTypes
	{
		Start,
		MainContent
	}

	public class MainWindow : Window
	{
		public static MainWindow singleton;

		public MainWindow()
		{
			singleton = this;
			this.InitializeComponent();
			App.AttachDevTools(this);
		}

		private void InitializeComponent()
		{
			// load pages
			StartPage.singleton = new StartPage();
			MainContent.singleton = new MainContent();

			// load main page
			AvaloniaXamlLoader.Load(this);
			LoadPage(PageTypes.Start);
		}

		public static void LoadPage(PageTypes type)
		{
			switch (type)
			{
				case PageTypes.Start: singleton.Content = StartPage.singleton; break;
				case PageTypes.MainContent: singleton.Content = MainContent.singleton; break;
			}
		}
	}
}
