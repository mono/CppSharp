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

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            // dependent types with virtuals have no own virtual layouts
            // so virtuals are considered different objects in template instantiations
            // therefore the method itself won't be visited, so let's visit it through the v-table
            if (Context.ParserOptions.IsMicrosoftAbi)
            {
                foreach (var method in from vfTable in @class.Layout.VFTables
                                       from component in vfTable.Layout.Components
                                       where component.Method != null
                                       select component.Method)
                    VisitMethodDecl(method);
            }
            else
            {
                if (@class.Layout.Layout == null)
                    return false;

                foreach (var method in from component in @class.Layout.Layout.Components
                                       where component.Method != null
                                       select component.Method)
                    VisitMethodDecl(method);
            }

            return true;
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
            var delegateName = Helpers.FormatTypesStringForIdentifier(typesBuilder);
            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
                delegateName = "Action_" + delegateName;
            else
                delegateName = "Func_" + delegateName;

            return delegateName;
        }

        private CSharpTypePrinter TypePrinter
        {
            get
            {
                if (typePrinter == null)
                {
                    typePrinter = new CSharpTypePrinter(Context) { FullyQualify = false };
                    typePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                    typePrinter.PushMarshalKind(CSharpMarshalKind.GenericDelegate);
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
