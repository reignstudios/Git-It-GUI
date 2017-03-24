﻿using Avalonia;
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
			switch (state)
			{
				case FileStates.NewInIndex:
				case FileStates.NewInWorkdir:
					return iconNew;

				case FileStates.DeletedFromIndex:
				case FileStates.DeletedFromWorkdir:
					return iconDeleted;

				case FileStates.ModifiedInIndex:
				case FileStates.ModifiedInWorkdir:
					return iconModified;

				case FileStates.RenamedInIndex:
				case FileStates.RenamedInWorkdir:
					return iconRenamed;

				case FileStates.TypeChangeInIndex:
				case FileStates.TypeChangeInWorkdir:
					return iconTypeChanged;

				case FileStates.Conflicted:
					return iconConflicted;
			}

			throw new Exception("Unsuported state: " + state);
		}
	}
}
