using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class CommitPage : UserControl
	{
		public static CommitPage singleton;

		public CommitPage()
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
