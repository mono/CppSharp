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

            public string Namespace { get; private set; }

            public string Signature { get; private set; }
        }

        public DelegatesPass()
        {
            Options.VisitClassBases = false;
            Options.VisitFunctionParameters = false;
            Options.VisitFunctionReturnType = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitTemplateArguments = false;
        }

        public override bool VisitLibrary(ASTContext context)
        {
            foreach (var library in Driver.Options.Modules.SelectMany(m => m.Libraries))
                libsDelegates[library] = new Dictionary<string, DelegateDefinition>();

            var unit = context.TranslationUnits.LastOrDefault(u => u.IsValid && u.IsGenerated &&
                !u.IsSystemHeader && u.HasDeclarations);

            if (unit == null)
                return false;

            var result = base.VisitLibrary(context);

            foreach (var module in Driver.Options.Modules.Where(m => namespacesDelegates.ContainsKey(m)))
                module.Units.Last(u => u.HasDeclarations).Declarations.Add(namespacesDelegates[module]);

            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            // dependent types with virtuals have no own virtual layouts
            // so virtuals are considered different objects in template instantiations
            // therefore the method itself won't be visited, so let's visit it through the v-table
            if (Driver.Options.IsMicrosoftAbi)
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
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore
                || method.TranslationUnit.IsSystemHeader)
                return false;

            var @params = method.GatherInternalParams(Driver.Options.IsItaniumLikeAbi, true).ToList();
            var delegateName = GenerateDelegateSignature(@params, method.ReturnType);

            Namespace namespaceDelegates;
            if (namespacesDelegates.ContainsKey(method.TranslationUnit.Module))
            {
                namespaceDelegates = namespacesDelegates[method.TranslationUnit.Module];
            }
            else
            {
                namespaceDelegates = new Namespace
                {
                    Name = DelegatesNamespace,
                    Namespace = method.TranslationUnit.Module.Units.Last()
                };
                namespacesDelegates.Add(method.TranslationUnit.Module, namespaceDelegates);
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
                    Namespace = namespaceDelegates
                };

            Generator.CurrentOutputNamespace = method.TranslationUnit.Module.OutputNamespace;
            var delegateString = @delegate.Visit(TypePrinter).Type;
            var existingDelegate = GetExistingDelegate(
                method.TranslationUnit.Module.Libraries, delegateString);
            if (existingDelegate != null)
            {
                Driver.Delegates.Add(method, existingDelegate);
                return true;
            }

            existingDelegate = new DelegateDefinition(
                method.TranslationUnit.Module.OutputNamespace, delegateString);
            Driver.Delegates.Add(method, existingDelegate);
            foreach (var library in method.TranslationUnit.Module.Libraries)
                libsDelegates[library].Add(delegateString, existingDelegate);

            namespaceDelegates.Declarations.Add(@delegate);

            return true;
        }

        private DelegateDefinition GetExistingDelegate(IList<string> libraries, string delegateString)
        {
            if (libraries.Count == 0)
                return Driver.Delegates.Values.FirstOrDefault(t => t.Signature == delegateString);

            DelegateDefinition @delegate = null;
            if (libraries.Union(
                Driver.Symbols.Libraries.Where(l => libraries.Contains(l.FileName)).SelectMany(
                    l => l.Dependencies)).Any(l => libsDelegates.ContainsKey(l) &&
                        libsDelegates[l].TryGetValue(delegateString, out @delegate)))
                return @delegate;

            return null;
        }

        private string GenerateDelegateSignature(IEnumerable<Parameter> @params, QualifiedType returnType)
        {
            TypePrinter.PushContext(CSharpTypePrinterContextKind.Native);

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
            var delegateName = Helpers.FormatTypesStringForIdentifier(typesBuilder).ToString();
            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
                delegateName = "Action_" + delegateName;
            else
                delegateName = "Func_" + delegateName;

            TypePrinter.PopContext();
            return delegateName;
        }

        private CSharpTypePrinter TypePrinter
        {
            get
            {
                if (typePrinter == null)
                {
                    typePrinter = new CSharpTypePrinter(Driver);
                    typePrinter.PushContext(CSharpTypePrinterContextKind.Native);
                }
                return typePrinter;
            }
        }

        private Dictionary<Module, Namespace> namespacesDelegates = new Dictionary<Module, Namespace>();
        private CSharpTypePrinter typePrinter;
        private static readonly Dictionary<string, Dictionary<string, DelegateDefinition>> libsDelegates =
            new Dictionary<string, Dictionary<string, DelegateDefinition>>();
    }
}
