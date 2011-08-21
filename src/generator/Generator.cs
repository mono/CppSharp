//
// Generator.cs: C++ Interop Code Generator
//
//

using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;

using Templates;
using NDesk.Options;
using Mono.Cxxi;

public class Generator {

	// Command line arguments
	public string InputFileName { get; set; }
	public string OutputDir { get; set; }
	public string FilterFile { get; set; }

	// In the future we might support more than one of these at once...
	public Lib Lib { get; set; }

	// Classes to generate code for
	public Dictionary<Node, Namespace> NodeToNamespace { get; set; }

	private FilterMode default_filter_mode;
	public FilterMode DefaultFilterMode { get { return default_filter_mode; } set { default_filter_mode = value; } }
	public Dictionary<string, Filter> Filters { get; set; }

	// Code templates
	public LibsBase LibsTemplate { get; set; }
	public ClassBase ClassTemplate { get; set; }
	public EnumBase EnumTemplate { get; set; }

	public static int Main (String[] args) {
		var generator = new Generator ();
		generator.Run (args);
		return 0;
	}

	void Run (String[] args) {
		Lib = new Lib ();
		NodeToNamespace = new Dictionary<Node, Namespace> ();

		if (ParseArguments (args) != 0) {
			Environment.Exit (1);
		}

		Node root = LoadXml (InputFileName);

		if (FilterFile != null)
			Filters = Filter.Load (XDocument.Load (FilterFile), out default_filter_mode);

		CreateTypes (root);

		CreateMembers ();

		GenerateCode ();
	}

	int ParseArguments (String[] args) {
		bool help = false;

		var p = new OptionSet {
				{ "h|?|help", "Show this help message", v => help = v != null },
				{ "o=|out=", "Set the output directory", v => OutputDir = v },
				{ "ns=|namespace=", "Set the namespace of the generated code", v => Lib.BaseNamespace = v },
				{ "lib=", "The base name of the C++ library, i.e. 'qt' for libqt.so", v =>Lib.BaseName = v },
				{ "filters=", "A file containing filter directives for filtering classes", v => FilterFile = v },
				{ "inline=", "Inline methods in lib are: notpresent (default), present, surrogatelib (present in %lib%-inline)", v => Lib.InlinePolicy = (InlineMethods)Enum.Parse (typeof (InlineMethods), v, true) }
			};

		try {
			args = p.Parse (args).ToArray ();
		} catch (OptionException) {
			Console.WriteLine ("Try `generator --help' for more information.");
			return 1;
		}

		if (help) {
			p.WriteOptionDescriptions (Console.Error);
			return 1;
		}

		if (args.Length != 1) {
			Console.WriteLine ("Usage: generator <options> <input xml file>");
			return 1;
		}

		// Code templates
		LibsTemplate = new CSharpLibs ();
		ClassTemplate = new CSharpClass ();
		EnumTemplate = new CSharpEnum ();

		InputFileName = args [0];

		if (Lib.BaseName == null) {
			Console.WriteLine ("The --lib= option is required.");
			return 1;
		}

		if (Lib.BaseNamespace == null) {
			Lib.BaseNamespace = Path.GetFileNameWithoutExtension (Lib.BaseName);
		}

		if (OutputDir == null)
			OutputDir = "output";

		return 0;
	}

	Node LoadXml (string file) {
		XmlReader reader = XmlReader.Create (file, new XmlReaderSettings ());

		Node[] parents = new Node [1024];

		Node root = null;

		while (reader.Read()) {
			if (reader.IsStartElement ()) {
				string type = reader.Name;

				var attributes = new Dictionary<string, string> ();
				while (reader.MoveToNextAttribute ()) {
					attributes [reader.Name] = reader.Value;
				}

				Node n = new Node {
						Id = "",
						Type = type,
						Attributes = attributes,
						Children = new List<Node> ()
				};

				if (attributes.ContainsKey ("id")) {
					n.Id = attributes ["id"];
					Node.IdToNode [n.Id] = n;
				}

				if (attributes.ContainsKey ("name"))
					n.Name = attributes ["name"];

				if (parents [reader.Depth - 1] != null) {
					//Console.WriteLine (parents [reader.Depth - 1].type + " -> " + e.type);
					parents [reader.Depth - 1].Children.Add (n);
				}
				parents [reader.Depth] = n;

				if (n.Type == "GCC_XML" && root == null)
					root = n;
			}
		}

		return root;
	}

