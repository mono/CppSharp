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
            return TryGetDeclaration(t, out @class);
        }

        public static bool TryGetDeclaration<T>(this Type t, out T decl) where T : Declaration
        {
            t = t.Desugar();

            TagType tagType = null;
            var type = t as TemplateSpecializationType;
            if (type != null)
            {
                if (type.IsDependent)
                {
                    if (type.Template is TypeAliasTemplate)
                    {
                        Class @class;
                        type.Desugared.Type.TryGetClass(out @class);
                        decl = @class as T;
                        return decl != null;
                    }

                    var classTemplate = type.Template as ClassTemplate;
                    if (classTemplate != null)
                    {
                        var templatedClass = classTemplate.TemplatedClass;
                        decl = templatedClass.CompleteDeclaration == null
                            ? templatedClass as T
                            : (T) templatedClass.CompleteDeclaration;
                        return decl != null;
                    }

                    var templateTemplateParameter = type.Template as TemplateTemplateParameter;
                    if (templateTemplateParameter != null)
                        return (decl = templateTemplateParameter.TemplatedDecl as T) != null;
                }
                tagType = (TagType) (type.Desugared.Type.GetFinalPointee() ?? type.Desugared.Type);
            }
            else
            {
                tagType = t as TagType;
            }

            if (tagType != null)
            {
                decl = tagType.Declaration as T;
                return decl != null;
            }

            decl = null;
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
            var typeDef = t as TypedefType;
            if (typeDef != null)
            {
                var decl = typeDef.Declaration.Type;
                if (decl != null)
                    return decl.Desugar();
            }

            var substType = t as TemplateParameterSubstitutionType;
            if (substType != null)
            {
                var replacement = substType.Replacement.Type;
                if (replacement != null)
                    return replacement.Desugar();
            }

            var attributedType = t as AttributedType;
            if (attributedType != null)
                return attributedType.Equivalent.Type.Desugar();

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
                return memberPtr.QualifiedPointee.Type;
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

        public static PointerType GetFinalPointer(this Type t)
        {
            var type = t as PointerType;

            if (type == null)
                return null;
            
            var pointee = type.Desugar().GetPointee();

            if (pointee.IsPointer())
                return pointee.GetFinalPointer();

            return type;
        }

        public static bool ResolvesTo(this QualifiedType type, QualifiedType other)
        {
            if (!type.Qualifiers.Equals(other.Qualifiers))
                return false;

            var left = type.Type.Desugar();
            var right = other.Type.Desugar();
            var leftPointer = left as PointerType;
            var rightPointer = right as PointerType;
            if (leftPointer != null && rightPointer != null)
            {
                return leftPointer.Modifier == rightPointer.Modifier &&
                    leftPointer.QualifiedPointee.ResolvesTo(rightPointer.QualifiedPointee);
            }
            return left.Equals(right);
        }
    }
}