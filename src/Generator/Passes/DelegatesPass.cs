using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    public class DelegatesPass : TranslationUnitPass
    {
        public const string DelegatesNamespace = "Delegates";

        public class DelegateDefinition
        {
            public DelegateDefinition(string @namespace, string signature)
            {
                Namespace = @namespace;
                Signature = signature;
            }

            public string Namespace { get; }

            public string Signature { get; }
        }

        public DelegatesPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitASTContext(ASTContext context)
        {
            foreach (var module in Options.Modules)
                libsDelegates[module] = new Dictionary<string, DelegateDefinition>();

            var unit = context.TranslationUnits.GetGenerated().LastOrDefault();

            if (unit == null)
                return false;

            var result = base.VisitASTContext(context);

            foreach (var module in Options.Modules.Where(m => namespacesDelegates.ContainsKey(m)))
                namespacesDelegates[module].Namespace.Declarations.Add(namespacesDelegates[module]);

            return result;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore)
                return false;

            var @params = method.GatherInternalParams(Context.ParserOptions.IsItaniumLikeAbi, true).ToList();
            var delegateName = GenerateDelegateSignature(@params, method.ReturnType);

            var module = method.TranslationUnit.Module;

            Namespace namespaceDelegates;
            if (namespacesDelegates.ContainsKey(module))
            {
                namespaceDelegates = namespacesDelegates[module];
            }
            else
            {
                Namespace parent = null;
                if (string.IsNullOrEmpty(module.OutputNamespace))
                {
                    var group = module.Units.SelectMany(u => u.Declarations).OfType<Namespace>(
                        ).GroupBy(d => d.Name).Where(g => g.Any(d => d.HasDeclarations)).ToList();
                    if (group.Count == 1)
                        parent = group.Last().Last();
                }
                if (parent == null)
                    parent = module.Units.Last();
                namespaceDelegates = new Namespace
                {
                    Name = DelegatesNamespace,
                    Namespace = parent
                };
                namespacesDelegates.Add(module, namespaceDelegates);
            }

            var @delegate = new TypedefDecl
                {
                    Name = delegateName,
                    QualifiedType = new QualifiedType(
                        new PointerType(
                            new QualifiedType(
                                new FunctionType
                                {
                                    CallingConvention = method.CallingConvention,
                                    IsDependent = method.IsDependent,
                                    Parameters = @params,
                                    ReturnType = method.ReturnType
                                }))),
                    Namespace = namespaceDelegates,
                    IsSynthetized = true,
                    Access = AccessSpecifier.Private
                };

            var delegateString = @delegate.Visit(TypePrinter).Type;
            var existingDelegate = GetExistingDelegate(
                method.TranslationUnit.Module, delegateString);

            if (existingDelegate != null)
            {
                Context.Delegates.Add(method, existingDelegate);
                return true;
            }

            existingDelegate = new DelegateDefinition(module.OutputNamespace, delegateString);
            Context.Delegates.Add(method, existingDelegate);

            libsDelegates[module].Add(delegateString, existingDelegate);

            namespaceDelegates.Declarations.Add(@delegate);

            return true;
        }

        private DelegateDefinition GetExistingDelegate(Module module, string delegateString)
        {
            if (module.Libraries.Count == 0)
                return Context.Delegates.Values.FirstOrDefault(t => t.Signature == delegateString);

            DelegateDefinition @delegate = null;
            if (new[] { module }.Union(module.Dependencies).Any(
                m => libsDelegates.ContainsKey(m) && libsDelegates[m].TryGetValue(
                    delegateString, out @delegate)))
                return @delegate;

            return null;
        }

        private string GenerateDelegateSignature(IEnumerable<Parameter> @params, QualifiedType returnType)
        {
            var typesBuilder = new StringBuilder();
            if (!returnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                typesBuilder.Insert(0, returnType.Type.CSharpType(TypePrinter));
                typesBuilder.Append('_');
            }
            foreach (var parameter in @params)
            {
                typesBuilder.Append(parameter.CSharpType(TypePrinter));
                typesBuilder.Append('_');
            }
            if (typesBuilder.Length > 0)
                typesBuilder.Remove(typesBuilder.Length - 1, 1);
            var delegateName = FormatTypesStringForIdentifier(typesBuilder);
            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
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
                .Replace('.', '_').Replace(' ', '_').Replace("::", "_");
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

        private Dictionary<Module, Namespace> namespacesDelegates = new Dictionary<Module, Namespace>();
        private CSharpTypePrinter typePrinter;
        private static readonly Dictionary<Module, Dictionary<string, DelegateDefinition>> libsDelegates =
            new Dictionary<Module, Dictionary<string, DelegateDefinition>>();
    }
}
