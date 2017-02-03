using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace MessageBox
{
	public class MainWindow : Window
	{
		private Grid grid;
		private TextBlock message;
		private Button okButton, cancelButton;
		bool writeCancleOnQuit = true;

		public MainWindow()
		{
			AvaloniaXamlLoader.Load(this);
			App.AttachDevTools(this);
			Closed += MainWindow_Closed;

			// load ui
			grid = this.Find<Grid>("grid");
			message = this.Find<TextBlock>("message");
			okButton = this.Find<Button>("okButton");
			cancelButton = this.Find<Button>("cancelButton");

			// bind ui
			okButton.Click += OkButton_Click;
			cancelButton.Click += CancelButton_Click;

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
					case "-Title": Title = values[1]; break;
					case "-Message": message.Text = values[1]; break;

					case "-Type":
						if (values[1] == "Ok")
						{
							cancelButton.IsVisible = false;
						}
						else if (values[1] == "OkCancel")
						{
							cancelButton.IsVisible = true;
						}
						else if (values[1] == "YesNo")
						{
							cancelButton.IsVisible = true;
							cancelButton.Content = "No";
							okButton.Content = "Yes";
						}
						break;

					default:
						Console.Write(string.Format("ERROR:Invalid arg type ({0})", values[0]));
						grid.IsVisible = false;
						break;
				}
			}
		}

		private void OkButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Console.Write("SUCCEEDED:Ok");
			writeCancleOnQuit = false;
			Close();
		}

		private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Close();
		}

		private void MainWindow_Closed(object sender, System.EventArgs e)
		{
			if (writeCancleOnQuit) Console.Write("SUCCEEDED:Cancel");
		}
	}
}
