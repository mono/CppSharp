using System;
using System.Collections.Generic;

namespace Cxxi
{
	public interface TypeTransform
	{
		void TransformType(Type type);
		void TransformTagType(TagType tag);
		void TransformArrayType(ArrayType array);
		void TransformFunctionType(FunctionType function);
		void TransformPointerType(PointerType pointer);
		void TransformBuiltinType(BuiltinType builtin);
		void TransformTypedefType (TypedefType typedef);
		void TransformDeclaration(Declaration declaration);
	}

	/// <summary>
	/// Represents a C++ type reference.
	/// </summary>
	public abstract class Type
	{
		public Type()
		{
		}

		public bool IsPrimitiveType(PrimitiveType primitive)
		{
			var builtin = this as BuiltinType;
			if (builtin != null)
				return builtin.Type == primitive;
			return false;
		}

		public bool IsPointerToPrimitiveType(PrimitiveType primitive)
		{
			var ptr = this as PointerType;
			if (ptr == null)
				return false;
			return ptr.Pointee.IsPrimitiveType(primitive);
		}

		public bool IsPointerTo<T>(out T type) where T : Type
		{
			var ptr = this as PointerType;
			
			if (ptr == null)
			{
				type = null;
				return false;
			}
			
			type = ptr.Pointee as T;
			return type != null;
		}

		public virtual void Transform(TypeTransform transform)
		{
			transform.TransformType(this);
		}

		// Converts the type to a C# type.
		public abstract string ToCSharp();

		public override string ToString()
		{
			return ToCSharp();
		}
	}

	/// <summary>
	/// Represents a C++ tag type reference.
	/// </summary>
	public class TagType : Type
	{
		public TagType()
		{
		}

		public Declaration Declaration;

		public override string ToCSharp()
		{
			if (Declaration == null)
				return string.Empty;
			return Declaration.Name;
		}

		public override void Transform(TypeTransform transform)
		{
			transform.TransformTagType(this);
		}
	}

	/// <summary>
	/// Represents an C/C++ array type.
	/// </summary>
	public class ArrayType : Type
	{
		public enum ArraySize
		{
			Constant,
			Variable
		}

		public ArrayType()
		{
		}

		// Type of the array elements.
		public Type Type;

		// Size type of array.
		public ArraySize SizeType;

		// In case of a constant size array.
		public long Size;

		public override string ToCSharp()
		{
			// C# only supports fixed arrays in unsafe sections
			// and they are constrained to a set of built-in types.
			
			return string.Format("{0}[]", Type);
		}

		public override void Transform(TypeTransform transform)
		{
			Type.Transform(transform);
		}
	}

	/// <summary>
	/// Represents an C/C++ function type.
	/// </summary>
	public class FunctionType : Type
	{
		// Return type of the function.
		public Type ReturnType;

		// Argument types.
		public List<Type> Arguments;

		public FunctionType()
		{
			Arguments = new List<Type>();
		}

		public override string ToCSharp()
		{
			string args = string.Empty;

			if (Arguments.Count > 0)
				args = ToArgumentString();

			if (ReturnType.IsPrimitiveType(PrimitiveType.Void))
			{
				if (!string.IsNullOrEmpty(args))
					args = string.Format("<{0}>", args);
				return string.Format("Action{0}", args);
			}

			if (!string.IsNullOrEmpty(args))
				args = string.Format(", {0}", args);

			return string.Format("Func<{0}{1}>",
				ReturnType.ToCSharp(), args);
		}

		public string ToArgumentString()
		{
			var s = string.Empty;

			for (int i = 0; i < Arguments.Count; ++i)
			{
				var arg = Arguments[i];
				s += arg.ToCSharp();
				if (i < Arguments.Count - 1)
					s += ", ";
			}

			return s;
		}

		public string ToDelegateString()
		{
			return string.Format("delegate {0} {{0}}({1})",
				ReturnType.ToCSharp(), ToArgumentString());
		}

		public override void Transform(TypeTransform transform)
		{
			ReturnType.Transform(transform);
		}
	}

