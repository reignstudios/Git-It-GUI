using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitItGUI.Core.App
{
	public class StartPage : Page
	{
		public delegate void LoadItemCallbackMethod(IReadOnlyList<string> items);
		public event LoadItemCallbackMethod LoadItemCallback;

		private string selectedRepo;
		private List<string> repos;
		
		public void RepoList_SelectItem(string item)
		{
			selectedRepo = item;
		}

		public void Button_OpenSelected()
		{
			if (!RepoManager.OpenRepo(selectedRepo, true))
			{
				// remove bad repo from list
				MessageBox.Show("Failed to open repo: " + selectedRepo);
				AppManager.RemoveRepoFromHistory(selectedRepo);
				return;
			}

			Application.LoadPage(Application.repoPage);
		}

		public void Button_OpenRepoPath(string repoPath)
		{
			if (!RepoManager.OpenRepo(repoPath, true))
			{
				MessageBox.Show("Failed to open repo: " + repoPath);
				return;
			}
			
			Application.LoadPage(Application.repoPage);
		}

		internal override void OnLoad()
		{
			repos = new List<string>();
			foreach (var repoPath in AppManager.repositories)
			{
				repos.Add(repoPath);
			}

			if (LoadItemCallback != null) LoadItemCallback(repos);
			base.OnLoad();
		}
	}
}
