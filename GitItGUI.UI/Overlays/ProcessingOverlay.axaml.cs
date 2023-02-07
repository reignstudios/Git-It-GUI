using GitItGUI.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Rendering.Composition;

namespace GitItGUI.UI.Overlays
{
	/// <summary>
	/// Interaction logic for ProcessingOverlay.xaml
	/// </summary>
	public partial class ProcessingOverlay : UserControl
	{
		private double rot;
		private Stopwatch stopwatch;

		public ProcessingOverlay()
		{
			InitializeComponent();
			DebugLog.WriteCallback += DebugLog_WriteCallback;
			stopwatch = new Stopwatch();
			CompositionTarget.Rendering += CompositionTarget_Rendering;
		}

		private void DebugLog_WriteCallback(string value)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				statusTextBox.Text = value;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate()
				{
					statusTextBox.Text = value;
				});
			}
		}

		private void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			if (IsVisible)
			{
				spinnerImage.RenderTransform = new RotateTransform(rot);
				stopwatch.Stop();
				rot += (stopwatch.ElapsedMilliseconds / 1000d) * 60;
				stopwatch.Restart();
			}
		}

		public void SetStatusText(string text)
		{
			statusTextBox.Text = text;
		}
	}
}
