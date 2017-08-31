using GitItGUI.Core;
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
using System.Windows.Threading;

namespace GitItGUI.UI.Screens
{
    /// <summary>
    /// Interaction logic for RepoUserControl.xaml
    /// </summary>
    public partial class RepoScreen : UserControl
    {
		public static RepoScreen singleton;
		public RepoManager repoManager;
		public Dispatcher repoDispatcher;

		public RepoScreen()
        {
			singleton = this;
            InitializeComponent();
			grid.Visibility = Visibility.Hidden;
		}

		public void Init()
		{
			repoManager = new RepoManager(MainWindow.singleton.Dispatcher, RepoReadyCallback);
		}

		private void RepoReadyCallback(Dispatcher dispatcher)
		{
			repoDispatcher = dispatcher;
			grid.Visibility = Visibility.Visible;
		}

		public void Dispose()
		{
			repoManager.Dispose();
		}

		public bool OpenRepo(string folderPath)
		{
			return repoManager.OpenRepo(folderPath);
		}
    }
}
