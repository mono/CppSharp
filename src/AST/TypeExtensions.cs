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
            var tag = t.Desugar() as TagType;

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

        public static bool IsPointerToEnum(this Type t)
        {
            var ptr = t as PointerType;
            if (ptr == null)
                return false;
            return ptr.Pointee.IsEnumType();
        }

        public static bool IsPointerToEnum(this Type t, out Enumeration @enum)
        {
            var ptr = t as PointerType;
            if (ptr == null)
            {
                @enum = null;
                return false;
            }
            return ptr.Pointee.TryGetEnum(out @enum);
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

        public static bool TryGetClass(this Type t, out Class @class, Class value = null)
        {
            return TryGetDeclaration(t, out @class, value);
        }

        public static bool TryGetDeclaration<T>(this Type t, out T decl, T value = null) where T : Declaration
        {
            t = t.Desugar();

            TagType tagType = null;
            if (t is TemplateSpecializationType type)
            {
                if (type.IsDependent)
                {
                    switch (type.Template)
                    {
                        case TypeAliasTemplate _:
                            type.Desugared.Type.TryGetDeclaration(out decl, value);
                            return decl != null;
                        case ClassTemplate classTemplate:
                            {
                                var templatedClass = classTemplate.TemplatedClass;
                                decl = templatedClass.CompleteDeclaration == null
                                    ? templatedClass as T
                                    : (T)templatedClass.CompleteDeclaration;

                                if (decl == null)
                                    return false;

                                if (value != null)
                                    type.Template = new ClassTemplate { TemplatedDecl = value };

                                return true;
                            }
                        case TemplateTemplateParameter templateTemplateParameter:
                            return (decl = templateTemplateParameter.TemplatedDecl as T) != null;
                    }
                }
                tagType = (type.Desugared.Type.GetFinalPointee() ?? type.Desugared.Type) as TagType;
            }
            else
            {
                tagType = t as TagType;
            }

            if (tagType != null)
            {
                decl = tagType.Declaration as T;
                if (decl != null)
                {
                    if (value != null)
                        tagType.Declaration = value;
                    return true;
                }
                return false;
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

        public static Type Desugar(this Type t, bool resolveTemplateSubstitution = true)
        {
            var typeDef = t as TypedefType;
            if (typeDef != null)
            {
                var decl = typeDef.Declaration.Type;
                if (decl != null)
                    return decl.Desugar(resolveTemplateSubstitution);
            }

            if (resolveTemplateSubstitution)
            {
                var substType = t as TemplateParameterSubstitutionType;
                if (substType != null)
                {
                    var replacement = substType.Replacement.Type;
                    if (replacement != null)
                        return replacement.Desugar(resolveTemplateSubstitution);
                }
            }

            var injectedType = t as InjectedClassNameType;
            if (injectedType != null)
            {
                if (injectedType.InjectedSpecializationType.Type != null)
                    return injectedType.InjectedSpecializationType.Type.Desugar(
                        resolveTemplateSubstitution);
                return new TagType(injectedType.Class);
            }

            var attributedType = t as AttributedType;
            if (attributedType != null)
                return attributedType.Equivalent.Type.Desugar(resolveTemplateSubstitution);

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

        /// <summary>
        /// If t is a pointer type the type pointed to by t will be returned.
        /// Otherwise the default qualified type.
        /// </summary>
        public static QualifiedType GetQualifiedPointee(this Type t)
        {
            var ptr = t as PointerType;
            if (ptr != null)
                return ptr.QualifiedPointee;
            var memberPtr = t as MemberPointerType;
            if (memberPtr != null)
                return memberPtr.QualifiedPointee;
            return new QualifiedType();
        }

        /// <summary>
        /// If t is a pointer type the type pointed to by t will be returned
        /// after fully dereferencing it. Otherwise the default qualified type.
        /// For example int** -> int.
        /// </summary>
        public static QualifiedType GetFinalQualifiedPointee(this Type t)
        {
            var finalPointee = t.GetQualifiedPointee();
            var pointee = finalPointee;
            while (pointee.Type != null)
            {
                pointee = pointee.Type.GetQualifiedPointee();
                if (pointee.Type != null)
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

        public static bool IsConstRef(this QualifiedType type)
        {
            Type desugared = type.Type.Desugar();
            return desugared.IsReference() && type.IsConst();
        }

        public static bool IsConstRefToPrimitive(this QualifiedType type)
        {
            Type desugared = type.Type.Desugar();
            Type pointee = desugared.GetFinalPointee().Desugar();
            pointee = (pointee.GetFinalPointee() ?? pointee).Desugar();
            return desugared.IsReference() &&
                (pointee.IsPrimitiveType() || pointee.IsEnum()) && type.IsConst();
        }

        public static bool IsConst(this QualifiedType type)
        {
            return type.Type != null && (type.Qualifiers.IsConst ||
                type.Type.GetQualifiedPointee().IsConst());
        }

        public static QualifiedType StripConst(this QualifiedType type)
        {
            var qualifiers = type.Qualifiers;
            qualifiers.IsConst = false;
            type.Qualifiers = qualifiers;

            var ptr = type.Type as PointerType;
            if (ptr != null)
            {
                var pointee = ptr.QualifiedPointee;
                var pointeeQualifiers = pointee.Qualifiers;
                pointeeQualifiers.IsConst = false;
                pointee.Qualifiers = pointeeQualifiers;
                ptr.QualifiedPointee = pointee;
            }

            return type;
        }

        public static bool IsConstCharString(this Type type)
        {
            var desugared = type.Desugar();

            if (!(desugared is PointerType))
                return false;

            var pointer = desugared as PointerType;
            return IsConstCharString(pointer);
        }

        public static bool IsConstCharString(this PointerType pointer)
        {
            if (pointer.IsReference)
                return false;

            var pointee = pointer.Pointee.Desugar();

            return (pointee.IsPrimitiveType(PrimitiveType.Char) ||
                    pointee.IsPrimitiveType(PrimitiveType.Char16) ||
                    pointee.IsPrimitiveType(PrimitiveType.Char32) ||
                    pointee.IsPrimitiveType(PrimitiveType.WideChar)) &&
                    pointer.QualifiedPointee.Qualifiers.IsConst;
        }

        public static bool IsDependentPointer(this Type type)
        {
            var desugared = type.Desugar();
            if (desugared.IsAddress())
            {
                var pointee = desugared.GetFinalPointee().Desugar();
                return pointee.IsDependent
                    && !(pointee is TemplateSpecializationType)
                    && !(pointee is InjectedClassNameType);
            }
            return false;
        }

        public static Module GetModule(this Type type)
        {
            Declaration declaration;
            if (!(type.GetFinalPointee() ?? type).TryGetDeclaration(out declaration))
                return null;

            declaration = declaration.CompleteDeclaration ?? declaration;
            if (declaration.Namespace == null || declaration.TranslationUnit.Module == null)
                return null;

            return declaration.TranslationUnit.Module;
        }

        public static long GetSizeInBytes(this ArrayType array)
        {
            return GetSizeInBits(array) / 8;
        }

        public static long GetSizeInBits(this ArrayType array)
        {
            return array.Size * array.ElementSize;
        }

        internal static bool TryGetReferenceToPtrToClass(this Type type, out Type classType)
        {
            classType = null;
            var @ref = type.Desugar().AsLvReference();
            if (@ref == null)
                return false;

            var @ptr = @ref.Pointee.Desugar(false).AsPtr();
            if (@ptr == null)
                return false;

            var @class = @ptr.Pointee;
            if (!@class.IsClass())
                return false;

            classType = @class;
            return true;
        }

        internal static PointerType AsLvReference(this Type type)
        {
            var reference = type as PointerType;
            return reference?.Modifier == PointerType.TypeModifier.LVReference ? reference : null;
        }

        internal static PointerType AsPtr(this Type type)
        {
            var ptr = type as PointerType;
            return ptr?.Modifier == PointerType.TypeModifier.Pointer ? ptr : null;
        }
    }
}