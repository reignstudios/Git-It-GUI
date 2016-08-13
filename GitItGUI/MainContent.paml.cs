using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GitItGUI
{
	public class MainContent : UserControl
	{
		public static MainContent singleton;

		public MainContent()
		{
			singleton = this;
			this.InitializeComponent();
		}

		private void InitializeComponent()
		{
			// load pages
			ChangesPage.singleton = new ChangesPage();
			BranchesPage.singleton = new BranchesPage();
			HistoryPage.singleton = new HistoryPage();
			RepoPage.singleton = new RepoPage();
			AppSettingsPage.singleton = new AppSettingsPage();

			// load main page
			AvaloniaXamlLoader.Load(this);

			var changesPage = this.Find<TabItem>("changesPage");
			changesPage.Content = ChangesPage.singleton;

			var branchesPage = this.Find<TabItem>("branchesPage");
			branchesPage.Content = BranchesPage.singleton;

			var historyPage = this.Find<TabItem>("historyPage");
			historyPage.Content = HistoryPage.singleton;

			var repoPage = this.Find<TabItem>("repoPage");
			repoPage.Content = RepoPage.singleton;

			var appSettingsPage = this.Find<TabItem>("appSettingsPage");
			appSettingsPage.Content = AppSettingsPage.singleton;
		}
	}
}
