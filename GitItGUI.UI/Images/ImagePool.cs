using GitCommander;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace GitItGUI.UI.Images
{
    static class ImagePool
    {
		public static Bitmap newImage, deletedImage, conflictedImage, modifiedImage, renamedImage, typeChangedImage, unknownImage;

		static ImagePool()
		{
			newImage = new Bitmap("Images/new.png");

			deletedImage = new Bitmap("Images/deleted.png");
			conflictedImage = new Bitmap("Images/conflicted.png");
			modifiedImage = new Bitmap("Images/modified.png");
			renamedImage = new Bitmap("Images/renamed.png");
			typeChangedImage = new Bitmap("Images/typeChanged.png");
			unknownImage = new Bitmap("Images/unknown.png");
		}

		public static Bitmap GetImage(FileStates state)
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
