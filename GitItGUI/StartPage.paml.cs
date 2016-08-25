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
			AvaloniaXamlLoader.Load(this);

			var recentStackPanel = this.Find<StackPanel>("recentStackPanel");
			for (int i = 0; i != 5; ++i)
			{
				var button = new Button();
				button.Content = "Repo Path...";
				button.Click += recentButton_Click;
				recentStackPanel.Children.Add(button);
			}
		}

		private void recentButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
