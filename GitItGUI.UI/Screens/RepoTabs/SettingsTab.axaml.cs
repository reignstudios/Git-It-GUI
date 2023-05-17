using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

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
			applyButton.IsVisible = false;
			cancelButton.IsVisible = false;
        }

		public void Refresh()
		{
			refreshMode = true;
			sigName.Text = RepoScreen.singleton.repoManager.signatureName;
			sigEmail.Text = RepoScreen.singleton.repoManager.signatureEmail;
			isLocalToggleButton.IsChecked = RepoScreen.singleton.repoManager.signatureIsLocal;
			applyButton.IsVisible = false;
			cancelButton.IsVisible = false;
			refreshMode = false;
		}

		private void applyButton_Click(object sender, RoutedEventArgs e)
		{
			//// validate name
			//if (sigName.Text.Length < 3)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "Signature name to short");
			//	return;
			//}

			//// validate email
			//var match = Regex.Match(sigEmail.Text, @"(.*)@(.*)\.(\w*)");
			//if (!match.Success)
			//{
			//	MainWindow.singleton.ShowMessageOverlay("Alert", "Invalid signature email");
			//	return;
			//}

			//// apply
			//applyButton.IsVisible = false;
			//cancelButton.IsVisible = false;
			//string name = sigName.Text, email = sigEmail.Text;
			//bool isLocal = isLocalToggleButton.IsChecked == true;
			//MainWindow.singleton.ShowProcessingOverlay();
			//RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			//{
			//	RepoScreen.singleton.repoManager.UpdateSignature(name, email, isLocal ? GitCommander.SignatureLocations.Local : GitCommander.SignatureLocations.Global);
			//	MainWindow.singleton.HideProcessingOverlay();
			//});
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		private void sigName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (applyButton != null && !refreshMode)
			{
				applyButton.IsVisible = true;
				cancelButton.IsVisible = true;
			}
		}

		private void sigEmail_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (applyButton != null && !refreshMode)
			{
				applyButton.IsVisible = true;
				cancelButton.IsVisible = true;
			}
		}

		private void isLocalToggleButton_Checked(object sender, RoutedEventArgs e)
		{
			if (applyButton != null && !refreshMode)
			{
				applyButton.IsVisible = true;
				cancelButton.IsVisible = true;
			}
		}
	}
}
