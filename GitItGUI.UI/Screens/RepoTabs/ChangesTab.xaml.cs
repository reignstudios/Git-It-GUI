using GitItGUI.UI.Images;
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
    /// Interaction logic for ChangesTab.xaml
    /// </summary>
    public partial class ChangesTab : UserControl
    {
        public ChangesTab()
        {
            InitializeComponent();

			var p = previewTextBox.Document.Blocks.FirstBlock as Paragraph;
			p.LineHeight = 1;

			var range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
			range.Text = "+ Addition" + Environment.NewLine;
			range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Green);

			range = new TextRange(previewTextBox.Document.ContentEnd, previewTextBox.Document.ContentEnd);
			range.Text = "- Subtraction";
			range.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
		}

		public void Refresh()
		{
			stagedChangesListBox.Items.Clear();
			unstagedChangesListBox.Items.Clear();
			foreach (var fileState in RepoScreen.singleton.repoManager.GetFileStates())
			{
				var item = new ListBoxItem();

				var button = new Button();
				button.Width = 20;
				button.Height = 20;
				button.HorizontalAlignment = HorizontalAlignment.Left;
				button.Background = new SolidColorBrush(Colors.Transparent);
				button.BorderBrush = new SolidColorBrush(Colors.LightGray);
				button.BorderThickness = new Thickness(1);
				var image = new Image();
				image.Source = ImagePool.GetImage(fileState.state);
				button.Content = image;

				var label = new Label();
				label.Margin = new Thickness(20, 0, 0, 0);
				label.Content = fileState.filename;
				label.ContextMenu = new ContextMenu();
				var openFileMenu = new MenuItem();
				openFileMenu.Header = "Open file";
				openFileMenu.ToolTip = fileState.filename;
				openFileMenu.Click += OpenFileMenu_Click;
				var openFileLocationMenu = new MenuItem();
				openFileLocationMenu.Header = "Open file location";
				openFileMenu.ToolTip = fileState.filename;
				openFileLocationMenu.Click += OpenFileLocationMenu_Click;
				label.ContextMenu.Items.Add(openFileMenu);
				label.ContextMenu.Items.Add(openFileLocationMenu);

				var grid = new Grid();
				grid.Children.Add(button);
				grid.Children.Add(label);
				item.Content = grid;
				if (fileState.IsStaged()) stagedChangesListBox.Items.Add(item);
				else unstagedChangesListBox.Items.Add(item);
			}
		}

		private void ToolButton_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			button.ContextMenu.IsOpen = true;
		}

		private void OpenFileMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			RepoScreen.singleton.repoManager.OpenFile((string)item.ToolTip);
		}

		private void OpenFileLocationMenu_Click(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)sender;
			RepoScreen.singleton.repoManager.OpenFileLocation((string)item.ToolTip);
		}
	}
}
