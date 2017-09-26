namespace GitCommander
{
	public partial class Repository
	{
		public class LFS
		{
			private Repository repository;

			public LFS(Repository repository)
			{
				this.repository = repository;
			}

			private bool SimpleLFSInvoke(string args, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
			{
				var result = repository.RunExe("git", "lfs " + args, stdCallback: stdCallback, stdErrorCallback: stdErrorCallback);
				repository.lastResult = result.output;
				repository.lastError = result.errors;

				return string.IsNullOrEmpty(repository.lastError);
			}

			public bool Install()
			{
				lock (repository)
				{
					return SimpleLFSInvoke("install");
				}
			}

			public bool Uninstall()
			{
				lock (repository)
				{
					return SimpleLFSInvoke("uninstall");
				}
			}

			public bool Track(string ext)
			{
				lock (repository)
				{
					return SimpleLFSInvoke(string.Format("track \"*{0}\"", ext));
				}
			}

			public bool Untrack(string ext)
			{
				lock (repository)
				{
					return SimpleLFSInvoke(string.Format("untrack \"*{0}\"", ext));
				}
			}

			public bool GetVersion(out string version)
			{
				lock (repository)
				{
					bool result = SimpleLFSInvoke("version");
					version = repository.lastResult;
					return result;
				}
			}
		}
	}
}
