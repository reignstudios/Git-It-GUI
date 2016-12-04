using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;

namespace GitItGUI
{
	public class StartPage : UserControl, NavigationPage
	{
		public static StartPage singleton;

		// ui objects
		StackPanel recentStackPanel;
		Button cloneButton, openButton, settingsButton;

		public StartPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			recentStackPanel = this.Find<StackPanel>("recentStackPanel");
			cloneButton = this.Find<Button>("cloneButton");
			openButton = this.Find<Button>("openButton");
			settingsButton = this.Find<Button>("settingsButton");
			cloneButton.Click += OpenButton_Click;
			openButton.Click += CloneButton_Click;
			settingsButton.Click += SettingsButton_Click;
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.AppSettings);
		}

		public void NavigatedTo()
		{
			
		}

		public void NavigatedFrom()
		{
			
		}

		public void RefreshUI()
		{
			// fill resent
			foreach (var repo in AppManager.settings.repositories)
			{
				var button = new Button();
				button.Content = repo.path;
				button.Click += RecentButton_Click;
				recentStackPanel.Children.Add(button);
			}
		}

		private void CloneButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("TODO");
		}

		private async void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			var dlg = new OpenFolderDialog();
			var path = await dlg.ShowAsync();
			if (string.IsNullOrEmpty(path)) return;

			// open repo
			if (!RepoManager.OpenRepo(path))
			{
				// remove bad repo from list
				MessageBox.Show("Failed to open repo: " + path);
				return;
			}

			// load main repo page
			MainWindow.LoadPage(PageTypes.MainContent);
		}

		private void RecentButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			string path = (string)button.Content;

			// open repo
			if (!RepoManager.OpenRepo(path))
			{
				// remove bad repo from list
				MessageBox.Show("Failed to open repo: " + path);
				recentStackPanel.Children.Remove(button);
				return;
			}

			// load main repo page
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
