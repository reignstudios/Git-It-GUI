using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class NameWindow : Window
	{
		public static string name;

		// ui objects
		TextBox nameTextBox;
		Button okButton, cancleButton;

		public NameWindow()
		{
			this.InitializeComponent();
			App.AttachDevTools(this);
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
