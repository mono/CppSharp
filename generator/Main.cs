using System;
using NDesk.Options;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;

using Mono.VisualC.Code;
using Mono.VisualC.Code.Atoms;

using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace CPPInterop {
	class Generator {

		public string Source { get; set; }
		public Dictionary<string, string> Classes;
		public string Dir {get; set;}

		string templateClass;
		string templateStruct;
		string templateInterface;

		public static void Main (string[] args)
		{
			bool help = false;
			Generator gen = new Generator ();
			var p = new OptionSet () {
				{ "h|?|help", v => help = v != null },
				{ "f=", v => gen.Source = v },
				{ "o=", v => gen.Dir = v }
			};

			List<string> extra = null;
			try {
				extra = p.Parse(args);
			} catch (OptionException){
				Console.WriteLine ("Try `generator --help' for more information.");
				return;
			}

			if (gen.Source == null) {
				Console.Error.WriteLine ("-f required");
				return;
			}

			if (gen.Dir == null)
				gen.Dir = "output";
			Directory.CreateDirectory (gen.Dir);

			gen.Run ();
		}

		public Generator ()
		{
			Classes = new Dictionary<string, string>();
			templateClass = File.ReadAllText ("class.template");
			templateStruct = File.ReadAllText ("struct.template");
			templateInterface = File.ReadAllText ("interface.template");
		}

		XmlNode find (XmlNode root, XmlAttribute att)
		{
			if (att != null)
				return find (root, att.Value);
			return null;
		}

		XmlNode find (XmlNode root, string id)
		{
			XmlNode n = root.SelectSingleNode ("/GCC_XML/*[@id='" + id + "']");
			if (n.Name == "Typedef")
				return n;
			if (n.Attributes["type"] != null)
				return find (root, n.Attributes["type"].Value);
			return n;
		}

		public void Run ()
		{
			XmlDocument xmldoc = new XmlDocument ();
			xmldoc.Load (Source);
			XmlNodeList namespaces = xmldoc.SelectNodes ("/GCC_XML/Namespace[@name != '::' and @name != '' and @name != 'std']");
			XmlNodeList classes = xmldoc.SelectNodes ("/GCC_XML/Class[not(@incomplete)]");

			foreach (XmlNode clas in classes) {
				var f = xmldoc.SelectSingleNode ("/GCC_XML/File[@id='" + clas.Attributes["file"].Value + "']/@name");
				if (f != null && f.Value.StartsWith ("/"))
					continue;

				string name = clas.Attributes["name"].Value;
				if (Classes.ContainsKey (name))
					continue;

				var cu = new CodeUnit { ManagedNamespace = "Qt.Core" };
				var classAtom = new Class (name) { StaticCppLibrary = "Qt.Libs.QtCore" };
				cu.Atoms.AddLast (classAtom);

				string size = clas.Attributes["size"].Value;
				string members = clas.Attributes["members"].Value;

				//StringBuilder str = new StringBuilder();
				foreach (string id in members.Split(new char[]{' '})) {
					if (id.Equals (String.Empty))
						continue;
					XmlNode n = find (xmldoc.DocumentElement, id);

					switch (n.Name) {
						case "Method":
						break;
						default:
						continue;
					}

					if (n.Attributes["access"] == null || n.Attributes["access"].Value != "public")
						continue;

					//str.Append ("\t\t\t");
					string mname = n.Attributes["name"].Value;

					XmlNode ret = find (xmldoc.DocumentElement, n.Attributes["returns"]);

					string rett = ret.Attributes["name"].Value;
					bool virt = n.Attributes["virtual"] != null && n.Attributes["virtual"].Value == "1";
					bool stat = n.Attributes["static"] != null && n.Attributes["static"].Value == "1";

					//if (virt)
						//str.Append ("[Virtual] ");

					//str.Append (rett + " " + mname + " (CppInstancePtr @this");

					List<Method.Parameter> args = new List<Method.Parameter> ();
					var methodAtom = new Method (mname) {
						RetType = rett,
						IsVirtual = virt,
						IsStatic = stat,
						Parameters = args
					};

					int c = 0;
					foreach (XmlNode arg in n.SelectNodes ("Argument")) {
						string argname;
						if (arg.Attributes["name"] == null)
							argname = "arg" + c;
						else
							argname = arg.Attributes["name"].Value;

						XmlNode argt = find (xmldoc.DocumentElement, arg.Attributes["type"].Value);
						string argtype = argt.Attributes["name"].Value;
						//str.Append (", " + argtype + " " + argname);
						args.Add (new Method.Parameter { Name = argname, Type = argtype });

						// tee hee
						c++;
					}

					//str.AppendLine (");");
					classAtom.Atoms.AddLast (methodAtom);
				}

				Classes.Add (name, sanitize(name) + ".cs");

				FileStream fs = File.Create (Path.Combine (Dir, Classes[name]));
				StreamWriter sw = new StreamWriter(fs);

				/*
				StringBuilder sb = new StringBuilder();
				string strstruct = String.Format (templateStruct, name);
				string strinterface = String.Format (templateInterface, name, "", str.ToString());
				sb.Append (string.Format (templateClass,
				                          "Qt.Core",
				                          name,
				                          "ICppObject",
				                          strinterface,
				                          strstruct,
				                          size));
				sw.Write (sb.ToString());
				*/
				new CSharpCodeProvider ().GenerateCodeFromCompileUnit (cu.WrapperToCodeDom (), sw, new CodeGeneratorOptions ());

				sw.Flush ();
				sw.Close ();
			}
		}



		static string sanitize (string name)
		{
			return name.Replace ("<", "_").Replace (">", "_").Replace(":", "_").Replace("*", "_").Replace (",", "_").Replace(" ", "_");
		}
	}
}
