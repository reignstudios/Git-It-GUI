using GitCommander;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GitItGUI.UI.Images
{
    static class ImagePool
    {
		public static BitmapImage newImage, deletedImage, conflictedImage, modifiedImage, renamedImage, typeChangedImage, unknownImage;

		static ImagePool()
		{
			var uriSource = new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/new.png");
			newImage = new BitmapImage(uriSource);

			uriSource = new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/deleted.png");
			deletedImage = new BitmapImage(uriSource);

			uriSource = new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/conflicted.png");
			conflictedImage = new BitmapImage(uriSource);

			uriSource = new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/modified.png");
			modifiedImage = new BitmapImage(uriSource);

			uriSource = new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/renamed.png");
			renamedImage = new BitmapImage(uriSource);

			uriSource = new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/typeChanged.png");
			typeChangedImage = new BitmapImage(uriSource);

			uriSource = new Uri(@"pack://application:,,,/GitItGUI.UI;component/Images/unknown.png");
			unknownImage = new BitmapImage(uriSource);
		}

		public static BitmapImage GetImage(FileStates state)
		{
			if ((state & FileStates.Conflicted) != 0) return conflictedImage;
			else if ((state & FileStates.NewInIndex) != 0 || (state & FileStates.NewInWorkdir) != 0) return newImage;
			else if ((state & FileStates.DeletedFromIndex) != 0 || (state & FileStates.DeletedFromWorkdir) != 0) return deletedImage;
			else if ((state & FileStates.ModifiedInIndex) != 0 || (state & FileStates.ModifiedInWorkdir) != 0) return modifiedImage;
			else if ((state & FileStates.RenamedInIndex) != 0 || (state & FileStates.RenamedInWorkdir) != 0) return renamedImage;
			else if ((state & FileStates.TypeChangeInIndex) != 0 || (state & FileStates.TypeChangeInWorkdir) != 0) return typeChangedImage;
			else return unknownImage;
		}
    }
}
