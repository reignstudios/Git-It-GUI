using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class CommitPage : UserControl
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

		private void CommitButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			
		}

		private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