	void CreateTypes (Node root) {

		foreach (Node n in root.Children) {
			if (n.IsTrue ("incomplete") || !n.HasValue ("name") || n.Attributes ["name"] == "::")
				continue;

			Namespace ns;
			switch (n.Type) {

			case "Class":
			case "Struct":
				ns = new Class (n);
				break;

			case "Enumeration":
				ns = new Enumeration (n);
				break;

			case "Namespace":
				ns = new Namespace (n);
				break;

			default:
				continue;

			}

			NodeToNamespace [n] = ns;
			Lib.Namespaces.Add (ns);
		}

		for (var i = 0; i < Lib.Namespaces.Count; i++) {
			Namespace ns = Lib.Namespaces [i];
			SetParentNamespace (ns);

			var filter = GetFilterOrDefault (ns);
			if (filter.Mode == FilterMode.Exclude)
				NodeToNamespace.Remove (ns.Node);

			if (filter.Mode != FilterMode.Include) {
				Lib.Namespaces.RemoveAt (i);
				i--;
				continue;
			}

			var klass = ns as Class;
			if (klass == null)
				continue;

			// Compute bases
			foreach (Node bn in klass.Node.Children.Where (o => o.Type == "Base")) {
				Class baseClass = NodeToNamespace [bn.NodeForAttr ("type")] as Class;
				Debug.Assert (baseClass != null);
				klass.BaseClasses.Add (baseClass);
			}
		}
	}

	void SetParentNamespace (Namespace ns)
	{
		Namespace parent = null;
		if (ns.Node.HasValue ("context") && NodeToNamespace.TryGetValue (Node.IdToNode [ns.Node.Attributes ["context"]], out parent))
		{
			SetParentNamespace (parent);
			ns.ParentNamespace = parent;
		}
	}

	void CreateMembers () {

		foreach (var ns in Lib.Namespaces) {

			var parentClass = ns.ParentNamespace as Class;

			var @enum = ns as Enumeration;
			if (@enum != null) {
				if (parentClass != null)
					parentClass.NestedEnums.Add (@enum);

				foreach (var enumValue in @enum.Node.Children.Where (o => o.Type == "EnumValue")) {
					int val;
					var item = new Enumeration.Item { Name = enumValue.Attributes ["name"] };

					if (enumValue.HasValue ("init") && int.TryParse (enumValue.Attributes ["init"], out val))
						item.Value = val;

					@enum.Items.Add (item);
				}

				continue;
			}

			var klass = ns as Class;
			if (klass == null || !klass.Node.HasValue ("members"))
				continue;

			if (parentClass != null)
				parentClass.NestedClasses.Add (klass);

			int fieldCount = 0;
			foreach (Node n in klass.Node ["members"].Split (new[] {' '}, StringSplitOptions.RemoveEmptyEntries).Select (id => Node.IdToNode [id])) {
				bool ctor = false;
				bool dtor = false;
				bool skip = false;

				switch (n.Type) {
				case "Field":
					var fieldType = GetType (GetTypeNode (n));
					if (fieldType.ElementType == CppTypes.Unknown && fieldType.ElementTypeName == null)
						fieldType = new CppType (CppTypes.Void, CppModifiers.Pointer);

					string fieldName;
					if (n.Name != "")
						fieldName = n.Name;
					else
						fieldName = "field" + fieldCount++;

					klass.Fields.Add (new Field (fieldName, fieldType, (Access)Enum.Parse (typeof (Access), n ["access"])));
					break;

				case "Constructor":
					ctor = true;
					break;
				case "Destructor":
					dtor = true;
					break;
				case "Method":
					break;
				default:
					continue;
				}

				if (n.Name == "timerEvent")
					Console.WriteLine ("foo");

				if ((!dtor && n.HasValue ("overrides") && CheckPrimaryBases (klass, b => b.Node.CheckValueList ("members", n.Attributes ["overrides"]))) || // excl. virtual methods from primary base (except dtor)
				    (!n.IsTrue ("extern") && !n.IsTrue ("inline")))
					continue;

				if (n.IsTrue ("inline") && Lib.InlinePolicy == InlineMethods.NotPresent)
					skip = true;

				string name = dtor ? "Destruct" : n.Name;

				var method = new Method (n) {
						Name = name,
						Access = (Access)Enum.Parse (typeof (Access), n.Attributes ["access"]),
						IsVirtual = n.IsTrue ("virtual"),
						IsStatic = n.IsTrue ("static"),
						IsConst = n.IsTrue ("const"),
						IsInline = n.IsTrue ("inline"),
						IsArtificial = n.IsTrue ("artificial"),
						IsConstructor = ctor,
						IsDestructor = dtor
				};

				if (method.Access == Access.@private)
					skip = true;

				if (dtor || method.IsArtificial)
					method.GenWrapperMethod = false;

				CppType retType;
				if (n.HasValue ("returns"))
					retType = GetType (n.NodeForAttr ("returns"));
				else
					retType = CppTypes.Void;
				if (retType.ElementType == CppTypes.Unknown) {
					retType = CppTypes.Void;
					skip = true;
				}
				if (CppTypeToManaged (retType) == null) {
					//Console.WriteLine ("\t\tS: " + retType);
					retType = CppTypes.Void;
					skip = true;
				}

				method.ReturnType = retType;

				int c = 0;
				var argTypes = new List<CppType> ();
				foreach (Node arg in n.Children.Where (o => o.Type == "Argument")) {
					string argname;
					if (arg.Name == null || arg.Name == "")
						argname = "arg" + c;
					else
						argname = arg.Name;

					var argtype = GetType (GetTypeNode (arg));
					if (argtype.ElementType == CppTypes.Unknown) {
						//Console.WriteLine ("Skipping method " + klass.Name + "::" + member.Name + " () because it has an argument with unknown type '" + TypeNodeToString (arg) + "'.");
						argtype = new CppType (CppTypes.Void, CppModifiers.Pointer);
						skip = true;
					}

					if (CppTypeToManaged (argtype) == null) {
						//Console.WriteLine ("\t\tS: " + argtype);
						argtype = new CppType (CppTypes.Void, CppModifiers.Pointer);
						skip = true;
					}

					method.Parameters.Add (new Parameter (argname, argtype));
					argTypes.Add (argtype);

					c++;
				}
				if (skip && !method.IsVirtual)
					continue;
				else if (skip && method.IsVirtual)
					method.GenWrapperMethod = false;

				// FIXME: More complete type name check
				if (ctor && argTypes.Count == 1 && argTypes [0].ElementType == CppTypes.Class && argTypes [0].ElementTypeName == klass.Name && argTypes [0].Modifiers.Count == 2 && argTypes [0].Modifiers.Contains (CppModifiers.Const) && argTypes [0].Modifiers.Contains (CppModifiers.Reference))
					method.IsCopyCtor = true;
				
				Console.WriteLine ("\t" + klass.Name + "." + method.Name);

				klass.Methods.Add (method);
			}

			foreach (var method in klass.Methods) {
				if (AddAsProperty (klass, method))
					method.GenWrapperMethod = false;
			}

			Field f2 = klass.Fields.FirstOrDefault (f => f.Type.ElementType == CppTypes.Unknown);
			if (f2 != null) {
				Console.WriteLine ("Skipping " + klass.Name + " because field " + f2.Name + " has unknown type.");
				klass.Disable = true;
			}
		}
	}

