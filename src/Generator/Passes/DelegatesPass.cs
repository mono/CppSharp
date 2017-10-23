﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    public class DelegatesPass : TranslationUnitPass
    {
        public DelegatesPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitASTContext(ASTContext context)
        {
            bool result = base.VisitASTContext(context);

            foreach (var @delegate in delegates)
                @delegate.Namespace.Declarations.Add(@delegate);
            delegates.Clear();

            if (!Options.IsCSharpGenerator)
                return result;

            foreach (var module in Options.Modules.Where(namespacesDelegates.ContainsKey))
            {
                var @namespace = namespacesDelegates[module];
                @namespace.Namespace.Declarations.Add(@namespace);
            }

            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.Ignore)
                return false;

            return base.VisitClassDecl(@class);
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore)
                return false;

            method.FunctionType = CheckForDelegate(method.FunctionType, method);

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if((field.QualifiedType.Type is PointerType) && ((PointerType)field.QualifiedType.Type).Pointee is FunctionType)
            {
                field.QualifiedType = CheckForDelegate(field.QualifiedType, field);
                return true;
            }
            return false;
        }

        public override bool VisitInjectedClassNameType(InjectedClassNameType injected, TypeQualifiers quals)
        {
            return base.VisitInjectedClassNameType(injected, quals);
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            return base.VisitVariableDecl(variable);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) || function.Ignore)
                return false;

            function.ReturnType = CheckForDelegate(function.ReturnType, function);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (!base.VisitDeclaration(parameter) || parameter.Namespace == null ||
                parameter.Namespace.Ignore)
                return false;

            parameter.QualifiedType = CheckForDelegate(parameter.QualifiedType, parameter);

            return true;
        }

        private QualifiedType CheckForDelegate(QualifiedType type, ITypedDecl decl)
        {
            if (type.Type is TypedefType)
                return type;

            var desugared = type.Type.Desugar();
            if (desugared.IsDependent)
                return type;

            Type pointee = desugared.GetPointee() ?? desugared;
            if (pointee is TypedefType)
                return type;

            var functionType = pointee.Desugar() as FunctionType;
            if (functionType == null)
                return type;

            TypedefDecl @delegate = GetDelegate(type, decl);
            return new QualifiedType(new TypedefType { Declaration = @delegate });
        }

        private TypedefDecl GetDelegate(QualifiedType type, ITypedDecl typedDecl)
        {
            var decl = (Declaration)typedDecl;
            string delegateName = null;
            FunctionType newFunctionType = GetNewFunctionType(typedDecl, type);
            if (Options.GenerateRawCBindings && decl.Namespace is Function)
                delegateName = $"{decl.Namespace.Name}_delegate";

            else delegateName = GetDelegateName(newFunctionType, TypePrinter);
            var access = typedDecl is Method ? AccessSpecifier.Private : AccessSpecifier.Public;
         
            Module module = decl.TranslationUnit.Module;
            var existingDelegate = delegates.Find(t => Match(t, delegateName, module));
            if (existingDelegate != null)
            {
                // Ensure a delegate used for a virtual method and a type is public
                if (existingDelegate.Access == AccessSpecifier.Private &&
                    access == AccessSpecifier.Public)
                    existingDelegate.Access = access;

                // Check if there is an existing delegate with a different calling convention
                if (((FunctionType) existingDelegate.Type.GetPointee()).CallingConvention ==
                    newFunctionType.CallingConvention)
                    return existingDelegate;

                // Add a new delegate with the calling convention appended to its name
                delegateName += '_' + newFunctionType.CallingConvention.ToString();
                existingDelegate = delegates.Find(t => Match(t, delegateName, module));
                if (existingDelegate != null)
                    return existingDelegate;
            }

            var namespaceDelegates = GetDeclContextForDelegates(decl.Namespace);
            var delegateType = new QualifiedType(new PointerType(new QualifiedType(newFunctionType)));
            existingDelegate = new TypedefDecl
                {
                    Access = access,
                    Name = delegateName,
                    Namespace = namespaceDelegates,
                    QualifiedType = delegateType,
                    IsSynthetized = true
                };
            delegates.Add(existingDelegate);

            return existingDelegate;
        }

        private FunctionType GetNewFunctionType(ITypedDecl decl, QualifiedType type)
        {
            var functionType = new FunctionType();
            var method = decl as Method;
            if (method != null && method.FunctionType == type)
            {
                functionType.Parameters.AddRange(
                    method.GatherInternalParams(Context.ParserOptions.IsItaniumLikeAbi, true));
                functionType.CallingConvention = method.CallingConvention;
                functionType.IsDependent = method.IsDependent;
                functionType.ReturnType = method.ReturnType;
            }
            else
            {
                var funcTypeParam = (FunctionType) decl.Type.Desugar().GetFinalPointee().Desugar();
                functionType = new FunctionType(funcTypeParam);
            }

            for (int i = 0; i < functionType.Parameters.Count; i++)
                functionType.Parameters[i].Name = $"_{i}";

            return functionType;
        }

        private static bool Match(TypedefDecl t, string delegateName, Module module)
        {
            return t.Name == delegateName &&
                (module == t.TranslationUnit.Module ||
                 module.Dependencies.Contains(t.TranslationUnit.Module));
        }

        private DeclarationContext GetDeclContextForDelegates(DeclarationContext @namespace)
        {
            if (Options.GenerateRawCBindings && @namespace is Class)
            {
                return @namespace.Namespace;
            }
            if (Options.IsCLIGenerator || Options.GenerateRawCBindings)
                return @namespace is Function ? @namespace.Namespace : @namespace;

            

            var module = @namespace.TranslationUnit.Module;
            if (namespacesDelegates.ContainsKey(module))
                return namespacesDelegates[module];

            Namespace parent = null;
            if (string.IsNullOrEmpty(module.OutputNamespace))
            {
                var groups = module.Units.SelectMany(u => u.Declarations).OfType<Namespace>(
                    ).GroupBy(d => d.Name).Where(g => g.Any(d => d.HasDeclarations)).ToList();
                if (groups.Count == 1)
                    parent = groups.Last().Last();
            }

            if (parent == null)
                parent = module.Units.Last();

            var namespaceDelegates = new Namespace
                {
                    Name = "Delegates",
                    Namespace = parent
                };
            namespacesDelegates.Add(module, namespaceDelegates);

            return namespaceDelegates;
        }

        internal static string GetDelegateName(FunctionType functionType, CppSharp.Generators.TypePrinter typePrinter)
        {
        

            var typesBuilder = new StringBuilder();
            if (!functionType.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                typesBuilder.Insert(0, functionType.ReturnType.Visit(typePrinter));
                typesBuilder.Append('_');
            }

            foreach (var parameter in functionType.Parameters)
            {
                typesBuilder.Append(parameter.Visit(typePrinter));
                typesBuilder.Append('_');
            }

            if (typesBuilder.Length > 0)
                typesBuilder.Remove(typesBuilder.Length - 1, 1);

            var delegateName = FormatTypesStringForIdentifier(typesBuilder);
            if (functionType.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void))
                delegateName.Insert(0, "Action_");
            else
                delegateName.Insert(0, "Func_");

            return delegateName.ToString();
        }

        private static StringBuilder FormatTypesStringForIdentifier(StringBuilder types)
        {
            // TODO: all of this needs proper general fixing by only leaving type names
            return types.Replace("global::System.", string.Empty)
                .Replace("[MarshalAs(UnmanagedType.LPStr)] ", string.Empty)
                .Replace("[MarshalAs(UnmanagedType.LPWStr)] ", string.Empty)
                .Replace("global::", string.Empty).Replace("*", "Ptr")
                .Replace('.', '_').Replace(' ', '_').Replace("::", "_")
                .Replace("[]", "Array");
        }

        private CSharpTypePrinter TypePrinter
        {
            get
            {
                if (typePrinter == null)
                {
                    typePrinter = new CSharpTypePrinter(Context);
                    typePrinter.PushContext(TypePrinterContextKind.Native);
                    typePrinter.PushMarshalKind(MarshalKind.GenericDelegate);
                }
                return typePrinter;
            }
        }

        private Dictionary<Module, DeclarationContext> namespacesDelegates = new Dictionary<Module, DeclarationContext>();
        private CSharpTypePrinter typePrinter;

        /// <summary>
        /// The generated typedefs. The tree can't be modified while
        /// iterating over it, so we collect all the typedefs and add them at the end.
        /// </summary>
        private readonly List<TypedefDecl> delegates = new List<TypedefDecl>();
    }
}
