using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace MergeConflicPicker
{
	public class MainWindow : Window
	{
		private Grid grid;
		private TextBox fileInConflictTextBox;
		private Button keepMineButton, useTheirsButton, cancelButton, runMergeToolButton;
		private bool writeCancleOnQuit = true;

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
			runMergeToolButton = this.Find<Button>("runMergeToolButton");

			// bind ui
			keepMineButton.Click += KeepMineButton_Click;
			useTheirsButton.Click += UseTheirsButton_Click;
			cancelButton.Click += CancelButton_Click;
			runMergeToolButton.Click += RunMergeToolButton_Click; ;

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
					case "-FileInConflic": fileInConflictTextBox.Text = values[1]; break;
					case "-IsBinary": runMergeToolButton.IsVisible = values[1] == "False"; break;
					default:
						Console.Write(string.Format("ERROR:Invalid arg type ({0})", values[0]));
						grid.IsVisible = false;
						break;
				}
			}
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			if (writeCancleOnQuit) Console.Write("SUCCEEDED:Canceled");
		}

		private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Close();
		}

		private void UseTheirsButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Console.Write("SUCCEEDED:UseTheirs");
			writeCancleOnQuit = false;
			Close();
		}

		private void KeepMineButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Console.Write("SUCCEEDED:KeepMine");
			writeCancleOnQuit = false;
			Close();
		}

		private void RunMergeToolButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			Console.Write("SUCCEEDED:RunMergeTool");
			writeCancleOnQuit = false;
			Close();
		}
	}
}
