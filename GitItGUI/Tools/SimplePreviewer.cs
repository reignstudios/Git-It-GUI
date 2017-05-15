using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.IO;

namespace AvaloniaPV
{
    public static class SimplePreviewer
    {
		public static bool Init(bool forceOn = false)
		{
			if (forceOn)
			{
				AppBuilder.Configure<App>().UsePlatformDetect().Start<XamlWindow>();
				return true;
			}

			bool pass = false;
			foreach (var arg in Environment.GetCommandLineArgs())
			{
				if (arg == "-usepv")
				{
					pass = true;
					break;
				}
			}

			if (pass) AppBuilder.Configure<App>().UsePlatformDetect().Start<XamlWindow>();
			return pass;
		}
    }

	class App : Application
	{
		public override void Initialize()
		{
			string xaml = @"
<Application xmlns='https://github.com/avaloniaui'>
  <Application.Styles>
    <StyleInclude Source='resm:Avalonia.Themes.Default.DefaultTheme.xaml?assembly=Avalonia.Themes.Default'/>
    <StyleInclude Source='resm:Avalonia.Themes.Default.Accents.BaseLight.xaml?assembly=Avalonia.Themes.Default'/>
  </Application.Styles>
</Application>
			";
			
			var loader = new AvaloniaXamlLoader();
			loader.Load(xaml, this);
			base.Initialize();
		}
	}

	public class XamlWindow : Window
	{
		private PreviewWindow previewWindow;
		private Button openButton;
		private TextBox filepathTextBox;

		public XamlWindow()
		{
			var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
	Title='Simple Previewer - XAML'
	Width='800' Height='64'>

	<Grid>
		<Button Name='openButton' Width='228' Height='32' HorizontalAlignment='Left' VerticalAlignment='Bottom' Margin='10,-10,-10,10'>Open / Reload</Button>
		<TextBox Name='filepathTextBox' Height='32' VerticalAlignment='Bottom' Margin='280,-10,10,10'/>
	</Grid>

</Window>
";

			BeginInit();
			var loader = new AvaloniaXamlLoader();
			loader.Load(xaml, this);
			
			openButton = this.Find<Button>("openButton");
			filepathTextBox = this.Find<TextBox>("filepathTextBox");
			openButton.Click += OpenButton_Click;

			previewWindow = new PreviewWindow();
			previewWindow.Closed += PreviewWindow_Closed;
			previewWindow.Show();
		}

		private void PreviewWindow_Closed(object sender, EventArgs e)
		{
			Close();
		}

		private void OpenButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
		{
			previewWindow.Open(filepathTextBox.Text);
		}
	}

	public class PreviewWindow : Window
	{
		private Grid grid;

		public PreviewWindow()
		{
			var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
	Title='Simple Previewer - Visual'
	Width='500' Height='500'>

	<Grid Name='grid' Background='Gray'>
		<TextBlock HorizontalAlignment='Center' VerticalAlignment='Center'>Nothing loaded...</TextBlock>
	</Grid>

</Window>
";

			BeginInit();
			var loader = new AvaloniaXamlLoader();
			loader.Load(xaml, this);

			grid = this.Find<Grid>("grid");
		}

		public void Open(string path)
		{
			try
			{
				using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
				using (var reader = new StreamReader(stream))
				{
					BeginInit();
					var loader = new AvaloniaXamlLoader();
					loader.Load(stream, this);
				}
			}
			catch (Exception e)
			{
				HandleError(e);
			}
		}

		private void HandleError(Exception e)
		{
			var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
	Title='Simple Previewer - Visual'
	Width='500' Height='500'>

	<Grid Name='grid' Background='DarkRed'>
		<TextBox Name='errorBox' HorizontalAlignment='Center' VerticalAlignment='Center' TextWrapping='Wrap' IsReadOnly='True'>Error</TextBox>
	</Grid>

</Window>
";

				BeginInit();
				var loader = new AvaloniaXamlLoader();
				loader.Load(xaml, this);
				this.Find<TextBox>("errorBox").Text = "ERROR: " + e.Message;
		}
	}
}
