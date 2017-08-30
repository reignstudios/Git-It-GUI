using GitItGUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace GitItGUI.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static MainWindow singleton;

		public MainWindow()
		{
			singleton = this;
			InitializeComponent();

			AppManager.Init();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			repoScreen.Dispose();
			AppManager.Dispose();
			base.OnClosing(e);
		}

		public void Navigate(UserControl screen)
		{
			startScreen.Visibility = Visibility.Hidden;
			settingsScreen.Visibility = Visibility.Hidden;
			repoScreen.Visibility = Visibility.Hidden;
			if (screen == startScreen) startScreen.Visibility = Visibility.Visible;
			else if (screen == settingsScreen) settingsScreen.Visibility = Visibility.Visible;
			else if (screen == repoScreen) repoScreen.Visibility = Visibility.Visible;
		}
	}
}
