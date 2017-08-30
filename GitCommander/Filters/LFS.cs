namespace GitCommander
{
	public static partial class Repository
	{
		public static class LFS
		{
			private static bool SimpleLFSInvoke(string args, StdCallbackMethod stdCallback = null, StdCallbackMethod stdErrorCallback = null)
			{
				var result = Tools.RunExe("git", "lfs " + args, stdCallback: stdCallback, stdErrorCallback: stdErrorCallback);
				lastResult = result.output;
				lastError = result.errors;

				return string.IsNullOrEmpty(lastError);
			}

			public static bool Install()
			{
				return SimpleLFSInvoke("install");
			}

			public static bool Uninstall()
			{
				return SimpleLFSInvoke("uninstall");
			}

			public static bool Track(string ext)
			{
				return SimpleLFSInvoke(string.Format("track \"*{0}\"", ext));
			}

			public static bool Untrack(string ext)
			{
				return SimpleLFSInvoke(string.Format("untrack \"*{0}\"", ext));
			}

			public static bool GetVersion(out string version)
			{
				bool result = SimpleLFSInvoke("version");
				version = lastResult;
				return result;
			}
		}
	}
}
