using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// C++ function pointers are converted to delegates. If the function pointer is typedef'd then a delegate is
    /// generated with that name, otherwise the generic Action or Func delegates are used. The delegates are marhsalled
    /// using the GetDelegateForFunctionPointer and GetFunctionPointerForDelegate methods; unfortunately, these methods
    /// don't support generic types, so marshalling fails when function pointers are used without typedefs. This pass
    /// generates explicit delegates for these function pointers when used as function arguments or return types.
    /// </summary>
    public class GenerateAnonymousDelegatesPass : TranslationUnitPass
    {
        /// <summary>
        /// The generated typedefs. The tree can't be modified while iterating over it, so we collect all the typedefs
        /// and add them at the end.
        /// </summary>
        private readonly Dictionary<DeclarationContext, List<TypedefDecl>> allTypedefs =
            new Dictionary<DeclarationContext, List<TypedefDecl>>();

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            bool result = base.VisitTranslationUnit(unit);

            foreach (var typedef in allTypedefs)
                typedef.Key.Declarations.AddRange(typedef.Value);
            allTypedefs.Clear();

            return result;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            function.ReturnType = CheckType(function.Namespace, function.ReturnType);
            foreach (var parameter in function.Parameters)
            {
                parameter.QualifiedType = CheckType(function.Namespace, parameter.QualifiedType);
            }
            return true;
        }

        /// <summary>
        /// Generates a new typedef for the given type if necessary and returns the new type.
        /// </summary>
        /// <param name="namespace">The namespace the typedef will be added to.</param>
        /// <param name="type">The type to check.</param>
        /// <returns>The new type.</returns>
        private QualifiedType CheckType(DeclarationContext @namespace, QualifiedType type)
        {
            var pointerType = type.Type as PointerType;
            if (pointerType == null)
                return type;

            var functionType = pointerType.Pointee as FunctionType;
            if (functionType == null)
                return type;

            List<TypedefDecl> typedefs;
            if (!allTypedefs.TryGetValue(@namespace, out typedefs))
            {
                typedefs = new List<TypedefDecl>();
                allTypedefs.Add(@namespace, typedefs);
            }

            var typedef = FindMatchingTypedef(typedefs, functionType);
            if (typedef == null)
            {
                for (int i = 0; i < functionType.Parameters.Count; i++)
                {
                    functionType.Parameters[i].Name = string.Format("_{0}", i);
                }

                typedef = new TypedefDecl
                {
                    Access = AccessSpecifier.Public,
                    Name = string.Format("__AnonymousDelegate{0}", typedefs.Count),
                    Namespace = @namespace,
                    QualifiedType = type,
                    IsSynthetized = true
                };
                typedefs.Add(typedef);
            }

            var typedefType = new TypedefType
            {
                Declaration = typedef
            };
            return new QualifiedType(typedefType);
        }

        /// <summary>
        /// Finds a typedef with the same return type and parameter types.
        /// </summary>
        /// <param name="typedefs">The typedef list to search.</param>
        /// <param name="functionType">The function to match.</param>
        /// <returns>The matching typedef, or null if not found.</returns>
        private TypedefDecl FindMatchingTypedef(List<TypedefDecl> typedefs, FunctionType functionType)
        {
            return (from typedef in typedefs
                let type = (FunctionType)typedef.Type.GetPointee()
                where type.ReturnType == functionType.ReturnType &&
                      type.Parameters.SequenceEqual(functionType.Parameters, new ParameterTypeComparer())
                select typedef).SingleOrDefault();
        }
    }
}