	//
	// Property support
	//
    bool AddAsProperty (Class klass, Method method) {
		// if it's const, returns a value, has no parameters, and there is no other method with the same name
		//  in this class assume it's a property getter (for now?)
		if (method.IsConst && !method.ReturnType.Equals (CppTypes.Void) && !method.Parameters.Any () &&
			klass.Methods.Count (o => o.Name == method.Name) == 1) {
			Property property;

			property = klass.Properties.Where (o => o.Name == method.FormattedName).FirstOrDefault ();
			if (property != null) {
				property.GetMethod = method;
			} else {
				property = new Property (method.FormattedName, method.ReturnType) { GetMethod = method };
				klass.Properties.Add (property);
			}

			return true;
		}

		// if it's name starts with "set", does not return a value, and has one arg (besides this ptr)
		// and there is no other method with the same name...
		if (method.Name.ToLower ().StartsWith ("set") && method.ReturnType.Equals (CppTypes.Void) &&
			method.Parameters.Count == 1 && klass.Methods.Count (o => o.Name == method.Name) == 1) {
			string getterName = method.Name.Substring (3).TrimStart ('_').ToLower ();

			string pname = method.FormattedName.Substring (3);
			Property property = null;

			// ...AND there is a corresponding getter method that returns the right type, then assume it's a property setter
			bool doIt = false;
			property = klass.Properties.Where (o => o.Name == pname).FirstOrDefault ();
			if (property != null) {
				doIt = property.GetMethod != null && property.GetMethod.ReturnType.Equals (method.Parameters[0].Type);
			} else {
				Method getter = klass.Methods.Where (o => o.Name == getterName).FirstOrDefault ();
				doIt = getter != null && getter.ReturnType.Equals (method.Parameters[0].Type);
			}
			if (doIt) {
				if (property != null) {
					property.SetMethod = method;
				} else {
					property = new Property (pname, method.Parameters [0].Type) { SetMethod = method };
					klass.Properties.Add (property);
				}

				// set the method's arg name to "value" so that the prop setter works right
				var valueParam = method.Parameters[0];
				valueParam.Name = "value";

				return true;
			}
		}

		return false;
	}

	// Checks klass's primary base, primary base's primary base, and so on up the hierarchy
	public bool CheckPrimaryBases (Class klass, Func<Class, bool> predicate)
	{
		if (klass.BaseClasses.Count == 0)
			return false;
		var primaryBase = klass.BaseClasses [0];
		return predicate (primaryBase) || CheckPrimaryBases (primaryBase, predicate);
	}

