using System;
using System.Collections.Generic;
using SourceLocation = CppSharp.AST.SourceLocation;
using CppSharp.Parser.AST;
using CppSharp.AST.Extensions;

namespace CppSharp
{
    #region Parser AST visitors

    /// <summary>
    /// Implements the visitor pattern for the generated type bindings.
    /// </summary>
    public abstract class TypeVisitor<TRet>
    {
        public abstract TRet VisitTag(TagType type);
        public abstract TRet VisitArray(ArrayType type);
        public abstract TRet VisitFunction(FunctionType type);
        public abstract TRet VisitPointer(PointerType type);
        public abstract TRet VisitMemberPointer(MemberPointerType type);
        public abstract TRet VisitTypedef(TypedefType type);
        public abstract TRet VisitAttributed(AttributedType type);
        public abstract TRet VisitDecayed(DecayedType type);
        public abstract TRet VisitTemplateSpecialization(TemplateSpecializationType type);
        public abstract TRet VisitDependentTemplateSpecialization(DependentTemplateSpecializationType type);
        public abstract TRet VisitTemplateParameter(TemplateParameterType type);
        public abstract TRet VisitTemplateParameterSubstitution(TemplateParameterSubstitutionType type);
        public abstract TRet VisitInjectedClassName(InjectedClassNameType type);
        public abstract TRet VisitDependentName(DependentNameType type);
        public abstract TRet VisitBuiltin(BuiltinType type);
        public abstract TRet VisitPackExpansion(PackExpansionType type);
        public abstract TRet VisitUnaryTransform(UnaryTransformType type);
        public abstract TRet VisitUnresolvedUsing(UnresolvedUsingType type);
        public abstract TRet VisitVector(VectorType type);

