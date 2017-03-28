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
				button.Content = repo;
				button.Click += RecentButton_Click;
				recentStackPanel.Children.Add(button);
			}
		}

		private void CloneButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.Clone);
		}

		private async void OpenButton_Click(object sender, RoutedEventArgs e)
		{
			// select path
			grid.IsVisible = false;
			var dlg = new OpenFolderDialog();
			var path = await dlg.ShowAsync();
			if (string.IsNullOrEmpty(path))
			{
				grid.IsVisible = true;
				return;
			}

			// open repo
			if (!RepoManager.OpenRepo(path, true))
			{
				MessageBox.Show("Failed to open repo: " + path);
				grid.IsVisible = true;
				return;
			}

			// load main repo page
			grid.IsVisible = true;
			MainContent.singleton.tabControlNavigateIndex = 0;
			MainWindow.LoadPage(PageTypes.MainContent);
		}

		private void RecentButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			string path = (string)button.Content;

			// open repo
			if (!RepoManager.OpenRepo(path, true))
			{
				// remove bad repo from list
				MessageBox.Show("Failed to open repo: " + path);
				recentStackPanel.Children.Remove(button);
				AppManager.RemoveRepoFromHistory(path);
				return;
			}

			// load main repo page
			MainContent.singleton.tabControlNavigateIndex = 0;
			MainWindow.LoadPage(PageTypes.MainContent);
		}
	}
}
