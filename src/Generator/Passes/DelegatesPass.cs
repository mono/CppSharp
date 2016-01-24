using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.CSharp;
using System;

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
            foreach (var library in Driver.Options.Libraries.Where(l => !libsDelegates.ContainsKey(l)))
                libsDelegates.Add(library, new Dictionary<string, DelegateDefinition>());

            var unit = context.TranslationUnits.Last(u => u.IsValid && u.IsGenerated &&
                !u.IsSystemHeader && u.HasDeclarations);
            namespaceDelegates = new Namespace { Name = DelegatesNamespace, Namespace = unit };

            var result = base.VisitLibrary(context);

            if (namespaceDelegates.Declarations.Count > 0)
                unit.Declarations.Add(namespaceDelegates);

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
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore)
                return false;

            var @params = method.GatherInternalParams(Driver.Options.IsItaniumLikeAbi, true).ToList();
            var delegateName = GenerateDelegateSignature(@params, method.ReturnType);

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

            var delegateString = @delegate.Visit(TypePrinter).Type;
            var existingDelegate = GetExistingDelegate(delegateString);
            if (existingDelegate != null)
            {
                Driver.Delegates.Add(method, existingDelegate);
                return true;
            }

            existingDelegate = new DelegateDefinition(Driver.Options.OutputNamespace, delegateString);
            Driver.Delegates.Add(method, existingDelegate);
            foreach (var library in Driver.Options.Libraries)
                libsDelegates[library].Add(delegateString, existingDelegate);

            namespaceDelegates.Declarations.Add(@delegate);

            return true;
        }

        private DelegateDefinition GetExistingDelegate(string delegateString)
        {
            if (Driver.Options.Libraries.Count == 0)
                return Driver.Delegates.Values.FirstOrDefault(t => t.Signature == delegateString);

            DelegateDefinition @delegate = null;
            if (Driver.Options.Libraries.Union(
                Driver.Symbols.Libraries.SelectMany(l => l.Dependencies)).Any(
                    l => libsDelegates.ContainsKey(l) &&
                    libsDelegates[l].TryGetValue(delegateString, out @delegate)))
                return @delegate;

            return null;
        }

        private string GenerateDelegateSignature(IEnumerable<Parameter> @params, QualifiedType returnType)
        {
            var typePrinter = new CSharpTypePrinter(Driver);
            typePrinter.PushContext(CSharpTypePrinterContextKind.Native);

            var typesBuilder = new StringBuilder();
            if (!returnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                typesBuilder.Insert(0, returnType.Type.CSharpType(typePrinter));
                typesBuilder.Append('_');
            }
            foreach (var parameter in @params)
            {
                typesBuilder.Append(parameter.CSharpType(typePrinter));
                typesBuilder.Append('_');
            }
            if (typesBuilder.Length > 0)
                typesBuilder.Remove(typesBuilder.Length - 1, 1);
            var delegateName = typesBuilder.Replace("global::System.", string.Empty).Replace(
                "*", "Ptr").Replace('.', '_').ToString();
            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
                delegateName = "Action_" + delegateName;
            else
                delegateName = "Func_" + delegateName;

            typePrinter.PopContext();
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

        private Namespace namespaceDelegates;
        private CSharpTypePrinter typePrinter;
        private static readonly Dictionary<string, Dictionary<string, DelegateDefinition>> libsDelegates =
            new Dictionary<string, Dictionary<string, DelegateDefinition>>();
    }
}