        public TRet Visit(Parser.AST.Type type)
        {
            if (type == null)
                return default(TRet);

            switch (type.Kind)
            {
                case TypeKind.Tag:
                    {
                        var _type = TagType.__CreateInstance(type.__Instance);
                        return VisitTag(_type);
                    }
                case TypeKind.Array:
                    {
                        var _type = ArrayType.__CreateInstance(type.__Instance);
                        return VisitArray(_type);
                    }
                case TypeKind.Function:
                    {
                        var _type = FunctionType.__CreateInstance(type.__Instance);
                        return VisitFunction(_type);
                    }
                case TypeKind.Pointer:
                    {
                        var _type = PointerType.__CreateInstance(type.__Instance);
                        return VisitPointer(_type);
                    }
                case TypeKind.MemberPointer:
                    {
                        var _type = MemberPointerType.__CreateInstance(type.__Instance);
                        return VisitMemberPointer(_type);
                    }
                case TypeKind.Typedef:
                    {
                        var _type = TypedefType.__CreateInstance(type.__Instance);
                        return VisitTypedef(_type);
                    }
                case TypeKind.Attributed:
                    {
                        var _type = AttributedType.__CreateInstance(type.__Instance);
                        return VisitAttributed(_type);
                    }
                case TypeKind.Decayed:
                    {
                        var _type = DecayedType.__CreateInstance(type.__Instance);
                        return VisitDecayed(_type);
                    }
                case TypeKind.TemplateSpecialization:
                    {
                        var _type = TemplateSpecializationType.__CreateInstance(type.__Instance);
                        return VisitTemplateSpecialization(_type);
                    }
                case TypeKind.DependentTemplateSpecialization:
                    {
                        var _type = DependentTemplateSpecializationType.__CreateInstance(type.__Instance);
                        return VisitDependentTemplateSpecialization(_type);
                    }
                case TypeKind.TemplateParameter:
                    {
                        var _type = TemplateParameterType.__CreateInstance(type.__Instance);
                        return VisitTemplateParameter(_type);
                    }
                case TypeKind.TemplateParameterSubstitution:
                    {
                        var _type = TemplateParameterSubstitutionType.__CreateInstance(type.__Instance);
                        return VisitTemplateParameterSubstitution(_type);
                    }
                case TypeKind.InjectedClassName:
                    {
                        var _type = InjectedClassNameType.__CreateInstance(type.__Instance);
                        return VisitInjectedClassName(_type);
                    }
                case TypeKind.DependentName:
                    {
                        var _type = DependentNameType.__CreateInstance(type.__Instance);
                        return VisitDependentName(_type);
                    }
                case TypeKind.Builtin:
                    {
                        var _type = BuiltinType.__CreateInstance(type.__Instance);
                        return VisitBuiltin(_type);
                    }
                case TypeKind.PackExpansion:
                    {
                        var _type = PackExpansionType.__CreateInstance(type.__Instance);
                        return VisitPackExpansion(_type);
                    }
                case TypeKind.UnaryTransform:
                    {
                        var _type = UnaryTransformType.__CreateInstance(type.__Instance);
                        return VisitUnaryTransform(_type);
                    }
                case TypeKind.UnresolvedUsing:
                    {
                        var _type = UnresolvedUsingType.__CreateInstance(type.__Instance);
                        return VisitUnresolvedUsing(_type);
                    }
                case TypeKind.Vector:
                    {
                        var _type = VectorType.__CreateInstance(type.__Instance);
                        return VisitVector(_type);
                    }
            }

            throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Implements the visitor pattern for the generated decl bindings.
    /// </summary>
    public abstract class DeclVisitor<TRet>
    {
        public abstract TRet VisitTranslationUnit(TranslationUnit decl);
        public abstract TRet VisitNamespace(Namespace decl);
        public abstract TRet VisitTypedef(TypedefDecl decl);
        public abstract TRet VisitTypeAlias(TypeAlias decl);
        public abstract TRet VisitParameter(Parameter decl);
        public abstract TRet VisitFunction(Function decl);
        public abstract TRet VisitMethod(Method decl);
        public abstract TRet VisitEnumeration(Enumeration decl);
        public abstract TRet VisitEnumerationItem(Enumeration.Item decl);
        public abstract TRet VisitVariable(Variable decl);
        public abstract TRet VisitFriend(Friend decl);
        public abstract TRet VisitField(Field decl);
        public abstract TRet VisitAccessSpecifier(AccessSpecifierDecl decl);
        public abstract TRet VisitClass(Class decl);
        public abstract TRet VisitTypeAliasTemplate(TypeAliasTemplate decl);
        public abstract TRet VisitClassTemplate(ClassTemplate decl);
        public abstract TRet VisitClassTemplateSpecialization(
            ClassTemplateSpecialization decl);
        public abstract TRet VisitClassTemplatePartialSpecialization(
            ClassTemplatePartialSpecialization decl);
        public abstract TRet VisitFunctionTemplate(FunctionTemplate decl);
        public abstract TRet VisitVarTemplate(VarTemplate decl);
        public abstract TRet VisitVarTemplateSpecialization(
            VarTemplateSpecialization decl);
        public abstract TRet VisitVarTemplatePartialSpecialization(
            VarTemplatePartialSpecialization decl);
        public abstract TRet VisitTemplateTemplateParameter(TemplateTemplateParameter decl);
        public abstract TRet VisitTypeTemplateParameter(TypeTemplateParameter decl);
        public abstract TRet VisitNonTypeTemplateParameter(NonTypeTemplateParameter decl);
        public abstract TRet VisitUnresolvedUsingTypename(UnresolvedUsingTypename decl);

        public virtual TRet Visit(Parser.AST.Declaration decl)
        {
            switch (decl.Kind)
            {
                case DeclarationKind.TranslationUnit:
                    {
                        var _decl = TranslationUnit.__CreateInstance(decl.__Instance);
                        return VisitTranslationUnit(_decl);
                    }
                case DeclarationKind.Namespace:
                    {
                        var _decl = Namespace.__CreateInstance(decl.__Instance);
                        return VisitNamespace(_decl);
                    }
                case DeclarationKind.Typedef:
                    {
                        var _decl = TypedefDecl.__CreateInstance(decl.__Instance);
                        return VisitTypedef(_decl);
                    }
                case DeclarationKind.TypeAlias:
                    {
                        var _decl = TypeAlias.__CreateInstance(decl.__Instance);
                        return VisitTypeAlias(_decl);
                    }
                case DeclarationKind.Parameter:
                    {
                        var _decl = Parameter.__CreateInstance(decl.__Instance);
                        return VisitParameter(_decl);
                    }
                case DeclarationKind.Function:
                    {
                        var _decl = Function.__CreateInstance(decl.__Instance);
                        return VisitFunction(_decl);
                    }
                case DeclarationKind.Method:
                    {
                        var _decl = Method.__CreateInstance(decl.__Instance);
                        return VisitMethod(_decl);
                    }
                case DeclarationKind.Enumeration:
                    {
                        var _decl = Enumeration.__CreateInstance(decl.__Instance);
                        return VisitEnumeration(_decl);
                    }
                case DeclarationKind.EnumerationItem:
                    {
                        var _decl = Enumeration.Item.__CreateInstance(decl.__Instance);
                        return VisitEnumerationItem(_decl);
                    }
                case DeclarationKind.Variable:
                    {
                        var _decl = Variable.__CreateInstance(decl.__Instance);
                        return VisitVariable(_decl);
                    }
                case DeclarationKind.VarTemplate:
                    {
                        var _decl = VarTemplate.__CreateInstance(decl.__Instance);
                        return VisitVarTemplate(_decl);
                    }
                case DeclarationKind.VarTemplateSpecialization:
                    {
                        var _decl = VarTemplateSpecialization.__CreateInstance(decl.__Instance);
                        return VisitVarTemplateSpecialization(_decl);
                    }
                case DeclarationKind.VarTemplatePartialSpecialization:
                    {
                        var _decl = VarTemplatePartialSpecialization.__CreateInstance(decl.__Instance);
                        return VisitVarTemplatePartialSpecialization(_decl);
                    }
                case DeclarationKind.Friend:
                    {
                        var _decl = Friend.__CreateInstance(decl.__Instance);
                        return VisitFriend(_decl);
                    }
                case DeclarationKind.Field:
                    {
                        var _decl = Field.__CreateInstance(decl.__Instance);
                        return VisitField(_decl);
                    }
                case DeclarationKind.AccessSpecifier:
                    {
                        var _decl = AccessSpecifierDecl.__CreateInstance(decl.__Instance);
                        return VisitAccessSpecifier(_decl);
                    }
                case DeclarationKind.Class:
                    {
                        var _decl = Class.__CreateInstance(decl.__Instance);
                        return VisitClass(_decl);
                    }
                case DeclarationKind.ClassTemplate:
                    {
                        var _decl = ClassTemplate.__CreateInstance(decl.__Instance);
                        return VisitClassTemplate(_decl);
                    }
                case DeclarationKind.ClassTemplateSpecialization:
                    {
                        var _decl = ClassTemplateSpecialization.__CreateInstance(decl.__Instance);
                        return VisitClassTemplateSpecialization(_decl);
                    }
                case DeclarationKind.ClassTemplatePartialSpecialization:
                    {
                        var _decl = ClassTemplatePartialSpecialization.__CreateInstance(decl.__Instance);
                        return VisitClassTemplatePartialSpecialization(_decl);
                    }
                case DeclarationKind.FunctionTemplate:
                    {
                        var _decl = FunctionTemplate.__CreateInstance(decl.__Instance);
                        return VisitFunctionTemplate(_decl);
                    }
                case DeclarationKind.TypeAliasTemplate:
                    {
                        var _decl = TypeAliasTemplate.__CreateInstance(decl.__Instance);
                        return VisitTypeAliasTemplate(_decl);
                    }
                case DeclarationKind.TemplateTemplateParm:
                    {
                        var _decl = TemplateTemplateParameter.__CreateInstance(decl.__Instance);
                        return VisitTemplateTemplateParameter(_decl);
                    }
                case DeclarationKind.TemplateTypeParm:
                    {
                        var _decl = TypeTemplateParameter.__CreateInstance(decl.__Instance);
                        return VisitTypeTemplateParameter(_decl);
                    }
                case DeclarationKind.NonTypeTemplateParm:
                    {
                        var _decl = NonTypeTemplateParameter.__CreateInstance(decl.__Instance);
                        return VisitNonTypeTemplateParameter(_decl);
                    }
                case DeclarationKind.UnresolvedUsingTypename:
                    {
                        var _decl = UnresolvedUsingTypename.__CreateInstance(decl.__Instance);
                        return VisitUnresolvedUsingTypename(_decl);
                    }
            }

            throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Implements the visitor pattern for the generated comment bindings.
    /// </summary>
    public abstract class CommentsVisitor<TRet>
    {
        public abstract TRet VisitFullComment(FullComment comment);

        protected abstract TRet VisitBlockCommandComment(BlockCommandComment comment);

        protected abstract TRet VisitParamCommandComment(ParamCommandComment comment);

        protected abstract TRet VisitTParamCommandComment(TParamCommandComment comment);

        protected abstract TRet VisitVerbatimBlockComment(VerbatimBlockComment comment);

        protected abstract TRet VisitVerbatimLineComment(VerbatimLineComment comment);

        protected abstract TRet VisitParagraphComment(ParagraphComment comment);

        protected abstract TRet VisitHTMLStartTagComment(HTMLStartTagComment comment);

        protected abstract TRet VisitHTMLEndTagComment(HTMLEndTagComment comment);

        protected abstract TRet VisitTextComment(TextComment comment);

        protected abstract TRet VisitInlineCommandComment(InlineCommandComment comment);

        protected abstract TRet VisitVerbatimBlockLineComment(VerbatimBlockLineComment comment);

        public virtual TRet Visit(Comment comment)
        {
            switch (comment.Kind)
            {
                case CommentKind.FullComment:
                    return VisitFullComment(FullComment.__CreateInstance(comment.__Instance));
                case CommentKind.BlockCommandComment:
                    return VisitBlockCommandComment(BlockCommandComment.__CreateInstance(comment.__Instance));
                case CommentKind.ParamCommandComment:
                    return VisitParamCommandComment(ParamCommandComment.__CreateInstance(comment.__Instance));
                case CommentKind.TParamCommandComment:
                    return VisitTParamCommandComment(TParamCommandComment.__CreateInstance(comment.__Instance));
                case CommentKind.VerbatimBlockComment:
                    return VisitVerbatimBlockComment(VerbatimBlockComment.__CreateInstance(comment.__Instance));
                case CommentKind.VerbatimLineComment:
                    return VisitVerbatimLineComment(VerbatimLineComment.__CreateInstance(comment.__Instance));
                case CommentKind.ParagraphComment:
                    return VisitParagraphComment(ParagraphComment.__CreateInstance(comment.__Instance));
                case CommentKind.HTMLStartTagComment:
                    return VisitHTMLStartTagComment(HTMLStartTagComment.__CreateInstance(comment.__Instance));
                case CommentKind.HTMLEndTagComment:
                    return VisitHTMLEndTagComment(HTMLEndTagComment.__CreateInstance(comment.__Instance));
                case CommentKind.TextComment:
                    return VisitTextComment(TextComment.__CreateInstance(comment.__Instance));
                case CommentKind.InlineCommandComment:
                    return VisitInlineCommandComment(InlineCommandComment.__CreateInstance(comment.__Instance));
                case CommentKind.VerbatimBlockLineComment:
                    return VisitVerbatimBlockLineComment(VerbatimBlockLineComment.__CreateInstance(comment.__Instance));
            }

            throw new ArgumentOutOfRangeException();
        }
    }

    #endregion

    #region Parser AST converters

    /// <summary>
    /// This class converts from the C++ parser AST bindings to the
    /// AST defined in C#.
    /// </summary>
    public class ASTConverter
    {
        ASTContext Context { get; set; }
        readonly TypeConverter typeConverter;
        readonly DeclConverter declConverter;
        readonly CommentConverter commentConverter;
        readonly StmtConverter stmtConverter;
        readonly ExprConverter exprConverter;

        public ASTConverter(ASTContext context)
        {
            Context = context;
            typeConverter = new TypeConverter();
            commentConverter = new CommentConverter();
            stmtConverter = new StmtConverter();
            declConverter = new DeclConverter(typeConverter, commentConverter, stmtConverter);
            typeConverter.declConverter = declConverter;

            exprConverter = new ExprConverter();

            ConversionUtils.typeConverter = typeConverter;
            ConversionUtils.declConverter = declConverter;
            ConversionUtils.stmtConverter = stmtConverter;
            ConversionUtils.exprConverter = exprConverter;
        }

        public AST.ASTContext Convert()
        {
            var _ctx = new AST.ASTContext();

            for (uint i = 0; i < Context.TranslationUnitsCount; ++i)
            {
                var unit = Context.GetTranslationUnits(i);
                var _unit = (AST.TranslationUnit)declConverter.Visit(unit);
                _ctx.TranslationUnits.Add(_unit);
            }

            for (uint i = 0; i < Context.TranslationUnitsCount; i++)
            {
                var unit = Context.GetTranslationUnits(i);
                var _unit = (AST.TranslationUnit)declConverter.Visit(unit);
                declConverter.VisitDeclContext(unit, _unit);
            }

            foreach (var nativeObject in typeConverter.NativeObjects)
                nativeObject.Dispose();

            foreach (var nativeObject in declConverter.NativeObjects)
                nativeObject.Dispose();

            Context.Dispose();

            return _ctx;
        }
    }

    public class TypeConverter : TypeVisitor<AST.Type>
    {
        internal DeclConverter declConverter;

        public TypeConverter()
        {
            NativeObjects = new HashSet<IDisposable>();
        }

        public HashSet<IDisposable> NativeObjects { get; private set; }

        public AST.QualifiedType VisitQualified(QualifiedType qualType)
        {
            var _qualType = new AST.QualifiedType
            {
                Qualifiers = new AST.TypeQualifiers
                {
                    IsConst = qualType.Qualifiers.IsConst,
                    IsRestrict = qualType.Qualifiers.IsRestrict,
                    IsVolatile = qualType.Qualifiers.IsVolatile,
                },
                Type = Visit(qualType.Type)
            };

            return _qualType;
        }

        AST.ArrayType.ArraySize VisitArraySizeType(ArrayType.ArraySize size)
        {
            switch (size)
            {
                case ArrayType.ArraySize.Constant:
                    return AST.ArrayType.ArraySize.Constant;
                case ArrayType.ArraySize.Variable:
                    return AST.ArrayType.ArraySize.Variable;
                case ArrayType.ArraySize.Dependent:
                    return AST.ArrayType.ArraySize.Dependent;
                case ArrayType.ArraySize.Incomplete:
                    return AST.ArrayType.ArraySize.Incomplete;
                default:
                    throw new ArgumentOutOfRangeException("size");
            }
        }

        void VisitType(Parser.AST.Type origType, CppSharp.AST.Type type)
        {
            type.IsDependent = origType.IsDependent;
            NativeObjects.Add(origType);
        }

        public override AST.Type VisitTag(TagType type)
        {
            var _type = new AST.TagType();
            _type.Declaration = declConverter.Visit(type.Declaration);
            VisitType(type, _type);
            return _type;
        }

        public override AST.Type VisitArray(ArrayType type)
        {
            var _type = new AST.ArrayType
            {
                Size = type.Size,
                SizeType = VisitArraySizeType(type.SizeType),
                QualifiedType = VisitQualified(type.QualifiedType),
                ElementSize = type.ElementSize
            };
            VisitType(type, _type);
            return _type;
        }

        public override AST.Type VisitFunction(FunctionType type)
        {
            var _type = new AST.FunctionType();
            VisitType(type, _type);
            _type.ReturnType = VisitQualified(type.ReturnType);
            _type.CallingConvention = DeclConverter.VisitCallingConvention(
                type.CallingConvention);
            _type.ExceptionSpecType = VisitExceptionSpecType(type.ExceptionSpecType);

            for (uint i = 0; i < type.ParametersCount; ++i)
            {
                var param = type.GetParameters(i);
                var _param = declConverter.Visit(param) as AST.Parameter;
                _type.Parameters.Add(_param);
            }

            return _type;
        }

        public override AST.Type VisitPointer(PointerType type)
        {
            var _type = new AST.PointerType();
            _type.QualifiedPointee = VisitQualified(type.QualifiedPointee);
            _type.Modifier = VisitTypeModifier(type.Modifier);
            VisitType(type, _type);
            return _type;
        }

        AST.PointerType.TypeModifier VisitTypeModifier(PointerType.TypeModifier modifier)
        {
            switch (modifier)
            {
                case PointerType.TypeModifier.Value:
                    return AST.PointerType.TypeModifier.Value;
                case PointerType.TypeModifier.Pointer:
                    return AST.PointerType.TypeModifier.Pointer;
                case PointerType.TypeModifier.LVReference:
                    return AST.PointerType.TypeModifier.LVReference;
                case PointerType.TypeModifier.RVReference:
                    return AST.PointerType.TypeModifier.RVReference;
                default:
                    throw new ArgumentOutOfRangeException("modifier");
            }
        }

        private static AST.ExceptionSpecType VisitExceptionSpecType(
            ExceptionSpecType exceptionSpecType)
        {
            switch (exceptionSpecType)
            {
                case ExceptionSpecType.None:
                    return AST.ExceptionSpecType.None;
                case ExceptionSpecType.DynamicNone:
                    return AST.ExceptionSpecType.DynamicNone;
                case ExceptionSpecType.Dynamic:
                    return AST.ExceptionSpecType.Dynamic;
                case ExceptionSpecType.MSAny:
                    return AST.ExceptionSpecType.MSAny;
                case ExceptionSpecType.BasicNoexcept:
                    return AST.ExceptionSpecType.BasicNoexcept;
                case ExceptionSpecType.DependentNoexcept:
                    return AST.ExceptionSpecType.DependentNoexcept;
                case ExceptionSpecType.NoexceptFalse:
                    return AST.ExceptionSpecType.NoexceptFalse;
                case ExceptionSpecType.NoexceptTrue:
                    return AST.ExceptionSpecType.NoexceptTrue;
                case ExceptionSpecType.Unevaluated:
                    return AST.ExceptionSpecType.Unevaluated;
                case ExceptionSpecType.Uninstantiated:
                    return AST.ExceptionSpecType.Uninstantiated;
                case ExceptionSpecType.Unparsed:
                    return AST.ExceptionSpecType.Unparsed;
                default:
                    throw new ArgumentOutOfRangeException("exceptionSpecType");
            }
        }

        public override AST.Type VisitMemberPointer(MemberPointerType type)
        {
            var _type = new AST.MemberPointerType();
            VisitType(type, _type);
            _type.QualifiedPointee = VisitQualified(type.Pointee);
            return _type;
        }

        public override AST.Type VisitTypedef(TypedefType type)
        {
            var _type = new AST.TypedefType();
            VisitType(type, _type);
            _type.Declaration = (AST.TypedefNameDecl)declConverter.Visit(type.Declaration);
            return _type;
        }

        public override AST.Type VisitAttributed(AttributedType type)
        {
            var _type = new AST.AttributedType();
            VisitType(type, _type);
            _type.Modified = VisitQualified(type.Modified);
            _type.Equivalent = VisitQualified(type.Equivalent);
            return _type;
        }

        public override AST.Type VisitDecayed(DecayedType type)
        {
            var _type = new AST.DecayedType();
            _type.Decayed = VisitQualified(type.Decayed);
            _type.Original = VisitQualified(type.Original);
            _type.Pointee = VisitQualified(type.Pointee);
            VisitType(type, _type);
            return _type;
        }

        AST.TemplateArgument VisitTemplateArgument(TemplateArgument arg)
        {
            var _arg = new AST.TemplateArgument();
            _arg.Kind = VisitArgumentKind(arg.Kind);
            _arg.Type = VisitQualified(arg.Type);
            _arg.Declaration = declConverter.Visit(arg.Declaration);
            _arg.Integral = arg.Integral;
            NativeObjects.Add(arg);
            return _arg;
        }

        private AST.TemplateArgument.ArgumentKind VisitArgumentKind(TemplateArgument.ArgumentKind kind)
        {
            switch (kind)
            {
                case TemplateArgument.ArgumentKind.Type:
                    return AST.TemplateArgument.ArgumentKind.Type;
                case TemplateArgument.ArgumentKind.Declaration:
                    return AST.TemplateArgument.ArgumentKind.Declaration;
                case TemplateArgument.ArgumentKind.NullPtr:
                    return AST.TemplateArgument.ArgumentKind.NullPtr;
                case TemplateArgument.ArgumentKind.Integral:
                    return AST.TemplateArgument.ArgumentKind.Integral;
                case TemplateArgument.ArgumentKind.Template:
                    return AST.TemplateArgument.ArgumentKind.Template;
                case TemplateArgument.ArgumentKind.TemplateExpansion:
                    return AST.TemplateArgument.ArgumentKind.TemplateExpansion;
                case TemplateArgument.ArgumentKind.Expression:
                    return AST.TemplateArgument.ArgumentKind.Expression;
                case TemplateArgument.ArgumentKind.Pack:
                    return AST.TemplateArgument.ArgumentKind.Pack;
                default:
                    throw new ArgumentOutOfRangeException("kind");
            }
        }

        public override AST.Type VisitTemplateSpecialization(TemplateSpecializationType type)
        {
            var _type = new AST.TemplateSpecializationType();

            for (uint i = 0; i < type.ArgumentsCount; ++i)
            {
                var arg = type.GetArguments(i);
                var _arg = VisitTemplateArgument(arg);
                _type.Arguments.Add(_arg);
            }

            _type.Template = declConverter.Visit(type.Template) as AST.Template;
            _type.Desugared = VisitQualified(type.Desugared);

            VisitType(type, _type);

            return _type;
        }

        public override AST.Type VisitDependentTemplateSpecialization(
            DependentTemplateSpecializationType type)
        {
            var _type = new AST.DependentTemplateSpecializationType();

            for (uint i = 0; i < type.ArgumentsCount; ++i)
            {
                var arg = type.GetArguments(i);
                var _arg = VisitTemplateArgument(arg);
                _type.Arguments.Add(_arg);
            }

            _type.Desugared = VisitQualified(type.Desugared);

            VisitType(type, _type);

            return _type;
        }

        public override AST.Type VisitTemplateParameter(TemplateParameterType type)
        {
            var _type = new AST.TemplateParameterType();
            if (type.Parameter != null)
                _type.Parameter = (AST.TypeTemplateParameter)declConverter.Visit(type.Parameter);
            _type.Depth = type.Depth;
            _type.Index = type.Index;
            _type.IsParameterPack = type.IsParameterPack;
            VisitType(type, _type);
            return _type;
        }

        public override AST.Type VisitTemplateParameterSubstitution(TemplateParameterSubstitutionType type)
        {
            var _type = new AST.TemplateParameterSubstitutionType();
            _type.Replacement = VisitQualified(type.Replacement);
            _type.ReplacedParameter = (AST.TemplateParameterType)Visit(type.ReplacedParameter);
            VisitType(type, _type);
            return _type;
        }

        public override AST.Type VisitInjectedClassName(InjectedClassNameType type)
        {
            var _type = new AST.InjectedClassNameType();
            _type.Class = declConverter.Visit(type.Class) as AST.Class;
            _type.InjectedSpecializationType = VisitQualified(type.InjectedSpecializationType);
            VisitType(type, _type);
            return _type;
        }

        public override AST.Type VisitDependentName(DependentNameType type)
        {
            var _type = new AST.DependentNameType();
            VisitType(type, _type);
            _type.Qualifier = VisitQualified(type.Qualifier);
            _type.Identifier = type.Identifier;
            return _type;
        }

        public override AST.Type VisitBuiltin(BuiltinType type)
        {
            var _type = new AST.BuiltinType();
            _type.Type = VisitPrimitive(type.Type);
            VisitType(type, _type);
            return _type;
        }

        AST.PrimitiveType VisitPrimitive(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Null:
                    return AST.PrimitiveType.Null;
                case PrimitiveType.Void:
                    return AST.PrimitiveType.Void;
                case PrimitiveType.Bool:
                    return AST.PrimitiveType.Bool;
                case PrimitiveType.WideChar:
                    return AST.PrimitiveType.WideChar;
                case PrimitiveType.Char:
                    return AST.PrimitiveType.Char;
                case PrimitiveType.UChar:
                    return AST.PrimitiveType.UChar;
                case PrimitiveType.SChar:
                    return AST.PrimitiveType.SChar;
                case PrimitiveType.Char16:
                    return AST.PrimitiveType.Char16;
                case PrimitiveType.Char32:
                    return AST.PrimitiveType.Char32;
                case PrimitiveType.Short:
                    return AST.PrimitiveType.Short;
                case PrimitiveType.UShort:
                    return AST.PrimitiveType.UShort;
                case PrimitiveType.Int:
                    return AST.PrimitiveType.Int;
                case PrimitiveType.UInt:
                    return AST.PrimitiveType.UInt;
                case PrimitiveType.Long:
                    return AST.PrimitiveType.Long;
                case PrimitiveType.ULong:
                    return AST.PrimitiveType.ULong;
                case PrimitiveType.LongLong:
                    return AST.PrimitiveType.LongLong;
                case PrimitiveType.ULongLong:
                    return AST.PrimitiveType.ULongLong;
                case PrimitiveType.Int128:
                    return AST.PrimitiveType.Int128;
                case PrimitiveType.UInt128:
                    return AST.PrimitiveType.UInt128;
                case PrimitiveType.Half:
                    return AST.PrimitiveType.Half;
                case PrimitiveType.Float:
                    return AST.PrimitiveType.Float;
                case PrimitiveType.Double:
                    return AST.PrimitiveType.Double;
                case PrimitiveType.LongDouble:
                    return AST.PrimitiveType.LongDouble;
                case PrimitiveType.Float128:
                    return AST.PrimitiveType.Float128;
                case PrimitiveType.IntPtr:
                    return AST.PrimitiveType.IntPtr;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public override AST.Type VisitPackExpansion(PackExpansionType type)
        {
            var _type = new AST.PackExpansionType();
            VisitType(type, _type);
            return _type;
        }

        public override AST.Type VisitUnaryTransform(UnaryTransformType type)
        {
            var _type = new AST.UnaryTransformType();
            VisitType(type, _type);
            _type.Desugared = VisitQualified(type.Desugared);
            _type.BaseType = VisitQualified(type.BaseType);
            return _type;
        }

        public override AST.Type VisitUnresolvedUsing(UnresolvedUsingType type)
        {
            var _type = new AST.UnresolvedUsingType();
            VisitType(type, _type);
            _type.Declaration = (AST.UnresolvedUsingTypename)
                declConverter.Visit(type.Declaration);
            return _type;
        }

        public override AST.Type VisitVector(VectorType type)
        {
            var _type = new AST.VectorType();
            _type.NumElements = type.NumElements;
            _type.ElementType = VisitQualified(type.ElementType);
            return _type;
        }
    }

    public unsafe class DeclConverter : DeclVisitor<AST.Declaration>
    {
        readonly TypeConverter typeConverter;
        readonly CommentConverter commentConverter;
        readonly StmtConverter stmtConverter;

        readonly Dictionary<(IntPtr, DeclarationKind), AST.Declaration> Declarations;
        readonly Dictionary<IntPtr, AST.PreprocessedEntity> PreprocessedEntities;
        readonly Dictionary<IntPtr, AST.FunctionTemplateSpecialization> FunctionTemplateSpecializations;

        public DeclConverter(TypeConverter type, CommentConverter comment, StmtConverter stmt)
        {
            NativeObjects = new HashSet<IDisposable>();
            typeConverter = type;
            commentConverter = comment;
            stmtConverter = stmt;
            Declarations = new Dictionary<(IntPtr, DeclarationKind), AST.Declaration>();
            PreprocessedEntities = new Dictionary<IntPtr, AST.PreprocessedEntity>();
            FunctionTemplateSpecializations = new Dictionary<IntPtr, AST.FunctionTemplateSpecialization>();
        }

        public HashSet<IDisposable> NativeObjects { get; private set; }

        public override AST.Declaration Visit(Declaration decl)
        {
            if (decl == null)
                return null;

            if (decl.OriginalPtr == IntPtr.Zero)
                throw new NotSupportedException("Original pointer must not be null");

            var originalPtr = decl.OriginalPtr;

            // Check if the declaration was already handled and return its
            // existing instance.
            var key = (decl.OriginalPtr, decl.Kind);
            if (CheckForDuplicates(decl) && Declarations.TryGetValue(key, out var visit))
                return visit;

            return base.Visit(decl);
        }

        private AST.AccessSpecifier VisitAccessSpecifier(AccessSpecifier access)
        {
            switch (access)
            {
                case AccessSpecifier.Private:
                    return AST.AccessSpecifier.Private;
                case AccessSpecifier.Protected:
                    return AST.AccessSpecifier.Protected;
                case AccessSpecifier.Public:
                    return AST.AccessSpecifier.Public;
            }

            throw new ArgumentOutOfRangeException();
        }

        private AST.TagKind VisitTagKind(TagKind tagKind)
        {
            switch (tagKind)
            {
                case TagKind.Struct:
                    return AST.TagKind.Struct;
                case TagKind.Interface:
                    return AST.TagKind.Interface;
                case TagKind.Union:
                    return AST.TagKind.Union;
                case TagKind.Class:
                    return AST.TagKind.Class;
                case TagKind.Enum:
                    return AST.TagKind.Enum;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        AST.BaseClassSpecifier VisitBaseClassSpecifier(BaseClassSpecifier @base)
        {
            var _base = new AST.BaseClassSpecifier
            {
                IsVirtual = @base.IsVirtual,
                Access = VisitAccessSpecifier(@base.Access),
                Type = typeConverter.Visit(@base.Type),
                Offset = @base.Offset
            };

            NativeObjects.Add(@base);

            return _base;
        }

        AST.RawComment VisitRawComment(RawComment rawComment)
        {
            var _rawComment = new AST.RawComment
            {
                Kind = ConvertRawCommentKind(rawComment.Kind),
                BriefText = rawComment.BriefText,
                Text = rawComment.Text,
            };

            if (rawComment.FullCommentBlock != null)
                _rawComment.FullComment = commentConverter.Visit(rawComment.FullCommentBlock)
                    as AST.FullComment;

            NativeObjects.Add(rawComment);

            return _rawComment;
        }

        private AST.CommentKind ConvertRawCommentKind(RawCommentKind kind)
        {
            switch (kind)
            {
                case RawCommentKind.Invalid:
                    return AST.CommentKind.Invalid;
                case RawCommentKind.OrdinaryBCPL:
                    return AST.CommentKind.BCPL;
                case RawCommentKind.OrdinaryC:
                    return AST.CommentKind.C;
                case RawCommentKind.BCPLSlash:
                    return AST.CommentKind.BCPLSlash;
                case RawCommentKind.BCPLExcl:
                    return AST.CommentKind.BCPLExcl;
                case RawCommentKind.JavaDoc:
                    return AST.CommentKind.JavaDoc;
                case RawCommentKind.Qt:
                    return AST.CommentKind.Qt;
                case RawCommentKind.Merged:
                    return AST.CommentKind.Merged;
                default:
                    throw new ArgumentOutOfRangeException("kind");
            }
        }

        bool CheckForDuplicates(Declaration decl)
        {
            return decl.OriginalPtr.ToPointer() != (void*)(0x1);
        }

        void VisitDeclaration(Declaration decl, AST.Declaration _decl)
        {
            var key = (decl.OriginalPtr, decl.Kind);

            if (CheckForDuplicates(decl))
            {
                if (Declarations.ContainsKey(key))
                    throw new NotSupportedException("Duplicate declaration processed");
            }

            // Add the declaration to the map so that we can check if have
            // already handled it and return the declaration.
            Declarations[key] = _decl;

            _decl.Access = VisitAccessSpecifier(decl.Access);
            _decl.Name = decl.Name;
            _decl.USR = decl.USR;
            _decl.Namespace = Visit(decl.Namespace) as AST.DeclarationContext;
            _decl.Location = new SourceLocation(decl.Location.ID);
            _decl.LineNumberStart = decl.LineNumberStart;
            _decl.LineNumberEnd = decl.LineNumberEnd;
            _decl.DebugText = decl.DebugText;
            _decl.IsIncomplete = decl.IsIncomplete;
            _decl.IsDependent = decl.IsDependent;
            _decl.IsImplicit = decl.IsImplicit;
            _decl.IsInvalid = decl.IsInvalid;
            _decl.DefinitionOrder = decl.DefinitionOrder;
            _decl.AlignAs = decl.AlignAs;
            _decl.MaxFieldAlignment = decl.MaxFieldAlignment;
            _decl.IsDeprecated = decl.IsDeprecated;

            if (decl.CompleteDeclaration != null)
                _decl.CompleteDeclaration = Visit(decl.CompleteDeclaration);
            if (decl.Comment != null)
                _decl.Comment = VisitRawComment(decl.Comment);

            for (uint i = 0; i < decl.PreprocessedEntitiesCount; ++i)
            {
                var entity = decl.GetPreprocessedEntities(i);
                var _entity = VisitPreprocessedEntity(entity);
                _decl.PreprocessedEntities.Add(_entity);
            }

            _decl.OriginalPtr = decl.OriginalPtr;

            NativeObjects.Add(decl);

            for (uint i = 0; i < decl.RedeclarationsCount; i++)
            {
                var redecl = decl.GetRedeclarations(i);
                _decl.Redeclarations.Add(Visit(redecl));
            }

        }

        public void VisitDeclContext(DeclarationContext ctx, AST.DeclarationContext _ctx)
        {
            for (uint i = 0; i < ctx.NamespacesCount; ++i)
            {
                var decl = ctx.GetNamespaces(i);
                var _decl = Visit(decl) as AST.Namespace;
                // HACK: we have an irreproducible case where an STD name-space is added to a custom header
                if (_decl.Namespace == _ctx)
                {
                    _ctx.Declarations.Add(_decl);
                }
            }

            for (uint i = 0; i < ctx.EnumsCount; ++i)
            {
                var decl = ctx.GetEnums(i);
                var _decl = Visit(decl) as AST.Enumeration;
                _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.FunctionsCount; ++i)
            {
                var decl = ctx.GetFunctions(i);
                var _decl = Visit(decl) as AST.Function;
                _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.TemplatesCount; ++i)
            {
                var decl = ctx.GetTemplates(i);
                var _decl = Visit(decl) as AST.Template;
                _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.ClassesCount; ++i)
            {
                var decl = ctx.GetClasses(i);
                var _decl = Visit(decl) as AST.Class;
                if (!_decl.IsIncomplete || _decl.IsOpaque)
                    _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.TypedefsCount; ++i)
            {
                var decl = ctx.GetTypedefs(i);
                var _decl = Visit(decl) as AST.TypedefDecl;
                _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.TypeAliasesCount; ++i)
            {
                var decl = ctx.GetTypeAliases(i);
                var _decl = Visit(decl) as AST.TypeAlias;
                _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.VariablesCount; ++i)
            {
                var decl = ctx.GetVariables(i);
                var _decl = Visit(decl) as AST.Variable;
                _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.FriendsCount; ++i)
            {
                var decl = ctx.GetFriends(i);
                var _decl = Visit(decl) as AST.Friend;
                _ctx.Declarations.Add(_decl);
            }

            for (uint i = 0; i < ctx.NamespacesCount; ++i)
            {
                var decl = ctx.GetNamespaces(i);
                var _decl = (AST.Namespace)Visit(decl);
                VisitDeclContext(decl, _decl);
            }

            for (uint i = 0; i < ctx.ClassesCount; ++i)
            {
                var decl = ctx.GetClasses(i);
                var _decl = (AST.Class)Visit(decl);
                if (!_decl.IsIncomplete || _decl.IsOpaque)
                    VisitClass(decl, _decl);
            }

            // Anonymous types
        }

        public override AST.Declaration VisitTranslationUnit(TranslationUnit decl)
        {
            var _unit = new AST.TranslationUnit();
            _unit.FilePath = decl.FileName;
            _unit.IsSystemHeader = decl.IsSystemHeader;

            for (uint i = 0; i < decl.MacrosCount; ++i)
            {
                var macro = decl.GetMacros(i);
                var _macro = VisitMacroDefinition(macro);
                _unit.Macros.Add(_macro);
            }

            VisitDeclaration(decl, _unit);
            return _unit;
        }

        public override AST.Declaration VisitNamespace(Namespace decl)
        {
            var _namespace = new AST.Namespace();
            VisitDeclaration(decl, _namespace);
            _namespace.IsInline = decl.IsInline;

            return _namespace;
        }

        public override AST.Declaration VisitTypedef(TypedefDecl decl)
        {
            var _typedef = new AST.TypedefDecl();
            VisitDeclaration(decl, _typedef);
            _typedef.QualifiedType = typeConverter.VisitQualified(decl.QualifiedType);

            return _typedef;
        }

        public override AST.Declaration VisitTypeAlias(TypeAlias decl)
        {
            var _typeAlias = new AST.TypeAlias();
            VisitDeclaration(decl, _typeAlias);
            _typeAlias.QualifiedType = typeConverter.VisitQualified(decl.QualifiedType);
            if (decl.DescribedAliasTemplate != null)
                _typeAlias.DescribedAliasTemplate = (AST.TypeAliasTemplate)Visit(decl.DescribedAliasTemplate);

            return _typeAlias;
        }

        public override AST.Declaration VisitParameter(Parameter decl)
        {
            var _param = new AST.Parameter();
            VisitDeclaration(decl, _param);

            _param.QualifiedType = typeConverter.VisitQualified(decl.QualifiedType);
            _param.IsIndirect = decl.IsIndirect;
            _param.HasDefaultValue = decl.HasDefaultValue;
            _param.Index = decl.Index;
            _param.DefaultArgument = VisitStatement(decl.DefaultArgument);
            if (decl.DefaultValue != null)
            {
                _param.DefaultValue = stmtConverter.Visit(decl.DefaultValue);
            }

            return _param;
        }

        private AST.ExpressionObsolete VisitStatement(StatementObsolete statement)
        {
            if (statement == null)
                return null;

            AST.ExpressionObsolete expression;
            switch (statement.Class)
            {
                case StatementClassObsolete.BinaryOperator:
                    var binaryOperator = BinaryOperatorObsolete.__CreateInstance(statement.__Instance);
                    expression = new AST.BinaryOperatorObsolete(VisitStatement(binaryOperator.LHS),
                        VisitStatement(binaryOperator.RHS), binaryOperator.OpcodeStr);
                    break;
                case StatementClassObsolete.CallExprClass:
                    var callExpression = new AST.CallExprObsolete();
                    var callExpr = CallExprObsolete.__CreateInstance(statement.__Instance);
                    for (uint i = 0; i < callExpr.ArgumentsCount; i++)
                    {
                        var argument = VisitStatement(callExpr.GetArguments(i));
                        callExpression.Arguments.Add(argument);
                    }
                    expression = callExpression;
                    break;
                case StatementClassObsolete.DeclRefExprClass:
                    expression = new AST.BuiltinTypeExpressionObsolete();
                    expression.Class = AST.StatementClass.DeclarationReference;
                    break;
                case StatementClassObsolete.CXXOperatorCallExpr:
                    expression = new AST.BuiltinTypeExpressionObsolete();
                    expression.Class = AST.StatementClass.CXXOperatorCall;
                    break;
                case StatementClassObsolete.CXXConstructExprClass:
                    var constructorExpression = new AST.CXXConstructExprObsolete();
                    var constructorExpr = CXXConstructExprObsolete.__CreateInstance(statement.__Instance);
                    for (uint i = 0; i < constructorExpr.ArgumentsCount; i++)
                    {
                        var argument = VisitStatement(constructorExpr.GetArguments(i));
                        constructorExpression.Arguments.Add(argument);
                    }
                    expression = constructorExpression;
                    break;
                default:
                    expression = new AST.BuiltinTypeExpressionObsolete();
                    break;
            }
            expression.Declaration = Visit(statement.Decl);
            expression.String = statement.String;

            return expression;
        }

        public static string CleanSignature(string signature)
        {
            // Sometimes the parser might return multiline signatures, including the function body.
            // It should be dealt with in the native parser eventually, but fix it up here for now.
            var bracket = signature.IndexOf("{", StringComparison.Ordinal);
            if (bracket != -1)
                signature = signature.Substring(0, bracket);
            signature = signature.Replace("\n", "").Trim();
            return signature;
        }

        public void VisitFunction(Function function, AST.Function _function)
        {
            VisitDeclaration(function, _function);
            VisitDeclContext(function, _function);

            _function.ReturnType = typeConverter.VisitQualified(function.ReturnType);
            _function.IsReturnIndirect = function.IsReturnIndirect;
            _function.HasThisReturn = function.HasThisReturn;
            _function.IsConstExpr = function.IsConstExpr;
            _function.IsVariadic = function.IsVariadic;
            _function.IsInline = function.IsInline;
            _function.IsPure = function.IsPure;
            _function.IsDeleted = function.IsDeleted;
            _function.IsDefaulted = function.IsDefaulted;
            _function.FriendKind = VisitFriendKind(function.FriendKind);
            _function.OperatorKind = VisitCXXOperatorKind(function.OperatorKind);
            _function.Mangled = function.Mangled;
            _function.Signature = CleanSignature(function.Signature);
            _function.Body = function.Body;
            _function.CallingConvention = VisitCallingConvention(function.CallingConvention);
            if (function.InstantiatedFrom != null)
                _function.InstantiatedFrom = (AST.Function)Visit(function.InstantiatedFrom);

            for (uint i = 0; i < function.ParametersCount; ++i)
            {
                var param = function.GetParameters(i);
                var _param = Visit(param) as AST.Parameter;
                _function.Parameters.Add(_param);
            }

            if (function.BodyStmt != null)
            {
                var _stmt = stmtConverter.Visit(function.BodyStmt);
                _function.BodyStmt = _stmt;
            }

            _function.FunctionType = typeConverter.VisitQualified(function.QualifiedType);
            if (function.SpecializationInfo != null)
                _function.SpecializationInfo = VisitFunctionTemplateSpecialization(
                    function.SpecializationInfo);
        }

        public override AST.Declaration VisitFunction(Function decl)
        {
            var _function = new CppSharp.AST.Function();
            VisitFunction(decl, _function);

            return _function;
        }

        public override AST.Declaration VisitMethod(Method decl)
        {
            var _method = new AST.Method();
            VisitFunction(decl, _method);

            _method.IsVirtual = decl.IsVirtual;
            _method.IsStatic = decl.IsStatic;
            _method.IsConst = decl.IsConst;
            _method.IsImplicit = decl.IsImplicit;
            _method.IsExplicit = decl.IsExplicit;
            _method.IsVolatile = decl.IsVolatile;

            switch (decl.RefQualifier)
            {
                case RefQualifierKind.None:
                    _method.RefQualifier = AST.RefQualifier.None;
                    break;
                case RefQualifierKind.LValue:
                    _method.RefQualifier = AST.RefQualifier.LValue;
                    break;
                case RefQualifierKind.RValue:
                    _method.RefQualifier = AST.RefQualifier.RValue;
                    break;
            }

            _method.Kind = VisitCXXMethodKind(decl.MethodKind);

            _method.IsDefaultConstructor = decl.IsDefaultConstructor;
            _method.IsCopyConstructor = decl.IsCopyConstructor;
            _method.IsMoveConstructor = decl.IsMoveConstructor;

            _method.ConversionType = typeConverter.VisitQualified(decl.ConversionType);

            for (uint i = 0; i < decl.OverriddenMethodsCount; i++)
            {
                var @override = decl.GetOverriddenMethods(i);
                _method.OverriddenMethods.Add((AST.Method)Visit(@override));
            }

            return _method;
        }

        AST.CXXMethodKind VisitCXXMethodKind(CXXMethodKind methodKind)
        {
            switch (methodKind)
            {
                case CXXMethodKind.Normal:
                    return AST.CXXMethodKind.Normal;
                case CXXMethodKind.Constructor:
                    return AST.CXXMethodKind.Constructor;
                case CXXMethodKind.Destructor:
                    return AST.CXXMethodKind.Destructor;
                case CXXMethodKind.Conversion:
                    return AST.CXXMethodKind.Conversion;
                case CXXMethodKind.Operator:
                    return AST.CXXMethodKind.Operator;
                case CXXMethodKind.UsingDirective:
                    return AST.CXXMethodKind.UsingDirective;
                default:
                    throw new ArgumentOutOfRangeException("methodKind");
            }
        }

        AST.CXXOperatorKind VisitCXXOperatorKind(CXXOperatorKind operatorKind)
        {
            switch (operatorKind)
            {
                case CXXOperatorKind.None:
                    return AST.CXXOperatorKind.None;
                case CXXOperatorKind.New:
                    return AST.CXXOperatorKind.New;
                case CXXOperatorKind.Delete:
                    return AST.CXXOperatorKind.Delete;
                case CXXOperatorKind.ArrayNew:
                    return AST.CXXOperatorKind.Array_New;
                case CXXOperatorKind.ArrayDelete:
                    return AST.CXXOperatorKind.Array_Delete;
                case CXXOperatorKind.Plus:
                    return AST.CXXOperatorKind.Plus;
                case CXXOperatorKind.Minus:
                    return AST.CXXOperatorKind.Minus;
                case CXXOperatorKind.Star:
                    return AST.CXXOperatorKind.Star;
                case CXXOperatorKind.Slash:
                    return AST.CXXOperatorKind.Slash;
                case CXXOperatorKind.Percent:
                    return AST.CXXOperatorKind.Percent;
                case CXXOperatorKind.Caret:
                    return AST.CXXOperatorKind.Caret;
                case CXXOperatorKind.Amp:
                    return AST.CXXOperatorKind.Amp;
                case CXXOperatorKind.Pipe:
                    return AST.CXXOperatorKind.Pipe;
                case CXXOperatorKind.Tilde:
                    return AST.CXXOperatorKind.Tilde;
                case CXXOperatorKind.Exclaim:
                    return AST.CXXOperatorKind.Exclaim;
                case CXXOperatorKind.Equal:
                    return AST.CXXOperatorKind.Equal;
                case CXXOperatorKind.Less:
                    return AST.CXXOperatorKind.Less;
                case CXXOperatorKind.Greater:
                    return AST.CXXOperatorKind.Greater;
                case CXXOperatorKind.PlusEqual:
                    return AST.CXXOperatorKind.PlusEqual;
                case CXXOperatorKind.MinusEqual:
                    return AST.CXXOperatorKind.MinusEqual;
                case CXXOperatorKind.StarEqual:
                    return AST.CXXOperatorKind.StarEqual;
                case CXXOperatorKind.SlashEqual:
                    return AST.CXXOperatorKind.SlashEqual;
                case CXXOperatorKind.PercentEqual:
                    return AST.CXXOperatorKind.PercentEqual;
                case CXXOperatorKind.CaretEqual:
                    return AST.CXXOperatorKind.CaretEqual;
                case CXXOperatorKind.AmpEqual:
                    return AST.CXXOperatorKind.AmpEqual;
                case CXXOperatorKind.PipeEqual:
                    return AST.CXXOperatorKind.PipeEqual;
                case CXXOperatorKind.LessLess:
                    return AST.CXXOperatorKind.LessLess;
                case CXXOperatorKind.GreaterGreater:
                    return AST.CXXOperatorKind.GreaterGreater;
                case CXXOperatorKind.LessLessEqual:
                    return AST.CXXOperatorKind.LessLessEqual;
                case CXXOperatorKind.GreaterGreaterEqual:
                    return AST.CXXOperatorKind.GreaterGreaterEqual;
                case CXXOperatorKind.EqualEqual:
                    return AST.CXXOperatorKind.EqualEqual;
                case CXXOperatorKind.ExclaimEqual:
                    return AST.CXXOperatorKind.ExclaimEqual;
                case CXXOperatorKind.LessEqual:
                    return AST.CXXOperatorKind.LessEqual;
                case CXXOperatorKind.GreaterEqual:
                    return AST.CXXOperatorKind.GreaterEqual;
                case CXXOperatorKind.AmpAmp:
                    return AST.CXXOperatorKind.AmpAmp;
                case CXXOperatorKind.PipePipe:
                    return AST.CXXOperatorKind.PipePipe;
                case CXXOperatorKind.PlusPlus:
                    return AST.CXXOperatorKind.PlusPlus;
                case CXXOperatorKind.MinusMinus:
                    return AST.CXXOperatorKind.MinusMinus;
                case CXXOperatorKind.Comma:
                    return AST.CXXOperatorKind.Comma;
                case CXXOperatorKind.ArrowStar:
                    return AST.CXXOperatorKind.ArrowStar;
                case CXXOperatorKind.Arrow:
                    return AST.CXXOperatorKind.Arrow;
                case CXXOperatorKind.Call:
                    return AST.CXXOperatorKind.Call;
                case CXXOperatorKind.Subscript:
                    return AST.CXXOperatorKind.Subscript;
                case CXXOperatorKind.Conditional:
                    return AST.CXXOperatorKind.Conditional;
                case CXXOperatorKind.Coawait:
                    return AST.CXXOperatorKind.Coawait;
                default:
                    throw new ArgumentOutOfRangeException("operatorKind");
            }
        }

        internal static AST.CallingConvention VisitCallingConvention(CallingConvention callingConvention)
        {
            switch (callingConvention)
            {
                case CallingConvention.Default:
                    return AST.CallingConvention.Default;
                case CallingConvention.C:
                    return AST.CallingConvention.C;
                case CallingConvention.StdCall:
                    return AST.CallingConvention.StdCall;
                case CallingConvention.ThisCall:
                    return AST.CallingConvention.ThisCall;
                case CallingConvention.FastCall:
                    return AST.CallingConvention.FastCall;
                case CallingConvention.Unknown:
                    return AST.CallingConvention.Unknown;
                default:
                    throw new ArgumentOutOfRangeException("callingConvention");
            }
        }

        private static AST.FriendKind VisitFriendKind(FriendKind friendKind)
        {
            switch (friendKind)
            {
                case FriendKind.None:
                    return AST.FriendKind.None;
                case FriendKind.Declared:
                    return AST.FriendKind.Declared;
                case FriendKind.Undeclared:
                    return AST.FriendKind.Undeclared;
                default:
                    throw new ArgumentOutOfRangeException("friendKind");
            }
        }

        public override AST.Declaration VisitEnumeration(Enumeration decl)
        {
            var _enum = new AST.Enumeration();
            VisitDeclaration(decl, _enum);

            _enum.Modifiers = VisitEnumModifiers(decl.Modifiers);
            _enum.Type = typeConverter.Visit(decl.Type);
            _enum.BuiltinType = typeConverter.Visit(decl.BuiltinType)
                as AST.BuiltinType;

            for (uint i = 0; i < decl.ItemsCount; ++i)
            {
                var item = decl.GetItems(i);
                var _item = Visit(item) as AST.Enumeration.Item;
                _item.Namespace = _enum;
                _enum.AddItem(_item);
            }

            return _enum;
        }

        private AST.Enumeration.EnumModifiers VisitEnumModifiers(
            Enumeration.EnumModifiers modifiers)
        {
            return (AST.Enumeration.EnumModifiers)modifiers;
        }

        public override AST.Declaration VisitEnumerationItem(Enumeration.Item decl)
        {
            var _item = new AST.Enumeration.Item
            {
                Expression = decl.Expression,
                Value = decl.Value,
            };
            VisitDeclaration(decl, _item);

            return _item;
        }

        public void VisitVariable(Variable decl, AST.Variable _variable)
        {
            VisitDeclaration(decl, _variable);
            _variable.IsConstExpr = decl.IsConstExpr;
            _variable.Mangled = decl.Mangled;
            _variable.QualifiedType = typeConverter.VisitQualified(
                decl.QualifiedType);
            _variable.Initializer = VisitStatement(decl.Initializer);
        }

        public override AST.Declaration VisitVariable(Variable decl)
        {
            var _variable = new AST.Variable();
            VisitVariable(decl, _variable);

            return _variable;
        }

        public override AST.Declaration VisitFriend(Friend decl)
        {
            var _friend = new AST.Friend();
            VisitDeclaration(decl, _friend);
            _friend.Declaration = Visit(decl.Declaration);

            return _friend;
        }

        public override AST.Declaration VisitField(Field decl)
        {
            var _field = new AST.Field();
            VisitDeclaration(decl, _field);

            _field.QualifiedType = typeConverter.VisitQualified(
                decl.QualifiedType);
            _field.Access = VisitAccessSpecifier(decl.Access);
            _field.Class = Visit(decl.Class) as AST.Class;
            _field.IsBitField = decl.IsBitField;
            _field.BitWidth = decl.BitWidth;

            return _field;
        }

        public override AST.Declaration VisitAccessSpecifier(AccessSpecifierDecl decl)
        {
            var _access = new AST.AccessSpecifierDecl();
            VisitDeclaration(decl, _access);

            return _access;
        }

        void VisitClass(Class @class, AST.Class _class)
        {
            VisitDeclContext(@class, _class);

            for (uint i = 0; i < @class.BasesCount; ++i)
            {
                var @base = @class.GetBases(i);
                var _base = VisitBaseClassSpecifier(@base);
                _class.Bases.Add(_base);
            }

            for (uint i = 0; i < @class.FieldsCount; ++i)
            {
                var field = @class.GetFields(i);
                var _field = Visit(field) as AST.Field;
                _class.Fields.Add(_field);
            }

            for (uint i = 0; i < @class.MethodsCount; ++i)
            {
                var method = @class.GetMethods(i);
                var _method = Visit(method) as AST.Method;
                _class.Methods.Add(_method);
            }

            for (uint i = 0; i < @class.SpecifiersCount; ++i)
            {
                var spec = @class.GetSpecifiers(i);
                var _spec = Visit(spec) as AST.AccessSpecifierDecl;
                _class.Specifiers.Add(_spec);
            }

            _class.IsPOD = @class.IsPOD;
            _class.IsAbstract = @class.IsAbstract;
            _class.IsUnion = @class.IsUnion;
            _class.TagKind = VisitTagKind(@class.TagKind);
            _class.IsDynamic = @class.IsDynamic;
            _class.IsPolymorphic = @class.IsPolymorphic;
            _class.HasNonTrivialDefaultConstructor = @class.HasNonTrivialDefaultConstructor;
            _class.HasNonTrivialCopyConstructor = @class.HasNonTrivialCopyConstructor;
            _class.HasNonTrivialDestructor = @class.HasNonTrivialDestructor;
            _class.IsExternCContext = @class.IsExternCContext;
            _class.IsInjected = @class.IsInjected;

            if (@class.Layout != null)
            {
                _class.Layout = VisitClassLayout(@class.Layout);
                if (_class.BaseClass != null)
                {
                    AST.LayoutBase @base = _class.Layout.Bases.Find(
                        b => b.Class == _class.BaseClass);
                    _class.BaseClass.Layout.HasSubclassAtNonZeroOffset = @base.Offset > 0;
                }
            }
        }

        public override AST.Declaration VisitClass(Class @class)
        {
            var _class = new AST.Class();
            VisitDeclaration(@class, _class);

            return _class;
        }

        AST.RecordArgABI VisitRecordArgABI(RecordArgABI argAbi)
        {
            switch (argAbi)
            {
                case RecordArgABI.Default:
                    return AST.RecordArgABI.Default;
                case RecordArgABI.DirectInMemory:
                    return AST.RecordArgABI.DirectInMemory;
                case RecordArgABI.Indirect:
                    return AST.RecordArgABI.Indirect;
            }

            throw new NotImplementedException();
        }

        AST.ClassLayout VisitClassLayout(ClassLayout layout)
        {
            var _layout = new AST.ClassLayout
            {
                ABI = VisitCppAbi(layout.ABI),
                ArgABI = VisitRecordArgABI(layout.ArgABI),
                Layout = VisitVTableLayout(layout.Layout),
                HasOwnVFPtr = layout.HasOwnVFPtr,
                VBPtrOffset = layout.VBPtrOffset,
                Alignment = layout.Alignment,
                Size = layout.Size,
                DataSize = layout.DataSize
            };

            for (uint i = 0; i < layout.VFTablesCount; ++i)
            {
                var vftableInfo = layout.GetVFTables(i);
                var _vftableInfo = VisitVFTableInfo(vftableInfo);
                _layout.VFTables.Add(_vftableInfo);
            }

            for (uint i = 0; i < layout.FieldsCount; i++)
            {
                var field = layout.GetFields(i);
                var _field = new AST.LayoutField();
                _field.Offset = field.Offset;
                _field.Name = field.Name;
                _field.QualifiedType = typeConverter.VisitQualified(field.QualifiedType);
                _field.FieldPtr = field.FieldPtr;
                _layout.Fields.Add(_field);
            }

            for (uint i = 0; i < layout.BasesCount; i++)
            {
                var @base = layout.GetBases(i);
                var _base = new AST.LayoutBase();
                _base.Offset = @base.Offset;
                _base.Class = (AST.Class)Visit(@base.Class);
                _layout.Bases.Add(_base);
            }

            return _layout;
        }

        AST.CppAbi VisitCppAbi(CppAbi abi)
        {
            switch (abi)
            {
                case CppAbi.Itanium:
                    return AST.CppAbi.Itanium;
                case CppAbi.Microsoft:
                    return AST.CppAbi.Microsoft;
                case CppAbi.ARM:
                    return AST.CppAbi.ARM;
                case CppAbi.iOS:
                    return AST.CppAbi.iOS;
                case CppAbi.iOS64:
                    return AST.CppAbi.iOS64;
                case CppAbi.WebAssembly:
                    return AST.CppAbi.WebAssembly;
                default:
                    throw new ArgumentOutOfRangeException("abi");
            }
        }

        AST.VFTableInfo VisitVFTableInfo(VFTableInfo vftableInfo)
        {
            var _vftableInfo = new AST.VFTableInfo
            {
                VBTableIndex = vftableInfo.VBTableIndex,
                VFPtrOffset = vftableInfo.VFPtrOffset,
                VFPtrFullOffset = vftableInfo.VFPtrFullOffset,
                Layout = VisitVTableLayout(vftableInfo.Layout)
            };

            NativeObjects.Add(vftableInfo);

            return _vftableInfo;
        }

        AST.VTableLayout VisitVTableLayout(VTableLayout layout)
        {
            var _layout = new AST.VTableLayout();

            for (uint i = 0; i < layout.ComponentsCount; ++i)
            {
                var component = layout.GetComponents(i);
                var _component = VisitVTableComponent(component);
                _layout.Components.Add(_component);
            }

            return _layout;
        }

        AST.VTableComponent VisitVTableComponent(VTableComponent component)
        {
            var _component = new AST.VTableComponent
            {
                Kind = VisitVTableComponentKind(component.Kind),
                Offset = component.Offset,
                Declaration = Visit(component.Declaration)
            };

            NativeObjects.Add(component);

            return _component;
        }

        AST.VTableComponentKind VisitVTableComponentKind(VTableComponentKind kind)
        {
            switch (kind)
            {
                case VTableComponentKind.VCallOffset:
                    return AST.VTableComponentKind.VCallOffset;
                case VTableComponentKind.VBaseOffset:
                    return AST.VTableComponentKind.VBaseOffset;
                case VTableComponentKind.OffsetToTop:
                    return AST.VTableComponentKind.OffsetToTop;
                case VTableComponentKind.RTTI:
                    return AST.VTableComponentKind.RTTI;
                case VTableComponentKind.FunctionPointer:
                    return AST.VTableComponentKind.FunctionPointer;
                case VTableComponentKind.CompleteDtorPointer:
                    return AST.VTableComponentKind.CompleteDtorPointer;
                case VTableComponentKind.DeletingDtorPointer:
                    return AST.VTableComponentKind.DeletingDtorPointer;
                case VTableComponentKind.UnusedFunctionPointer:
                    return AST.VTableComponentKind.UnusedFunctionPointer;
                default:
                    throw new ArgumentOutOfRangeException("kind");
            }
        }

        public void VisitTemplate(Template template, AST.Template _template)
        {
            VisitDeclaration(template, _template);

            _template.TemplatedDecl = Visit(template.TemplatedDecl);

            for (uint i = 0; i < template.ParametersCount; ++i)
            {
                var param = template.GetParameters(i);
                var _param = Visit(param);
                _template.Parameters.Add(_param);
            }
        }

        public override AST.Declaration VisitTypeAliasTemplate(TypeAliasTemplate decl)
        {
            var _decl = new AST.TypeAliasTemplate();
            VisitTemplate(decl, _decl);

            return _decl;
        }

        public override AST.Declaration VisitClassTemplate(ClassTemplate decl)
        {
            var _decl = new AST.ClassTemplate();
            VisitTemplate(decl, _decl);
            for (uint i = 0; i < decl.SpecializationsCount; ++i)
            {
                var spec = decl.GetSpecializations(i);
                var _spec = (AST.ClassTemplateSpecialization)Visit(spec);
                _decl.Specializations.Add(_spec);
            }
            return _decl;
        }

        public override AST.Declaration VisitClassTemplateSpecialization(
            ClassTemplateSpecialization decl)
        {
            var _decl = new AST.ClassTemplateSpecialization();
            VisitClassTemplateSpecialization(decl, _decl);
            return _decl;
        }

        private void VisitClassTemplateSpecialization(ClassTemplateSpecialization decl,
            AST.ClassTemplateSpecialization _decl)
        {
            VisitDeclaration(decl, _decl);
            VisitClass(decl, _decl);
            _decl.SpecializationKind = VisitSpecializationKind(decl.SpecializationKind);
            _decl.TemplatedDecl = (AST.ClassTemplate)Visit(decl.TemplatedDecl);
            for (uint i = 0; i < decl.ArgumentsCount; ++i)
            {
                var arg = decl.GetArguments(i);
                var _arg = VisitTemplateArgument(arg);
                _decl.Arguments.Add(_arg);
            }
        }

        private AST.TemplateArgument VisitTemplateArgument(TemplateArgument arg)
        {
            var _arg = new AST.TemplateArgument();
            _arg.Kind = VisitTemplateArgumentKind(arg.Kind);
            _arg.Type = typeConverter.VisitQualified(arg.Type);
            _arg.Declaration = Visit(arg.Declaration);
            _arg.Integral = arg.Integral;
            NativeObjects.Add(arg);
            return _arg;
        }

        private AST.TemplateArgument.ArgumentKind VisitTemplateArgumentKind(
            TemplateArgument.ArgumentKind argumentKind)
        {
            switch (argumentKind)
            {
                case TemplateArgument.ArgumentKind.Declaration:
                    return AST.TemplateArgument.ArgumentKind.Declaration;
                case TemplateArgument.ArgumentKind.Expression:
                    return AST.TemplateArgument.ArgumentKind.Expression;
                case TemplateArgument.ArgumentKind.Integral:
                    return AST.TemplateArgument.ArgumentKind.Integral;
                case TemplateArgument.ArgumentKind.NullPtr:
                    return AST.TemplateArgument.ArgumentKind.NullPtr;
                case TemplateArgument.ArgumentKind.Pack:
                    return AST.TemplateArgument.ArgumentKind.Pack;
                case TemplateArgument.ArgumentKind.Template:
                    return AST.TemplateArgument.ArgumentKind.Template;
                case TemplateArgument.ArgumentKind.TemplateExpansion:
                    return AST.TemplateArgument.ArgumentKind.TemplateExpansion;
                case TemplateArgument.ArgumentKind.Type:
                    return AST.TemplateArgument.ArgumentKind.Type;
                default:
                    throw new NotImplementedException();
            }
        }

        private AST.TemplateSpecializationKind VisitSpecializationKind(
            TemplateSpecializationKind templateSpecializationKind)
        {
            switch (templateSpecializationKind)
            {
                case TemplateSpecializationKind.ExplicitInstantiationDeclaration:
                    return AST.TemplateSpecializationKind.ExplicitInstantiationDeclaration;
                case TemplateSpecializationKind.ExplicitInstantiationDefinition:
                    return AST.TemplateSpecializationKind.ExplicitInstantiationDefinition;
                case TemplateSpecializationKind.ExplicitSpecialization:
                    return AST.TemplateSpecializationKind.ExplicitSpecialization;
                case TemplateSpecializationKind.ImplicitInstantiation:
                    return AST.TemplateSpecializationKind.ImplicitInstantiation;
                case TemplateSpecializationKind.Undeclared:
                    return AST.TemplateSpecializationKind.Undeclared;
                default:
                    throw new NotImplementedException();
            }
        }

        public override AST.Declaration VisitClassTemplatePartialSpecialization(
            ClassTemplatePartialSpecialization decl)
        {
            var _decl = new AST.ClassTemplatePartialSpecialization();
            VisitClassTemplateSpecialization(decl, _decl);
            for (uint i = 0; i < decl.ParametersCount; ++i)
            {
                var param = decl.GetParameters(i);
                var _param = Visit(param);
                _decl.Parameters.Add(_param);
            }
            return _decl;
        }

        public override AST.Declaration VisitFunctionTemplate(FunctionTemplate decl)
        {
            var _decl = new AST.FunctionTemplate();
            VisitTemplate(decl, _decl);
            for (uint i = 0; i < decl.SpecializationsCount; ++i)
            {
                var _spec = VisitFunctionTemplateSpecialization(decl.GetSpecializations(i));
                _decl.Specializations.Add(_spec);
            }
            return _decl;
        }

        private AST.FunctionTemplateSpecialization VisitFunctionTemplateSpecialization(
            FunctionTemplateSpecialization spec)
        {
            if (FunctionTemplateSpecializations.ContainsKey(spec.__Instance))
                return FunctionTemplateSpecializations[spec.__Instance];

            var _spec = new AST.FunctionTemplateSpecialization();
            FunctionTemplateSpecializations.Add(spec.__Instance, _spec);
            _spec.Template = (AST.FunctionTemplate)Visit(spec.Template);
            _spec.SpecializedFunction = (AST.Function)Visit(spec.SpecializedFunction);
            _spec.SpecializationKind = VisitSpecializationKind(spec.SpecializationKind);
            for (uint i = 0; i < spec.ArgumentsCount; ++i)
            {
                var _arg = VisitTemplateArgument(spec.GetArguments(i));
                _spec.Arguments.Add(_arg);
            }
            NativeObjects.Add(spec);
            return _spec;
        }

        public override AST.Declaration VisitVarTemplate(VarTemplate decl)
        {
            var _decl = new AST.VarTemplate();
            VisitTemplate(decl, _decl);
            for (uint i = 0; i < decl.SpecializationsCount; ++i)
            {
                var spec = decl.GetSpecializations(i);
                var _spec = (AST.VarTemplateSpecialization)Visit(spec);
                _decl.Specializations.Add(_spec);
            }
            return _decl;
        }

        public override AST.Declaration VisitVarTemplateSpecialization(
            VarTemplateSpecialization decl)
        {
            var _decl = new AST.VarTemplateSpecialization();
            VisitVarTemplateSpecialization(decl, _decl);
            return _decl;
        }

        private void VisitVarTemplateSpecialization(VarTemplateSpecialization decl,
            AST.VarTemplateSpecialization _decl)
        {
            VisitVariable(decl, _decl);
            _decl.SpecializationKind = VisitSpecializationKind(decl.SpecializationKind);
            _decl.TemplatedDecl = (AST.VarTemplate)Visit(decl.TemplatedDecl);
            for (uint i = 0; i < decl.ArgumentsCount; ++i)
            {
                var arg = decl.GetArguments(i);
                var _arg = VisitTemplateArgument(arg);
                _decl.Arguments.Add(_arg);
            }
        }

        public override AST.Declaration VisitVarTemplatePartialSpecialization(
            VarTemplatePartialSpecialization decl)
        {
            var _decl = new AST.VarTemplatePartialSpecialization();
            VisitVarTemplateSpecialization(decl, _decl);
            return _decl;
        }

        void VisitPreprocessedEntity(PreprocessedEntity entity, AST.PreprocessedEntity _entity)
        {
            if (PreprocessedEntities.ContainsKey(entity.__Instance))
            {
                _entity.MacroLocation = PreprocessedEntities[entity.__Instance].MacroLocation;
                return;
            }

            _entity.MacroLocation = VisitMacroLocation(entity.MacroLocation);
            PreprocessedEntities.Add(entity.__Instance, _entity);
            NativeObjects.Add(entity);
        }

        private AST.PreprocessedEntity VisitPreprocessedEntity(PreprocessedEntity entity)
        {
            switch (entity.Kind)
            {
                case DeclarationKind.MacroDefinition:
                    var macroDefinition = MacroDefinition.__CreateInstance(entity.__Instance);
                    return VisitMacroDefinition(macroDefinition);
                case DeclarationKind.MacroExpansion:
                    var macroExpansion = MacroExpansion.__CreateInstance(entity.__Instance);
                    return VisitMacroExpansion(macroExpansion);
                default:
                    throw new ArgumentOutOfRangeException("entity");
            }
        }

        private AST.MacroLocation VisitMacroLocation(MacroLocation location)
        {
            switch (location)
            {
                case MacroLocation.Unknown:
                    return AST.MacroLocation.Unknown;
                case MacroLocation.ClassHead:
                    return AST.MacroLocation.ClassHead;
                case MacroLocation.ClassBody:
                    return AST.MacroLocation.ClassBody;
                case MacroLocation.FunctionHead:
                    return AST.MacroLocation.FunctionHead;
                case MacroLocation.FunctionParameters:
                    return AST.MacroLocation.FunctionParameters;
                case MacroLocation.FunctionBody:
                    return AST.MacroLocation.FunctionBody;
                default:
                    throw new ArgumentOutOfRangeException("location");
            }
        }

        public AST.MacroDefinition VisitMacroDefinition(MacroDefinition macroDefinition)
        {
            var _macro = new AST.MacroDefinition();
            VisitPreprocessedEntity(macroDefinition, _macro);
            _macro.Name = macroDefinition.Name;
            _macro.Expression = macroDefinition.Expression;
            return _macro;
        }

        public AST.MacroExpansion VisitMacroExpansion(MacroExpansion macroExpansion)
        {
            var _macro = new AST.MacroExpansion();
            VisitPreprocessedEntity(macroExpansion, _macro);
            _macro.Name = macroExpansion.Name;
            _macro.Text = macroExpansion.Text;
            if (macroExpansion.Definition != null)
                _macro.Definition = VisitMacroDefinition(macroExpansion.Definition);
            return _macro;
        }

        public override AST.Declaration VisitTypeTemplateParameter(TypeTemplateParameter decl)
        {
            var templateParameter = new AST.TypeTemplateParameter();
            VisitDeclaration(decl, templateParameter);
            templateParameter.DefaultArgument = typeConverter.VisitQualified(decl.DefaultArgument);
            templateParameter.Depth = decl.Depth;
            templateParameter.Index = decl.Index;
            templateParameter.IsParameterPack = decl.IsParameterPack;
            return templateParameter;
        }

        public override AST.Declaration VisitNonTypeTemplateParameter(NonTypeTemplateParameter decl)
        {
            var nonTypeTemplateParameter = new AST.NonTypeTemplateParameter();
            VisitDeclaration(decl, nonTypeTemplateParameter);
            nonTypeTemplateParameter.DefaultArgument = VisitStatement(decl.DefaultArgument);
            nonTypeTemplateParameter.Depth = decl.Depth;
            nonTypeTemplateParameter.Position = decl.Position;
            nonTypeTemplateParameter.Index = decl.Index;
            nonTypeTemplateParameter.IsParameterPack = decl.IsParameterPack;
            nonTypeTemplateParameter.IsPackExpansion = decl.IsPackExpansion;
            nonTypeTemplateParameter.IsExpandedParameterPack = decl.IsExpandedParameterPack;
            nonTypeTemplateParameter.Type = typeConverter.VisitQualified(decl.Type);
            return nonTypeTemplateParameter;
        }

        public override AST.Declaration VisitTemplateTemplateParameter(TemplateTemplateParameter decl)
        {
            var templateTemplateParameter = new AST.TemplateTemplateParameter();
            VisitTemplate(decl, templateTemplateParameter);
            templateTemplateParameter.IsParameterPack = decl.IsParameterPack;
            templateTemplateParameter.IsPackExpansion = decl.IsPackExpansion;
            templateTemplateParameter.IsExpandedParameterPack = decl.IsExpandedParameterPack;
            return templateTemplateParameter;
        }

        public override AST.Declaration VisitUnresolvedUsingTypename(UnresolvedUsingTypename decl)
        {
            var unresolvedUsingTypename = new AST.UnresolvedUsingTypename();
            VisitDeclaration(decl, unresolvedUsingTypename);
            return unresolvedUsingTypename;
        }
    }

    public unsafe class CommentConverter : CommentsVisitor<AST.Comment>
    {
        public override AST.Comment VisitFullComment(FullComment comment)
        {
            var fullComment = new AST.FullComment();
            for (uint i = 0; i < comment.BlocksCount; i++)
                fullComment.Blocks.Add((AST.BlockContentComment)Visit(comment.GetBlocks(i)));
            return fullComment;
        }

        protected override AST.Comment VisitBlockCommandComment(BlockCommandComment comment)
        {
            var blockCommandComment = new AST.BlockCommandComment();
            VisitBlockCommandComment(blockCommandComment, comment);
            return blockCommandComment;
        }

        protected override AST.Comment VisitParamCommandComment(ParamCommandComment comment)
        {
            var paramCommandComment = new AST.ParamCommandComment();
            switch (comment.Direction)
            {
                case ParamCommandComment.PassDirection.In:
                    paramCommandComment.Direction = AST.ParamCommandComment.PassDirection.In;
                    break;
                case ParamCommandComment.PassDirection.Out:
                    paramCommandComment.Direction = AST.ParamCommandComment.PassDirection.Out;
                    break;
                case ParamCommandComment.PassDirection.InOut:
                    paramCommandComment.Direction = AST.ParamCommandComment.PassDirection.InOut;
                    break;
            }
            paramCommandComment.ParamIndex = comment.ParamIndex;
            VisitBlockCommandComment(paramCommandComment, comment);
            return paramCommandComment;
        }

        protected override AST.Comment VisitTParamCommandComment(TParamCommandComment comment)
        {
            var paramCommandComment = new AST.TParamCommandComment();
            for (uint i = 0; i < comment.PositionCount; i++)
                paramCommandComment.Position.Add(comment.GetPosition(i));
            VisitBlockCommandComment(paramCommandComment, comment);
            return paramCommandComment;
        }

        protected override AST.Comment VisitVerbatimBlockComment(VerbatimBlockComment comment)
        {
            var verbatimBlockComment = new AST.VerbatimBlockComment();
            for (uint i = 0; i < comment.LinesCount; i++)
                verbatimBlockComment.Lines.Add((AST.VerbatimBlockLineComment)Visit(comment.GetLines(i)));
            VisitBlockCommandComment(verbatimBlockComment, comment);
            return verbatimBlockComment;
        }

        protected override AST.Comment VisitVerbatimLineComment(VerbatimLineComment comment)
        {
            var verbatimLineComment = new AST.VerbatimLineComment { Text = comment.Text };
            VisitBlockCommandComment(verbatimLineComment, comment);
            return verbatimLineComment;
        }

        protected override AST.Comment VisitParagraphComment(ParagraphComment comment)
        {
            var paragraphComment = new AST.ParagraphComment();
            for (uint i = 0; i < comment.ContentCount; i++)
                paragraphComment.Content.Add((AST.InlineContentComment)Visit(comment.GetContent(i)));
            paragraphComment.IsWhitespace = comment.IsWhitespace;
            return paragraphComment;
        }

        protected override AST.Comment VisitHTMLStartTagComment(HTMLStartTagComment comment)
        {
            var htmlStartTagComment = new AST.HTMLStartTagComment();
            for (uint i = 0; i < comment.AttributesCount; i++)
            {
                var attribute = new AST.HTMLStartTagComment.Attribute();
                var _attribute = comment.GetAttributes(i);
                attribute.Name = _attribute.Name;
                attribute.Value = _attribute.Value;
                htmlStartTagComment.Attributes.Add(attribute);
            }
            htmlStartTagComment.TagName = comment.TagName;
            htmlStartTagComment.HasTrailingNewline = comment.HasTrailingNewline;
            return htmlStartTagComment;
        }

        protected override AST.Comment VisitHTMLEndTagComment(HTMLEndTagComment comment)
        {
            return new AST.HTMLEndTagComment
            {
                TagName = comment.TagName,
                HasTrailingNewline = comment.HasTrailingNewline
            };
        }

        protected override AST.Comment VisitTextComment(TextComment comment)
        {
            return new AST.TextComment
            {
                Text = comment.Text,
                HasTrailingNewline = comment.HasTrailingNewline
            };
        }

        protected override AST.Comment VisitInlineCommandComment(InlineCommandComment comment)
        {
            var inlineCommandComment = new AST.InlineCommandComment
            {
                HasTrailingNewline = comment.HasTrailingNewline,
                CommandId = comment.CommandId
            };
            switch (comment.CommentRenderKind)
            {
                case InlineCommandComment.RenderKind.RenderNormal:
                    inlineCommandComment.CommentRenderKind = AST.InlineCommandComment.RenderKind.RenderNormal;
                    break;
                case InlineCommandComment.RenderKind.RenderBold:
                    inlineCommandComment.CommentRenderKind = AST.InlineCommandComment.RenderKind.RenderBold;
                    break;
                case InlineCommandComment.RenderKind.RenderMonospaced:
                    inlineCommandComment.CommentRenderKind = AST.InlineCommandComment.RenderKind.RenderMonospaced;
                    break;
                case InlineCommandComment.RenderKind.RenderEmphasized:
                    inlineCommandComment.CommentRenderKind = AST.InlineCommandComment.RenderKind.RenderEmphasized;
                    break;
                case InlineCommandComment.RenderKind.RenderAnchor:
                    inlineCommandComment.CommentRenderKind = AST.InlineCommandComment.RenderKind.RenderAnchor;
                    break;
            }
            for (uint i = 0; i < comment.ArgumentsCount; i++)
            {
                var argument = new AST.InlineCommandComment.Argument { Text = comment.GetArguments(i).Text };
                inlineCommandComment.Arguments.Add(argument);
            }
            return inlineCommandComment;
        }

        protected override AST.Comment VisitVerbatimBlockLineComment(VerbatimBlockLineComment comment)
        {
            return new AST.VerbatimBlockLineComment { Text = comment.Text };
        }

        private void VisitBlockCommandComment(AST.BlockCommandComment blockCommandComment, BlockCommandComment comment)
        {
            blockCommandComment.CommandId = comment.CommandId;
            if (comment.ParagraphComment != null)
                blockCommandComment.ParagraphComment = (AST.ParagraphComment)Visit(comment.ParagraphComment);
            for (uint i = 0; i < comment.ArgumentsCount; i++)
            {
                var argument = new AST.BlockCommandComment.Argument { Text = comment.GetArguments(i).Text };
                blockCommandComment.Arguments.Add(argument);
            }
        }
    }

    public static class ConversionUtils
    {
        public static TypeConverter typeConverter;
        public static DeclConverter declConverter;
        public static StmtConverter stmtConverter;
        public static ExprConverter exprConverter;

        public static AST.QualifiedType VisitQualifiedType(
            QualifiedType qualifiedType)
        {
            return typeConverter.VisitQualified(qualifiedType);
        }

        public static AST.SourceRange VisitSourceRange(Parser.SourceRange loc)
        {
            return new AST.SourceRange();
        }

        public static AST.SourceLocation VisitSourceLocation(
            Parser.SourceLocation loc)
        {
            return new AST.SourceLocation(loc.ID);
        }

        public static AST.Declaration VisitDeclaration(Parser.AST.Declaration decl)
        {
            return declConverter.Visit(decl);
        }

        public static AST.Stmt VisitStatement(Parser.AST.Stmt stmt)
        {
            return stmtConverter.Visit(stmt);
        }

        public static AST.Expr VisitExpression(Parser.AST.Expr expr)
        {
            return exprConverter.Visit(expr);
        }

        public static AST.TemplateArgument VisitTemplateArgument(
            Parser.AST.TemplateArgument templateArg)
        {
            return new AST.TemplateArgument();
        }
    }

    #endregion
}
