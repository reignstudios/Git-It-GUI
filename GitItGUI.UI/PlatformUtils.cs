using System.Windows.Forms;
using System.Windows.Interop;

namespace GitItGUI.UI.Utils
{
	static class PlatformUtils
    {
		public static bool SelectFolder(out string folderPath)
		{
			var dlg = new FolderBrowserDialog();
			var nativeWindow = new NativeWindow();
			nativeWindow.AssignHandle(new WindowInteropHelper(MainWindow.singleton).Handle);
			if (dlg.ShowDialog(nativeWindow) == DialogResult.OK)
			{
				folderPath = dlg.SelectedPath;
				return true;
			}

			folderPath = null;
			return false;
		}
	}
}
