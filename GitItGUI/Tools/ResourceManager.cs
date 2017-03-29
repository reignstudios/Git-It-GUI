using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using GitCommander;

namespace GitItGUI
{
	static class ResourceManager
	{
		public static Bitmap iconNew, iconRenamed, iconTypeChanged, iconModified, iconDeleted, iconConflicted;

		private static Bitmap LoadBitmap(string filename)
		{
			// alternative load method
			//icon = new Image();
			//var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			//using (var stream = assembly.GetManifestResourceStream(iconFilename))
			//{
			//	icon.Source = new Bitmap(stream);
			//}

			using (var stream = AvaloniaLocator.Current.GetService<IAssetLoader>().Open(new Uri(filename)))
			{
				return new Bitmap(stream);
			}
		}

		public static void Init()
		{
			iconNew = LoadBitmap("resm:GitItGUI.Icons.new.png");
			iconRenamed = LoadBitmap("resm:GitItGUI.Icons.renamed.png");
			iconTypeChanged = LoadBitmap("resm:GitItGUI.Icons.typeChanged.png");
			iconModified = LoadBitmap("resm:GitItGUI.Icons.modified.png");
			iconDeleted = LoadBitmap("resm:GitItGUI.Icons.deleted.png");
			iconConflicted = LoadBitmap("resm:GitItGUI.Icons.conflicted.png");
		}

		public static Bitmap GetResource(FileStates state)
		{
			if (FileState.IsAnyStates(state, new FileStates[]{FileStates.NewInIndex, FileStates.NewInWorkdir})) return iconNew;
			if (FileState.IsAnyStates(state, new FileStates[]{FileStates.DeletedFromIndex, FileStates.DeletedFromWorkdir})) return iconDeleted;
			if (FileState.IsAnyStates(state, new FileStates[]{FileStates.ModifiedInIndex, FileStates.ModifiedInWorkdir})) return iconModified;
			if (FileState.IsAnyStates(state, new FileStates[]{FileStates.RenamedInIndex, FileStates.RenamedInWorkdir})) return iconRenamed;
			if (FileState.IsAnyStates(state, new FileStates[]{FileStates.TypeChangeInIndex, FileStates.TypeChangeInWorkdir})) return iconTypeChanged;
			if (FileState.IsAnyStates(state, new FileStates[]{FileStates.Conflicted})) return iconConflicted;

			throw new Exception("Unsuported state: " + state);
		}
	}
}
