//
// Mono.VisualC.Interop.CppType.cs: Abstracts a C++ type declaration
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;

using Mono.VisualC.Interop.Util;

namespace Mono.VisualC.Interop {

	// These can be used anywhere a CppType could be used.
	public enum CppTypes {
		Unknown,
		Class,
		Struct,
		Enum,
		Union,
		Void,
		Bool,
		Char,
		Int,
		Float,
		Double,
		// for template type parameters
		Typename
	}

	public struct CppType {

		// FIXME: Passing these as delegates allows for the flexibility of doing processing on the
		//  type (i.e. to correctly mangle the function pointer arguments if the managed type is a delegate),
		//  however this does not make it very easy to override the default mappings at runtime.
		public static List<Func<CppType,Type>> CppTypeToManagedMap = new List<Func<CppType, Type>> () {
			(t) => (t.ElementType == CppTypes.Class ||
			        t.ElementType == CppTypes.Struct ||
			       (t.ElementType == CppTypes.Unknown && t.ElementTypeName != null)) &&
			       (t.Modifiers.Count (m => m == CppModifiers.Pointer) == 1 ||
			        t.Modifiers.Contains (CppModifiers.Reference))? typeof (ICppObject) : null,

			// void* gets IntPtr
			(t) => t.ElementType == CppTypes.Void && t.Modifiers.Contains (CppModifiers.Pointer)? typeof (IntPtr) : null,

			// single pointer to char gets string
			(t) => t.ElementType == CppTypes.Char && t.Modifiers.Count (m => m == CppModifiers.Pointer) == 1? typeof (string) : null,
			// pointer to pointer to char gets string[]
			(t) => t.ElementType == CppTypes.Char && t.Modifiers.Count (m => m == CppModifiers.Pointer) == 2? typeof (string).MakeArrayType () : null,

			// arrays
			(t) => t.Modifiers.Contains (CppModifiers.Array)? t.Subtract (CppModifiers.Array).ToManagedType ().MakeArrayType () : null,

			// convert single pointers to primatives to managed byref
			(t) => t.Modifiers.Count (m => m == CppModifiers.Pointer) == 1? t.Subtract (CppModifiers.Pointer).ToManagedType ().MakeByRefType () : null,
			// more than one level of indirection gets IntPtr type
			(t) => t.Modifiers.Contains (CppModifiers.Pointer)? typeof (IntPtr) : null,

			(t) => t.Modifiers.Contains (CppModifiers.Reference)? t.Subtract (CppModifiers.Reference).ToManagedType ().MakeByRefType () : null,

			(t) => t.ElementType == CppTypes.Int && t.Modifiers.Contains (CppModifiers.Short) && t.Modifiers.Contains (CppModifiers.Unsigned)? typeof (ushort) : null,
			(t) => t.ElementType == CppTypes.Int && t.Modifiers.Contains (CppModifiers.Long) && t.Modifiers.Contains (CppModifiers.Unsigned)? typeof (ulong) : null,
			(t) => t.ElementType == CppTypes.Int && t.Modifiers.Contains (CppModifiers.Short)? typeof (short) : null,
			(t) => t.ElementType == CppTypes.Int && t.Modifiers.Contains (CppModifiers.Long)? typeof (long) : null,
			(t) => t.ElementType == CppTypes.Int && t.Modifiers.Contains (CppModifiers.Unsigned)? typeof (uint) : null,

			(t) => t.ElementType == CppTypes.Void?   typeof (void)   : null,
			(t) => t.ElementType == CppTypes.Bool?   typeof (bool)   : null,
			(t) => t.ElementType == CppTypes.Char?   typeof (char)   : null,
			(t) => t.ElementType == CppTypes.Int?    typeof (int)    : null,
			(t) => t.ElementType == CppTypes.Float?  typeof (float)  : null,
			(t) => t.ElementType == CppTypes.Double? typeof (double) : null
		};

