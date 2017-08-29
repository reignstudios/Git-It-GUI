using System.IO;
using System.Text.RegularExpressions;

namespace GitItGUI.Core
{
	public delegate void RunExeCallbackMethod(string stdLine);

	static class Tools
	{
		public static bool IsBinaryFileData(Stream stream, bool disposeStream = false)
		{
			const int maxByteRead = 1024 * 1024 * 8;

			// if the file is to large consider a data file (8mb)
			if (stream.Length > maxByteRead)
			{
				if (disposeStream) stream.Dispose();
				return true;
			}

			// check for \0 characters and if they accure before the end of file, this is a data file
			int b = stream.ReadByte();
			while (b != -1)
			{
				if (b == 0 && stream.Position < maxByteRead)
				{
					if (disposeStream) stream.Dispose();
					return true;
				}

				b = stream.ReadByte();
			}

			if (disposeStream) stream.Dispose();
			return false;
		}

		public static bool IsBinaryFileData(string filename)
		{
			using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
			{
				return IsBinaryFileData(stream);
			}
		}

		public static bool IsGitLFSPtr(string data)
		{
			if (data.Length >= 1024) return false;
			var match = Regex.Match(data, @"version https://git-lfs.github.com/spec/v1.*oid sha256:.*size\s\n*", RegexOptions.Singleline);
			return match.Success;
		}
	}
}
