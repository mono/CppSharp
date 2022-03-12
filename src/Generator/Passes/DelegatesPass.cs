using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    public class DelegatesPass : TranslationUnitPass
    {
        public DelegatesPass() => VisitOptions.ClearFlags(
            VisitFlags.ClassBases | VisitFlags.FunctionReturnType |
            VisitFlags.NamespaceEnums | VisitFlags.NamespaceTemplates |
            VisitFlags.TemplateArguments);

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

        public override bool VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
        {
            if (!base.VisitClassTemplateSpecializationDecl(specialization) ||
                !specialization.IsGenerated || !specialization.TemplatedDecl.TemplatedDecl.IsGenerated)
                return false;

            foreach (TemplateArgument arg in specialization.Arguments.Where(
                a => a.Kind == TemplateArgument.ArgumentKind.Type))
            {
                arg.Type = CheckForDelegate(arg.Type, specialization);
            }

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore)
                return false;

            var functionType = new FunctionType
            {
                CallingConvention = method.CallingConvention,
                IsDependent = method.IsDependent,
                ReturnType = method.ReturnType
            };

            TypePrinter.PushMarshalKind(MarshalKind.VTableReturnValue);
            functionType.Parameters.AddRange(
                method.GatherInternalParams(Context.ParserOptions.IsItaniumLikeAbi, true));

            method.FunctionType = CheckForDelegate(new QualifiedType(functionType),
                method.Namespace, @private: true);
            TypePrinter.PopMarshalKind();

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) || function.Ignore)
                return false;

            function.ReturnType = CheckForDelegate(function.ReturnType,
                function.Namespace);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (parameter.Namespace?.TranslationUnit?.Module == null && namespaces.Count > 0)
                parameter.Namespace = namespaces.Peek();

            if (!base.VisitDeclaration(parameter) || parameter.Namespace == null ||
                parameter.Namespace.Ignore)
                return false;

            parameter.QualifiedType = CheckForDelegate(parameter.QualifiedType,
                parameter.Namespace);

            return true;
        }

        public override bool VisitProperty(Property property)
        {
            if (!base.VisitProperty(property))
                return false;

            property.QualifiedType = CheckForDelegate(property.QualifiedType,
                property.Namespace);

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (field.Namespace?.TranslationUnit?.Module != null)
                namespaces.Push(field.Namespace);

            if (!base.VisitFieldDecl(field))
                return false;

            field.QualifiedType = CheckForDelegate(field.QualifiedType,
                field.Namespace);

            namespaces.Clear();

            return true;
        }

        private QualifiedType CheckForDelegate(QualifiedType type,
            DeclarationContext declarationContext, bool @private = false)
        {
            if (type.Type is TypedefType)
                return type;

            var desugared = type.Type.Desugar();
            if (desugared.IsDependent)
                return type;

            Type pointee = desugared.GetPointee() ?? desugared;
            if (pointee is TypedefType)
                return type;

            desugared = pointee.Desugar();
            FunctionType functionType = desugared as FunctionType;
            if (functionType == null && !desugared.IsPointerTo(out functionType))
                return type;

            TypedefDecl @delegate = GetDelegate(functionType, declarationContext, @private);
            return new QualifiedType(new TypedefType { Declaration = @delegate });
        }

        private TypedefDecl GetDelegate(FunctionType functionType,
            DeclarationContext declarationContext, bool @private = false)
        {
            var delegateName = GetDelegateName(functionType);
            var access = @private ? AccessSpecifier.Private : AccessSpecifier.Public;
            Module module = declarationContext.TranslationUnit.Module;
            var existingDelegate = delegates.Find(t => Match(t, delegateName, module));
            if (existingDelegate != null)
            {
                // Ensure a delegate used for a virtual method and a type is public
                if (existingDelegate.Access == AccessSpecifier.Private &&
                    access == AccessSpecifier.Public)
                    existingDelegate.Access = access;

                // Check if there is an existing delegate with a different calling convention
                if (((FunctionType)existingDelegate.Type.GetPointee()).CallingConvention ==
                    functionType.CallingConvention)
                    return existingDelegate;

                // Add a new delegate with the calling convention appended to its name
                delegateName += '_' + functionType.CallingConvention.ToString();
                existingDelegate = delegates.Find(t => Match(t, delegateName, module));
                if (existingDelegate != null)
                    return existingDelegate;
            }

            var namespaceDelegates = GetDeclContextForDelegates(declarationContext);
            var delegateType = new QualifiedType(new PointerType(new QualifiedType(functionType)));
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

        private static bool Match(TypedefDecl t, string delegateName, Module module)
        {
            return t.Name == delegateName &&
                (module == t.TranslationUnit.Module ||
                 module.Dependencies.Contains(t.TranslationUnit.Module));
        }

        private DeclarationContext GetDeclContextForDelegates(DeclarationContext @namespace)
        {
            if (Options.IsCLIGenerator)
                return @namespace is Function ? @namespace.Namespace : @namespace;

            var module = @namespace.TranslationUnit.Module;
            if (namespacesDelegates.ContainsKey(module))
                return namespacesDelegates[module];

            Namespace parent = null;
            if (string.IsNullOrEmpty(module.OutputNamespace))
            {
                var groups = module.Units.Where(u => u.IsGenerated).SelectMany(
                    u => u.Declarations).OfType<Namespace>(
                    ).GroupBy(d => d.Name).Where(g => g.Any(d => d.HasDeclarations)).ToList();
                if (groups.Count == 1)
                    parent = groups.Last().Last();
                else
                {
                    foreach (var g in groups)
                    {
                        parent = g.ToList().Find(ns => ns.Name == module.LibraryName || ns.Name == module.OutputNamespace);
                        if (parent != null)
                            break;
                    }
                }
            }

            if (parent == null)
                parent = module.Units.Last(u => u.IsGenerated);

            var namespaceDelegates = new Namespace
            {
                Name = "Delegates",
                Namespace = parent
            };
            namespacesDelegates.Add(module, namespaceDelegates);

            return namespaceDelegates;
        }

        private string GetDelegateName(FunctionType functionType)
        {
            var typesBuilder = new StringBuilder();
            if (!functionType.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                typesBuilder.Insert(0, functionType.ReturnType.Visit(TypePrinter));
                typesBuilder.Append('_');
            }

            foreach (var parameter in functionType.Parameters)
            {
                typesBuilder.Append(parameter.Visit(TypePrinter));
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
                .Replace("[MarshalAs(UnmanagedType.LPUTF8Str)] ", string.Empty)
                .Replace("[MarshalAs(UnmanagedType.LPWStr)] string", "wstring")
                .Replace("[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF8Marshaller))] string", "string8")
                .Replace("[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF32Marshaller))] string", "string32")
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
        private readonly Stack<DeclarationContext> namespaces = new Stack<DeclarationContext>();
    }
}
