using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	/// Interaction logic for ProcessingOverlay.xaml
	/// </summary>
	public partial class ProcessingOverlay : UserControl
	{
		private double rot;
		private Stopwatch stopwatch;

		public ProcessingOverlay()
		{
			InitializeComponent();
			stopwatch = new Stopwatch();
			CompositionTarget.Rendering += CompositionTarget_Rendering;
		}

		private void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			if (Visibility == Visibility.Visible)
			{
				spinnerImage.LayoutTransform = new RotateTransform(rot);
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
