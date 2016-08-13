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
			this.InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
