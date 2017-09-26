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

namespace GitItGUI.UI.Screens.RepoTabs
{
    /// <summary>
    /// Interaction logic for HistoryTab.xaml
    /// </summary>
    public partial class HistoryTab : UserControl
    {
        public HistoryTab()
        {
            InitializeComponent();
        }

		private void historyButton_Click(object sender, RoutedEventArgs e)
		{
			MainWindow.singleton.ShowWaitingOverlay();
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				RepoScreen.singleton.repoManager.OpenGitk();
				MainWindow.singleton.HideWaitingOverlay();
			});
		}
	}
}
