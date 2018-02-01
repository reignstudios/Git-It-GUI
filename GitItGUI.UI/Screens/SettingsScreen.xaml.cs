using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GitItGUI.Core;

namespace GitItGUI.UI.Screens
{
    /// <summary>
    /// Interaction logic for SettingsScreen.xaml
    /// </summary>
    public partial class SettingsScreen : UserControl
    {
		public static SettingsScreen singleton;

		public SettingsScreen()
        {
			singleton = this;
			InitializeComponent();
        }

		public void Setup()
		{
			autoRefreshCheckBox.IsChecked = AppManager.settings.autoRefreshChanges;
			showLSFTagsCheckBox.IsChecked = AppManager.settings.showLFSTags;
			switch (AppManager.settings.mergeDiffTool)
			{
				case MergeDiffTools.None: mergeDiffToolComboBox.SelectedIndex = 0; break;
				case MergeDiffTools.Meld: mergeDiffToolComboBox.SelectedIndex = 1; break;
				case MergeDiffTools.kDiff3: mergeDiffToolComboBox.SelectedIndex = 2; break;
				case MergeDiffTools.P4Merge: mergeDiffToolComboBox.SelectedIndex = 3; break;
				case MergeDiffTools.DiffMerge: mergeDiffToolComboBox.SelectedIndex = 4; break;
			}
		}

		public void Apply()
		{
			AppManager.settings.autoRefreshChanges = autoRefreshCheckBox.IsChecked == true;
			AppManager.settings.showLFSTags = showLSFTagsCheckBox.IsChecked == true;
			var mergeTool = MergeDiffTools.kDiff3;
			switch (mergeDiffToolComboBox.SelectedIndex)
			{
				case 0: mergeTool = MergeDiffTools.None; break;
				case 1: mergeTool = MergeDiffTools.Meld; break;
				case 2: mergeTool = MergeDiffTools.kDiff3; break;
				case 3: mergeTool = MergeDiffTools.P4Merge; break;
				case 4: mergeTool = MergeDiffTools.DiffMerge; break;
			}

			AppManager.SetMergeDiffTool(mergeTool);
			ValidateDiffMergeTool();
		}

		public void ValidateDiffMergeTool()
		{
			if (AppManager.isMergeToolInstalled || AppManager.settings.mergeDiffTool == MergeDiffTools.None) return;
			MainWindow.singleton.ShowMessageOverlay("Warning", "Diff/Merge tool not installed!\nSome app functions will fail.\nGo to app settings.");
		}

		private void doneButton_Click(object sender, RoutedEventArgs e)
		{
			Apply();
			MainWindow.singleton.Navigate(StartScreen.singleton);
		}
	}
}
