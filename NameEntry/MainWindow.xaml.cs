using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace NameEntry
{
	public class MainWindow : Window
	{
		private Grid grid;
		private TextBlock captionTextBlock;
		private TextBox nameTextBox;
		private Button okButton, cancelButton;
		bool writeCancleOnQuit = true;

		public MainWindow()
		{
			AvaloniaXamlLoader.Load(this);
			App.AttachDevTools(this);
			Closed += MainWindow_Closed;

			// load ui
			grid = this.Find<Grid>("grid");
			captionTextBlock = this.Find<TextBlock>("captionTextBlock");
			nameTextBox = this.Find<TextBox>("nameTextBox");
			okButton = this.Find<Button>("okButton");
			cancelButton = this.Find<Button>("cancelButton");

			// bind ui
			okButton.Click += OkButton_Click;
			cancelButton.Click += CancelButton_Click;

			// get args
			var args = Environment.GetCommandLineArgs();

			// check for errors
			if (args.Length != 2)
			{
				Console.Write("ERROR:Invalid arg count: " + args.Length);
				grid.IsVisible = false;
				return;
			}

			for (int i = 1; i != args.Length; ++i)
			{
				var arg = args[i];
				var values = arg.Split('=');
				if (values.Length != 2)
				{
					Console.Write(string.Format("ERROR:Invalid arg ({0})", arg));
					grid.IsVisible = false;
					return;
				}

				switch (values[0])
				{
					case "-Caption": captionTextBlock.Text = values[1]; break;

					default:
						Console.Write(string.Format("ERROR:Invalid arg type ({0})", values[0]));
						grid.IsVisible = false;
						break;
				}
			}
		}

		private void OkButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			// check for errors
			if (string.IsNullOrEmpty(nameTextBox.Text) || nameTextBox.Text.Length <= 3)
			{
				Console.Write("ERROR:Invalid name entry");
				writeCancleOnQuit = false;
				Close();
				return;
			}

			// finish normally
			Console.Write("SUCCEEDED:Ok:" + nameTextBox.Text);
			writeCancleOnQuit = false;
			Close();
		}

		private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Close();
		}

		private void MainWindow_Closed(object sender, System.EventArgs e)
		{
			if (writeCancleOnQuit) Console.Write("SUCCEEDED:Cancel:N/A");
		}
	}
}
