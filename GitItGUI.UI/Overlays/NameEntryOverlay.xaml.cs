using GitItGUI.UI.Screens;
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

namespace GitItGUI.UI.Overlays
{
	/// <summary>
	/// Interaction logic for NameEntryOverlay.xaml
	/// </summary>
	public partial class NameEntryOverlay : UserControl
	{
		public delegate void DoneCallbackMethod(string name, string remoteName, bool succeeded);
		private DoneCallbackMethod doneCallback;

		public NameEntryOverlay()
		{
			InitializeComponent();
		}

		public void Setup(string currentName, bool showRemotesOption, DoneCallbackMethod doneCallback)
		{
			this.doneCallback = doneCallback;
			nameTextBox.Text = currentName;
			remoteLabel.Visibility = showRemotesOption ? Visibility.Visible : Visibility.Hidden;
			remoteComboBox.Visibility = showRemotesOption ? Visibility.Visible : Visibility.Hidden;
			if (showRemotesOption)
			{
				remoteComboBox.Items.Clear();
				var item = new ComboBoxItem();
				item.Content = "[Local Branch]";
				item.ToolTip = "This branch will only be tracked by your computer";
				remoteComboBox.Items.Add(item);
				foreach (var remote in RepoScreen.singleton.repoManager.remoteStates)
				{
					item = new ComboBoxItem();
					item.Content = remote.name;
					item.ToolTip = "URL: " + remote.url;
					remoteComboBox.Items.Add(item);
				}

				remoteComboBox.SelectedIndex = 0;
			}
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
			if (doneCallback != null) doneCallback(null, null, false);
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
			var item = (ComboBoxItem)remoteComboBox.SelectedItem;
			if (doneCallback != null)
			{
				string remoteName = null;
				if (item != null) remoteName = item.Content as string;
				doneCallback(nameTextBox.Text, remoteName, true);
			}
		}
	}
}
