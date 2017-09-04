using GitItGUI.UI.Screens;
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

namespace GitItGUI.UI.Overlays
{
	/// <summary>
	/// Interaction logic for MergeConflictOverlay.xaml
	/// </summary>
	public partial class MergeConflictOverlay : UserControl
	{
		public MergeConflictOverlay()
		{
			InitializeComponent();
		}

		public void SetFilePath(string filePath)
		{
			filePathLabel.Text = filePath;
		}

		private void openFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			RepoScreen.singleton.repoManager.OpenFile(filePathLabel.Text);
		}

		private void openFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
		{
			RepoScreen.singleton.repoManager.OpenFileLocation(filePathLabel.Text);
		}
	}
}
