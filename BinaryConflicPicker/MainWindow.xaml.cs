using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace BinaryConflicPicker
{
	public class MainWindow : Window
	{
		private Grid grid;
		private TextBox fileInConflictTextBox;
		private Button keepMineButton, useTheirsButton, cancelButton;

		public MainWindow()
		{
			AvaloniaXamlLoader.Load(this);
			App.AttachDevTools(this);
			Closed += MainWindow_Closed;

			// load ui
			grid = this.Find<Grid>("grid");
			fileInConflictTextBox = this.Find<TextBox>("fileInConflictTextBox");
			keepMineButton = this.Find<Button>("keepMineButton");
			useTheirsButton = this.Find<Button>("useTheirsButton");
			cancelButton = this.Find<Button>("cancelButton");

			// bind ui
			keepMineButton.Click += KeepMineButton_Click;
			useTheirsButton.Click += UseTheirsButton_Click;
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
					case "-FileInConflic": fileInConflictTextBox.Text = values[1]; break;
					default:
						Console.Write(string.Format("ERROR:Invalid arg type ({0})", values[0]));
						grid.IsVisible = false;
						break;
				}
			}
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			if (grid.IsVisible) Console.Write("SUCCEEDED:Canceled");
		}

		private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Close();
		}

		private void UseTheirsButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Console.Write("SUCCEEDED:UseTheirs");
		}

		private void KeepMineButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Console.Write("SUCCEEDED:KeepMine");
		}
	}
}
