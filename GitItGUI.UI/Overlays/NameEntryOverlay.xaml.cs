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
		public delegate void DoneCallbackMethod(string name, bool succeeded);
		private DoneCallbackMethod doneCallback;

		public NameEntryOverlay()
		{
			InitializeComponent();
		}

		public void Setup(string currentName, DoneCallbackMethod doneCallback)
		{
			this.doneCallback = doneCallback;
			nameTextBox.Text = currentName;
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
			if (doneCallback != null) doneCallback(null, false);
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
			if (doneCallback != null) doneCallback(nameTextBox.Text, true);
		}
	}
}