	/// <summary>
	/// Represents a C++ pointer/reference type.
	/// </summary>
	public class PointerType : Type
	{
		public PointerType()
		{
		
		}

		/// <summary>
		/// Represents the modifiers on a C++ type reference.
		/// </summary>
		public enum TypeModifier
		{
			Value,
			Pointer,
			// L-value references
			LVReference,
			// R-value references
			RVReference
		}

		static string ConvertModifierToString(TypeModifier modifier)
		{
			switch (modifier)
			{
				case TypeModifier.Value: return string.Empty;
				case TypeModifier.Pointer:
				case TypeModifier.LVReference:
				case TypeModifier.RVReference: return "*";
			}

			return string.Empty;
		}

		public Type Pointee;

		public TypeModifier Modifier;

		public override string ToCSharp()
		{
			if (Pointee is FunctionType)
			{
				var function = Pointee as FunctionType;
				return function.ToCSharp();
			}

			if (Pointee is TagType)
				return Pointee.ToCSharp();

			return "IntPtr";

			//return string.Format("{0}{1}",
			//	Pointee.ToCSharp(), ConvertModifierToString(Modifier));
		}

		public override void Transform(TypeTransform transform)
		{
			Pointee.Transform(transform);
		}
	}

	public class TypedefType : Type
	{
		public TypedefType()
		{
		
		}

		public Declaration Declaration;

		public override void Transform(TypeTransform transform)
		{
			transform.TransformTypedefType(this);
		}

		public override string ToCSharp()
		{
			return Declaration.Name;
		}
	}

	#region Primitives

	/// <summary>
	/// Represents the C++ built-in types.
	/// </summary>
	public enum PrimitiveType
	{
		Null,
		Void,
		Bool,
		WideChar,
		Int8,
		UInt8,
		Int16,
		UInt16,
		Int32,
		UInt32,
		Int64,
		UInt64,
		Float,
		Double
	}

	/// <summary>
	/// Represents an instance of a C++ built-in type.
	/// </summary>
	public class BuiltinType : Type
	{
		public BuiltinType()
		{
		}

		public BuiltinType(PrimitiveType type)
		{
			Type = type;
		}

		// Primitive type of built-in type.
		public PrimitiveType Type;

		public override string ToCSharp()
		{
			return Type.ConvertToTypeName();
		}

		public override void Transform(TypeTransform transform)
		{
		}
	}

	public static class PrimitiveTypeExtensions
	{
		public static System.Type ConvertToType(this PrimitiveType Primitive)
		{
			switch (Primitive)
			{
				case PrimitiveType.Bool: return typeof(bool);
				case PrimitiveType.Void: return typeof(void);
				case PrimitiveType.WideChar: return typeof(char);
				case PrimitiveType.Int8: return typeof(sbyte);
				case PrimitiveType.UInt8: return typeof(byte);
				case PrimitiveType.Int16: return typeof(short);
				case PrimitiveType.UInt16: return typeof(ushort);
				case PrimitiveType.Int32: return typeof(int);
				case PrimitiveType.UInt32: return typeof(uint);
				case PrimitiveType.Int64: return typeof(long);
				case PrimitiveType.UInt64: return typeof(ulong);
				case PrimitiveType.Float: return typeof(float);
				case PrimitiveType.Double: return typeof(double);
			}

			return typeof(int);
		}

		public static string ConvertToTypeName(this PrimitiveType Primitive)
		{
			switch (Primitive)
			{
				case PrimitiveType.Bool: return "bool";
				case PrimitiveType.Void: return "void";
				case PrimitiveType.WideChar: return "char";
				case PrimitiveType.Int8: return "sbyte";
				case PrimitiveType.UInt8: return "byte";
				case PrimitiveType.Int16: return "short";
				case PrimitiveType.UInt16: return "ushort";
				case PrimitiveType.Int32: return "int";
				case PrimitiveType.UInt32: return "uint";
				case PrimitiveType.Int64: return "long";
				case PrimitiveType.UInt64: return "ulong";
				case PrimitiveType.Float: return "float";
				case PrimitiveType.Double: return "double";
			}

			return String.Empty;
		}
	}

	#endregion
}