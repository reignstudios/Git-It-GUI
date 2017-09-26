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
			switch (state)
			{
				case FileStates.NewInIndex:
				case FileStates.NewInWorkdir:
					return newImage;

				case FileStates.DeletedFromIndex:
				case FileStates.DeletedFromWorkdir:
					return deletedImage;

				case FileStates.ModifiedInIndex:
				case FileStates.ModifiedInWorkdir:
					return modifiedImage;

				case FileStates.RenamedInIndex:
				case FileStates.RenamedInWorkdir:
					return renamedImage;

				case FileStates.TypeChangeInIndex:
				case FileStates.TypeChangeInWorkdir:
					return typeChangedImage;

				case FileStates.Conflicted:
					return conflictedImage;

				default:
					return unknownImage;
			}
		}
    }
}
