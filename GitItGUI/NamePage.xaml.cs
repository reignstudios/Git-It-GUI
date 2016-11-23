using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;

namespace GitItGUI
{
	public class NamePage : UserControl, NavigationPage
	{
		public static NamePage singleton;
		public static PageTypes pageToLoadOnExit;
		public static bool succeeded;
		public static string value;

		// ui
		private TextBox nameTextBox;
		private Button okButton, cancleButton;

		public NamePage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui
			nameTextBox = this.Find<TextBox>("nameTextBox");
			okButton = this.Find<Button>("okButton");
			cancleButton = this.Find<Button>("cancleButton");

			// bind ui
			okButton.Click += OkButton_Click;
			cancleButton.Click += CancleButton_Click;
		}

		private void CancleButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			nameTextBox.Text = "";
			value = nameTextBox.Text;
			succeeded = false;
			MainWindow.LoadPage(pageToLoadOnExit);
		}

		private void OkButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(nameTextBox.Text))
			{
				Debug.Log("Must enter a value", true);
				return;
			}

			if (nameTextBox.Text.Length <= 3)
			{
				Debug.Log("Value to short", true);
				return;
			}

			value = nameTextBox.Text;
			succeeded = true;
			MainWindow.LoadPage(pageToLoadOnExit);
		}

		public void NavigatedFrom()
		{
			
		}

		public void NavigatedTo()
		{
			nameTextBox.Text = "";
			value = nameTextBox.Text;
		}
	}
}
