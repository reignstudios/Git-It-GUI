using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class ChangesPage : UserControl
	{
		public static ChangesPage singleton;

		public ChangesPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);


		}
	}
}
