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
        public const string DelegatesNamespace = "Delegates";

        public DelegatesPass()
        {
            Options.VisitClassBases = false;
            Options.VisitFunctionParameters = false;
            Options.VisitFunctionReturnType = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitTemplateArguments = false;
        }

        public static Dictionary<Function, TypedefDecl> Delegates
        {
            get { return delegates; }
        }

        public override bool VisitLibrary(ASTContext context)
        {
            foreach (var library in Driver.Options.Libraries.Where(l => !libsDelegates.ContainsKey(l)))
                libsDelegates.Add(library, new Dictionary<string, TypedefDecl>());

            var unit = context.TranslationUnits.Last(u => u.IsValid && u.IsGenerated &&
                !u.IsSystemHeader && u.HasDeclarations);
            namespaceDelegates = new Namespace { Name = DelegatesNamespace, Namespace = unit };

            var result = base.VisitLibrary(context);

            if (namespaceDelegates.Declarations.Count > 0)
                unit.Declarations.Add(namespaceDelegates);

            return result;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore)
                return false;

            var @params = method.GatherInternalParams(Driver.Options.IsItaniumLikeAbi, true).ToList();
            var delegateName = GenerateDelegateSignature(@params, method.ReturnType);
            var existingDelegate = GetExistingDelegate(delegateName);
            if (existingDelegate != null)
            {
                delegates.Add(method, existingDelegate);
                return true;
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
            delegates.Add(method, @delegate);
            foreach (var library in Driver.Options.Libraries)
                libsDelegates[library].Add(delegateName, @delegate);

            namespaceDelegates.Declarations.Add(@delegate);

            return true;
        }

        private TypedefDecl GetExistingDelegate(string delegateName)
        {
            TypedefDecl @delegate = null;

            if (Driver.Options.Libraries.Count == 0)
                return @delegates.Values.FirstOrDefault(v => v.Name == delegateName);

            if (Driver.Options.Libraries.Union(Driver.Symbols.Libraries.SelectMany(l => l.Dependencies)).Any(
                l => libsDelegates.ContainsKey(l) && libsDelegates[l].TryGetValue(delegateName, out @delegate)))
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

        private Namespace namespaceDelegates;
        private static readonly Dictionary<string, Dictionary<string, TypedefDecl>> libsDelegates = new Dictionary<string, Dictionary<string, TypedefDecl>>(); 
        private static readonly Dictionary<Function, TypedefDecl> delegates = new Dictionary<Function, TypedefDecl>();
    }
}
