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

	/// <summary>
	/// A module represents a parsed C++ translation unit.
	/// </summary>
	[DebuggerDisplay("File = {FileName}, Ignored = {Ignore}")]
	public class Module : Namespace
	{
		public Module(string file)
		{
			Macros = new List<MacroDefine>();
			FilePath = file;
			Ignore = false;
		}

		/// Contains the macros present in the unit.
		public List<MacroDefine> Macros;

		/// If the module should be ignored.
		public bool Ignore;
		
		/// Contains the path to the file.
		public string FilePath;

		/// Contains the name of the file.
		public string FileName
		{
			get { return Path.GetFileName(FilePath); }
		}
	}

	/// <summary>
	/// A library contains all the modules.
	/// </summary>
	public class Library
	{
		public string Name;
		public string Native;
		public List<Module> Modules;

		public Library(string name, string native)
		{
			Name = name;
			Native = native;
			Modules = new List<Module>();
		}

		/// Finds an existing module or creates a new one given a file path.
		public Module FindOrCreateModule(string file)
		{
			var module = Modules.Find(m => m.FilePath.Equals(file));

			if (module == null)
			{
				module = new Module(file);
				Modules.Add(module);
			}

			return module;
		}

		/// Finds an existing enum in the library modules.
		public Enumeration FindEnum(string name)
		{
			foreach (var module in Modules)
			{
				var type = module.FindEnum(name);
				if (type != null) return type;
			}

			return null;
		}

		/// Finds an existing struct/class in the library modules.
		public Class FindClass(string name, bool create = false)
		{
			foreach (var module in Modules)
			{
				var type = module.FindClass(name, create);
				if (type != null) return type;
			}

			return null;
		}
	}
}