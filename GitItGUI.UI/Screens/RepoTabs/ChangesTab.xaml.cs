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
    }
}
