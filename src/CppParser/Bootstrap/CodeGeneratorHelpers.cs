using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;

namespace CppSharp
{
    class TemplateArgumentHandler
    {
        public static string GetTemplateArgumentTypeName(TemplateArgument arg, CppTypePrinter printer)
        {
            switch (arg.Kind)
            {
                case TemplateArgument.ArgumentKind.Type:
                    return GetTemplateTypeArgName(arg.Type, printer);
                case TemplateArgument.ArgumentKind.Declaration:
                    return GetTemplateDeclArgName(arg.Declaration, printer);
                case TemplateArgument.ArgumentKind.NullPtr:
                    return "nullptr_t";
                case TemplateArgument.ArgumentKind.Integral:
                    return GetTemplateIntegralArgName(arg.Integral, printer);
                default:
                    throw new NotImplementedException($"Unhandled template argument kind: {arg.Kind}");
            }
        }

        private static string GetTemplateTypeArgName(QualifiedType type, CppTypePrinter printer)
        {
            var typeStr = type.Type.Visit(printer).Type;
            if (type.Type.IsPointer())
                return $"{typeStr}*";
            return typeStr;
        }

        private static string GetTemplateDeclArgName(Declaration decl, CppTypePrinter printer)
        {
            return decl?.Visit(printer).Type ?? "nullptr";
        }

        private static string GetTemplateIntegralArgName(long value, CppTypePrinter printer)
        {
            return value.ToString();
        }
    }

    internal static class CodeGeneratorHelpers
    {
        internal static CppTypePrinter CppTypePrinter;

        public static bool IsAbstractStmt(Class @class) =>
            IsAbstractStmt(@class.Name);

        public static bool IsAbstractStmt(string @class) =>
            @class is "Stmt"
                or "ValueStmt"
                or "NoStmt"
                or "SwitchCase"
                or "AsmStmt"
                or "Expr"
                or "FullExpr"
                or "CastExpr"
                or "ExplicitCastExpr"
                or "AbstractConditionalOperator"
                or "CXXNamedCastExpr"
                or "OverloadExpr"
                or "CoroutineSuspendExpr";

        public static bool SkipClass(Class @class)
        {
            if (!@class.IsGenerated)
                return true;

            if (@class.Access != AccessSpecifier.Public)
                return true;

            return false;
        }

