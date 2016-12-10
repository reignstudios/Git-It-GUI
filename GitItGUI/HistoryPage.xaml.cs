using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Diagnostics;
using System;
using GitItGUI.Core;

namespace GitItGUI
{
	public class HistoryPage : UserControl
	{
		public static HistoryPage singleton;

		private Button openGitkButton;

		public HistoryPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui
			openGitkButton = this.Find<Button>("openGitkButton");

			// bind ui
			openGitkButton.Click += OpenGitkButton_Click;
		}

		private void OpenGitkButton_Click(object sender, RoutedEventArgs e)
		{
			RepoManager.OpenGitk();
		}
	}
}
