using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Mono.VisualC.Code;
using Mono.VisualC.Code.Atoms;

namespace CPPInterop {

	public static class Postfixes {
		public static readonly List<IPostfix> List = new List<IPostfix> () {
			new DuplicateMemberFix ()
		};
	}

	public interface IPostfix {
		bool TryFix (string errorMessage, Generator gen);

	}

	public class DuplicateMemberFix : IPostfix {
		private static readonly Regex ID = new Regex("The type `(.+)' already contains a definition for `(.+)'");

		public DuplicateMemberFix ()
		{
		}

		public bool TryFix (string errorMessage, Generator gen)
		{
			Match m = ID.Match (errorMessage);
			if (!m.Success)
				return false;

			string typeName = m.Groups [1].Value;
			string oldMember = m.Groups [2].Value;
			string newMember = oldMember + "2";
			bool memberFound = false;

			CodeContainer type = gen.GetPath (typeName.Split ('.').Skip (1).ToArray ());

			foreach (var method in type.Atoms.OfType<Method> ()) {
				if (method.FormattedName == oldMember) {
					method.FormattedName = newMember;
					memberFound = true;
					break;
				}
			}

			if (!memberFound) {
				foreach (var prop in type.Atoms.OfType<Property> ()) {
					if (prop.Name == oldMember) {
						prop.Name = newMember;
						memberFound = true;
						break;
					}
				}
			}

			if (memberFound) {
				//Console.WriteLine ("Renaming \"{0}\" to \"{1}\" to prevent name collision in class \"{2}\" ...", oldMember, newMember, typeName);
				gen.Save ();
			}

			return memberFound;
		}

	}
}

