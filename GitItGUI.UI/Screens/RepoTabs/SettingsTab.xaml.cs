using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace GitItGUI.UI.Screens.RepoTabs
{
    /// <summary>
    /// Interaction logic for SettingsTab.xaml
    /// </summary>
    public partial class SettingsTab : UserControl
    {
		private bool refreshMode;

        public SettingsTab()
        {
            InitializeComponent();
			applyButton.Visibility = Visibility.Hidden;
        }

		public void Refresh()
		{
			refreshMode = true;
			sigName.Text = RepoScreen.singleton.repoManager.signatureName;
			sigEmail.Text = RepoScreen.singleton.repoManager.signatureEmail;
			isLocalToggleButton.IsChecked = RepoScreen.singleton.repoManager.signatureIsLocal;
			applyButton.Visibility = Visibility.Hidden;
			refreshMode = false;
		}

		private void applyButton_Click(object sender, RoutedEventArgs e)
		{
			// validate name
			if (sigName.Text.Length < 3)
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "Signature name to short");
				return;
			}

			// validate email
			var match = Regex.Match(sigEmail.Text, @"(.*)@(.*)\.(\w*)");
			if (!match.Success)
			{
				MainWindow.singleton.ShowMessageOverlay("Alert", "Invalid signature email");
				return;
			}

			// apply
			applyButton.Visibility = Visibility.Hidden;
			string name = sigName.Text, email = sigEmail.Text;
			bool isLocal = isLocalToggleButton.IsChecked == true;
			MainWindow.singleton.ShowProcessingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				RepoScreen.singleton.repoManager.UpdateSignature(name, email, isLocal ? GitCommander.SignatureLocations.Local : GitCommander.SignatureLocations.Global);
				MainWindow.singleton.HideProcessingOverlay();
			});
		}

		private void sigName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (applyButton != null && !refreshMode) applyButton.Visibility = Visibility.Visible;
		}

		private void sigEmail_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (applyButton != null && !refreshMode) applyButton.Visibility = Visibility.Visible;
		}

		private void isLocalToggleButton_Checked(object sender, RoutedEventArgs e)
		{
			if (applyButton != null && !refreshMode) applyButton.Visibility = Visibility.Visible;
		}
	}
}
