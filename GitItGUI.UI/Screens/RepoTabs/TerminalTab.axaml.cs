using GitItGUI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Interactivity;

namespace GitItGUI.UI.Screens.RepoTabs
{
    /// <summary>
    /// Interaction logic for TerminalTab.xaml
    /// </summary>
    public partial class TerminalTab : UserControl
    {
		private bool refreshPending;

        public TerminalTab()
        {
            InitializeComponent();
			DebugLog.WriteCallback += DebugLog_WriteCallback;
			cmdTextBox.KeyDown += CmdTextBox_KeyDown;
        }

		public void ScrollToEnd()
		{
			terminalTextBox.ScrollToEnd();
		}

		public void CheckRefreshPending()
		{
			if (!refreshPending) return;
			refreshPending = false;
			RepoScreen.singleton.Refresh();
		}

		public void Refresh()
		{
			const int maxLength = 60000;
			string text = terminalTextBox.Text;
			if (text.Length > maxLength) terminalTextBox.Text = text.Remove(0, text.Length - maxLength);
			ScrollToEnd();
		}
		
		private void DebugLog_WriteCallback(string value)
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				terminalTextBox.Text += value + Environment.NewLine;
			}
			else
			{
				Dispatcher.UIThread.InvokeAsync(delegate()
				{
					terminalTextBox.Text += value + Environment.NewLine;
				});
			}
		}

		private void CmdTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Return) runCmdButton_Click(null, null);
		}

		private void runCmdButton_Click(object sender, RoutedEventArgs e)
		{
			refreshPending = true;
			string cmd = cmdTextBox.Text;
			cmdTextBox.Text = string.Empty;
			RepoScreen.singleton.repoManager.dispatcher.InvokeAsync(delegate()
			{
				RepoScreen.singleton.repoManager.repository.RunGenericCmd(cmd);
				Dispatcher.UIThread.InvokeAsync(delegate()
				{
					ScrollToEnd();
				});
			});
		}
	}
}
