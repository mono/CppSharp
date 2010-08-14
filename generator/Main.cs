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
	class Generator {
		public static readonly Regex TemplateRegEx = new Regex ("([^\\<]+)\\<(.+)\\>$");
		public static readonly string [] genericTypeArgs = new string [] { "T", "U", "V", "W", "X", "Y", "Z" };

		public string Source { get; set; }
		public string Dir {get; set;}
		public bool ShouldValidate { get; set; }
		public string Library {get; set;}

		private string nspace;
		public string Namespace {
			get { return nspace; }
			set {
				nspace = value;
				enumerations.ManagedNamespace = value;
				unions.ManagedNamespace = value;
			}
		}

		public Dictionary<string, string> Classes;
		public HashSet<string> Enumerations;
		public HashSet<string> Unions;
		public Dictionary<string,CodeTypeDeclaration> UnknownTypes;

		public CodeDomProvider Provider { get; set; }
		public CodeGeneratorOptions Options { get; set; }

		private CodeUnit currentUnit;
		private CodeUnit enumerations;
		private CodeUnit unions;
		private Dictionary<string,Property> properties;

		private HashSet<string> fileList;

		public static void Main (string[] args)
		{
			bool help = false;
			Generator gen = new Generator ();
			var p = new OptionSet () {
				{ "h|?|help", v => help = v != null },
				{ "validate", v => gen.ShouldValidate = v != null },
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
			UnknownTypes = new Dictionary<string,CodeTypeDeclaration> ();

			Enumerations = new HashSet<string> ();
			Unions = new HashSet<string> ();

			enumerations = new CodeUnit { ManagedNamespace = Namespace };
			unions = new CodeUnit { ManagedNamespace = Namespace };
			properties = new Dictionary<string, Property> ();

			fileList = new HashSet<string> ();
		}

		public void Run ()
		{
			Console.WriteLine ("Generating bindings...");

			XmlDocument xmldoc = new XmlDocument ();
			xmldoc.Load (Source);

			// FIXME: Support namespaces!!!
			//XmlNodeList namespaces = xmldoc.SelectNodes ("/GCC_XML/Namespace[@name != '::' and @name != '' and @name != 'std']");

			ProcessClasses (xmldoc);
			GenerateStaticLibField ();

			if (enumerations.Atoms.Any ())
				SaveFile (enumerations.WrapperToCodeDom (Provider), "Enums");
			if (unions.Atoms.Any ())
				SaveFile (unions.WrapperToCodeDom (Provider), "Unions");

			if (ShouldValidate) {
				GenerateUnknownTypeStubs ();
				Validate ();
				//File.Delete (fileList.Where (f => f.StartsWith ("UnknownTypes")).Single ());
			}
		}

		void ProcessClasses (XmlDocument xmldoc)
		{
			XmlNodeList classes = xmldoc.SelectNodes ("/GCC_XML/Class[not(@incomplete)]");
			foreach (XmlNode clas in classes) {
				var f = xmldoc.SelectSingleNode ("/GCC_XML/File[@id='" + clas.Attributes["file"].Value + "']/@name");
				if (f != null && f.Value.StartsWith ("/"))
					continue;

				string name = clas.Attributes["name"].Value;
				if (Classes.ContainsKey (name))
					continue;

				// FIXME: better way to do this
				CppType [] replaceArgs = null;
				Match m = TemplateRegEx.Match (name);
				if (m.Success) {
					string baseName = m.Groups [1].Value;
					if (Classes.ContainsKey (baseName))
						continue;

					replaceArgs = m.Groups [2].Value.Split (',').Select (s => new CppType (s)).ToArray ();
					string [] ras = new string [replaceArgs.Length];
					string [] letters = new string [replaceArgs.Length];
					for (int i = 0; i < replaceArgs.Length; i++) {
						letters [i] = genericTypeArgs [i];
						ras [i] = string.Format ("{0} with {1}", replaceArgs [i].ToString (), letters [i]);
					}

					Console.Error.WriteLine ("Warning: Creating generic type {0}<{1}> from the instantiated template {2} by replacing {3} (very buggy!!!)", baseName, string.Join (",", letters), name, string.Join (", ", ras));
					name = baseName;
				}

				currentUnit = new CodeUnit { ManagedNamespace = Namespace };
				var classAtom = new Class (name) {
					StaticCppLibrary = string.Format ("{0}.Libs.{1}", Namespace, Library)
				};
				if (replaceArgs != null) {
					for (int i = 0; i < replaceArgs.Length; i++)
						classAtom.TemplateArguments.Add (genericTypeArgs [i]);
				}

				currentUnit.Atoms.AddLast (classAtom);
				Classes.Add (name, fname (name));

				CppType currentType = new CppType (CppTypes.Class, name);;
				if (replaceArgs != null)
					currentType.Modify (new CppModifiers.TemplateModifier (replaceArgs));
				CheckType (currentType);

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


					if (ShouldValidate)
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
					MethodSignature sig = new MethodSignature { Name = methodAtom.FormattedName, Arguments = argTypes };
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

						// ...AND there is a corresponding getter method, then assume it's a property setter
						if (properties.TryGetValue (pname, out propertyAtom) || findMethod (xmldoc.DocumentElement, members, getterName) != null) {

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


				SaveFile (currentUnit.WrapperToCodeDom (Provider), name);
			}
		}

		void Validate ()
		{
			Console.WriteLine ("Validating bindings...");

			var compileParams = new CompilerParameters {
				GenerateInMemory = true,
				TreatWarningsAsErrors = false
			};
			compileParams.ReferencedAssemblies.Add (typeof (CppLibrary).Assembly.CodeBase);

			CompilerResults results = Provider.CompileAssemblyFromFile (compileParams, fileList.ToArray ());

			if (results.Errors.Count > 0) {
				foreach (CompilerError error in results.Errors)
					Console.Error.WriteLine ("{0}({1},{2}): error {3}: {4}", error.FileName, error.Line, error.Column, error.ErrorNumber, error.ErrorText);

				Console.Error.WriteLine ("Validation failed with {0} compilation errors.", results.Errors.Count);
			}

			//Type [] types = validationAssembly.GetExportedTypes ();
			// do stuff...

		}

		CppType ProcessEnum (XmlNode enm)
		{
			bool hasName = false;
			string ename = "Enum" + Enumerations.Count;

			if (enm.Attributes ["name"] != null && enm.Attributes ["name"].Value != "") {
				hasName = true;
				ename = enm.Attributes ["name"].Value;
			}

			if (!hasName || !Enumerations.Contains (ename)) {

				Enumeration enumAtom = new Enumeration (enm.Attributes ["name"].Value);
				foreach (XmlNode v in enm.SelectNodes ("EnumValue"))
					enumAtom.Items.Add (new Enumeration.Item { Name = v.Attributes ["name"].Value, Value = Convert.ToInt32 (v.Attributes ["init"].Value) });
	
				if (hasName) // assume it might be shared between classes
					enumerations.Atoms.AddLast (enumAtom);
				else
					currentUnit.Atoms.AddLast (enumAtom);

				Enumerations.Add (ename);
			}

			return new CppType (CppTypes.Enum, ename);
		}

		CppType ProcessUnion (XmlNode root, XmlNode union)
		{
			bool hasName = false;
			string uname = "Union" + Unions.Count;

			if (union.Attributes ["name"] != null && union.Attributes ["name"].Value != "") {
				hasName = true;
				uname = union.Attributes ["name"].Value;
			}

			if (!hasName || !Unions.Contains (uname)) {

				Union unionAtom = new Union (uname);
				foreach (string id in union.Attributes["members"].Value.Split (' ').Where (id => !id.Equals (string.Empty))) {
					XmlNode n = root.SelectSingleNode ("/GCC_XML/*[@id = '" + id + "']");

					// FIXME: Support union constructors/destructors?
					if (n.Name != "Field")
						continue;

					Field field = new Field (n.Attributes ["name"].Value, findType (root, n.Attributes ["type"]));
					unionAtom.Atoms.AddLast (field);
				}
	
				if (hasName) // assume it might be shared between classes
					unions.Atoms.AddLast (unionAtom);
				else
					currentUnit.Atoms.AddLast (unionAtom);

				Unions.Add (uname);
			}

			return new CppType (CppTypes.Union, uname);
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
		{
			var ccu = new CodeCompileUnit ();
			var ns = new CodeNamespace (Namespace);

			foreach (var type in UnknownTypes)
				ns.Types.Add (type.Value);

			ccu.Namespaces.Add (ns);
			SaveFile (ccu, "UnknownTypes");
		}

		void SaveFile (CodeCompileUnit ccu, string baseName)
		{
			string name = Path.Combine (Dir, fname (baseName));
			if (File.Exists (name) && fileList.Contains (name))
				return;
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
			    inType.ElementTypeName == toReplace.ElementTypeName &&
			    inType.Modifiers.StartsWith (toReplace.Modifiers))
				return new CppType (CppTypes.Typename, tn, inType.Modifiers.Skip (toReplace.Modifiers.Count).ToArray ());

			foreach (var tempMod in inType.Modifiers.OfType<CppModifiers.TemplateModifier> ())
				for (int i = 0; i < tempMod.Types.Length; i++)
					tempMod.Types [i] = replaceType (tempMod.Types [i], toReplace, tn);

			return inType;
		}

		// FIXME: Do something trickier than just throw ElementTypeName in here?
		void CheckType (CppType cpptype)
		{
			string type = cpptype.ElementTypeName;
			if (type == null) return;

			bool typeFound = Classes.ContainsKey (type) || Enumerations.Contains (type) || Unions.Contains (type);
			bool alreadyUnknown = UnknownTypes.ContainsKey (type);

			if (!typeFound && !alreadyUnknown) {
				var ctd = new CodeTypeDeclaration (type) {
					TypeAttributes = TypeAttributes.Public,
					IsClass = true,
					IsPartial = true
				};

				if (cpptype.Modifiers.Contains (CppModifiers.Template)) {
					var template = cpptype.Modifiers.OfType<CppModifiers.TemplateModifier> ().Single ();
					for (int i = 0; i < template.Types.Length; i++)
						ctd.TypeParameters.Add (genericTypeArgs [i]);
				}

				UnknownTypes.Add (type, ctd);
			} else if (typeFound && alreadyUnknown)
				UnknownTypes.Remove (type);
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

			case "FundamentalType": return modifiers.ApplyTo (new CppType (name));
			case "Class": return modifiers.ApplyTo (new CppType (CppTypes.Class, name));
			case "Struct": return modifiers.ApplyTo (new CppType (CppTypes.Struct, name));
			case "Union": return modifiers.ApplyTo (ProcessUnion (root, n));
			case "Enumeration": return modifiers.ApplyTo (ProcessEnum (n));

			// FIXME: support function pointers betters
			case "FunctionType": return modifiers.ApplyTo (CppTypes.Void);
			}

			throw new NotImplementedException ("Unknown type node: " + n.Name);
		}

		string fname (string name)
		{
			return name.Replace ("<", "_").Replace (">", "_").Replace(":", "_").Replace("*", "_").Replace (",", "_").Replace(" ", "_") + "." + Provider.FileExtension;
		}
	}
}
