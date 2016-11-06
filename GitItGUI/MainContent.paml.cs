using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class MainContent : UserControl
	{
		public static MainContent singleton;

		public MainContent()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);
		}
	}
}