		public static List<Func<Type,CppType>> ManagedToCppTypeMap = new List<Func<Type,CppType>> () {
			(t) => typeof (void).Equals (t)  ? CppTypes.Void   : CppTypes.Unknown,
			(t) => typeof (bool).Equals (t)  ? CppTypes.Bool   : CppTypes.Unknown,
			(t) => typeof (char).Equals (t)  ? CppTypes.Char   : CppTypes.Unknown,
			(t) => typeof (int).Equals (t)   ? CppTypes.Int    : CppTypes.Unknown,
			(t) => typeof (float).Equals (t) ? CppTypes.Float  : CppTypes.Unknown,
			(t) => typeof (double).Equals (t)? CppTypes.Double : CppTypes.Unknown,

			(t) => typeof (short).Equals (t) ? new CppType (CppModifiers.Short, CppTypes.Int) : CppTypes.Unknown,
			(t) => typeof (long).Equals (t)  ? new CppType (CppModifiers.Long, CppTypes.Int)  : CppTypes.Unknown,
			(t) => typeof (uint).Equals (t)  ? new CppType (CppModifiers.Unsigned, CppTypes.Int) : CppTypes.Unknown,
			(t) => typeof (ushort).Equals (t)? new CppType (CppModifiers.Unsigned, CppModifiers.Short, CppTypes.Int) : CppTypes.Unknown,
			(t) => typeof (ulong).Equals (t)?  new CppType (CppModifiers.Unsigned, CppModifiers.Long, CppTypes.Int) : CppTypes.Unknown,

			// strings mangle as "const char*" by default
			(t) => typeof (string).Equals (t)? new CppType (CppModifiers.Const, CppTypes.Char, CppModifiers.Pointer) : CppTypes.Unknown,
			// StringBuilder gets "char*"
			(t) => typeof (StringBuilder).Equals (t)? new CppType (CppTypes.Char, CppModifiers.Pointer) : CppTypes.Unknown,

			// delegate types get special treatment
			(t) => typeof (Delegate).IsAssignableFrom (t)? CppType.ForDelegate (t) : CppTypes.Unknown,

			// ... and of course ICppObjects do too!
			// FIXME: We assume c++ class not struct. There should probably be an attribute
			//   we can apply to managed wrappers to indicate if the underlying C++ type is actually declared struct
			(t) => typeof (ICppObject).IsAssignableFrom (t)? new CppType (CppTypes.Class, t.Name, CppModifiers.Pointer) : CppTypes.Unknown,

			// convert managed type modifiers to C++ type modifiers like so:
			//  ref types to C++ references
			//  pointer types to C++ pointers
			//  array types to C++ arrays
			(t) => {
				CppType cppType = CppType.ForManagedType (t.GetElementType ());
				if (t.IsByRef) cppType.Modifiers.Add (CppModifiers.Reference);
				if (t.IsPointer) cppType.Modifiers.Add (CppModifiers.Pointer);
				if (t.IsArray) cppType.Modifiers.Add (CppModifiers.Array);
				return cppType;
			}
		};

		public CppTypes ElementType { get; set; }

		// if the ElementType is Union, Struct, Class, or Enum
		//  this will contain the name of said type
		public string ElementTypeName { get; set; }

		// this is initialized lazily to avoid unnecessary heap
		//  allocations if possible
		private List<CppModifiers> internalModifiers;
		public List<CppModifiers> Modifiers {
			get {
				if (internalModifiers == null)
					internalModifiers = new List<CppModifiers> ();

				return internalModifiers;
			}
		}

		// here, you can pass in things like "const char*" or "const Foo * const"
		//  DISCLAIMER: this is really just for convenience for now, and is not meant to be able
		//  to parse even moderately complex C++ type declarations.
		public CppType (string type) : this (Regex.Split (type, "\\s+(?![^\\>]*\\>|[^\\[\\]]*\\])"))
		{
		}

		public CppType (params object[] cppTypeSpec) : this ()
		{
			ElementType = CppTypes.Unknown;
			ElementTypeName = null;

			Parse (cppTypeSpec);
		}

		private void Parse (object [] parts)
		{
			foreach (object part in parts) {

				if (part is CppModifiers) {
					Modifiers.Add ((CppModifiers)part);
					continue;
				}

				if (part is CppModifiers []) {
					Modifiers.AddRange ((CppModifiers [])part);
					continue;
				}

				if (part is CppTypes) {
					ElementType = (CppTypes)part;
					continue;
				}

				Type managedType = part as Type;
				if (managedType != null) {
					CppType mapped = CppType.ForManagedType (managedType);
					ApplyTo (mapped);
					continue;
				}

				string strPart = part as string;
				if (strPart != null) {
					var parsed = CppModifiers.Parse (strPart);
					if (parsed.Any ()) {
						Modifiers.AddRange (parsed);
						strPart = CppModifiers.Remove (strPart);
					}

					// if we have something left, it must be a type name
					strPart = strPart.Trim ();
					if (strPart != "") {

						// FIXME: Use Enum.TryParse here if we decide to make this NET_4_0 only
						try {
							CppTypes cppTypes = (CppTypes)Enum.Parse (typeof (CppTypes), strPart, true);
							ElementType = cppTypes;
							continue;
						} catch { }

						// otherwise it is the element type name...
						ElementTypeName = strPart;
					}
				}
			}
		}

