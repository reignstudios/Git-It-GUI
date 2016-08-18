using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class BranchesPage : UserControl
	{
		public static BranchesPage singleton;

		public BranchesPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);


		}
	}
}
