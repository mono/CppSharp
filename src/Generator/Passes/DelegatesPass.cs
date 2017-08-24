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

        private void AddDelegatesToDictionary(Declaration decl, Module module)
        {
            CallingConvention callingConvention;
            bool isDependent = false;
            QualifiedType returnType;
            List<Parameter> @params;
            if (decl is Method)
            {
                var method = (Method) decl;
                @params = method.GatherInternalParams(Context.ParserOptions.IsItaniumLikeAbi, true).ToList();
                callingConvention = method.CallingConvention;
                isDependent = method.IsDependent;
                returnType = method.ReturnType;
            }
            else
            {
                var param = (Parameter) decl;
                var funcTypeParam = param.Type.Desugar().GetFinalPointee().Desugar() as FunctionType;
                @params = funcTypeParam.Parameters;
                callingConvention = funcTypeParam.CallingConvention;
                isDependent = funcTypeParam.IsDependent;
                returnType = funcTypeParam.ReturnType;
            }

            var delegateName = GenerateDelegateSignature(@params, returnType);
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

            var functionType = new FunctionType
                {
                    CallingConvention = callingConvention,
                    IsDependent = isDependent,
                    ReturnType = returnType
                };
            functionType.Parameters.AddRange(@params);
            var @delegate = new TypedefDecl
            {
                    Name = delegateName,
                    QualifiedType = new QualifiedType(
                        new PointerType(new QualifiedType(functionType))),
                    Namespace = namespaceDelegates,
                    IsSynthetized = true,
                    Access = decl is Method ? AccessSpecifier.Private : AccessSpecifier.Public
                };

            var delegateString = @delegate.Visit(TypePrinter).Type;
            var existingDelegate = GetExistingDelegate(
                module, delegateString);

            if (existingDelegate != null)
            {
                Context.Delegates.Add(decl, existingDelegate);
                return;
            }

            existingDelegate = new DelegateDefinition(module.OutputNamespace, delegateString);
            Context.Delegates.Add(decl, existingDelegate);

            libsDelegates[module].Add(delegateString, existingDelegate);

            namespaceDelegates.Declarations.Add(@delegate);
        }

        private void VisitFunctionTypeParameters(Function function)
        {
            foreach (var param in from param in function.Parameters
                                  where !(param.Type is TypedefType)
                                  let paramType = param.Type.Desugar()
                                  where paramType.IsAddress() &&
                                      paramType.GetPointee() is FunctionType
                                  select param)
            {
                var module = function.TranslationUnit.Module;
                AddDelegatesToDictionary(param, module);
            }
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) || function.Ignore)
                return false;

            // HACK : We should shift this call to VisitFunctionTypeParameters in VisitParameter. We can't do it now
            // because we need to use the module and a since a parameter's namespace is null, we can't reach the
            // translation unit and thus the module
            VisitFunctionTypeParameters(function);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore)
                return false;
            
            var module = method.TranslationUnit.Module;
            AddDelegatesToDictionary(method, module);

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
                typesBuilder.Insert(0, returnType.Visit(TypePrinter));
                typesBuilder.Append('_');
            }
            foreach (var parameter in @params)
            {
                typesBuilder.Append(parameter.Visit(TypePrinter));
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
