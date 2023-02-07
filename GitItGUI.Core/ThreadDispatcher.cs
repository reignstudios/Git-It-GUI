using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Core
{
	public class ThreadDispatcher
	{
		private Thread thread;
		private bool running;

		public ThreadDispatcher()
		{
			thread = Thread.CurrentThread;
		}

		public void Run()
		{
			running = true;
			while (running)
			{
				Thread.Sleep(1);
			}
		}

		public void Stop()
		{
			running = false;
		}

		public bool CheckAccess()
		{
			return true;// TODO
		}

		public delegate void InvokeCallback();
		public void InvokeAsync(InvokeCallback callback)
		{
			// TODO
		}
	}
}
