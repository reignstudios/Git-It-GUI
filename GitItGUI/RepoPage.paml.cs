using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class RepoPage : UserControl
	{
		public static RepoPage singleton;

		public RepoPage()
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
