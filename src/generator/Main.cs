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

namespace CPPInterop {
	public class Generator {
		public static readonly string [] genericTypeArgs = new string [] { "T", "U", "V", "W", "X", "Y", "Z" };

		public string Source { get; set; }
		public string Dir {get; set;}
		public bool AbiTest { get; set; }
		public string Library {get; set;}

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
		private Dictionary<string,Property> properties;

		private int enumCount = 0;
		private int unionCount = 0;

		private HashSet<string> fileList;

		public static void Main (string[] args)
		{
			bool help = false;
			Generator gen = new Generator ();
			var p = new OptionSet () {
				{ "h|?|help", v => help = v != null },
				{ "testabi", v => gen.AbiTest = v != null },
				{ "f=", v => gen.Source = v },
				{ "o=", v => gen.Dir = v },
				{ "ns=", v => gen.Namespace = v },
				{ "lib=", v => gen.Library = v },
				{ "lang=", v => gen.Provider = CodeDomProvider.CreateProvider (v) }
			};

			List<string> extra = null;
			try {
				extra = p.Parse(args);
			} catch (OptionException){
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

			gen.Options = new CodeGeneratorOptions () {
				BlankLinesBetweenMembers = false
			};

			gen.Run ();
		}

		public Generator ()
		{
			Classes = new Dictionary<string, string>();
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
			XmlDocument xmldoc = new XmlDocument ();
			xmldoc.Load (Source);

			ProcessClasses (xmldoc);

			// save files
			Save ();
			GenerateStaticLibField ();

			Console.WriteLine ("Validating bindings...");
			GenerateUnknownTypeStubs ();
			Assembly asm = Validate ();

			if (asm != null && AbiTest) {
				Console.WriteLine ("Testing Itanium ABI name mangling against bindings...");
				TestAbi (asm);
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



		void ProcessClasses (XmlDocument xmldoc)
		{
			// FIXME: Figure out how to eliminate struct noise
			XmlNodeList classes = xmldoc.SelectNodes ("/GCC_XML/Class[not(@incomplete)]" /* |/GCC_XML/Struct[not(@incomplete)]" */);
			foreach (XmlNode clas in classes) {
				var f = xmldoc.SelectSingleNode ("/GCC_XML/File[@id='" + clas.Attributes["file"].Value + "']/@name");
				if (f != null && f.Value.StartsWith ("/"))
					continue;

				if (clas.Attributes ["name"] == null || clas.Attributes ["name"].Value == "")
					continue;

				//currentUnit = new CodeUnit { ManagedNamespace = Namespace };

				string name = clas.Attributes ["name"].Value;
				string ns = GetNamespace (xmldoc.DocumentElement, clas);
				CppType currentType = new CppType (clas.Name.ToLower (), ns != null? ns + "::" + name : name);
				IEnumerable<CodeAtom> nested = null;

				// FIXME: better way to do this (GCC-XML output doesn't seem to leave much choice)
				CppType [] replaceArgs = null;
				var templated = currentType.Modifiers.OfType<CppModifiers.TemplateModifier> ().SingleOrDefault ();
				if (templated != null) {
					string baseName = currentType.ElementTypeName;
					if (CheckType (currentType, true, out nested))
						continue;

					replaceArgs = templated.Types;

					string [] ras = new string [replaceArgs.Length];
					string [] letters = new string [replaceArgs.Length];
					for (int i = 0; i < replaceArgs.Length; i++) {
						letters [i] = genericTypeArgs [i];
						ras [i] = string.Format ("{0} with {1}", replaceArgs [i].ToString (), letters [i]);
					}
					Console.Error.WriteLine ("Warning: Creating generic type {0}<{1}> from the instantiated template {2} by replacing {3} (very buggy!!!)", baseName, string.Join (",", letters), name, string.Join (", ", ras));

					name = baseName;
				} else if (CheckType (currentType, true, out nested))
					continue;


				var classAtom = new Class (name) {
					StaticCppLibrary = string.Format ("{0}.Libs.{1}", Namespace, Library)
				};
				GetContainer (xmldoc.DocumentElement, clas, Tree).Atoms.AddLast (classAtom);
				//GetContainer (xmldoc.DocumentElement, clas, currentUnit).Atoms.AddLast (classAtom);

				if (nested != null) {
					foreach (var orphan in nested)
						classAtom.Atoms.AddLast (orphan);
				}

				// add tempate type args
				if (replaceArgs != null) {
					for (int i = 0; i < replaceArgs.Length; i++)
						classAtom.TemplateArguments.Add (genericTypeArgs [i]);
				}

				// FIXME: Handle when base class name is fully qualified
				foreach (XmlNode baseNode in clas.SelectNodes ("Base")) {
					classAtom.Bases.Add (new Class.BaseClass {
						Name = find (xmldoc.DocumentElement, baseNode.Attributes ["type"]).Attributes ["name"].Value,
						Access = baseNode.Attributes ["access"].Value == "public"? Access.Public :
						         baseNode.Attributes ["access"].Value == "protected"? Access.Protected :
						         Access.Private,
						IsVirtual = baseNode.Attributes ["virtual"].Value == "1"
					});
				}

				//string size = clas.Attributes["size"].Value;
				var members = clas.Attributes["members"].Value.Split (' ').Where (id => !id.Equals (string.Empty)).ToArray ();
				Dictionary<MethodSignature,string> methods = new Dictionary<MethodSignature, string> ();

				foreach (string id in members) {
					XmlNode n = xmldoc.SelectSingleNode ("/GCC_XML/*[@id='" + id + "']");

					bool ctor = false;
					bool dtor = false;
					int fieldCount = 0;

					switch (n.Name) {
					case "Constructor":
						ctor = true;
						break;
					case "Destructor":
						dtor = true;
						break;
					case "Method":
						break;
					case "Field":
						CppType fieldType = findType (xmldoc.DocumentElement, n.Attributes["type"]);
						string fieldName = "field" + fieldCount++;
						if (n.Attributes ["name"] != null && n.Attributes ["name"].Value != "")
							fieldName = n.Attributes ["name"].Value;
						classAtom.Atoms.AddLast (new Field (fieldName, fieldType));
						continue;
					default:
						continue;
					}

					// Now we're processing a method...

					if (n.Attributes ["access"] == null || n.Attributes ["access"].Value != "public" ||
					   (n.Attributes ["overrides"] != null && n.Attributes ["overrides"].Value != "" && !dtor) ||
					   n.Attributes ["extern"] == null || n.Attributes ["extern"].Value != "1")
						continue;

					string mname = n.Attributes ["name"].Value;

					CppType retType = findType (xmldoc.DocumentElement, n.Attributes ["returns"]);
					if (replaceArgs != null) {
						for (int i = 0; i < replaceArgs.Length; i++)
							retType = replaceType (retType, replaceArgs [i], genericTypeArgs [i]);
					}

					var methodAtom = new Method (dtor? "Destruct" : mname) {
						RetType = retType,
						IsVirtual = n.Attributes ["virtual"] != null && n.Attributes ["virtual"].Value == "1",
						IsStatic = n.Attributes ["static"] != null && n.Attributes ["static"].Value == "1",
						IsConst = n.Attributes ["const"] != null && n.Attributes ["const"].Value == "1",
						IsConstructor = ctor,
						IsDestructor = dtor
					};


					if (AbiTest)
						methodAtom.Mangled = new NameTypePair<Type> { Name = n.Attributes ["mangled"].Value, Type = typeof (ItaniumAbi) };

					XmlNodeList argNodes = n.SelectNodes ("Argument");
					CppType [] argTypes = new CppType [argNodes.Count];
					int c = 0;
					foreach (XmlNode arg in argNodes) {
						string argname;
						if (arg.Attributes ["name"] == null)
							argname = "arg" + c;
						else
							argname = arg.Attributes ["name"].Value;

						CppType argtype = findType (xmldoc.DocumentElement, arg.Attributes ["type"].Value);
						if (replaceArgs != null) {
							for (int i = 0; i < replaceArgs.Length; i++)
								argtype = replaceType (argtype, replaceArgs [i], genericTypeArgs [i]);
						}

						methodAtom.Parameters.Add (new NameTypePair<CppType> { Name = argname, Type = argtype });
						argTypes [c] = argtype;

						// tee hee
						c++;
					}

					// Try to filter out duplicate methods
					MethodSignature sig = new MethodSignature (methodAtom.FormattedName, argTypes);
					string conflictingSig;
					if (methods.TryGetValue (sig, out conflictingSig)) {
						// FIXME: add comment to explain why it's commented out
						string demangled = n.Attributes ["demangled"].Value;
						Console.Error.WriteLine ("Warning: Method \"{0}\" in class {1} omitted because it conflicts with \"{2}\"", demangled, name, conflictingSig);
						methodAtom.Comment = string.Format ("FIXME:Method \"{0}\" omitted because it conflicts with \"{1}\"", demangled, conflictingSig);
						methodAtom.CommentedOut = true;
					} else
						methods.Add (sig, n.Attributes ["demangled"].Value);

					// Record unknown types in parameters and return type
					CheckType (methodAtom.RetType);
					foreach (var arg in methodAtom.Parameters)
						CheckType (arg.Type);

					// Detect if this is a property...

					// if it's const, returns a value, has no parameters, and there is no other method with the same name
					//  in this class assume it's a property getter (for now?)
					if (methodAtom.IsConst && !retType.Equals (CppTypes.Void) && !methodAtom.Parameters.Any () &&
					    findMethod (xmldoc.DocumentElement, members, "@id != '" + id + "' and @name = '" + mname + "'") == null) {
						var pname = methodAtom.FormattedName;
						Property propertyAtom;

						if (properties.TryGetValue (pname, out propertyAtom)) {
							propertyAtom.Getter = methodAtom;
						} else {
							propertyAtom = new Property (pname) { Getter = methodAtom };
							properties.Add (pname, propertyAtom);
							classAtom.Atoms.AddLast (propertyAtom);
						}

						continue;
					}

					// if it's name starts with "set", does not return a value, and has one arg (besides this ptr)
					// and there is no other method with the same name...
					if (mname.ToLower ().StartsWith ("set") && retType.Equals (CppTypes.Void) && methodAtom.Parameters.Count == 1 &&
					    findMethod (xmldoc.DocumentElement, members, "@id != '" + id + "' and @name = '" + mname + "'") == null) {
						string getterName = "translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz') = '" +
							mname.Substring (3).TrimStart ('_').ToLower () + "'";

						string pname = methodAtom.FormattedName.Substring (3);
						Property propertyAtom = null;

						// ...AND there is a corresponding getter method that returns the right type, then assume it's a property setter
						bool doIt = false;
						if (properties.TryGetValue (pname, out propertyAtom)) {
							doIt = propertyAtom.Getter.RetType.Equals (methodAtom.Parameters [0].Type);
						} else {
							XmlNode getter = findMethod (xmldoc.DocumentElement, members, getterName);
							doIt = (getter != null && findType (xmldoc.DocumentElement, getter.Attributes ["returns"]).Equals (methodAtom.Parameters [0].Type));
						}
						if (doIt) {
							if (propertyAtom != null) {
								propertyAtom.Setter = methodAtom;
							} else {
								propertyAtom = new Property (pname) { Setter = methodAtom };
								properties.Add (pname, propertyAtom);
								classAtom.Atoms.AddLast (propertyAtom);
							}

							// set the method's arg name to "value" so that the prop setter works right
							var valueParam = methodAtom.Parameters [0];
							valueParam.Name = "value";
							methodAtom.Parameters [0] = valueParam;

							continue;
						}
					}

					classAtom.Atoms.AddLast (methodAtom);
				}


				//SaveFile (currentUnit.WrapperToCodeDom (Provider), name);
			}
		}

		Assembly Validate ()
		{
			var compileParams = new CompilerParameters {
				GenerateInMemory = true,
				IncludeDebugInformation = true,
				WarningLevel = 0,
				TreatWarningsAsErrors = false
			};
			compileParams.ReferencedAssemblies.Add (typeof (CppLibrary).Assembly.CodeBase);

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
			var classes = assembly.GetExportedTypes ().Select (t => new { Name = Regex.Replace (t.Name, "\\`.$", ""), Interface = t.GetNestedType ("I" + t.Name)})
			                                          .Where (k => k.Interface != null);
			var abi = new ItaniumAbi ();

			foreach (var klass in classes) {
				bool klassSuccess = true;

				foreach (var method in klass.Interface.GetMethods ()) {

					var testAttribute = (AbiTestAttribute)method.GetCustomAttributes (typeof (AbiTestAttribute), false).FirstOrDefault ();

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

		string GetNamespace (XmlNode root, XmlNode n)
		{
			XmlNode ns = find (root, n.Attributes ["context"]);
			if (ns == null)
				return null;

			string nsname;
			if (ns.Name == "Namespace")
				nsname = ns.Attributes ["name"].Value;
			else if (ns.Name == "Class" || ns.Name == "Struct")
				nsname = new CppType (ns.Attributes ["name"].Value).ElementTypeName;
			else
				throw new NotSupportedException ("Unknown context: " + ns.Name);

			if (nsname == "::")
				return null;

			string parent = GetNamespace (root, ns);
			if (parent != null)
				return parent + "::" + nsname;

			return nsname;
		}

		CodeContainer GetContainer (XmlNode root, XmlNode n, CodeContainer def)
		{
			XmlNode ns = find (root, n.Attributes ["context"]);
			if (ns == null)
				return def;

			string nsname;
			if (ns.Name == "Namespace")
				nsname = ns.Attributes ["name"].Value;
			else if (ns.Name == "Class" || ns.Name == "Struct")
				nsname = new CppType (ns.Attributes ["name"].Value).ElementTypeName;
			else
				throw new NotSupportedException ("Unknown context: " + ns.Name);

			if (nsname == "::")
				return def;

			CodeContainer parent = GetContainer (root, ns, def);
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

		CppType ProcessEnum (XmlNode root, XmlNode enm)
		{
			bool hasName = false;
			string ename = "Enum" + enumCount++;

			if (enm.Attributes ["name"] != null && enm.Attributes ["name"].Value != "") {
				hasName = true;
				ename = enm.Attributes ["name"].Value;
			}

			CppType enumType = new CppType (CppTypes.Enum, ename);

			if (hasName) {
				string ns = GetNamespace (root, enm);
				if (ns != null)
					enumType = new CppType (CppTypes.Enum, ns + "::" + ename);
			}

			IEnumerable<CodeAtom> dontCare;
			if (!hasName || !CheckType (enumType, true, out dontCare)) {

				Enumeration enumAtom = new Enumeration (ename);
				foreach (XmlNode v in enm.SelectNodes ("EnumValue"))
					enumAtom.Items.Add (new Enumeration.Item { Name = v.Attributes ["name"].Value, Value = Convert.ToInt32 (v.Attributes ["init"].Value) });

				//GetContainer (root, enm, flatUnit).Atoms.AddLast (enumAtom);

				CodeContainer nested = GetContainer (root, enm, Tree);//GetContainer (root, enm, currentUnit);
				//if (hasName && !(nested is Class)) // assume it might be used by other classes
				//	GetContainer (root, enm, enumerations).Atoms.AddLast (enumAtom);
				//else
					nested.Atoms.AddLast (enumAtom);
			}

			return enumType;
		}

		CppType ProcessUnion (XmlNode root, XmlNode union)
		{
			bool hasName = false;
			string uname = "Union" + unionCount++;

			if (union.Attributes ["name"] != null && union.Attributes ["name"].Value != "") {
				hasName = true;
				uname = union.Attributes ["name"].Value;
			}

			CppType unionType = new CppType (CppTypes.Union, uname);

			if (hasName) {
				string ns = GetNamespace (root, union);
				if (ns != null)
					unionType = new CppType (CppTypes.Union, ns + "::" + uname);
			}

			IEnumerable<CodeAtom> orphans = null;
			if (!hasName || !CheckType (unionType, true, out orphans)) {

				Union unionAtom = new Union (uname);
				foreach (string id in union.Attributes["members"].Value.Split (' ').Where (id => !id.Equals (string.Empty))) {
					XmlNode n = root.SelectSingleNode ("/GCC_XML/*[@id = '" + id + "']");

					// FIXME: Support union constructors/destructors?
					if (n.Name != "Field")
						continue;

					Field field = new Field (n.Attributes ["name"].Value, findType (root, n.Attributes ["type"]));
					unionAtom.Atoms.AddLast (field);
				}

				//GetContainer (root, union, flatUnit).Atoms.AddLast (unionAtom);

				CodeContainer nested = GetContainer (root, union, Tree);//GetContainer (root, union, currentUnit);
				//if (hasName && !(nested is Class)) // assume it might be used by other classes
				//	GetContainer (root, union, unions).Atoms.AddLast (unionAtom);
				//else
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
			var cls = new CodeTypeDeclaration ("Libs") {
				TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Class,
				IsPartial = true,
				IsClass = true
			};
			var field = new CodeMemberField (typeof (CppLibrary), Library) {
				Attributes = MemberAttributes.Public | MemberAttributes.Static,
				InitExpression = new CodeObjectCreateExpression (typeof (CppLibrary), new CodePrimitiveExpression (Library))
			};
			cls.Members.Add (field);
			ns.Types.Add (cls);
			ccu.Namespaces.Add (ns);

			SaveFile (ccu, name);
		}

		void GenerateUnknownTypeStubs ()
		{/*
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

				if (CheckType (type, true, out orphans))
					continue;

				if (type.Namespaces != null)
					container = GetPath (Tree, type.Namespaces, true);

				var atom = new Class (type.ElementTypeName);

				int i = 0;
				foreach (var param in type.Modifiers.OfType<CppModifiers.TemplateModifier> ()) {
					if (param.Types != null) {
						foreach (var t in param.Types)
							atom.TemplateArguments.Add (genericTypeArgs [i++]);
					} else
						atom.TemplateArguments.Add (genericTypeArgs [i++]);
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
				name = fname (baseName + i);
			}

			var sw = File.CreateText (Path.Combine (Dir, name));
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
			if (/* inType.ElementType == toReplace.ElementType && */
			    ((inType.Namespaces != null && toReplace.Namespaces != null && inType.Namespaces.SequenceEqual (toReplace.Namespaces)) ||
			        inType.Namespaces == null && toReplace.Namespaces == null) &&
			    inType.ElementTypeName == toReplace.ElementTypeName &&
			    inType.Modifiers.StartsWith (toReplace.Modifiers))
				return new CppType (CppTypes.Typename, tn, inType.Modifiers.Skip (toReplace.Modifiers.Count).ToArray ());

			foreach (var tempMod in inType.Modifiers.OfType<CppModifiers.TemplateModifier> ())
				for (int i = 0; i < tempMod.Types.Length; i++)
					tempMod.Types [i] = replaceType (tempMod.Types [i], toReplace, tn);

			return inType;
		}

		// returns true if the type was already created
		bool CheckType (CppType type)
		{
			IEnumerable<CodeAtom> dontCare;
			return CheckType (type, false, out dontCare);
		}
		bool CheckType (CppType type, bool toBeCreated, out IEnumerable<CodeAtom> orphanedAtoms)
		{
			orphanedAtoms = null;
			if (type.ElementTypeName == null || type.ElementTypeName == "" || type.ElementType == CppTypes.Typename)
				return true;

			// check template parameters recursively
			foreach (var paramType in type.Modifiers.OfType<CppModifiers.TemplateModifier> ().Where (t => t.Types != null).SelectMany (t => t.Types))
				CheckType (paramType);

			bool typeFound = false;
			type.ElementType = CppTypes.Unknown;
			for (int i = 0; i < type.Modifiers.Count; i++) {
				if (type.Modifiers [i] != CppModifiers.Template)
					type.Modifiers.RemoveAt (i);
			}

			string qualifiedName = type.ToString ();
			bool alreadyUnknown = UnknownTypes.Contains (qualifiedName);

			CodeContainer place = Tree;
			if (type.Namespaces != null)
				place = GetPath (type.Namespaces);

			if (place != null) {
				typeFound = place.Atoms.OfType<Class> ().Where (c => c.Name == type.ElementTypeName).Any () ||
				            place.Atoms.OfType<Enumeration> ().Where (e => e.Name == type.ElementTypeName).Any () ||
				            place.Atoms.OfType<Union> ().Where (u => u.Name == type.ElementTypeName).Any ();

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

		static XmlNode find (XmlNode root, XmlAttribute att)
		{
			if (att != null)
				return find (root, att.Value);
			return null;
		}

		static XmlNode find (XmlNode root, string id)
		{
			XmlNode n = root.SelectSingleNode ("/GCC_XML/*[@id='" + id + "']");
			//if (n.Name == "Typedef")
			//	return n;
			if (n.Attributes["type"] != null)
				return find (root, n.Attributes["type"].Value);
			return n;
		}

		static XmlNode findMethod (XmlNode root, string [] members, string predicate)
		{
			string isMember = "@id = '" + string.Join ("' or @id = '", members) + "'";
			return root.SelectSingleNode (string.Format ("/GCC_XML/Method[({0}) and ({1})]", predicate, isMember));
		}

		CppType findType (XmlNode root, XmlAttribute att)
		{
			if (att != null)
				return findType (root, att.Value);
			return CppTypes.Void;
		}
		CppType findType (XmlNode root, string id)
		{
			return findType (root, id, new CppType ());
		}

		CppType findType (XmlNode root, string id, CppType modifiers)
		{
			XmlNode n = root.SelectSingleNode ("/GCC_XML/*[@id='" + id + "']");

			string name = "unknown";
			if (n.Attributes ["name"] != null)
				name = n.Attributes ["name"].Value;
			switch (n.Name) {
			case "ArrayType": return findType (root, n.Attributes ["type"].Value, modifiers.Modify (CppModifiers.Array));
			case "PointerType": return findType (root, n.Attributes ["type"].Value, modifiers.Modify (CppModifiers.Pointer));
			case "ReferenceType": return findType (root, n.Attributes ["type"].Value, modifiers.Modify (CppModifiers.Reference));
			case "CvQualifiedType":
				return findType (root, n.Attributes ["type"].Value,
				                 modifiers.Modify (n.Attributes ["const"] != null && n.Attributes ["const"].Value == "1"? CppModifiers.Const : CppModifiers.Volatile));

			case "Typedef": return findType (root, n.Attributes ["type"].Value, modifiers);

			case "FundamentalType": return modifiers.CopyTypeFrom (new CppType (name));
			case "Class": return modifiers.CopyTypeFrom (new CppType (CppTypes.Class, name));
			case "Struct": return modifiers.CopyTypeFrom (new CppType (CppTypes.Struct, name));
			case "Union": return modifiers.CopyTypeFrom (ProcessUnion (root, n));
			case "Enumeration": return modifiers.CopyTypeFrom (ProcessEnum (root, n));

			// FIXME: support function pointers betters
			case "FunctionType": return modifiers.CopyTypeFrom (CppTypes.Void);
			}

			throw new NotImplementedException ("Unknown type node: " + n.Name);
		}

		string fname (string name)
		{
			return name.Replace ("<", "_").Replace (">", "_").Replace(":", "_").Replace("*", "_").Replace (",", "_").Replace(" ", "_") + "." + Provider.FileExtension;
		}
	}
}
