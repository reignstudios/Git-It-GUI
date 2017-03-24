using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommander
{
	public class RemoteState
	{
		public string name {get; internal set;}
		public string url {get; internal set;}

		public RemoteState() {}
		public RemoteState(string name, string url)
		{
			this.name = name;
			this.url = url;
		}

		public override string ToString()
		{
			return name;
		}
	}

	public static partial class Repository
	{
		public static bool GetRemoteURL(string remote, out string url)
		{
			var result = Tools.RunExe("git", string.Format("config --get remote.{0}.url", remote));
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			if (!string.IsNullOrEmpty(lastError) || string.IsNullOrEmpty(lastResult))
			{
				url = null;
				return false;
			}

			url = lastResult;
			return true;
		}

		public static bool GetRemoteStates(out RemoteState[] remoteStates)
		{
			var states = new List<RemoteState>();
			void stdCallback(string line)
			{
				var remote = new RemoteState() {name = line};
				states.Add(remote);
			}
			
			var result = Tools.RunExe("git", "remote show", stdCallback:stdCallback);
			lastResult = result.stdResult;
			lastError = result.stdErrorResult;

			if (!string.IsNullOrEmpty(lastError))
			{
				remoteStates = null;
				return false;
			}

			// get remote urls
			foreach (var remote in states)
			{
				if (GetRemoteURL(remote.name, out string url))
				{
					remote.url = url;
				}
				else
				{
					remoteStates = null;
					return false;
				}
			}
			
			remoteStates = states.ToArray();
			return true;
		}
	}
}
