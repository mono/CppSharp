//
// Mono.Cxxi.Util.ReflectionHelper.cs: Helper methods for common reflection API tasks
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010-2011 Alexander Corrado
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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Reflection.Emit;

namespace Mono.Cxxi.Util {
	internal static class ReflectionHelper {

		public static MethodInfo GetMethodInfoForDelegate (Type delType)
		{
			return delType.GetMethod ("Invoke");
		}

		public static Type[] GetDelegateParameterTypes (Type delType)
		{
			MethodInfo invoke = GetMethodInfoForDelegate (delType);
			if (invoke == null)
				return null;

			return GetMethodParameterTypes (invoke);
		}

		public static Type[] GetMethodParameterTypes (MethodInfo method)
		{
			ParameterInfo[] parameters = method.GetParameters ();
			Type[] parameterTypes = new Type [parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
				parameterTypes [i] = parameters [i].ParameterType;

			return parameterTypes;
		}

		public static void ApplyMethodParameterAttributes (MethodInfo source, MethodBuilder target, bool forPInvoke)
		{
			ParameterInfo[] parameters = source.GetParameters ();
			foreach (var param in parameters)
				ApplyAttributes (param, target.DefineParameter, forPInvoke);
		}

		public static void ApplyMethodParameterAttributes (MethodInfo source, DynamicMethod target, bool forPInvoke)
		{
			ParameterInfo[] parameters = source.GetParameters ();
			foreach (var param in parameters)
				ApplyAttributes (param, target.DefineParameter, forPInvoke);
		}

		public static ParameterBuilder ApplyAttributes (ParameterInfo param, Func<int,ParameterAttributes,string,ParameterBuilder> makePB, bool forPInvoke)
		{
			ParameterAttributes attr = param.Attributes;
			CustomAttributeBuilder marshalAsAttr = null;

			MarshalAsAttribute existingMarshalAs = param.GetCustomAttributes (typeof (MarshalAsAttribute),
																			  false).FirstOrDefault () as MarshalAsAttribute;

			/* if (forPInvoke &&
			   typeof (ICppObject).IsAssignableFrom (param.ParameterType) &&
			   !param.ParameterType.Equals (typeof (CppInstancePtr)) &&
			   existingMarshalAs == null)
			   {
			   ConstructorInfo ctor = typeof (MarshalAsAttribute).GetConstructor (new Type[] { typeof (UnmanagedType) });
			   object[] args = new object [] { UnmanagedType.CustomMarshaler };
			   FieldInfo[] fields = new FieldInfo [] { typeof (MarshalAsAttribute).GetField("MarshalTypeRef") };
			   object[] values = new object [] { typeof (CppObjectMarshaler) };

			   marshalAsAttr = new CustomAttributeBuilder (ctor, args, fields, values);
			   attr = attr | ParameterAttributes.HasFieldMarshal;
			   } else */ if (forPInvoke && existingMarshalAs != null) {
				// FIXME: This still doesn't feel like it's working right.. especially for virtual functions
				ConstructorInfo ctor = typeof (MarshalAsAttribute).GetConstructor (new Type[] { typeof (UnmanagedType) });
				object[] args = new object [] { existingMarshalAs.Value };

				var fields = from field in typeof (MarshalAsAttribute).GetFields ()
					where field.GetValue (existingMarshalAs) != null
					select field;

				var values = from field in fields select field.GetValue (existingMarshalAs);

				marshalAsAttr = new CustomAttributeBuilder (ctor, args, fields.ToArray (), values.ToArray ());
				attr = attr | ParameterAttributes.HasFieldMarshal;
			}
                         
			ParameterBuilder pb = makePB (param.Position, attr, param.Name);
			if (marshalAsAttr != null) pb.SetCustomAttribute (marshalAsAttr);
			return pb;
		}
	}
}
