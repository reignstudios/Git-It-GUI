namespace GitItGUI.Core
{
	public static class VersionInfo
	{
		public const string version = "2.0.0";

		#if DEBUG
		public const string versionType = version + "d";
		#else
		public const string versionType = version + "r";
		#endif
	}
}
