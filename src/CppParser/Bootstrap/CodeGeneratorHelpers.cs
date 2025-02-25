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

            if (property.Name is "beginLoc" or "endLoc" &&
                @class.Name != "Stmt")
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

            //
            // Statement properties.
            //

            if (typeName.Contains("LabelDecl") ||
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
                return true;

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

            // General properties.
            if (typeName.Contains("_iterator") ||
                typeName.Contains("_range"))
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
            if (method.Ignore)
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
            qualifiedName = qualifiedName.StartsWith("clang::") ?
                qualifiedName.Substring("clang::".Length) : qualifiedName;

            qualifiedName = qualifiedName.StartsWith("clang.") ?
                qualifiedName.Substring("clang.".Length) : qualifiedName;

            return qualifiedName;
        }

        public static string GetDeclName(Declaration decl, GeneratorKind kind)
        {
            string name = decl.Name;

            if (kind == GeneratorKind.CPlusPlus)
            {
                if (Generators.C.CCodeGenerator.IsReservedKeyword(name))
                    name = $"_{name}";
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
            if (qualifiedType.Type.IsPointerTo(out TagType tagType))
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
                    ? "std::string" : "string";

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

            string className = null;
            if (typeName.Contains("FieldDecl"))
                className = "Field";
            else if (typeName.Contains("NamedDecl"))
                className = "Declaration";
            else if (typeName.Contains("CXXMethodDecl"))
                className = "Method";
            else if (typeName.Contains("FunctionDecl"))
                className = "Function";
            else if (typeName.Contains("FunctionTemplateDecl"))
                className = "FunctionTemplate";
            else if (typeName is "Decl" or "Decl*")
                className = "Declaration";

            if (className != null)
                return (typePrinter is CppTypePrinter) ?
                 $"{className}*" : className;

            return typeName;
        }

        public static AST.Type GetIteratorType(Method method)
        {
            var retType = method.ReturnType.Type;

            TemplateSpecializationType templateSpecType;
            TypedefType typedefType;
            TypedefNameDecl typedefNameDecl;

            if (retType is TemplateSpecializationType)
            {
                templateSpecType = retType as TemplateSpecializationType;
                typedefType = templateSpecType.Arguments[0].Type.Type as TypedefType;
                typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
            }
            else
            {
                typedefType = retType as TypedefType;
                typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
                templateSpecType = typedefNameDecl.Type as TemplateSpecializationType;
                typedefType = templateSpecType.Arguments[0].Type.Type as TypedefType;
                typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
                typedefType = typedefNameDecl.Type as TypedefType;
                if (typedefType != null)
                    typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
            }

            var iteratorType = typedefNameDecl.Type;
            if (iteratorType.IsPointerTo(out PointerType pointee))
                iteratorType = iteratorType.GetPointee();

            return iteratorType;
        }

        public static string GetIteratorTypeName(AST.Type iteratorType,
            TypePrinter typePrinter)
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

            else if (iteratorTypeName.Contains("CastIterator"))
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

            if (iteratorTypeName == "Decl")
                iteratorTypeName = "Declaration";

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
                currentClass = currentClass.HasBaseClass ?
                    currentClass.BaseClass : null;
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
    }
}