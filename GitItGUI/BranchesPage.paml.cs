using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitItGUI
{
	public class BranchesPage : UserControl
	{
		public static BranchesPage singleton;
		public static Branch activeBranch;

		// ui objects
		ListBox activeBranchListView, otherBranchListView;
		Button addButton, renameButton, mergeButton, deleteButton;

		List<string> activeBranchListViewItems, otherBranchListViewItems;

		public BranchesPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			activeBranchListViewItems = new List<string>();
			otherBranchListViewItems = new List<string>();
			activeBranchListView.Items = activeBranchListViewItems;
			otherBranchListView.Items = otherBranchListViewItems;
			MainWindow.UpdateUICallback += UpdateUI;
		}

		private void UpdateUI()
		{
			// clear ui
			activeBranchListViewItems.Clear();
			otherBranchListViewItems.Clear();
			
			// check if repo exists
			if (RepoPage.repo == null) return;

			// fill ui
			try
			{
				var branches = RepoPage.repo.Branches;
				foreach (var branch in branches)
				{
					// make sure we don't show remotes that match locals
					if (branch.IsRemote)
					{
						if (branch.FriendlyName == "origin/HEAD" || branch.FriendlyName == "origin/master") continue;

						bool found = false;
						foreach (var otherBranch in branches)
						{
							if (branch.FriendlyName == otherBranch.FriendlyName) continue;
							if (branch.FriendlyName.Replace("origin/", "") == otherBranch.FriendlyName)
							{
								found = true;
								break;
							}
						}

						if (found) continue;
					}

					// add branch to list
					activeBranchListViewItems.Add(branch.FriendlyName);
					if (!branch.IsRemote) otherBranchListViewItems.Add(branch.FriendlyName);
					if (branch.IsCurrentRepositoryHead)
					{
						//activeBranchListView.SelectedIndex = i;// fix this
						activeBranch = branch;
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("Refresh Branches Error: " + e.Message);
			}
			
			activeBranchComboBox_SelectionChanged(null, null);
		}

		private void activeBranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			try
			{
				string name = activeBranchListView.SelectedItem as string;

				// see if the user wants to add a remote to local
				if (name.Contains("origin/"))
				{
					name = name.Replace("origin/", "");
					if (MessageBox.Show("This branch is remote.\nWould you like to pull this branch locally?", "Alert", MessageBoxTypes.YesNo))
					{
						var branch = RepoPage.repo.CreateBranch(name);
						RepoPage.repo.Branches.Update(branch, b =>
						{
							b.Remote = "origin";
							b.UpstreamBranch = branch.CanonicalName;
						});

						//activeBranchListViewItems.Add(branchNameTextBox.Text);// needs a popup window to enter text
						RepoPage.repo.Checkout(branch);

						var options = new PullOptions();
						options.FetchOptions = new FetchOptions();
						options.FetchOptions.CredentialsProvider = (_url, _user, _cred) => RepoPage.credentials;
						options.FetchOptions.TagFetchMode = TagFetchMode.All;
						RepoPage.repo.Network.Pull(RepoPage.signature, options);
					}
					else
					{
						int i = 0;
						foreach (string item in activeBranchListView.Items)
						{
							if (item == activeBranch.FriendlyName)
							{
								activeBranchListView.SelectedIndex = i;
								return;
							}

							++i;
						}
					}
				}

				// change branch
				var selectedBranch = RepoPage.repo.Branches[name];
				if (activeBranch != selectedBranch)
				{
					var newBranch = RepoPage.repo.Checkout(selectedBranch);
					if (newBranch != selectedBranch) MessageBox.Show("Error checking out branch (do you have pending changes?)");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Checkout Branch Error: " + ex.Message);
			}
			
			RepoPage.Refresh();
		}

		private void mergeButton_Click(object sender, RoutedEventArgs e)
		{
			if (activeBranchListView.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Active' branch\n(If none exists commit something first)");
				return;
			}

			if (otherBranchListView.SelectedIndex < 0)
			{
				MessageBox.Show("Must select 'Other' branch");
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to merge '{0}' with '{1}'", otherBranchListView.SelectedItem, activeBranchListView.SelectedItem), "Warning", MessageBoxTypes.YesNo))
			{
				return;
			}

			try
			{
				var srcBround = RepoPage.repo.Branches[otherBranchListView.SelectedItem as string];
				RepoPage.repo.Merge(srcBround, RepoPage.signature);
				MessageBox.Show("Merge Succeeded!");
			}
			catch (Exception ex)
			{
				MessageBox.Show("Merge Branch Error: " + ex.Message);
			}
			
			ChangesPage.ResolveConflicts();
		}

		private void addNewBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (RepoPage.repo.Branches.Count() == 0)
			{
				MessageBox.Show("You must commit files to master before create new branches!");
				return;
			}

			if (string.IsNullOrEmpty(NameWindow.name))
			{
				MessageBox.Show("Must give the branch a name");
				return;
			}

			if (!Tools.IsSingleWord(NameWindow.name))
			{
				MessageBox.Show("No white space or special characters allowed");
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to add '{0}'", NameWindow.name), "Warning", MessageBoxTypes.YesNo))
			{
				return;
			}

			// see if the user wants to add remote info
			bool addRemoteInfo = false;
			if (MessageBox.Show("Will this branch be pushed to a remote server?", "Remote Tracking?", MessageBoxTypes.YesNo))
			{
				addRemoteInfo = true;
			}

			try
			{
				var branch = RepoPage.repo.CreateBranch(NameWindow.name);
				if (addRemoteInfo)
				{
					RepoPage.repo.Branches.Update(branch, b =>
					{
						b.Remote = "origin";
						b.UpstreamBranch = branch.CanonicalName;
					});
				}

				activeBranchListViewItems.Add(NameWindow.name);
				
				RepoPage.repo.Checkout(branch);
				activeBranch = branch;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Add new Branch Error: " + ex.Message);
				return;
			}

			if (addRemoteInfo) ChangesPage.PushNewBranch();
			RepoPage.Refresh();
		}

		private void deleteBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (otherBranchListView.SelectedItem == null)
			{
				MessageBox.Show("Must select branch");
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to delete '{0}'", otherBranchListView.SelectedItem as string), "Warning", MessageBoxTypes.YesNo))
			{
				return;
			}

			try
			{
				RepoPage.repo.Branches.Remove(otherBranchListView.SelectedItem as string);
				var remoteBranch = RepoPage.repo.Branches["origin/" + (otherBranchListView.SelectedItem as string)];
				if (remoteBranch != null)
				{
					RepoPage.repo.Branches.Remove(remoteBranch);
					Tools.RunExe("git", string.Format("push origin --delete {0}", otherBranchListView.SelectedItem as string), null);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Delete new Branch Error: " + ex.Message);
			}

			RepoPage.Refresh();
		}

		private void renameBranchButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(NameWindow.name))
			{
				MessageBox.Show("Must give the branch a name");
				return;
			}

			if (!Tools.IsSingleWord(NameWindow.name))
			{
				MessageBox.Show("No white space or special characters allowed");
				return;
			}

			if (NameWindow.name == "origin" || NameWindow.name == "master" || NameWindow.name == "HEAD")
			{
				MessageBox.Show("Cannot name branch: (origin, master or HEAD)");
				return;
			}

			if (!MessageBox.Show(string.Format("Are you sure you want to rename '{0}' to '{1}'", activeBranchListView.SelectedItem as string, NameWindow.name), "Warning", MessageBoxTypes.YesNo))
			{
				return;
			}

			try
			{
				var branch = RepoPage.repo.Branches.Rename(activeBranchListView.SelectedItem as string, NameWindow.name);
				RepoPage.repo.Branches.Update(branch, b =>
				{
					b.Remote = "origin";
					b.UpstreamBranch = branch.CanonicalName;
				});
				activeBranchListViewItems.Add(NameWindow.name);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Rename new Branch Error: " + ex.Message);
			}

			RepoPage.Refresh();
		}
	}
}
