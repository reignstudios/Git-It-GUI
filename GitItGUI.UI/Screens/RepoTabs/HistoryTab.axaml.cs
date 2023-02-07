using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for HistoryTab.xaml
    /// </summary>
    public partial class HistoryTab : UserControl
    {
		public static HistoryTab singleton;

        public HistoryTab()
        {
			singleton = this;
            InitializeComponent();
        }

		public void OpenHistory(string filename)
		{
			MainWindow.singleton.ShowWaitingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				RepoScreen.singleton.repoManager.OpenGitk(filename);
				MainWindow.singleton.HideWaitingOverlay();
			});
		}

		private void historyButton_Click(object sender, RoutedEventArgs e)
		{
			OpenHistory(null);
		}
	}
}
