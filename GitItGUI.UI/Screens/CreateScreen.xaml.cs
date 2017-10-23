using GitItGUI.Core;
using GitItGUI.UI.Utils;
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

namespace GitItGUI.UI.Screens
{
	/// <summary>
	/// Interaction logic for CreateScreen.xaml
	/// </summary>
	public partial class CreateScreen : UserControl
	{
		public static CreateScreen singleton;

		public CreateScreen()
		{
			singleton = this;
			InitializeComponent();
		}

		public void Setup()
		{
			repoPathTextBox.Text = string.Empty;
			enableLFSCheckBox.IsChecked = true;
			lfsDefaultsCheckBox.IsChecked = true;
		}

		private void selectPathButton_Click(object sender, RoutedEventArgs e)
		{
			if (PlatformUtils.SelectFolder(out string clonePath)) repoPathTextBox.Text = clonePath;
		}

		private void createButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(repoPathTextBox.Text))
			{
				DebugLog.LogWarning("Repository path cannot be empty");
				return;
			}

			RepoScreen.singleton.CreateRepo(repoPathTextBox.Text, enableLFSCheckBox.IsChecked == true, lfsDefaultsCheckBox.IsChecked == true);
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.Navigate(StartScreen.singleton);
		}

		private void enableLFSCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			lfsDefaultsCheckBox.IsEnabled = enableLFSCheckBox.IsChecked == true;
		}
	}
}
