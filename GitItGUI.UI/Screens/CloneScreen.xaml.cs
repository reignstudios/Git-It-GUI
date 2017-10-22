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
	/// Interaction logic for CloneScreen.xaml
	/// </summary>
	public partial class CloneScreen : UserControl
	{
		public static CloneScreen singleton;

		public CloneScreen()
		{
			singleton = this;
			InitializeComponent();
		}

		public void Setup()
		{
			repoUrlTextBox.Text = string.Empty;
			clonePathTextBox.Text = string.Empty;
		}

		private void selectPathButton_Click(object sender, RoutedEventArgs e)
		{
			if (PlatformUtils.SelectFolder(out string clonePath)) clonePathTextBox.Text = clonePath;
		}

		private void cloneButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(repoUrlTextBox.Text))
			{
				DebugLog.LogWarning("Repository URL cannot be empty");
				return;
			}

			if (string.IsNullOrEmpty(clonePathTextBox.Text))
			{
				DebugLog.LogWarning("Clone path cannot be empty");
				return;
			}

			RepoScreen.singleton.CloneRepo(clonePathTextBox.Text, repoUrlTextBox.Text);
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.Navigate(StartScreen.singleton);
		}
	}
}
