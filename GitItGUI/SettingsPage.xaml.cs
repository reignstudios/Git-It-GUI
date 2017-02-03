using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitItGUI.Core;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace GitItGUI
{
	public class SettingsPage : UserControl
	{
		public static SettingsPage singleton;

		private bool refreshMode;

		// UI objects
		Button applyChangesButton, closeRepoButton;
		TextBox sigNameTextBox, sigEmailTextBox, usernameTextBox, passwordTextBox;
		CheckBox gitlfsSupportCheckBox, validateGitignoreCheckbox;

		public SettingsPage()
		{
			singleton = this;
			AvaloniaXamlLoader.Load(this);

			// load ui items
			applyChangesButton = this.Find<Button>("applyChangesButton");
			closeRepoButton = this.Find<Button>("closeRepoButton");
			sigNameTextBox = this.Find<TextBox>("sigNameTextBox");
			sigEmailTextBox = this.Find<TextBox>("sigEmailTextBox");
			usernameTextBox = this.Find<TextBox>("usernameTextBox");
			passwordTextBox = this.Find<TextBox>("passwordTextBox");
			gitlfsSupportCheckBox = this.Find<CheckBox>("gitlfsSupportCheckBox");
			validateGitignoreCheckbox = this.Find<CheckBox>("validateGitignoreCheckbox");

			// apply bindings
			applyChangesButton.Click += ApplyChangesButton_Click;
			closeRepoButton.Click += CloseRepoButton_Click;
			sigNameTextBox.TextInput += TextInputChanged;
			sigEmailTextBox.TextInput += TextInputChanged;
			usernameTextBox.TextInput += TextInputChanged;
			passwordTextBox.TextInput += TextInputChanged;
			gitlfsSupportCheckBox.Click += CheckboxChanged;
			validateGitignoreCheckbox.Click += CheckboxChanged;

			// set ui defaults
			applyChangesButton.IsVisible = false;

			// bind managers
			RepoManager.RepoRefreshedCallback += RepoManager_RepoRefreshedCallback;
		}

		private void CloseRepoButton_Click(object sender, RoutedEventArgs e)
		{
			RepoManager.Close();
			MainWindow.LoadPage(PageTypes.Start);
		}

		private void RepoManager_RepoRefreshedCallback()
		{
			refreshMode = true;
			sigNameTextBox.Text = RepoManager.signatureName;
			sigEmailTextBox.Text = RepoManager.signatureEmail;
			usernameTextBox.Text = RepoManager.credentialUsername;
			passwordTextBox.Text = RepoManager.credentialPassword;
			gitlfsSupportCheckBox.IsChecked = RepoManager.lfsEnabled;
			validateGitignoreCheckbox.IsChecked = RepoManager.validateGitignoreCheckbox;
			refreshMode = false;
		}
		
		private void TextInputChanged(object sender, TextInputEventArgs e)
		{
			if (!refreshMode) NeedsToApplyChanges();
		}
		
		private void CheckboxChanged(object sender, RoutedEventArgs e)
		{
			if (!refreshMode) NeedsToApplyChanges();
		}

		private void NeedsToApplyChanges()
		{
			applyChangesButton.IsVisible = true;
		}

		private void ApplyChangesButton_Click(object sender, RoutedEventArgs e)
		{
			if (validateGitignoreCheckbox.IsChecked != RepoManager.validateGitignoreCheckbox) RepoManager.UpdateValidateGitignore(validateGitignoreCheckbox.IsChecked);
			if (gitlfsSupportCheckBox.IsChecked != RepoManager.lfsEnabled)
			{
				if (gitlfsSupportCheckBox.IsChecked) RepoManager.AddGitLFSSupport(true);
				else RepoManager.RemoveGitLFSSupport(false);
			}

			RepoManager.UpdateSignatureValues(sigNameTextBox.Text, sigEmailTextBox.Text);
			RepoManager.UpdateCredentialValues(usernameTextBox.Text, passwordTextBox.Text);
			applyChangesButton.IsVisible = false;
			RepoManager.SaveSettings();
			RepoManager.Refresh();
		}
	}
}
