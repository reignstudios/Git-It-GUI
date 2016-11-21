using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class NamePage : UserControl
	{
		public NamePage()
		{
			this.InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
