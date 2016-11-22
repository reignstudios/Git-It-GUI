using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class AppSettingsPage : UserControl
	{
		public static AppSettingsPage singleton;

		public AppSettingsPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}
	}
}
