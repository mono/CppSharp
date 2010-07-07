//
// Mono.VisualC.Interop.ABI.MsvcAbi.cs: An implementation of the Microsoft Visual C++ ABI
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

using Mono.VisualC.Interop;

namespace Mono.VisualC.Interop.ABI {
        public class MsvcAbi : CppAbi {
                public MsvcAbi ()
                {
                }

                public override CallingConvention GetCallingConvention (MethodInfo methodInfo)
		{
                        // FIXME: Varargs methods..?

			if (Modifiers.IsStatic (methodInfo))
				return CallingConvention.Cdecl;
			else
				return CallingConvention.ThisCall;
                }

                public override string GetMangledMethodName (MethodInfo methodInfo)
                {
                        string methodName = methodInfo.Name;
                        MethodType methodType = GetMethodType (methodInfo);
			ParameterInfo [] parameters = methodInfo.GetParameters ();

			StringBuilder nm = new StringBuilder ("?", 30);

			// FIXME: This has to include not only the name of the immediate containing class,
			//  but also all names of containing classes and namespaces up the hierarchy.
			nm.Append (methodName).Append ('@').Append (class_name).Append ("@@");

			// function modifiers are a matrix of consecutive uppercase letters
			// depending on access type and virtual (far)/static (far)/far modifiers

			// first, access type
			char funcModifier = 'Q'; // (public)
			if (Modifiers.IsProtected (methodInfo))
				funcModifier = 'I';
			else if (Modifiers.IsPrivate (methodInfo))
				funcModifier = 'A';

			// now, offset based on other modifiers
			if (Modifiers.IsStatic (methodInfo))
				funcModifier += 2;
			else if (Modifiers.IsVirtual (methodInfo)) // (do we need this?)
				funcModifier += 4;

			nm.Append (funcModifier);

			// FIXME: deal with storage classes for "this" i.e. the "const" in -> int foo () const;
			if (!Modifiers.IsStatic (methodInfo))
				nm.Append ('A');

			switch (GetCallingConvention (methodInfo)) {
			case CallingConvention.Cdecl:
				nm.Append ('A');
				break;
			case CallingConvention.ThisCall:
				nm.Append ('E');
				break;
			case CallingConvention.StdCall:
				nm.Append ('G');
				break;
			case CallingConvention.FastCall:
				nm.Append ('I');
				break;
			}

			// FIXME: handle const, volatile modifiers on return type
			nm.Append ("?A");

                }

        }
}

