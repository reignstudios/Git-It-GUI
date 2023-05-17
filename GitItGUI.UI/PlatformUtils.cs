//using System.Windows.Forms;
//using System.Windows.Interop;
//using WPFFolderBrowser;

namespace GitItGUI.UI.Utils
{
	static class PlatformUtils
    {
		public static bool SelectFolder(out string folderPath)
		{
			// ==============================
			// NOTE: keep this for legacy ref
			// ==============================
			/*var dlg = new FolderBrowserDialog();
			var nativeWindow = new NativeWindow();
			nativeWindow.AssignHandle(new WindowInteropHelper(MainWindow.singleton).Handle);
			if (dlg.ShowDialog(nativeWindow) == DialogResult.OK)
			{
				folderPath = dlg.SelectedPath;
				return true;
			}

			folderPath = null;
			return false;*/

			/*using (var dlg = new WPFFolderBrowserDialog("Select Folder Test"))
			{
				if (dlg.ShowDialog(MainWindow.singleton) == true)
				{
					folderPath = dlg.FileName;
					return true;
				}
			}*/

			folderPath = null;
			return false;
		}
	}
}
