using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cxxi.Templates
{
	public partial class CSharpModule
	{
		public Library Library;
		public Options Options;
		public Module Module;

		readonly string DefaultIndent = "    ";

		// from https://github.com/mono/mono/blob/master/mcs/class/System/Microsoft.CSharp/CSharpCodeGenerator.cs
		private static string[] keywords = new string[]
		{
			"abstract","event","new","struct","as","explicit","null","switch","base","extern",
			"this","false","operator","throw","break","finally","out","true",
			"fixed","override","try","case","params","typeof","catch","for",
			"private","foreach","protected","checked","goto","public",
			"unchecked","class","if","readonly","unsafe","const","implicit","ref",
			"continue","in","return","using","virtual","default",
			"interface","sealed","volatile","delegate","internal","do","is",
			"sizeof","while","lock","stackalloc","else","static","enum",
			"namespace",
			"object","bool","byte","float","uint","char","ulong","ushort",
			"decimal","int","sbyte","short","double","long","string","void",
			"partial", "yield", "where"
		};

		public static string SafeIdentifier(string proposedName)
		{
			proposedName = new string(((IEnumerable<char>)proposedName).Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
			return keywords.Contains(proposedName) ? "@" + proposedName : proposedName;
		}
	}

	public static class IntHelpers
	{
		public static bool IsPowerOfTwo(this long x)
		{
			return (x != 0) && ((x & (x - 1)) == 0);
		}
	}

	public static class StringHelpers
	{
		public static string CommonPrefix(this string[] ss)
		{
			if (ss.Length == 0)
			{
				return "";
			}

			if (ss.Length == 1)
			{
				return ss[0];
			}

			int prefixLength = 0;

			foreach (char c in ss[0])
			{
				foreach (string s in ss)
				{
					if (s.Length <= prefixLength || s[prefixLength] != c)
					{
						return ss[0].Substring(0, prefixLength);
					}
				}
				prefixLength++;
			}

			return ss[0]; // all strings identical
		}
	}
}