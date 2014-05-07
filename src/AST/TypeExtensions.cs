﻿namespace CppSharp.AST.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsPrimitiveType(this Type t)
        {
            PrimitiveType type;
            return t.IsPrimitiveType(out type);
        }

        public static bool IsPrimitiveType(this Type t, out PrimitiveType primitive)
        {
            var builtin = t.Desugar() as BuiltinType;
            if (builtin != null)
            {
                primitive = builtin.Type;
                return true;
            }

            primitive = PrimitiveType.Null;
            return false;
        }

        public static bool IsPrimitiveType(this Type t, PrimitiveType primitive)
        {
            PrimitiveType type;
            if (!t.IsPrimitiveType(out type))
                return false;

            return primitive == type;
        }

        public static bool IsEnumType(this Type t)
        {
            var tag = t as TagType;
            
            if (tag == null)
                return false;

            return tag.Declaration is Enumeration;
        }

        public static bool IsAddress(this Type t)
        {
            return t.IsPointer() || t.IsReference();
        }

        public static bool IsPointer(this Type t)
        {
            var functionPointer = t as MemberPointerType;
            if (functionPointer != null)
                return true;
            var pointer = t as PointerType;
            if (pointer == null)
                return false;
            return pointer.Modifier == PointerType.TypeModifier.Pointer;
        }

        public static bool IsReference(this Type t)
        {
            var pointer = t as PointerType;
            if (pointer == null)
                return false;
            return pointer.IsReference;
        }

        public static bool IsPointerToPrimitiveType(this Type t)
        {
            var ptr = t as PointerType;
            if (ptr == null)
                return false;
            PrimitiveType primitiveType;
            return ptr.Pointee.IsPrimitiveType(out primitiveType);
        }

        public static bool IsPointerToPrimitiveType(this Type t, out PrimitiveType primitive)
        {
            var ptr = t as PointerType;
            if (ptr == null)
            {
                primitive = PrimitiveType.Null;
                return false;
            }
            return ptr.Pointee.IsPrimitiveType(out primitive);
        }

        public static bool IsPointerToPrimitiveType(this Type t, PrimitiveType primitive)
        {
            var ptr = t as PointerType;
            if (ptr == null)
                return false;
            return ptr.Pointee.IsPrimitiveType(primitive);
        }

        public static bool IsPointerTo<T>(this Type t, out T type) where T : Type
        {
            var pointee = t.GetPointee();
            type = pointee as T;
            if (type == null)
            {
                var attributedType = pointee as AttributedType;
                if (attributedType != null)
                    type = attributedType.Modified.Type as T;
            }
            return type != null;
        }

        public static bool IsClass(this Type t)
        {
            Class @class;
            return t.TryGetClass(out @class);
        }

        public static bool TryGetClass(this Type t, out Class @class)
        {
            t = t.Desugar();

            var tag = t as TagType;
            if (tag != null)
            {
                @class = tag.Declaration as Class;
                return @class != null;
            }

            var type = t as TemplateSpecializationType;
            if (type != null)
            {
                var templatedClass = ((ClassTemplate)type.Template).TemplatedClass;
                @class = templatedClass.CompleteDeclaration == null
                    ? templatedClass
                    : (Class)templatedClass.CompleteDeclaration;
                return @class != null;
            }

            @class = null;
            return false;
        }

        public static bool IsEnum(this Type t)
        {
            Enumeration @enum;
            return t.TryGetEnum(out @enum);
        }

        public static bool TryGetEnum(this Type t, out Enumeration @enum)
        {
            var tag = t.Desugar() as TagType;

            if (tag == null)
            {
                @enum = null;
                return false;
            }

            @enum = tag.Declaration as Enumeration;
            return @enum != null;
        }

        public static Type Desugar(this Type t)
        {
            var type = t as TypedefType;

            if (type != null)
            {
                var decl = type.Declaration.Type;

                if (decl != null)
                    return decl.Desugar();
            }

            return t;
        }

        public static Type SkipPointerRefs(this Type t)
        {
            var type = t as PointerType;

            if (type != null)
            {
                var pointee = type.Pointee;

                if (type.IsReference())
                    return pointee.Desugar().SkipPointerRefs();
            }

            return t;
        }

        /// <summary>
        /// If t is a pointer type the type pointed to by t will be returned.
        /// Otherwise null.
        /// </summary>
        public static Type GetPointee(this Type t)
        {
            var ptr = t as PointerType;
            if (ptr != null)
                return ptr.Pointee;
            var memberPtr = t as MemberPointerType;
            if (memberPtr != null)
                return memberPtr.Pointee;
            return null;
        }

        /// <summary>
        /// If t is a pointer type the type pointed to by t will be returned
        /// after fully dereferencing it. Otherwise null.
        /// For example int** -> int.
        /// </summary>
        public static Type GetFinalPointee(this Type t)
        {
            var finalPointee = t.GetPointee();
            var pointee = finalPointee;
            while (pointee != null)
            {
                pointee = pointee.GetPointee();
                if (pointee != null)
                    finalPointee = pointee;
            }
            return finalPointee;
        }
    }
}