        public static bool SkipProperty(Property property, bool skipBaseCheck = false)
        {
            if (!property.IsGenerated)
                return true;

            if (property.Access != AccessSpecifier.Public)
                return true;

            var @class = property.Namespace as Class;

            if (!skipBaseCheck)
            {
                if (@class.GetBaseProperty(property) != null)
                    return true;
            }

            if (property.Name is "beginLoc" or "endLoc" && @class.Name != "Stmt")
                return true;

            switch (property.Name)
            {
                case "stmtClass":
                case "stmtClassName":
                    return true;
                case "isOMPStructuredBlock":
                    return true;
            }

            var typeName = property.Type.Visit(CppTypePrinter).Type;

            // General properties.
            if (typeName.Contains("_iterator") ||
                typeName.Contains("_range") ||
                property.Name.Contains("_begin") ||
                property.Name.Contains("_end") ||
                property.Name.Contains("_empty") ||
                property.Name.Contains("_size"))
                return true;


            return false;
            //
            // Statement properties.
            //

            /*if (typeName.Contains("LabelDecl") ||
                typeName.Contains("VarDecl") ||
                typeName.Contains("Token") ||
                typeName.Contains("CapturedDecl") ||
                typeName.Contains("CapturedRegionKind") ||
                typeName.Contains("RecordDecl") ||
                typeName.Contains("StringLiteral") ||
                typeName.Contains("SwitchCase") ||
                typeName.Contains("CharSourceRange") ||
                typeName.Contains("NestedNameSpecifierLoc") ||
                typeName.Contains("DeclarationNameInfo") ||
                typeName.Contains("DeclGroupRef"))
                return true;*/

            //
            // Expressions
            //

            // CastExpr
            if (property.Name == "targetUnionField")
                return true;

            // ShuffleVectorExprClass
            if (property.Name == "subExprs")
                return true;

            // InitListExprClass
            if (property.Name == "initializedFieldInUnion")
                return true;

            // ParenListExprClass
            if (property.Name == "exprs" && @class.Name == "ParenListExpr")
                return true;

            // EvalResult
            if (typeName.Contains("ExprValueKind") ||
                typeName.Contains("ExprObjectKind") ||
                typeName.Contains("ObjC"))
                return true;

            // DeclRefExpr
            if (typeName.Contains("ValueDecl") ||
                typeName.Contains("NestedNameSpecifier") ||
                typeName.Contains("TemplateArgumentLoc"))
                return true;

            // FloatingLiteral
            if (typeName.Contains("APFloatSemantics") ||
                typeName.Contains("fltSemantics") ||
                typeName.Contains("APFloat"))
                return true;

            // OffsetOfExpr
            // UnaryExprOrTypeTraitExpr
            // CompoundLiteralExpr
            // ExplicitCastExpr
            // ConvertVectorExpr
            // VAArgExpr
            if (typeName.Contains("TypeSourceInfo"))
                return true;

            // MemberExpr
            if (typeName.Contains("ValueDecl") ||
                typeName.Contains("DeclAccessPair") ||
                typeName.Contains("NestedNameSpecifier") ||
                typeName.Contains("TemplateArgumentLoc"))
                return true;

            // BinaryOperator
            if (typeName.Contains("FPOptions"))
                return true;

            // DesignatedInitExpr
            // ExtVectorElementExpr
            if (typeName.Contains("IdentifierInfo"))
                return true;

            // BlockExpr
            if (typeName.Contains("BlockDecl"))
                return true;

            // ArrayInitLoopExpr
            if (typeName.Contains("APInt"))
                return true;

            // MemberExpr
            if (typeName.Contains("BlockExpr") ||
                typeName.Contains("FunctionProtoType"))
                return true;

            //
            // C++ expression properties.
            //

            // MSPropertyRefExpr
            if (typeName.Contains("MSPropertyDecl"))
                return true;

            // CXXBindTemporaryExpr
            if (typeName.Contains("CXXTemporary"))
                return true;

            // CXXConstructExpr
            // CXXInheritedCtorInitExpr
            if (typeName.Contains("CXXConstructorDecl") ||
                typeName.Contains("ConstructionKind"))
                return true;

            // CXXInheritedCtorInitExpr
            if (typeName.Contains("LambdaCaptureDefault") ||
                typeName.Contains("TemplateParameterList"))
                return true;

            // CXXNewExpr
            if (property.Name == "placementArgs")
                return true;

            // TypeTraitExpr
            if (typeName == "TypeTrait")
                return true;

            // ArrayTypeTraitExpr
            if (typeName.Contains("ArrayTypeTrait"))
                return true;

            // ExpressionTraitExpr
            if (typeName.Contains("ExpressionTrait"))
                return true;

            // OverloadExpr
            // DependentScopeDeclRefExpr
            // UnresolvedMemberExpr
            if (typeName.Contains("DeclarationName"))
                return true;

            // SubstNonTypeTemplateParmExpr
            // SubstNonTypeTemplateParmPackExpr
            if (typeName.Contains("NonTypeTemplateParmDecl"))
                return true;

            // MaterializeTemporaryExpr
            if (typeName.Contains("StorageDuration"))
                return true;

            if (typeName.Contains("ArrayRef"))
                return true;

            // AtomicExpr
            if (typeName.Contains("unique_ptr<AtomicScopeModel, default_delete<AtomicScopeModel>>"))
                return true;

            if (typeName.Contains("Expr**"))
                return true;

            // GenericSelectionExpr
            if (typeName.Contains("AssociationIteratorTy"))
                return true;

            // CXXRewrittenBinaryOperator
            if (typeName.Contains("DecomposedForm"))
                return true;

            if (typeName.Contains("optional"))
                return true;

            // ConstantExpr (TODO: Fix this properly)
            if (property.Name.Contains("resultAsAP") ||
                property.Name.Contains("aPValueResult") ||
                property.Name.Contains("resultAPValueKind"))
                return true;

            return false;
        }

        public static bool SkipMethod(Method method)
        {
            if (method.IsGenerated)
                return true;

            var @class = method.Namespace as Class;
            if (@class.GetBaseMethod(method) != null)
                return true;

            if (method.Name == "children")
                return true;

            // CastExpr
            if (method.Name == "path")
                return true;

            // CXXNewExpr
            if (method.Name == "placement_arguments" && method.IsConst)
                return true;

            var typeName = method.ReturnType.Visit(CppTypePrinter).Type;
            if (typeName.Contains("const"))
                return true;

            if (!typeName.Contains("range"))
                return true;

            // OverloadExpr
            if (typeName.Contains("UnresolvedSet"))
                return true;

            // LambdaExpr
            if (method.Name == "captures")
                return true;

            var iteratorType = GetIteratorType(method);
            string iteratorTypeName = GetIteratorTypeName(iteratorType, CppTypePrinter);
            if (iteratorTypeName.Contains("LambdaCapture"))
                return true;

            // GenericSelectionExpr
            if (iteratorTypeName.Contains("AssociationIteratorTy"))
                return true;

            return false;
        }

