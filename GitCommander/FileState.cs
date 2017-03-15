using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommander
{
	public enum FileStates
	{
		Unknown,
		ModifiedInWorkdir,
		ModifiedInIndex,
		NewInWorkdir,
		NewInIndex,
		DeletedFromWorkdir,
		DeletedFromIndex,
		RenamedInWorkdir,
		RenamedInIndex,
		TypeChangeInWorkdir,
		TypeChangeInIndex,
		Conflicted
	}

	public class FileState
	{
		public string filePath;
		public FileStates state;
	}
}
