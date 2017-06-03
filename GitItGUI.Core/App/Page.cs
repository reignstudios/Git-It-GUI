using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Core.App
{
	public abstract class Page
	{
		public delegate void OnEventCallbackMethod(Page page);
		public static event OnEventCallbackMethod OnLoadCallback, OnUnloadCallback;

		internal virtual void OnLoad()
		{
			if (OnLoadCallback != null) OnLoadCallback(this);
		}

		internal virtual void OnUnload()
		{
			if (OnUnloadCallback != null) OnUnloadCallback(this);
		}
	}
}