        public static string GetQualifiedName(Declaration decl,
            TypePrinter typePrinter)
        {
            typePrinter.PushScope(TypePrintScopeKind.Qualified);
            var qualifiedName = decl.Visit(typePrinter).Type;
            typePrinter.PopScope();

            qualifiedName = CleanClangNamespaceFromName(qualifiedName);

            if (qualifiedName.Contains("ExprDependenceScope"))
                qualifiedName = qualifiedName
                    .Replace("ExprDependenceScope" + (typePrinter is CppTypePrinter ? "::" : ".")
                        , "");

            else if (qualifiedName.Contains("Semantics"))
                qualifiedName = qualifiedName.Replace(
                    typePrinter is CppTypePrinter ? "llvm::APFloatBase::Semantics" : "llvm.APFloatBase.Semantics"
                    , "FloatSemantics");

            return qualifiedName;
        }

        public static string GetQualifiedName(Declaration decl) =>
            GetQualifiedName(decl, CppTypePrinter);

        private static string CleanClangNamespaceFromName(string qualifiedName)
        {
            qualifiedName = qualifiedName.StartsWith("clang::")
                ? qualifiedName.Substring("clang::".Length)
                : qualifiedName;

            qualifiedName = qualifiedName.StartsWith("clang.")
                ? qualifiedName.Substring("clang.".Length)
                : qualifiedName;

            return qualifiedName;
        }

        public static string GetDeclName(Declaration decl, GeneratorKind kind)
        {
            string name = decl.Name;

            if (kind == GeneratorKind.CPlusPlus)
            {
                if (name == "inline")
                {
                    name = "isInline";
                }
                else if (Generators.C.CCodeGenerator.IsReservedKeyword(name))
                {
                    name = $"_{name}";
                }
            }
            else if (kind == GeneratorKind.CSharp)
            {
                bool hasConflict = false;
                switch (name)
                {
                    case "identKind":
                    case "literalOperatorKind":
                    case "resultStorageKind":
                    case "aDLCallKind":
                    case "initializationStyle":
                    case "capturedStmt":
                        hasConflict = true;
                        break;
                }

                if (!hasConflict)
                    name = CSharpSources.SafeIdentifier(
                        CaseRenamePass.ConvertCaseString(decl,
                            RenameCasePattern.UpperCamelCase));
            }
            else throw new NotImplementedException();

            return name;
        }

        public static string GetDeclName(Declaration decl)
        {
            return GetDeclName(decl, GeneratorKind.CPlusPlus);
        }

        public static AST.Type GetDeclType(AST.Type type,
            TypePrinter typePrinter)
        {
            var qualifiedType = new QualifiedType(type);
            if (qualifiedType.Type.IsPointerTo(out TagType _))
                qualifiedType = qualifiedType.StripConst();

            var typeName = qualifiedType.Type.Visit(typePrinter).Type;
            if (typeName.Contains("StringRef") || typeName.Contains("string"))
                type = new BuiltinType(PrimitiveType.String);

            return type;
        }

        public static string GetDeclTypeName(ITypedDecl decl) =>
            GetDeclTypeName(decl.Type, CppTypePrinter);

        public static string GetDeclTypeName(AST.Type type,
            TypePrinter typePrinter)
        {
            var declType = GetDeclType(type, typePrinter);
            var typeResult = declType.Visit(typePrinter);

            if (typeResult.Type.Contains("MSGuidDecl"))
                return typePrinter is CppTypePrinter
                    ? "std::string"
                    : "string";

            var typeName = typeResult.ToString();
            typeName = CleanClangNamespaceFromName(typeName);

            if (typeName.Contains("QualType"))
                typeName = "QualifiedType";
            else if (typeName.Contains("UnaryOperator::Opcode"))
                typeName = "UnaryOperatorKind";
            else if (typeName.Contains("BinaryOperator::Opcode"))
                typeName = "BinaryOperatorKind";

            else if (typeName.Contains("Semantics"))
                typeName = "FloatSemantics";

            else if (typeName.Contains("optional"))
            {
                if (typePrinter is CppTypePrinter)
                {
                    typeName = "std::" + typeName;
                }
                else
                {
                    var optType = (declType as TemplateSpecializationType)!.Arguments[0].Type;
                    typeResult = optType.Visit(typePrinter);
                    typeName = $"{typeResult}?";
                }
            }

            return typeName;
        }

        public static AST.Type GetIteratorType(Method method)
        {
            var retType = method.ReturnType.Type.Desugar();

            if (retType is TemplateSpecializationType templateSpecType)
                retType = templateSpecType.Arguments[0].Type.Type.Desugar();

            if (retType.IsPointerTo(out PointerType pointee))
                retType = pointee;

            return retType;
        }

