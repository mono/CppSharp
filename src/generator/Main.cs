using System;
using NDesk.Options;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;
using Mono.VisualC.Interop.Util;
using Mono.VisualC.Code;
using Mono.VisualC.Code.Atoms;

using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace Mono.VisualC.Tools.Generator {
	public class Generator {
		public static readonly string [] genericTypeArgs = new string [] { "T", "U", "V", "W", "X", "Y", "Z" };

		public string Source { get; set; }
		public string Dir { get; set; }
		public bool AbiTest { get; set; }
		public bool Validation { get; set; }
		public string Library { get; set; }

		private string nspace;
		public string Namespace {
			get { return nspace; }
			set {
				nspace = value;
				Tree.ManagedNamespace = value;
				//enumerations.ManagedNamespace = value;
				//unions.ManagedNamespace = value;
			}
		}

		public Dictionary<string, string> Classes;
		public HashSet<string> UnknownTypes;

		public CodeDomProvider Provider { get; set; }
		public CodeGeneratorOptions Options { get; set; }

		private CodeUnit Tree { get; set; }
		//private CodeUnit currentUnit;
		//private CodeUnit enumerations;
		//private CodeUnit unions;
		private Dictionary<string, Property> properties;

		private int enumCount = 0;
		private int unionCount = 0;

		private HashSet<string> fileList;

		public static void Main (string [] args)
		{
			bool help = false;
			Generator gen = new Generator ();
			gen.Validation = true;
			var p = new OptionSet { { "h|?|help", "Show this help message", v => help = v != null }, { "f=", "Set the xml outputted by gccxml to parse", v => gen.Source = v }, { "o=", "Set the output directory", v => gen.Dir = v }, { "ns=", "Set the namespace", v => gen.Namespace = v }, { "lib=", "Additional reference libraries", v => gen.Library = v }, { "lang=", "Language of the outputted library (C#, VB, etc)", v => gen.Provider = CodeDomProvider.CreateProvider (v) }, { "testabi", "Test the Itanium name mangling abi against the bindings", v => gen.AbiTest = v != null }, { "n", "Don't validate", v => gen.Validation = !(v != null) } };

			List<string> extra = null;
			try {
				extra = p.Parse (args);
			} catch (OptionException) {
				Console.WriteLine ("Try `generator --help' for more information.");
				return;
			}

			if (help) {
				p.WriteOptionDescriptions (Console.Error);
				return;
			}

			if (gen.Source == null) {
				Console.Error.WriteLine ("-f required");
				return;
			}

			if (gen.Dir == null)
				gen.Dir = "output";
			Directory.CreateDirectory (gen.Dir);

			if (gen.Namespace == null)
				gen.Namespace = "Wrappers";

			if (gen.Library == null)
				gen.Library = "Lib";
			if (gen.Library.EndsWith (".dll") || gen.Library.EndsWith (".so") || gen.Library.EndsWith (".dylib"))
				gen.Library = gen.Library.Substring (0, gen.Library.LastIndexOf ('.'));

			if (gen.Provider == null)
				gen.Provider = new CSharpCodeProvider ();

			gen.Options = new CodeGeneratorOptions { BlankLinesBetweenMembers = false };

			gen.Run ();
		}

		public Generator ()
		{
			Classes = new Dictionary<string, string> ();
			UnknownTypes = new HashSet<string> ();

			Tree = new CodeUnit { ManagedNamespace = Namespace };
			//enumerations = new CodeUnit { ManagedNamespace = Namespace };
			//unions = new CodeUnit { ManagedNamespace = Namespace };
			properties = new Dictionary<string, Property> ();

			fileList = new HashSet<string> ();
		}

		public void Run ()
		{
			Console.WriteLine ("Generating bindings...");

			Console.WriteLine ("\tLoading XML");
			XmlDocument xmldoc = new XmlDocument ();
			xmldoc.Load (Source);

			Console.WriteLine ("\tPreprocessing");
			List<Entry> entries = Preprocess (xmldoc);

			Console.WriteLine ("\tPreprocessing Classes");
			List<Entry> removed = PreprocessClasses (entries);

			Console.WriteLine ("\tProcessing Classes");
			ProcessClasses (entries, removed);

			// save files
			Console.WriteLine ("\tSaving Classes");
			Save ();

			Console.WriteLine ("\tGenerating Static lib");
			GenerateStaticLibField ();

			Console.WriteLine ("Generating unknown types");
			GenerateUnknownTypeStubs ();


			if (Validation) {
				Console.WriteLine ("Validating bindings...");
				Assembly asm = Validate ();

				if (asm != null && AbiTest) {
					Console.WriteLine ("Testing Itanium ABI name mangling against bindings...");
					TestAbi (asm);
				}
			}
			//File.Delete (fileList.Where (f => f.StartsWith ("UnknownTypes")).Single ());

		}



		public void Save ()
		{
			//if (enumerations.Atoms.Any ())
			//	SaveFile (enumerations.WrapperToCodeDom (Provider), "Enums");
			//if (unions.Atoms.Any ())
			//	SaveFile (unions.WrapperToCodeDom (Provider), "Unions");

			SaveFile (Tree.WrapperToCodeDom (Provider), "FlatFile");
		}


		/***************************************************************************/


		List<Entry> Preprocess (XmlDocument xmldoc)
		{
			XmlNode node = xmldoc.SelectSingleNode ("/GCC_XML");
			Entry.typelist = new Dictionary<string, Dictionary<string, Entry>> ();
			Entry.idlist = new Dictionary<string, Entry> ();
			return Preprocess (node);
		}

		int typecount = 0;
		List<Entry> Preprocess (XmlNode parent)
		{
			List<Entry> data = new List<Entry> ();
			foreach (XmlNode node in parent.ChildNodes) {
				Dictionary<string, string> entry = new Dictionary<string, string> ();
				foreach (XmlAttribute att in node.Attributes) {
					entry[att.Name] = att.Value;
				}

				Entry e = new Entry { id = "", type = node.Name, name = "", reftype = "", attributes = entry };

				if (entry.ContainsKey ("name"))
					e.id = e.name = entry["name"];

				if (entry.ContainsKey ("id"))
					e.id = entry["id"];

				if (entry.ContainsKey ("type"))
					e.reftype = entry["type"];

				if (node.HasChildNodes)
					e.children = Preprocess (node);
				else
					e.children = new List<Entry> ();

				if (!Entry.typelist.ContainsKey (e.type))
					Entry.typelist[e.type] = new Dictionary<string, Entry> ();

				if (entry.ContainsKey ("id"))
					Entry.idlist.Add (e.id, e);

				string id = e.id;
				int count = 0;
				while (Entry.typelist[e.type].ContainsKey (id)) {
					id = e.id + "|" + count++;
				}
				Entry.typelist[e.type].Add (id, e);
				data.Add (e);
			}
			return data;
		}

		List<Entry> PreprocessClasses (List<Entry> entries)
		{
			List<string> filter = new List<string>() {
				"QItemSelection"
			};


			List<Entry> removed = new List<Entry> (entries.Where (o => (o.type == "Class" || o.type == "Struct") && (o.IsTrue ("incomplete") || !o.HasValue ("name") || (Entry.idlist[o["file"]].name.StartsWith ("/")))));
			removed.AddRange (entries.Where (o => filter.Contains (o.name)).ToList ());
			entries.RemoveAll (o => (o.type == "Class" || o.type == "Struct") && (o.IsTrue ("incomplete") || !o.HasValue ("name") || (Entry.idlist[o["file"]].name.StartsWith ("/"))));
			entries.RemoveAll (o => filter.Contains (o.name));


			foreach (Entry clas in entries.Where (o => o.type == "Class" || o.type == "Struct")) {
				clas.computedName = clas.name;

				string ns = GetNamespace (clas);
				CppType currentType = new CppType (clas.type.ToLower (), ns != null ? ns + "::" + clas.computedName : clas.computedName);

				// FIXME: better way to do this (GCC-XML output doesn't seem to leave much choice)
				var templated = currentType.Modifiers.OfType<CppModifiers.TemplateModifier> ().SingleOrDefault ();
				if (templated != null) {
					clas.isTemplate = true;
					string baseName = currentType.ElementTypeName;
					string computedName = clas.computedName;
					clas.computedName = baseName;

					clas.atom = new Class (clas.computedName) {
						StaticCppLibrary = string.Format ("{0}.Libs.{1}", Namespace, Library),
						OriginalType = currentType
					};

					int i = 0;
					foreach (var t in templated.Types)
						clas.Class.TemplateArguments.Add (genericTypeArgs[i++]);

					clas.Class.GenericName = String.Format ("{0}<{1}>", baseName, string.Join (",", clas.Class.TemplateArguments.ToArray ()));
					Console.Error.WriteLine ("Warning: Creating generic type {0}<{1}> from the instantiated template {2} (very buggy!!!)",
					                         baseName,
					                         string.Join (",", clas.Class.TemplateArguments.ToArray ()), computedName);
				} else {
					clas.atom = new Class (clas.computedName) {
						StaticCppLibrary = string.Format ("{0}.Libs.{1}", Namespace, Library),
						OriginalType = currentType
					};
				}

				// FIXME: Handle when base class name is fully qualified
				foreach (Entry bs in clas.children.Where (o => o.type == "Base")) {
					clas.Class.Bases.Add (new Class.BaseClass { Name = bs.Base.Class != null ? (bs.Base.Class.GenericName ?? bs.Base.Class.Name) : bs.computedName, Access = bs.CheckValue ("access", "public") ? Access.Public : bs.CheckValue ("access", "protected") ? Access.Protected : Access.Private, IsVirtual = bs.IsTrue ("virtual") });
				}

				IEnumerable<CodeAtom> nested = null;
				if (IsCreated (clas.Class.OriginalType, true, out nested))
					continue;

				GetContainer (clas, Tree).Atoms.AddLast (clas.atom);
				if (nested != null) {
					foreach (var orphan in nested)
						clas.Class.Atoms.AddLast (orphan);
				}
			}

			return removed;
		}

		void ProcessClasses (List<Entry> entries, List<Entry> removed)
		{
			foreach (Entry clas in entries.Where (o => o.type == "Class" || o.type == "Struct")) {

				var templated = clas.Class.OriginalType.Modifiers.OfType<CppModifiers.TemplateModifier> ().SingleOrDefault ();
				Dictionary<MethodSignature, string> methods = new Dictionary<MethodSignature, string> ();

				foreach (Entry member in clas.Members) {
					bool ctor = false;
					bool dtor = false;
					int fieldCount = 0;

					switch (member.type) {
					case "Constructor":
						ctor = true;
						break;
					case "Destructor":
						dtor = true;
						break;
					case "Method":
						break;
					case "Field":
						CppType fieldType = findType (clas.Class, member.Base, removed);
						string fieldName = "field" + fieldCount++;
						if (member.name != "")
							fieldName = member.name;
						clas.Class.Atoms.AddLast (new Field (fieldName, fieldType));
						continue;
					default:
						continue;
					}

					// Now we're processing a method...

					if (!member.CheckValue ("access", "public") || (member.HasValue ("overrides") && !dtor) || !member.IsTrue ("extern"))
						continue;

					string mname = member.name;

					CppType retType = member.HasValue ("returns") ? findType (clas.Class, Entry.idlist[member["returns"]], removed) : CppTypes.Void;

					if (templated != null) {
						for (int i = 0; i < templated.Types.Length; i++)
							retType = replaceType (retType, templated.Types[i], genericTypeArgs[i]);
					}

					var methodAtom = new Method (dtor ? "Destruct" : mname) {
						RetType = retType,
						IsVirtual = member.IsTrue ("virtual"),
						IsStatic = member.IsTrue ("static"),
						IsConst = member.IsTrue ("const"),
						IsConstructor = ctor,
						IsDestructor = dtor,
						Klass = clas.Class
					};

					if (AbiTest)
						methodAtom.Mangled = new NameTypePair<Type> { Name = member.attributes["mangled"], Type = typeof(ItaniumAbi) };

					List<CppType> argTypes = new List<CppType> ();
					int c = 0;
					foreach (Entry arg in member.children.Where (o => o.type == "Argument")) {
						string argname;
						if (arg.name == "")
							argname = "arg" + c;
						else
							argname = arg.name;

						CppType argtype = findType (clas.Class, arg.Base, removed);
						var templat = argtype.Modifiers.OfType<CppModifiers.TemplateModifier> ().SingleOrDefault ();
						if (templated != null) {
							for (int i = 0; i < templated.Types.Length; i++)
								argtype = replaceType (argtype, templated.Types[i], genericTypeArgs[i]);
						}

						methodAtom.Parameters.Add (new NameTypePair<CppType> { Name = argname, Type = argtype });
						argTypes.Add (argtype);

						// tee hee
						c++;
					}


					// Try to filter out duplicate methods
					MethodSignature sig = new MethodSignature (methodAtom.FormattedName, argTypes);
					string conflictingSig;
					if (methods.TryGetValue (sig, out conflictingSig)) {
						// FIXME: add comment to explain why it's commented out
						string demangled = member.attributes["demangled"];
						//Console.Error.WriteLine ("Warning: Method \"{0}\" in class {1} omitted because it conflicts with \"{2}\"", demangled, name, conflictingSig);
						methodAtom.Comment = string.Format ("FIXME:Method \"{0}\" omitted because it conflicts with \"{1}\"", demangled, conflictingSig);
						methodAtom.CommentedOut = true;
					} else
						methods.Add (sig, member.attributes["demangled"]);

					// Record unknown types in parameters and return type
					IsCreated (methodAtom.RetType);
					foreach (var arg in methodAtom.Parameters)
						IsCreated (arg.Type);

					// Detect if this is a property...

					// if it's const, returns a value, has no parameters, and there is no other method with the same name
					//  in this class assume it's a property getter (for now?)
					if (methodAtom.IsConst && !retType.Equals (CppTypes.Void) && !methodAtom.Parameters.Any () && clas.Members.Where (o => o.name == mname).FirstOrDefault () == null) {
						var pname = methodAtom.FormattedName;
						Property propertyAtom;

						if (properties.TryGetValue (pname, out propertyAtom)) {
							propertyAtom.Getter = methodAtom;
						} else {
							propertyAtom = new Property (pname) { Getter = methodAtom };
							properties.Add (pname, propertyAtom);
							clas.Class.Atoms.AddLast (propertyAtom);
						}

						continue;
					}

					// if it's name starts with "set", does not return a value, and has one arg (besides this ptr)
					// and there is no other method with the same name...
					if (mname.ToLower ().StartsWith ("set") && retType.Equals (CppTypes.Void) && methodAtom.Parameters.Count == 1 && clas.Members.Where (o => o.name == mname).FirstOrDefault () == null) {

						string getterName = mname.Substring (3).TrimStart ('_').ToLower ();

						string pname = methodAtom.FormattedName.Substring (3);
						Property propertyAtom = null;

						// ...AND there is a corresponding getter method that returns the right type, then assume it's a property setter
						bool doIt = false;
						if (properties.TryGetValue (pname, out propertyAtom)) {
							doIt = propertyAtom.Getter != null && propertyAtom.Getter.RetType.Equals (methodAtom.Parameters[0].Type);
						} else {
							Entry getter = clas.Members.Where (o => o.name == getterName).FirstOrDefault ();
							doIt = (getter != null && findType (clas.Class, Entry.idlist[getter.attributes["returns"]], removed).Equals (methodAtom.Parameters[0].Type));
						}
						if (doIt) {
							if (propertyAtom != null) {
								propertyAtom.Setter = methodAtom;
							} else {
								propertyAtom = new Property (pname) { Setter = methodAtom };
								properties.Add (pname, propertyAtom);
								clas.Class.Atoms.AddLast (propertyAtom);
							}

							// set the method's arg name to "value" so that the prop setter works right
							var valueParam = methodAtom.Parameters[0];
							valueParam.Name = "value";
							methodAtom.Parameters[0] = valueParam;

							continue;
						}
					}
					clas.Class.Atoms.AddLast (methodAtom);
				}
			}
		}

		Assembly Validate ()
		{
			var compileParams = new CompilerParameters { GenerateInMemory = true, IncludeDebugInformation = true, WarningLevel = 0, TreatWarningsAsErrors = false };
			compileParams.ReferencedAssemblies.Add (typeof(CppLibrary).Assembly.Location);

			CompilerResults results = Provider.CompileAssemblyFromFile (compileParams, fileList.ToArray ());
			var errors = results.Errors.Cast<CompilerError> ().Where (e => !e.IsWarning);

			int count = 0;
			foreach (var error in errors) {

				if (count == 0) {
					foreach (var fix in Postfixes.List) {
						if (fix.TryFix (error.ErrorText, this))
							return Validate ();
					}
				}

				Console.Error.WriteLine ("{0}({1},{2}): error {3}: {4}", error.FileName, error.Line, error.Column, error.ErrorNumber, error.ErrorText);
				count++;
			}
			if (count > 0)
				Console.Error.WriteLine ("Validation failed with {0} compilation errors.", count);
			else
				Console.WriteLine ("Bindings compiled successfully.");


			return results.CompiledAssembly;
		}

		void TestAbi (Assembly assembly)
		{
			var classes = assembly.GetExportedTypes ().Select (t => new { Name = Regex.Replace (t.Name, "\\`.$", ""), Interface = t.GetNestedType ("I" + t.Name) }).Where (k => k.Interface != null);
			var abi = new ItaniumAbi ();

			foreach (var klass in classes) {
				bool klassSuccess = true;

				foreach (var method in klass.Interface.GetMethods ()) {

					var testAttribute = (AbiTestAttribute)method.GetCustomAttributes (typeof(AbiTestAttribute), false).FirstOrDefault ();

					string expected = testAttribute.MangledName;
					string actual = abi.GetMangledMethodName (klass.Name, method);

					if (actual != expected) {
						Console.Error.WriteLine ("Error: Expected \"{0}\" but got \"{1}\" when mangling method \"{2}\" in class \"{3}\"", expected, actual, method.ToString (), klass.Name);
						klassSuccess = false;
					}
				}

				if (klassSuccess)
					Console.WriteLine ("Successfully mangled all method names in class \"" + klass.Name + "\"");
			}

		}

		string GetNamespace (Entry entry)
		{
			string nsname;
			if (Entry.idlist[entry["context"]] == null)
				return null;

			Entry ns = entry.Namespace;
			if (ns.type == "Namespace")
				nsname = ns.name; else if (ns.type == "Class" || ns.type == "Struct")
				nsname = new CppType (ns.name).ElementTypeName;
			else
				throw new NotSupportedException ("Unknown context: " + ns.type);

			if (nsname == "::")
				return null;

			string parent = GetNamespace (ns);
			if (parent != null)
				return parent + "::" + nsname;

			return nsname;
		}

		CodeContainer GetContainer (Entry entry, CodeContainer def)
		{
			string nsname;
			if (Entry.idlist[entry.attributes["context"]] == null)
				return null;

			Entry ns = entry.Namespace;

			if (ns.type == "Namespace")
				nsname = ns.name; else if (ns.type == "Class" || ns.type == "Struct")
				nsname = new CppType (ns.name).ElementTypeName;
			else
				throw new NotSupportedException ("Unknown context: " + ns.type);

			if (nsname == "::")
				return def;
			CodeContainer parent = GetContainer (ns, def);
			CodeContainer container = parent.Atoms.OfType<CodeContainer> ().Where (a => a.Name == nsname).SingleOrDefault ();

			if (container == null) {
				container = new Namespace (nsname);
				parent.Atoms.AddLast (container);
			}

			return container;
		}

		public CodeContainer GetPath (string [] pathNames)
		{
			return GetPath (Tree, pathNames, false);
		}

		static CodeContainer GetPath (CodeContainer root, string [] pathNames, bool create)
		{
			foreach (var item in pathNames) {
				CodeContainer prospect = root.Atoms.OfType<CodeContainer> ().Where (a => a.Name == item).SingleOrDefault ();
				if (prospect == null) {
					if (create) {
						prospect = new Namespace (item);
						root.Atoms.AddLast (prospect);
					} else
						return null;
				}

				root = prospect;
			}

			return root;
		}

		CppType ProcessEnum (Entry entry)
		{
			string fullName = entry.name;
			if (entry.name == "") {
				fullName = "Enum" + enumCount++;
				string ns = GetNamespace (entry);
				if (ns != null)
					fullName = ns + "::" + entry.name;
			}
			CppType enumType = new CppType (CppTypes.Enum, fullName);

			IEnumerable<CodeAtom> dontCare;
			if (entry.name == "" || !IsCreated (enumType, true, out dontCare)) {

				Enumeration enumAtom = new Enumeration (fullName);
				foreach (Entry m in entry.children.Where (o => o.type == "EnumValue")) {
					enumAtom.Items.Add (new Enumeration.Item { Name = m.name, Value = Convert.ToInt32 (m.attributes["init"]) });
				}

				CodeContainer nested = GetContainer (entry, Tree);
				//GetContainer (root, enm, currentUnit);
				nested.Atoms.AddLast (enumAtom);
			}

			return enumType;
		}

		CppType ProcessUnion (Class clas, Entry entry, List<Entry> removed)
		{
			string fullName = entry.name;
			if (entry.name == "") {
				fullName = "Union" + unionCount++;
				string ns = GetNamespace (entry);
				if (ns != null)
					fullName = ns + "::" + entry.name;
			}

			CppType unionType = new CppType (CppTypes.Union, fullName);

			IEnumerable<CodeAtom> orphans = null;
			if (entry.name == "" || !IsCreated (unionType, true, out orphans)) {
				Union unionAtom = new Union (fullName);

				foreach (Entry m in entry.Members.Where (o => o.type == "Field")) {
					Field field = new Field (m.name, findType (clas, m.Base, removed));
					unionAtom.Atoms.AddLast (field);
				}

				CodeContainer nested = GetContainer (entry, Tree);
				//GetContainer (root, union, currentUnit);
				nested.Atoms.AddLast (unionAtom);

				if (orphans != null) {
					foreach (var orphan in orphans)
						unionAtom.Atoms.AddLast (orphan);
				}
			}
			return unionType;
		}

		void GenerateStaticLibField ()
		{
			string name = "Lib_" + Library;
			if (File.Exists (Path.Combine (Dir, fname (name))))
				return;

			var ccu = new CodeCompileUnit ();
			var ns = new CodeNamespace (Namespace);
			var cls = new CodeTypeDeclaration ("Libs") { TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class, IsPartial = true, IsClass = true };
			var field = new CodeMemberField (typeof(CppLibrary), Library) { Attributes = MemberAttributes.Public | MemberAttributes.Static, InitExpression = new CodeObjectCreateExpression (typeof(CppLibrary), new CodePrimitiveExpression (Library)) };
			cls.Members.Add (field);
			ns.Types.Add (cls);
			ccu.Namespaces.Add (ns);

			SaveFile (ccu, name);
		}

		void GenerateUnknownTypeStubs ()
		{
			/*
			var ccu = new CodeCompileUnit ();
			var ns = new CodeNamespace (Namespace);

			foreach (var type in UnknownTypes) {
				var ctd = type.Value as CodeTypeDeclaration;
				var newNS = type.Value as CodeNamespace;

				if (ctd != null)
					ns.Types.Add (ctd);
				else
					ccu.Namespaces.Add (newNS);
			}

			ccu.Namespaces.Add (ns);
			SaveFile (ccu, "UnknownTypes");
			*/
			var ukt = UnknownTypes.ToArray ();
			foreach (var unknownType in ukt) {
				CodeContainer container = Tree;
				CppType type = new CppType (unknownType);
				IEnumerable<CodeAtom> orphans;

				if (IsCreated (type, true, out orphans))
					continue;

				if (type.Namespaces != null)
					container = GetPath (Tree, type.Namespaces, true);

				var atom = new Class (type.ElementTypeName);

				int i = 0;
				foreach (var param in type.Modifiers.OfType<CppModifiers.TemplateModifier> ()) {
					if (param.Types != null) {
						foreach (var t in param.Types) {
							atom.TemplateArguments.Add (genericTypeArgs[i++]);
						}
					} else {
						atom.TemplateArguments.Add (genericTypeArgs[i++]);
					}
				}

				if (orphans != null) {
					foreach (var orphan in orphans)
						atom.Atoms.AddLast (orphan);
				}

				container.Atoms.AddLast (atom);
			}
		}

		void SaveFile (CodeCompileUnit ccu, string baseName)
		{
			string name = Path.Combine (Dir, fname (baseName));
			if (File.Exists (name) && fileList.Contains (name))
				File.Delete (name);
			if (File.Exists (name) && !fileList.Contains (name)) {
				int i = 1;
				while (File.Exists (Path.Combine (Dir, fname (baseName + i))))
					i++;
				name = Path.Combine (Dir, fname (baseName + i));
			}
			Console.WriteLine ("Generating " + name);

			var sw = File.CreateText (name);
			Provider.GenerateCodeFromCompileUnit (ccu, sw, Options);
			sw.Flush ();
			sw.Close ();
			sw.Dispose ();

			if (!fileList.Contains (name))
				fileList.Add (name);
		}

		static CppType replaceType (CppType inType, CppType toReplace, string tn)
		{
			// FIXME: The order of some modifiers is not significant.. is this a problem?
			if (			/* inType.ElementType == toReplace.ElementType && */((inType.Namespaces != null && toReplace.Namespaces != null && inType.Namespaces.SequenceEqual (toReplace.Namespaces)) || inType.Namespaces == null && toReplace.Namespaces == null) && inType.ElementTypeName == toReplace.ElementTypeName && inType.Modifiers.StartsWith (toReplace.Modifiers))
				return new CppType (CppTypes.Typename, tn, inType.Modifiers.Skip (toReplace.Modifiers.Count).ToArray ());

			foreach (var tempMod in inType.Modifiers.OfType<CppModifiers.TemplateModifier> ())
				for (int i = 0; i < tempMod.Types.Length; i++)
					tempMod.Types[i] = replaceType (tempMod.Types[i], toReplace, tn);

			return inType;
		}

		// returns true if the type was already created
		bool IsCreated (CppType type)
		{
			IEnumerable<CodeAtom> dontCare;
			return IsCreated (type, false, out dontCare);
		}
		bool IsCreated (CppType type, bool toBeCreated, out IEnumerable<CodeAtom> orphanedAtoms)
		{
			orphanedAtoms = null;
			if (type.ElementTypeName == null || type.ElementTypeName == "" || type.ElementType == CppTypes.Typename)
				return true;

			// check template parameters recursively
			foreach (var paramType in type.Modifiers.OfType<CppModifiers.TemplateModifier> ().Where (t => t.Types != null).SelectMany (t => t.Types))
				IsCreated (paramType);

			bool typeFound = false;
			type.ElementType = CppTypes.Unknown;
			for (int i = 0; i < type.Modifiers.Count; i++) {
				if (type.Modifiers[i] != CppModifiers.Template)
					type.Modifiers.RemoveAt (i);
			}

			string qualifiedName = type.ToString ();
			bool alreadyUnknown = UnknownTypes.Contains (qualifiedName);

			CodeContainer place = Tree;
			if (type.Namespaces != null)
				place = GetPath (type.Namespaces);

			if (place != null) {
				typeFound = place.Atoms.OfType<Class> ().Where (c => c.Name == type.ElementTypeName).Any () || place.Atoms.OfType<Enumeration> ().Where (e => e.Name == type.ElementTypeName).Any () || place.Atoms.OfType<Union> ().Where (u => u.Name == type.ElementTypeName).Any ();

				var sameNamedNamespace = place.Atoms.OfType<Namespace> ().Where (ns => ns.Name == type.ElementTypeName).SingleOrDefault ();
				if (sameNamedNamespace != null) {
					orphanedAtoms = sameNamedNamespace.Atoms;

					// FIXME: This could potentially be very slow
					Tree.Atoms.Remove (sameNamedNamespace);
					//currentUnit.Atoms.Remove (sameNamedNamespace);
					//enumerations.Atoms.Remove (sameNamedNamespace);
					//unions.Atoms.Remove (sameNamedNamespace);
				}
			}

			if (!typeFound && !toBeCreated && !alreadyUnknown) {
				/*CodeObject codeObject;

				var ctd = new CodeTypeDeclaration (type.ElementTypeName) {
					TypeAttributes = TypeAttributes.Public,
					IsClass = true
				};

				codeObject = ctd;

				if (type.Namespaces != null) {
					var ns = new CodeNamespace (Namespace + "." + string.Join (".", type.Namespaces));
					ns.Types.Add (ctd);
					codeObject = ns;
				}

				var template = type.Modifiers.OfType<CppModifiers.TemplateModifier> ().SingleOrDefault ();
				if (template != null) {
					for (int i = 0; i < template.Types.Length; i++)
						ctd.TypeParameters.Add (genericTypeArgs [i]);
				}
				 */
				UnknownTypes.Add (qualifiedName);
			} else if ((typeFound || toBeCreated) && alreadyUnknown)
				UnknownTypes.Remove (qualifiedName);

			return typeFound;
		}

		CppType findType (Class clas, Entry entry, List<Entry> removed)
		{
			if (entry == null)
				return CppTypes.Unknown;
			return findType (clas, entry, removed, new CppType ());
		}

		CppType findType (Class clas, Entry entry, List<Entry> removed, CppType modifiers)
		{
			if (entry == null)
				return CppTypes.Unknown;
			string name = entry.name == "" ? "unknown" : entry.name;

			switch (entry.type) {
			case "ArrayType":
				return findType (clas, entry.Base, removed, modifiers.Modify (CppModifiers.Array));
			case "PointerType":
				return findType (clas, entry.Base, removed, modifiers.Modify (CppModifiers.Pointer));
			case "ReferenceType":
				return findType (clas, entry.Base, removed, modifiers.Modify (CppModifiers.Reference));
			case "CvQualifiedType":
				return findType (clas, entry.Base, removed, modifiers.Modify ((from c in entry.attributes
					where c.Key == "const" && c.Value == "1"
					select CppModifiers.Const).DefaultIfEmpty (CppModifiers.Volatile).First ()));

			case "Typedef":
				return findType (clas, entry.Base, removed, modifiers);

			case "FundamentalType":
				return modifiers.CopyTypeFrom (new CppType (name));
			case "Class":
				if (removed.Contains (entry))
					return modifiers.CopyTypeFrom (CppTypes.Unknown);
				if (entry.Class != null && entry.Class.GenericName != null) {
					if (clas == entry.Class) {
						Console.WriteLine ("Replacing {0} with {1}", name, entry.Class.GenericName);
						return modifiers.CopyTypeFrom (new CppType (CppTypes.Class, entry.Class.GenericName));
					} else {
						Console.WriteLine ("Replacing {0} with Unknown", name);
//						return modifiers.CopyTypeFrom (new CppType (CppTypes.Class, name));
						return modifiers.CopyTypeFrom (CppTypes.Unknown);
					}
				} else
					return modifiers.CopyTypeFrom (new CppType (CppTypes.Class, name));
			case "Struct":
				if (removed.Contains (entry))
					return modifiers.CopyTypeFrom (CppTypes.Unknown);
				Console.WriteLine ("Return struct {0}", name);
				return modifiers.CopyTypeFrom (new CppType (CppTypes.Struct, name));
			case "Union":
				if (removed.Contains (entry))
					return modifiers.CopyTypeFrom (CppTypes.Unknown);
				Console.WriteLine ("Return union {0}", entry.name);
				return modifiers.CopyTypeFrom (ProcessUnion (clas, entry, removed));
			case "Enumeration":
				if (removed.Contains (entry))
					return modifiers.CopyTypeFrom (CppTypes.Unknown);
				Console.WriteLine ("Return enumeration {0}", entry.name);
				return modifiers.CopyTypeFrom (ProcessEnum (entry));

			// FIXME: support function pointers betters
			case "FunctionType":
				return modifiers.CopyTypeFrom (CppTypes.Void);
			case "MethodType":
				return modifiers.CopyTypeFrom (CppTypes.Void);
			case "Constructor":
				return CppTypes.Unknown;
			}

			throw new NotImplementedException ("Unknown type node: " + entry.type);
		}

		string fname (string name)
		{
			return name.Replace ("<", "_").Replace (">", "_").Replace (":", "_").Replace ("*", "_").Replace (",", "_").Replace (" ", "_") + "." + Provider.FileExtension;
		}
	}
}
