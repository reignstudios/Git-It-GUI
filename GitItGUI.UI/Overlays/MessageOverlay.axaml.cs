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

		public static bool optionChecked;

		public MessageOverlay()
		{
			InitializeComponent();
		}

		public void Setup(string title, string message, string option, MessageOverlayTypes type, DoneCallbackMethod doneCallback)
		{
			// cancel pending message
			if (this.doneCallback != null) doneCallback(MessageOverlayResults.Cancel);
			this.doneCallback = doneCallback;

			// setup
			titleTextBox.Text = title;
			messageLabel.Text = message;
			if (option != null)
			{
				optionCheckBox.IsChecked = true;
				optionCheckBox.Content = option;
				optionCheckBox.IsVisible = true;
			}
			else
			{
				optionCheckBox.IsChecked = false;
				optionCheckBox.IsVisible = false;
			}

			if (type == MessageOverlayTypes.Ok)
			{
				okButton.Content = "Ok";
				cancelButton.IsVisible = false;
			}
			else if (type == MessageOverlayTypes.OkCancel)
			{
				okButton.Content = "Ok";
				cancelButton.Content = "Cancel";
				cancelButton.IsVisible = true;
			}
			else
			{
				okButton.Content = "Yes";
				cancelButton.Content = "No";
				cancelButton.IsVisible = true;
			}
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			optionChecked = optionCheckBox.IsChecked == true;

			IsVisible = false;
			var callback = doneCallback;
			doneCallback = null;
			if (callback != null) callback(MessageOverlayResults.Ok);
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			IsVisible = false;
			var callback = doneCallback;
			doneCallback = null;
			if (callback != null) callback(MessageOverlayResults.Cancel);
		}
	}
}
