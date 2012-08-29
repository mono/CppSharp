using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Cxxi
{
	public enum CppAbi
	{
		Itanium,
		Microsoft,
		ARM
	}

	public enum InlineMethods
	{
		Present,
		Unavailable
	}

	[DebuggerDisplay("File = {FileName}, Ignored = {Ignore}")]
	public class Module
	{
		public Module(string File)
		{
			Global = new Namespace();
			Macros = new List<MacroDefine>();
			Namespaces = new List<Namespace>();
			FilePath = File;
			Ignore = false;
		}

		public bool Ignore;
		public string FilePath;

		public string FileName
		{
			get { return Path.GetFileName(FilePath); }
		}

		public List<MacroDefine> Macros;

		public Namespace Global;
		public List<Namespace> Namespaces;

		public bool HasDeclarations
		{
			get
			{
				return Global.HasDeclarations
					|| Namespaces.Exists(n => n.HasDeclarations);
			}
		}
	}

	public class Library
	{
		public Library(string name)
		{
			Name = name;
			Modules = new List<Module>();
		}

		public Module FindOrCreateModule(string File)
		{
			var module = Modules.Find(m => m.FilePath.Equals(File));

			if (module == null)
			{
				module = new Module(File);
				Modules.Add(module);
			}

			return module;
		}

		public Enumeration FindEnum(string Name)
		{
			foreach (var module in Modules)
			{
				var type = module.Global.FindEnum(Name);
				if (type != null) return type;
			}

			return null;
		}

		public Class FindClass(string Name)
		{
			foreach (var module in Modules)
			{
				var type = module.Global.FindClass(Name);
				if (type != null) return type;
			}

			return null;
		}

		public string Name { get; set; }
		public List<Module> Modules { get; set; }
	}
}