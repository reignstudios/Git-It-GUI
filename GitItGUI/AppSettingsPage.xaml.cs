using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;

namespace GitItGUI
{
	public class AppSettingsPage : UserControl, NavigationPage
	{
		public static AppSettingsPage singleton;

		private ListBox mergeDiffToolListBox;
		private CheckBox autoRefreshChanges;
		private Button doneButton;

		public AppSettingsPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			mergeDiffToolListBox = this.Find<ListBox>("mergeDiffToolListBox");
			autoRefreshChanges = this.Find<CheckBox>("autoRefreshChanges");
			doneButton = this.Find<Button>("doneButton");

			// apply bindings
			doneButton.Click += DoneButton_Click;
		}

		private void DoneButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			MainWindow.LoadPage(PageTypes.Start);
		}

		public void NavigatedTo()
		{
			switch (AppManager.mergeDiffTool)
			{
				case MergeDiffTools.Meld: mergeDiffToolListBox.SelectedIndex = 0; break;
				case MergeDiffTools.kDiff3: mergeDiffToolListBox.SelectedIndex = 1; break;
				case MergeDiffTools.P4Merge: mergeDiffToolListBox.SelectedIndex = 2; break;
				case MergeDiffTools.DiffMerge: mergeDiffToolListBox.SelectedIndex = 3; break;
				default: MessageBox.Show("Unsuported Merge/Diff tool type: " + AppManager.mergeDiffTool); break;
			}

			autoRefreshChanges.IsChecked = AppManager.autoRefreshChanges;
		}

		public void NavigatedFrom()
		{
			switch (mergeDiffToolListBox.SelectedIndex)
			{
				case 0: AppManager.SetMergeDiffTool(MergeDiffTools.Meld); break;
				case 1: AppManager.SetMergeDiffTool(MergeDiffTools.kDiff3); break;
				case 2: AppManager.SetMergeDiffTool(MergeDiffTools.P4Merge); break;
				case 3: AppManager.SetMergeDiffTool(MergeDiffTools.DiffMerge); break;
				default: MessageBox.Show("Unsuported Merge/Diff tool index: " + mergeDiffToolListBox.SelectedIndex); break;
			}

			AppManager.autoRefreshChanges = autoRefreshChanges.IsChecked;
		}
	}
}
