using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Win = System.Windows.Forms;

namespace GitItGUI
{
	enum MessageBoxTypes
	{
		Ok,
		OkCancel,
		YesNo
	}

	static class MessageBox
	{
		public static bool Show(string text)
		{
			return Show("Alert", text, MessageBoxTypes.Ok);
		}

		public static bool Show(string text, MessageBoxTypes type)
		{
			return Show("Alert", text, type);
		}

		public static bool Show(string text, string title, MessageBoxTypes type)
		{
			Win.DialogResult result = Win.DialogResult.None;
			switch (type)
			{
				case MessageBoxTypes.Ok: result = Win.MessageBox.Show(title, text, Win.MessageBoxButtons.OK); break;
				case MessageBoxTypes.OkCancel: result = Win.MessageBox.Show(title, text, Win.MessageBoxButtons.OKCancel); break;
				case MessageBoxTypes.YesNo: result = Win.MessageBox.Show(title, text, Win.MessageBoxButtons.YesNo); break;
			}

			if (result == Win.DialogResult.OK || result == Win.DialogResult.Yes) return true;
			return false;
		}
	}
}
