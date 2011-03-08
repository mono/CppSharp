// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//   Zoltan Varga <vargaz@gmail.com>
//
// Copyright (C) 2011 Novell Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Mono.VisualC.Code;
using Mono.VisualC.Code.Atoms;

namespace Mono.VisualC.Tools.Generator {

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

