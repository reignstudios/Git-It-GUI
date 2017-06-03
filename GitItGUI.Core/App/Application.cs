using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitItGUI.Core.App
{
	public static class Application
	{
		public delegate void InitErrorCallbackMethod(string error);
		public static event InitErrorCallbackMethod InitCallback;

		public delegate void TaskCallbackMethod();
		private static List<TaskCallbackMethod> tasks;
		public static event TaskCallbackMethod workerThreadTask;
		
		private static Thread thread;
		private static volatile bool threadRunning;

		private static Page lastPage;
		public static StartPage startPage {get; private set;}
		public static RepoPage repoPage {get; private set;}

		public static void Init()
		{
			// create pages
			startPage = new StartPage();
			repoPage = new RepoPage();

			// start main loop
			tasks = new List<TaskCallbackMethod>();
			thread = new Thread(WorkerThread);
			thread.Start();
		}

		public static void Dispose()
		{
			threadRunning = false;
			if (thread != null) thread.Join();
		}

		private static void WorkerThread()
		{
			threadRunning = true;

			// init
			if (!AppManager.Init())
			{
				if (InitCallback != null) InitCallback("AppManager failed");
				threadRunning = false;
				return;
			}

			// load start page
			LoadPage(startPage);

			// main loop
			while (threadRunning)
			{
				// invoke dispatcher tasks
				lock (tasks)
				{
					foreach (var task in tasks)
					{
						task();
					}

					tasks.Clear();
				}

				// finish
				if (workerThreadTask != null) workerThreadTask();
				Thread.Sleep(100);
			}
		}

		public static void Dispatch(TaskCallbackMethod task)
		{
			lock (tasks)
			{
				tasks.Add(task);
			}
		}

		internal static void LoadPage(Page page)
		{
			if (lastPage != null) lastPage.OnUnload();
			if (page != null) page.OnLoad();
			lastPage = page;
		}
	}
}
