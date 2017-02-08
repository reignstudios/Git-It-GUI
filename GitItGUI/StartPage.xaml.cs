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
		Grid grid;
		StackPanel recentStackPanel;
		Button cloneButton, openButton, settingsButton;

		public StartPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			grid = this.Find<Grid>("grid");
			recentStackPanel = this.Find<StackPanel>("recentStackPanel");
			cloneButton = this.Find<Button>("cloneButton");
			openButton = this.Find<Button>("openButton");
			settingsButton = this.Find<Button>("settingsButton");
			cloneButton.Click += CloneButton_Click;
			openButton.Click += OpenButton_Click;
			settingsButton.Click += SettingsButton_Click;
		}

		private void SettingsButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.AppSettings);
		}

		public void NavigatedTo()
		{
			RefreshUI();
		}

		public void NavigatedFrom()
		{
			
		}

		public void RefreshUI()
		{
			// fill repo list
			recentStackPanel.Children.Clear();
			foreach (var repo in AppManager.repositories)
			{
				var button = new Button();
				button.Content = repo.path;
				button.Click += RecentButton_Click;
				recentStackPanel.Children.Add(button);
			}
		}

		private void OpenRepo(string path)
		{
			// open repo
			if (!RepoManager.OpenRepo(path))
			{
				// remove bad repo from list
				MessageBox.Show("Failed to open repo: " + path);
				grid.IsVisible = true;
				return;
			}

			// load main repo page
			grid.IsVisible = true;
			MainWindow.LoadPage(PageTypes.MainContent);
		}

		private async void CloneButton_Click(object sender, RoutedEventArgs e)
		{
			// get url, username and password
			// TODO: create core app for this
			return;

			// get destination
			grid.IsVisible = false;
			var dlg = new OpenFolderDialog();
			var path = await dlg.ShowAsync();
			if (string.IsNullOrEmpty(path))
			{
				grid.IsVisible = true;
				return;
			}
			
			if (!RepoManager.Clone("", path, "", "", out path))
			{
				MessageBox.Show("Failed to clone repo: " + path);
				grid.IsVisible = true;
				return;
			}

			OpenRepo(path);
		}

		private async void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			grid.IsVisible = false;
			var dlg = new OpenFolderDialog();
			var path = await dlg.ShowAsync();
			if (string.IsNullOrEmpty(path))
			{
				grid.IsVisible = true;
				return;
			}

			OpenRepo(path);
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