        public static string GetIteratorTypeName(AST.Type iteratorType, TypePrinter typePrinter)
        {
            if (iteratorType.IsPointer())
                iteratorType = iteratorType.GetFinalPointee();

            typePrinter.PushScope(TypePrintScopeKind.Qualified);
            var iteratorTypeName = iteratorType.Visit(typePrinter).Type;
            typePrinter.PopScope();

            iteratorTypeName = CleanClangNamespaceFromName(iteratorTypeName);

            if (iteratorTypeName.Contains("ExprIterator"))
                iteratorTypeName = "Expr";

            else if (iteratorTypeName.Contains("StmtIterator"))
                iteratorTypeName = "Stmt";

            else if (iteratorTypeName.Contains("CastIterator") ||
                     iteratorTypeName.Contains("DeclContext::"))
            {
                if (iteratorType is TypedefType typedefType)
                    iteratorType = typedefType.Declaration.Type;

                var templateArg = ((TemplateSpecializationType)iteratorType).Arguments[0];
                iteratorType = templateArg.Type.Type;

                typePrinter.PushScope(TypePrintScopeKind.Qualified);
                iteratorTypeName = iteratorType.Visit(typePrinter).Type;
                typePrinter.PopScope();

                iteratorTypeName = CleanClangNamespaceFromName(iteratorTypeName);
            }

            if (typePrinter is CppTypePrinter)
                return $"{iteratorTypeName}*";

            return iteratorTypeName;
        }

        public static List<Class> GetBaseClasses(Class @class)
        {
            var baseClasses = new List<Class>();

            Class currentClass = @class;
            while (currentClass != null)
            {
                baseClasses.Add(currentClass);
                currentClass = currentClass.HasBaseClass ? currentClass.BaseClass : null;
            }

            baseClasses.Reverse();
            return baseClasses;
        }

        public static string RemoveFromEnd(string s, string suffix)
        {
            return s.EndsWith(suffix) ? s.Substring(0, s.Length - suffix.Length) : s;
        }

        public static string FirstLetterToUpperCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("There is no first letter");

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static bool SkipDeclProperty(Property property)
        {
            if (!property.IsGenerated || property.Access != AccessSpecifier.Public)
                return true;

            var @class = property.Namespace as Class;

            if (@class.GetBaseProperty(property) != null)
                return true;

            var typeName = property.Type.Visit(CppTypePrinter).Type;

            // Skip properties that deal with source locations/ranges
            if (typeName.Contains("SourceLocation") || typeName.Contains("SourceRange"))
                return true;

            // Skip Clang-specific internal properties
            if (typeName.Contains("ASTContext") || typeName.Contains("DeclContext"))
                return true;

            // Skip template-specific properties that are handled separately
            if (typeName.Contains("TemplateParameterList") ||
                typeName.Contains("TemplateArgument"))
                return true;

            return false;
        }

        public static bool SkipTypeProperty(Property property)
        {
            if (!property.IsGenerated || property.Access != AccessSpecifier.Public)
                return true;

            var @class = property.Namespace as Class;

            if (@class.GetBaseProperty(property) != null)
                return true;

            var typeName = property.Type.Visit(CppTypePrinter).Type;

            // Skip source location properties
            if (typeName.Contains("SourceLocation"))
                return true;

            // Skip internal Clang type properties
            if (typeName.Contains("TypeLoc") || typeName.Contains("ASTContext"))
                return true;

            return false;
        }

        public static bool IsAbstractType(Class @class) =>
            @class.Name is "Type"
                or "ArrayType"
                or "TagType"
                or "FunctionType";

        public static bool IsAbstractDecl(Class @class) =>
            @class.Name is "Decl"
                or "NamedDecl"
                or "ValueDecl"
                or "TypeDecl"
                or "DeclContext";

        public static string GetPropertyAccessorName(Property property, bool isGetter)
        {
            var baseName = FirstLetterToUpperCase(GetDeclName(property));
            return isGetter ? $"Get{baseName}" : $"Set{baseName}";
        }

        public static string GetPropertyTypeName(Property property, bool includeNamespace = true)
        {
            var typeName = GetDeclTypeName(property);

            // Handle special cases
            if (typeName.Contains("QualType") ||
                (property.Type.IsPointerTo(out TagType tagType) &&
                 tagType.Declaration?.Name.Contains("Type") == true))
            {
                return includeNamespace ? $"AST::{typeName}" : typeName;
            }

            // Handle template types
            if (property.Type is TemplateSpecializationType templateType)
            {
                var templateName = templateType.Template.TemplatedDecl.Name;
                return includeNamespace ? $"AST::{templateName}" : templateName;
            }

            return typeName;
        }

        public static bool IsTypeProperty(Property property)
        {
            if (property.Type.IsPointerTo(out TagType tagType))
                return tagType.Declaration?.Name.Contains("Type") == true;

            var typeName = GetDeclTypeName(property);
            return typeName.Contains("QualType") ||
                   typeName.Contains("Type") ||
                   (property.Type is TemplateSpecializationType templateType &&
                    templateType.Template.TemplatedDecl.Name.Contains("Type"));
        }
    }
}