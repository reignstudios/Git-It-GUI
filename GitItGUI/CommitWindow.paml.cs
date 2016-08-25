using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibGit2Sharp;
using System;

namespace GitItGUI
{
	public class CommitWindow : Window
	{
		// ui objects
		TextBox messageTextBox;
		Button cancelButton, okButton;

		public CommitWindow()
		{
			AvaloniaXamlLoader.Load(this);
			App.AttachDevTools(this);
		}

		//protected override void OnClosing(CancelEventArgs e)
		//{
		//	MainWindow.CanInteractWithUI(true);
		//	base.OnClosing(e);
		//}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			//Close();
		}

		private void commitButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(messageTextBox.Text))
			{
				MessageBox.Show("Must enter a commit message");
				return;
			}

			try
			{
				//RepoPage.repo.Commit(messageTextBox.Text, RepoPage.signature, RepoPage.signature);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Failed to commit: " + ex.Message);
				return;
			}

			//MainWindow.UpdateUI();
			//Close();
		}
	}
}
