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
	public enum MessageOverlayTypes
	{
		Ok,
		OkCancel,
		YesNo
	}

	public enum MessageOverlayResults
	{
		Ok,
		Cancel
	}

	/// <summary>
	/// Interaction logic for MessageOverlay.xaml
	/// </summary>
	public partial class MessageOverlay : UserControl
	{
		public delegate void DoneCallbackMethod(MessageOverlayResults result);
		private DoneCallbackMethod doneCallback;

		public MessageOverlay()
		{
			InitializeComponent();
		}

		public void Setup(string title, string message, MessageOverlayTypes type, DoneCallbackMethod doneCallback)
		{
			// cancel pending message
			if (doneCallback != null) doneCallback(MessageOverlayResults.Cancel);
			this.doneCallback = doneCallback;

			// setup
			titleLabel.Content = title;
			messageLabel.Text = message;
			if (type == MessageOverlayTypes.Ok)
			{
				okButton.Content = "Ok";
				cancelButton.Visibility = Visibility.Hidden;
			}
			else if (type == MessageOverlayTypes.OkCancel)
			{
				okButton.Content = "Ok";
				cancelButton.Content = "Cancel";
				cancelButton.Visibility = Visibility.Visible;
			}
			else
			{
				okButton.Content = "Yes";
				cancelButton.Content = "No";
				cancelButton.Visibility = Visibility.Visible;
			}
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
			if (doneCallback != null) doneCallback(MessageOverlayResults.Ok);
			doneCallback = null;
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			Visibility = Visibility.Hidden;
			if (doneCallback != null) doneCallback(MessageOverlayResults.Cancel);
			doneCallback = null;
		}
	}
}
