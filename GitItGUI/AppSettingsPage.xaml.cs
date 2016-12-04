using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class AppSettingsPage : UserControl, NavigationPage
	{
		public static AppSettingsPage singleton;

		private Button doneButton;

		public AppSettingsPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			doneButton = this.Find<Button>("doneButton");
			doneButton.Click += DoneButton_Click;
		}

		private void DoneButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.Start);
		}

		public void NavigatedTo()
		{
			
		}

		public void NavigatedFrom()
		{
			
		}
	}
}
