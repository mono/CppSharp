using System;
using System.Collections.Generic;
using System.IO;

public static class Glob
{
	static public string[] GetFiles(string[] patterns)
	{
		List<string> filelist = new List<string>();
		foreach (string pattern in patterns)
			filelist.AddRange(GetFiles(pattern));
		string[] files = new string[filelist.Count];
		filelist.CopyTo(files, 0);
		return files;
	}

	static public string[] GetFiles(string patternlist)
	{
		List<string> filelist = new List<string>();
		foreach (string pattern in
			patternlist.Split(Path.GetInvalidPathChars()))
		{
			string dir = Path.GetDirectoryName(pattern);
			if (String.IsNullOrEmpty(dir)) dir =
				 Directory.GetCurrentDirectory();
			filelist.AddRange(Directory.GetFiles(
				Path.GetFullPath(dir),
				Path.GetFileName(pattern)));
		}
		string[] files = new string[filelist.Count];
		filelist.CopyTo(files, 0);
		return files;
	}
}