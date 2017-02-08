using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Text.RegularExpressions;

namespace CommitEntry
{
	public class MainWindow : Window
	{
		private Grid grid;
		private Button cancelButton, commitButton;
		private TextBox messageTextBox;
		bool writeCancleOnQuit = true;

		public MainWindow()
		{
			AvaloniaXamlLoader.Load(this);
			App.AttachDevTools(this);
			Closed += MainWindow_Closed;

			// load ui items
			cancelButton = this.Find<Button>("cancelButton");
			commitButton = this.Find<Button>("commitButton");
			messageTextBox = this.Find<TextBox>("messageTextBox");

			// apply bindings
			cancelButton.Click += CancelButton_Click;
			commitButton.Click += CommitButton_Click;

			// get args
			var args = Environment.GetCommandLineArgs();
			for (int i = 1; i != args.Length; ++i)
			{
				var arg = args[i];
				var values = arg.Split('=');
				if (values.Length != 2)
				{
					Console.Write(string.Format("ERROR:Invalid arg ({0})", arg));
					grid.IsVisible = false;
					writeCancleOnQuit = false;
					return;
				}

				switch (values[0])
				{
					case "-Message": messageTextBox.Text = values[1]; break;

					default:
						Console.Write(string.Format("ERROR:Invalid arg type ({0})", values[0]));
						grid.IsVisible = false;
						break;
				}
			}
		}

		private void CommitButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			// check for errors
			if (string.IsNullOrEmpty(messageTextBox.Text) || messageTextBox.Text.Length <= 3 || !Regex.IsMatch(messageTextBox.Text, @"^[a-zA-Z0-9 \n\r\!\?]*$"))
			{
				Console.Write("ERROR:Invalid message entry");
				writeCancleOnQuit = false;
				Close();
				return;
			}

			// finish normally
			Console.Write("SUCCEEDED:Ok:" + messageTextBox.Text);
			writeCancleOnQuit = false;
			Close();
		}

		private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Close();
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			if (writeCancleOnQuit) Console.Write("SUCCEEDED:Cancel:N/A");
		}
	}
}
