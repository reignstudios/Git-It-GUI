using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitItGUI
{
	public class FileItem
	{
		private Image icon;
		public Image Icon {get {return icon;}}

		private string filename;
		public string Filename {get {return filename;}}

		public FileItem()
		{
			filename = "ERROR";
		}

		public FileItem(string iconFilename, string filename)
		{
			icon = new Image();
			icon.BeginInit();
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			using (var stream = assembly.GetManifestResourceStream(iconFilename))
			{
				icon.Source = new Bitmap(stream);
			}
			icon.EndInit();
			
			this.filename = filename;
		}
	}

	public class ChangesPage : UserControl
	{
		public static ChangesPage singleton;

		// ui objects
		Button refreshChangedButton, revertAllButton, stageAllButton, unstageAllButton, resolveSelectedButton, resolveAllButton;
		Button openDiffToolButton, commitStagedButton, syncChangesButton;
		ListBox unstagedChangesListView, stagedChangesListView;
		ScrollViewer diffTextBoxScrollViewer;
		TextBox diffTextBox;

		List<FileItem> unstagedChangesListViewItems, stagedChangesListViewItems;

		public ChangesPage()
		{
			singleton = this;
			LoadUI();
		}

		private void LoadUI()
		{
			AvaloniaXamlLoader.Load(this);

			unstagedChangesListView = this.Find<ListBox>("unstagedChangesListView");
			stagedChangesListView = this.Find<ListBox>("stagedChangesListView");

			// apply bindings
			unstagedChangesListViewItems = new List<FileItem>();
			stagedChangesListViewItems = new List<FileItem>();
			unstagedChangesListView.Items = unstagedChangesListViewItems;
			stagedChangesListView.Items = stagedChangesListViewItems;

			unstagedChangesListViewItems.Add(new FileItem("GitItGUI.Icons.AppIcon.png", "testing"));
			unstagedChangesListViewItems.Add(new FileItem("GitItGUI.Icons.AppIcon.png", "testing2"));
		}
	}
}
