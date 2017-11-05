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
		private bool isBinaryMode;

		public MergeConflictOverlay()
		{
			InitializeComponent();
		}

		public void Setup(string filePath, bool isBinaryMode, DoneCallbackMethod doneCallback)
		{
			this.doneCallback = doneCallback;
			this.isBinaryMode = isBinaryMode;
			WaitMode(filePath, string.IsNullOrEmpty(filePath));
		}

		private void WaitMode(string filePath, bool isWaiting)
		{
			filePathLabel.Text = filePath;
			userTheirsButton.IsEnabled = !isWaiting;
			useOursButton.IsEnabled = !isWaiting;
			mergeToolButton.IsEnabled = !isWaiting;
			mergeToolButton.Visibility = isBinaryMode ? Visibility.Hidden : Visibility.Visible;
			cancelButton.IsEnabled = !isWaiting;
		}

		private void openFileMenuItem_Click(object sender, RoutedEventArgs e)
		{
			RepoScreen.singleton.repoManager.OpenFile(filePathLabel.Text);
		}

		private void openFileLocationMenuItem_Click(object sender, RoutedEventArgs e)
		{
			RepoScreen.singleton.repoManager.OpenFileLocation(filePathLabel.Text);
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.Cancel);
			Visibility = Visibility.Hidden;
		}

		private void mergeToolButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.RunMergeTool);
			WaitMode(filePathLabel.Text, true);
		}

		private void userTheirsButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.UseTheirs);
			WaitMode(filePathLabel.Text, true);
		}

		private void useOursButton_Click(object sender, RoutedEventArgs e)
		{
			if (doneCallback != null) doneCallback(MergeConflictOverlayResults.UseOurs);
			WaitMode(filePathLabel.Text, true);
		}
	}
}