		// Applies the element type of the passed instance
		//  and combines its modifiers into this instance.
		//  Use when THIS instance may have attributes you want,
		//  but want the element type of the passed instance.
		public CppType ApplyTo (CppType type)
		{
			ElementType = type.ElementType;
			ElementTypeName = type.ElementTypeName;

			List<CppModifiers> oldModifiers = internalModifiers;
			internalModifiers = type.internalModifiers;

			if (oldModifiers != null)
				Modifiers.AddRange (oldModifiers);

			return this;
		}

		// Removes the modifiers on the passed instance from this instance
		public CppType Subtract (CppType type)
		{
			if (internalModifiers == null)
				return this;

			CppType current = this;
			foreach (var modifier in ((IEnumerable<CppModifiers>)type.Modifiers).Reverse ())
				current = current.Subtract (modifier);

			return current;
		}
		public CppType Subtract (CppModifiers modifier)
		{
			CppType newType = this;
			newType.internalModifiers = new List<CppModifiers> (((IEnumerable<CppModifiers>)newType.Modifiers).Reverse ().WithoutFirst (modifier));
			return newType;
		}

		// note: this adds modifiers "backwards" (it is mainly used by the generator)
		public CppType Modify (CppModifiers modifier)
		{
			CppType newType = this;
			var newModifier = new CppModifiers [] { modifier };

			if (newType.internalModifiers != null)
				newType.internalModifiers.AddFirst (newModifier);
			else
				newType.internalModifiers = new List<CppModifiers> (newModifier);

			return newType;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (obj.GetType () == typeof (CppTypes))
				return Equals (new CppType (obj));
			if (obj.GetType () != typeof (CppType))
				return false;
			CppType other = (CppType)obj;

			// FIXME: the order of some modifiers is not significant
			return (((internalModifiers == null || !internalModifiers.Any ()) &&
			         (other.internalModifiers == null || !other.internalModifiers.Any ())) ||
			        (internalModifiers != null && other.internalModifiers != null &&
			         internalModifiers.SequenceEqual (other.internalModifiers))) &&
			       ElementType == other.ElementType &&
			       ElementTypeName == other.ElementTypeName;
		}


		public override int GetHashCode ()
		{
			unchecked {
				return (internalModifiers != null? internalModifiers.SequenceHashCode () : 0) ^
				        ElementType.GetHashCode () ^
				       (ElementTypeName != null? ElementTypeName.GetHashCode () : 0);
			}
		}

		public override string ToString ()
		{
			StringBuilder cppTypeString = new StringBuilder ();

			if (ElementType != CppTypes.Unknown && ElementType != CppTypes.Typename)
				cppTypeString.Append (Enum.GetName (typeof (CppTypes), ElementType).ToLower ());

			if (ElementTypeName != null && ElementType != CppTypes.Typename) {
				if (cppTypeString.Length > 0)
					cppTypeString.Append (' ');
				cppTypeString.Append (ElementTypeName);
			}

			if (internalModifiers != null) {
				foreach (var modifier in internalModifiers) {
					string stringified = modifier.ToString ();

					if (cppTypeString.Length > 0)
						cppTypeString.Append (' ');
					cppTypeString.Append (stringified);
				}
			}

			return cppTypeString.ToString ();
		}

		public Type ToManagedType ()
		{
			CppType me = this;
			Type mappedType = (from checkType in CppTypeToManagedMap
			                   where checkType (me) != null
			                   select checkType (me)).FirstOrDefault ();

			return mappedType;
		}

		public static CppType ForManagedType (Type type)
		{

			CppType mappedType = (from checkType in ManagedToCppTypeMap
			                      where checkType (type).ElementType != CppTypes.Unknown
			                      select checkType (type)).FirstOrDefault ();

			return mappedType;
		}

		public static CppType ForDelegate (Type delType)
		{
			if (!typeof (Delegate).IsAssignableFrom (delType))
				throw new ArgumentException ("Argument must be a delegate type");

			throw new NotImplementedException ();
		}

		public static implicit operator CppType (CppTypes type) {
			return new CppType (type);
		}
	}
}

