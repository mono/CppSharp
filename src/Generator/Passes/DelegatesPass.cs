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
        private Dictionary<Declaration, DelegatesPass.DelegateDefinition> Delegates { get; set; } = 
            new Dictionary<Declaration, DelegatesPass.DelegateDefinition>();

        private class DelegateDefinition
        {
            public DelegateDefinition(string @namespace, string signature,
                AccessSpecifier access, CallingConvention callingConvention)
            {
                Namespace = @namespace;
                Signature = signature;
                Access = access;
                CallingConvention = callingConvention;
            }

            public string Namespace { get; }

            public string Signature { get; }

            public AccessSpecifier Access { get; set; }

            public CallingConvention CallingConvention { get; set; }
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

        /// <summary>
        /// The generated typedefs keyed by the qualified declaration context name. The tree can't be modified while
        /// iterating over it, so we collect all the typedefs and add them at the end.
        /// </summary>
        private readonly List<TypedefDecl> allTypedefs = new List<TypedefDecl>();

        private QualifiedType CheckType(DeclarationContext @namespace, QualifiedType type, Declaration decl)
        {
            if (type.Type.IsDependent)
                return type;

            var pointerType = type.Type as PointerType;
            if (pointerType == null)
                return type;

            var functionType = pointerType.Pointee as FunctionType;
            if (functionType == null)
                return type;

            var typedef = FindMatchingTypedef(allTypedefs, functionType);
            if (typedef == null)
            {
                for (int i = 0; i < functionType.Parameters.Count; i++)
                    functionType.Parameters[i].Name = $"_{i}";
                
                if (Options.IsCSharpGenerator)
                    return AddDelegateToDictionary(@namespace, decl);

                var delegateName = GenerateDelegateSignature(functionType.Parameters, functionType.ReturnType);

                typedef = new TypedefDecl
                {
                    Access = AccessSpecifier.Public,
                    Name = delegateName,
                    Namespace = @namespace,
                    QualifiedType = type,
                    IsSynthetized = true
                };
                allTypedefs.Add(typedef);
            }

            var typedefType = new TypedefType
            {
                Declaration = typedef
            };
            return new QualifiedType(typedefType);
        }

        public override bool VisitASTContext(ASTContext context)
        {
            bool result = base.VisitASTContext(context);

            foreach (var typedefDecl in allTypedefs)
            {
                typedefDecl.Namespace.Declarations.Add(typedefDecl);
            }
            allTypedefs.Clear();

            if (Options.IsCSharpGenerator)
            {
                foreach (var module in Options.Modules)
                    libsDelegates[module] = new Dictionary<string, DelegateDefinition>();

                var unit = context.TranslationUnits.GetGenerated().LastOrDefault();

                if (unit == null)
                    return false;

                foreach (var module in Options.Modules.Where(m => namespacesDelegates.ContainsKey(m)))
                    namespacesDelegates[module].Namespace.Declarations.Add(namespacesDelegates[module]);
            }

            return result;
        }

        private QualifiedType AddDelegateToDictionary(DeclarationContext @namespace, Declaration decl)
        {
            CallingConvention callingConvention;
            bool isDependent = false;
            QualifiedType returnType;
            List<Parameter> @params;
            bool isVirtualMethod = false;
            var module = @namespace.TranslationUnit.Module;
            
            if(decl is Method)
            {
                var method = (Method) decl;
                if (method.IsVirtual)
                    isVirtualMethod = true;
                else
                    isVirtualMethod = false;
            } 
               
            if ((decl is Method) && !isVirtualMethod)
            {
                var method = (Method)decl;
                var type = method.QualifiedType;
                var pointee = type.Type as PointerType;
                var functionType = pointee.GetPointee().Desugar() as FunctionType;
                @params = functionType.Parameters;
                isDependent = functionType.IsDependent;
                returnType = functionType.ReturnType;
                callingConvention = functionType.CallingConvention;
            }
            else if (decl is Method)
            {
                var method = (Method)decl;
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

            // decl will be in dictionary previously only when we've already visited it before
            // we visit it again here, only in cases when we return a funcPtr to a virtual method
            // in that case delegate created must be public too
            AccessSpecifier access = (decl is Method && isVirtualMethod) &&
                !(Delegates.ContainsKey(decl)) ? AccessSpecifier.Private : AccessSpecifier.Public;
            
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

            var @delegate = new TypedefDecl
                {
                    Name = delegateName,
                    QualifiedType = new QualifiedType(
                        new PointerType(
                            new QualifiedType(
                                new FunctionType
                                {
                                    CallingConvention = callingConvention,
                                    IsDependent = isDependent,
                                    Parameters = @params,
                                    ReturnType = returnType
                                }))),
                    Namespace = namespaceDelegates,
                    IsSynthetized = true,
                    Access = access
                };

            var typeDefType = new TypedefType
            {
                Declaration = @delegate
            };

            var delegateString = @delegate.Visit(TypePrinter).Type;
            var existingDelegate = GetExistingDelegate(
                module, delegateString);

            if (existingDelegate != null)
            {
                // Code to ensure that if a delegate is used for a virtual as well as something else, it finally ends up as public
                if (existingDelegate.Access == AccessSpecifier.Private && access == AccessSpecifier.Public)
                {
                    existingDelegate.Access = access;
                    if (libsDelegates.ContainsKey(module))
                    {
                        if (libsDelegates[module].ContainsKey(delegateString))
                            libsDelegates[module][delegateString].Access = access;
                    }
                    foreach (var dec in namespaceDelegates.Declarations.Where(m => m.Name == @delegate.Name))
                    {
                        ((TypedefDecl)dec).Access = AccessSpecifier.Public;
                        break;
                    }
                }
                // Code to check if there is an existing delegate with a different calling convention
                // Add the new delegate with calling convention appended to it's name
                if (existingDelegate.CallingConvention != callingConvention)
                {
                    delegateString += callingConvention.ToString();
                    existingDelegate = new DelegateDefinition(module.OutputNamespace,
                        delegateString, access, callingConvention);
                    @delegate.Name += callingConvention.ToString();
                    if (libsDelegates.ContainsKey(module))
                        if (!libsDelegates[module].ContainsKey(delegateString))
                            libsDelegates[module].Add(delegateString, existingDelegate);
                    if(!namespaceDelegates.Declarations.Contains(@delegate))
                        namespaceDelegates.Declarations.Add(@delegate);
                }
                
                var typeDefType1 = new TypedefType
                {
                    Declaration = @delegate
                };
                
                if(!Delegates.ContainsKey(decl))
                    Delegates.Add(decl, existingDelegate);
                return new QualifiedType(typeDefType1);
            }

            existingDelegate = new DelegateDefinition(module.OutputNamespace,
                delegateString, access, callingConvention);
            Delegates.Add(decl, existingDelegate);

            if(libsDelegates.ContainsKey(module))
                libsDelegates[module].Add(delegateString, existingDelegate);

            namespaceDelegates.Declarations.Add(@delegate);
            return new QualifiedType(typeDefType);
        }

        private static TypedefDecl FindMatchingTypedef(IEnumerable<TypedefDecl> typedefs, FunctionType functionType)
        {
            return (from typedef in typedefs
                    let type = (FunctionType)typedef.Type.GetPointee()
                    where type.ReturnType == functionType.ReturnType &&
                          type.Parameters.SequenceEqual(functionType.Parameters, ParameterTypeComparer.Instance)
                    select typedef).SingleOrDefault();
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) || function.Ignore)
                return false;

            function.ReturnType = CheckType(function.Namespace, function.ReturnType, function);
            foreach (var parameter in function.Parameters)
            {
                parameter.QualifiedType = CheckType(function.Namespace, parameter.QualifiedType, parameter);
                
                // HACK : We should shift this call to VisitFunctionTypeParameters in VisitParameter. We can't do it now
                // because we need to use the module and a since a parameter's namespace is null, we can't reach the
                // translation unit and thus the module
                if (Options.IsCSharpGenerator && !(parameter.Type is TypedefType) &&
                    parameter.Type.IsAddress() && (parameter.Type.GetPointee() is FunctionType))
                    parameter.QualifiedType = AddDelegateToDictionary(function.Namespace, parameter);
            }
            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.IsVirtual || method.Ignore)
                return false;

            if (Options.IsCSharpGenerator)
            {
                var module = method.TranslationUnit.Module;
                method.FunctionType = AddDelegateToDictionary(method.Namespace, method);
                if (method.ReturnType.Type.GetFinalPointee().Desugar() is FunctionType)
                    method.ReturnType = AddDelegateToDictionary(method.Namespace, method);
            }

            return true;
        }
        
        private DelegateDefinition GetExistingDelegate(Module module, string delegateString)
        {
            if (module.Libraries.Count == 0)
                return Delegates.Values.FirstOrDefault(t => t.Signature == delegateString);

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
