using GitItGUI.UI.Utils;
using System.Windows;
using System.Windows.Controls;

namespace GitItGUI.UI.Screens
{
	/// <summary>
	/// Interaction logic for StartUserControl.xaml
	/// </summary>
	public partial class StartScreen : UserControl
	{
		public static StartScreen singleton;

		public StartScreen()
		{
			singleton = this;
			InitializeComponent();
		}

		private void openButton_Click(object sender, RoutedEventArgs e)
		{
			if (PlatformUtils.SelectFolder(out string folderPath))
			{
				// TODO: open repo first
				MainWindow.singleton.Navigate(RepoScreen.singleton);
			}
		}
	}
}
