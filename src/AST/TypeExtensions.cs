namespace CppSharp.AST.Extensions
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
            var ptr = t as PointerType;
            
            if (ptr == null)
            {
                var functionPointer = t as MemberPointerType;
                if (functionPointer != null)
                {
                    type = functionPointer.Pointee as T;
                    return type != null;
                }
                type = null;
                return false;
            }
            
            type = ptr.Pointee as T;
            return type != null;
        }

        public static bool IsTagDecl<T>(this Type t, out T decl) where T : Declaration
        {
            var tag = t.Desugar() as TagType;
            
            if (tag == null)
            {
                decl = null;
                return false;
            }

            decl = tag.Declaration as T;
            return decl != null;
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

        public static Type GetFinalPointee(this PointerType pointer)
        {
            var pointee = pointer.Pointee;
            while (pointee.IsPointer())
            {
                var p = pointee as PointerType;
                if (p != null)
                    pointee = p.Pointee;
                else
                    return GetFinalPointee(pointee as MemberPointerType);
            }
            return pointee;
        }

        public static Type GetFinalPointee(this MemberPointerType pointer)
        {
            var pointee = pointer.Pointee;
            while (pointee.IsPointer())
            {
                var p = pointee as MemberPointerType;
                if (p != null)
                    pointee = p.Pointee;
                else
                    return GetFinalPointee(pointee as PointerType);
            }
            return pointee;
        }
    }
}