	// Return a CppType for the type node N, return CppTypes.Unknown for unknown types
	CppType GetType (Node n) {
		return GetType (n, new CppType ());
	}

	CppType GetType (Node n, CppType modifiers) {
		var fundamental = CppTypes.Unknown;

		switch (n.Type) {
		case "Typedef":
			return GetType (GetTypeNode (n), modifiers);
		case "ArrayType":
			return GetType (GetTypeNode (n), modifiers.Modify (CppModifiers.Array));
		case "PointerType":
			return GetType (GetTypeNode (n), modifiers.Modify (CppModifiers.Pointer));
		case "ReferenceType":
			return GetType (GetTypeNode (n), modifiers.Modify (CppModifiers.Reference));
		case "FundamentalType":
			return modifiers.CopyTypeFrom (new CppType (n.Name));
		case "CvQualifiedType":
			if (n.IsTrue ("const"))
				return GetType (GetTypeNode (n), modifiers.Modify (CppModifiers.Const));
			else
				throw new NotImplementedException ();
		case "Class":
			fundamental = CppTypes.Class;
			break;
		case "Struct":
			fundamental = CppTypes.Struct;
			break;
		case "Enumeration":
			fundamental = CppTypes.Enum;
			break;
		default:
			return CppTypes.Unknown;
		}

		if (!NodeToNamespace.ContainsKey (n)) {
			// FIXME: Do something better
			return CppTypes.Unknown;
		}

		return modifiers.CopyTypeFrom (new CppType (fundamental, NodeToNamespace [n].FullyQualifiedName));
	}

	Node GetTypeNode (Node n) {
		return Node.IdToNode [n.Attributes ["type"]];
	}

	// Return the System.Type name corresponding to T, or null
	//  Returned as a string, because other wrappers do not have System.Types yet
	public string CppTypeToManaged (CppType t) {

		Type mtype = t.ToManagedType ();
		if (mtype != null && mtype != typeof (ICppObject)) {
			return mtype.FullName;
		}

		switch (t.ElementType) {

		case CppTypes.Class:
		case CppTypes.Struct:
		case CppTypes.Enum:

			var filter = GetFilterOrDefault (t);
			var qname = filter.TypeName.Replace ("::", ".");

			if (filter.ImplType == ImplementationType.@struct && !IsByVal (t))
				return qname + "&";
			else
				return qname;

		}

		return null;
	}

	void GenerateCode () {
		Directory.CreateDirectory (OutputDir);

		// Generate Libs file
		using (TextWriter w = File.CreateText (Path.Combine (OutputDir, "Libs.cs"))) {
			LibsTemplate.Generator = this;
			LibsTemplate.Libs = new[] { Lib };
			w.Write (LibsTemplate.TransformText ());
		}


		// Generate user types
		foreach (Namespace ns in Lib.Namespaces) {
			if (ns.ParentNamespace is Class)
				continue;

			var klass = ns as Class;
			if (klass != null) {
				if (klass.Disable)
					continue;

				using (TextWriter w = File.CreateText (Path.Combine (OutputDir, klass.Name + ".cs"))) {
					ClassTemplate.Generator = this;
					ClassTemplate.Class = klass;
					ClassTemplate.Nested = false;
					w.Write (ClassTemplate.TransformText ());
				}

				continue;
			}

			var @enum = ns as Enumeration;
			if (@enum != null) {

				using (TextWriter w = File.CreateText (Path.Combine (OutputDir, @enum.Name + ".cs"))) {
					EnumTemplate.Generator = this;
					EnumTemplate.Enum = @enum;
					EnumTemplate.Nested = false;
					w.Write (EnumTemplate.TransformText ());
				}

				continue;
			}

		}
	}

	static public bool IsByVal (CppType t)
	{
		return ((t.ElementType == CppTypes.Class || t.ElementType == CppTypes.Struct) &&
		        !t.Modifiers.Contains (CppModifiers.Pointer) &&
		        !t.Modifiers.Contains (CppModifiers.Reference) &&
		        !t.Modifiers.Contains (CppModifiers.Array));
	}

	Filter GetFilterOrDefault (Namespace ns)
	{
		return GetFilterOrDefault (ns.FullyQualifiedName);
	}

	Filter GetFilterOrDefault (CppType cpptype)
	{
		var fqn = cpptype.ElementTypeName;
		if (cpptype.Namespaces != null)
			fqn = string.Join ("::", cpptype.Namespaces) + "::" + fqn;

		var newtype = new CppType (fqn, cpptype.Modifiers.Where (m => m == CppModifiers.Template));
		return GetFilterOrDefault (newtype.ToString ().Replace (" ", ""));
	}

	Filter GetFilterOrDefault (string fqn)
	{
		Filter result;
		if (Filters != null && Filters.TryGetValue (fqn, out result))
			return result;

		return new Filter { TypeName = fqn, Mode = default_filter_mode };
	}
}
