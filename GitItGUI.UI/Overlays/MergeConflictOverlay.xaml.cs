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
	public enum MergeConflictOverlayResults
	{
		Cancel,
		UseTheirs,
		UseOurs,
		RunMergeTool
	}

	/// <summary>
	/// Interaction logic for MergeConflictOverlay.xaml
	/// </summary>
	public partial class MergeConflictOverlay : UserControl
	{
		public delegate void DoneCallbackMethod(MergeConflictOverlayResults result);
		private DoneCallbackMethod doneCallback;

		public MergeConflictOverlay()
		{
			InitializeComponent();
		}

		public void Setup(string filePath, DoneCallbackMethod doneCallback)
		{
			this.doneCallback = doneCallback;

			if (string.IsNullOrEmpty(filePath))
			{
				filePathLabel.Text = string.Empty;
				userTheirsButton.IsEnabled = false;
				useOursButton.IsEnabled = false;
				mergeToolButton.IsEnabled = false;
				cancelButton.IsEnabled = false;
			}

			filePathLabel.Text = filePath;
			userTheirsButton.IsEnabled = true;
			useOursButton.IsEnabled = true;
			mergeToolButton.IsEnabled = true;
			cancelButton.IsEnabled = true;
		}

		private void openFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			RepoScreen.singleton.repoManager.OpenFile(filePathLabel.Text);
		}

		private void openFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
		{
			RepoScreen.singleton.repoManager.OpenFileLocation(filePathLabel.Text);
		}

		private void userTheirsButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.UseTheirs);
		}

		private void useOursButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.UseOurs);
		}

		private void mergeToolButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.RunMergeTool);
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.Cancel);
		}
	}
}
