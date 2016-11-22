using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;

namespace GitItGUI
{
	public class CommitPage : UserControl, NavigationPage
	{
		public static CommitPage singleton;

		private Button cancelButton, commitButton;
		private TextBox messageTextBox;

		public CommitPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			cancelButton = this.Find<Button>("cancelButton");
			commitButton = this.Find<Button>("commitButton");
			messageTextBox = this.Find<TextBox>("messageTextBox");

			// apply bindings
			cancelButton.Click += CancelButton_Click;
			commitButton.Click += CommitButton_Click;
		}

		public void NavigatedTo()
		{
			messageTextBox.Text = "";
		}

		public void NavigatedFrom()
		{
			
		}

		public void ClearMessage()
		{
			messageTextBox.Text = "";
		}

		private void CommitButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(messageTextBox.Text))
			{
				Debug.Log("Must enter a commit message!", true);
				return;
			}

			if (messageTextBox.Text.Length <= 3)
			{
				Debug.Log("Commit message to short!", true);
				return;
			}

			ChangesManager.CommitStagedChanges(messageTextBox.Text);
			MainWindow.LoadPage(PageTypes.MainContent);
		}

		private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
