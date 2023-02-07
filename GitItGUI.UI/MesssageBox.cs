using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.UI
{
	enum MessageBoxButton
	{
		OK
	}

	enum MessageBoxImage
	{
		Error
	}

	class MessageBox
	{
		public static void Show(string message)
		{
			Show(null, message);
		}

		public static void Show(Window window, string message)
		{
			// TODO
		}

		public static void Show(Window window, string message, string title, MessageBoxButton button, MessageBoxImage image)
		{
			// TODO
		}
	}
}
