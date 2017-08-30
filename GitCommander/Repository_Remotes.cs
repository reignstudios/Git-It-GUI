using System.Collections.Generic;

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

	public partial class Repository
	{
		public bool GetRemoteURL(string remote, out string url)
		{
			lock (this)
			{
				var result = RunExe("git", string.Format("config --get remote.{0}.url", remote));
				lastResult = result.output;
				lastError = result.errors;

				if (!string.IsNullOrEmpty(lastError) || string.IsNullOrEmpty(lastResult))
				{
					url = null;
					return false;
				}

				url = lastResult;
				return true;
			}
		}

		public bool GetRemoteStates(out RemoteState[] remoteStates)
		{
			var states = new List<RemoteState>();
			void stdCallback(string line)
			{
				var remote = new RemoteState() {name = line};
				states.Add(remote);
			}

			lock (this)
			{
				var result = RunExe("git", "remote show", stdCallback:stdCallback);
				lastResult = result.output;
				lastError = result.errors;

				if (!string.IsNullOrEmpty(lastError))
				{
					remoteStates = null;
					return false;
				}

				// get remote urls
				foreach (var remote in states)
				{
					string url;
					if (GetRemoteURL(remote.name, out url))
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
}
