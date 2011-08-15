using System;
using System.Linq;

using Mono.Cxxi;

namespace Templates {
	public static class CSharpLanguage {

		// from https://github.com/mono/mono/blob/master/mcs/class/System/Microsoft.CSharp/CSharpCodeGenerator.cs
		private static string[] keywords = new string[] {
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

		public static string SafeIdentifier (string proposedName)
		{
			return keywords.Contains (proposedName)? "@" + proposedName : proposedName;
		}

		public static string TypeName (string str, Context context)
		{
			switch (str) {
			case "System.Void":    return "void";
			case "System.Boolean": return "bool";
			case "System.Byte":    return "byte";
			case "System.SByte":   return "sbyte";
			case "System.Char":    return "char";
			case "System.Int16":   return "short";
			case "System.UInt16":  return "ushort";
			case "System.Decimal": return "decimal";
			case "System.Single":  return "float";
			case "System.Double":  return "double";
			case "System.Int32":   return "int";
			case "System.UInt32":  return "uint";
			case "System.Int64":   return "long";
			case "System.UInt64":  return "ulong";
			case "System.Object":  return "object";
			case "System.String":  return "string";
			}

			if (str.EndsWith ("&")) {
				if (context.Is (Context.Parameter))
					return "ref " + TypeName (str.TrimEnd ('&'), context);
				if (context.Is (Context.Return))
					return (context.Is (Context.Interface)? "[return: ByRef] " : "") + TypeName (str.TrimEnd ('&'), context);
				if (context.Is (Context.Generic))
					return "IntPtr";
			}

			// we are using System by default
			var lastDot = str.LastIndexOf ('.');
			if (str.StartsWith ("System") && lastDot == "System".Length)
				return str.Substring (lastDot + 1);
		
			return str;
		}


	}
}

