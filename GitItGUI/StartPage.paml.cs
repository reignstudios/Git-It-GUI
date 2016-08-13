using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class StartPage : UserControl
	{
		public static StartPage singleton;

		public StartPage()
		{
			singleton = this;
			this.InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);

			var historyStackPanel = this.Find<StackPanel>("historyStackPanel");
			for (int i = 0; i != 5; ++i)
			{
				var button = new Button();
				button.Content = "Repo Path...";
				button.Click += historyButton_Click;
				historyStackPanel.Children.Add(button);
			}
		}

		private void historyButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
