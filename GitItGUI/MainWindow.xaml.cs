using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace GitItGUI
{
	public enum PageTypes
	{
		CheckForUpdates,
		Start,
		MainContent
	}

	public delegate void UpdateUICallbackMethod();

	public class MainWindow : Window
	{
		public static MainWindow singleton;

		public static UpdateUICallbackMethod UpdateUICallback, FinishedUpdatingUICallback;
		//public static bool uiUpdating;
		public static XML.AppSettings appSettings;

		public MainWindow()
		{
			singleton = this;
			this.InitializeComponent();
			App.AttachDevTools(this);
			
			// load settings
			appSettings = Settings.Load<XML.AppSettings>(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + Settings.FolderName + "\\" + Settings.GuiFilename);
			if (appSettings.defaultGitLFS_Exts.Count == 0)
			{
				appSettings.defaultGitLFS_Exts.AddRange(new List<string>()
				{
					".psd", ".jpg", ".jpeg", ".png", ".bmp", ".tga",// image types
					".mpeg", ".mov", ".avi", ".mp4", ".wmv",// video types
					".wav", ".mp3", ".ogg", ".wma", ".acc",// audio types
					".zip", ".7z", ".rar", ".tar", ".gz",// compression types
					".fbx", ".obj", ".3ds", ".blend", ".ma", ".mb", ".dae",// 3d formats
					".pdf",// doc types
					".bin", ".data", ".raw", ".hex",// unknown binary types
				});
			}
		}

		private void InitializeComponent()
		{
			// load pages
			CheckForUpdatesPage.singleton = new CheckForUpdatesPage();
			StartPage.singleton = new StartPage();
			MainContent.singleton = new MainContent();

			// load main page
			AvaloniaXamlLoader.Load(this);
			this.Closed += MainWindow_Closed;

			LoadPage(PageTypes.CheckForUpdates);
			CheckForUpdatesPage.singleton.Check("http://reign-studios-services.com/GitItGUI/VersionInfo.xml");
		}

		public static void LoadPage(PageTypes type)
		{
			switch (type)
			{
				case PageTypes.CheckForUpdates: singleton.Content = CheckForUpdatesPage.singleton; break;
				case PageTypes.Start: singleton.Content = StartPage.singleton; break;
				case PageTypes.MainContent: singleton.Content = MainContent.singleton; break;
			}
		}

		public static void UpdateUI()
		{
			//uiUpdating = true;
			if (UpdateUICallback != null) UpdateUICallback();
			//uiUpdating = false;

			if (FinishedUpdatingUICallback != null) FinishedUpdatingUICallback();
		}

		public static void CanInteractWithUI(bool enabled)
		{
			//singleton.tabControl.IsEnabled = enabled;
		}

		private void MainWindow_Closed(object sender, EventArgs e)
		{
			RepoPage.Dispose();
			Settings.Save<XML.AppSettings>(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + Settings.FolderName + "\\" + Settings.GuiFilename, appSettings);
		}
	}
}
