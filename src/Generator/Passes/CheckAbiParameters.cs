using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass checks for ABI-specific details that need to be fixed
    /// in the generated code.
    /// 
    /// In both the Microsoft and Itanium ABI, some functions return types
    /// and parameter can be returned indirectly. In the case of an indirect
    /// return type we need to add an extra pointer parameter to the function
    /// and use that to call the function instead. In the case of parameters
    /// then the type of that parameter is converted to a pointer.
    /// 
    /// Itanium ABI reference (3.1.4 Return values):
    /// http://refspecs.linux-foundation.org/cxxabi-1.83.html#calls
    ///
    /// Microsoft ABI reference:
    /// http://blog.aaronballman.com/2012/02/describing-the-msvc-abi-for-structure-return-types/
    /// </summary>
    public class CheckAbiParameters : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (!VisitDeclaration(function))
                return false;

            if (function.IsReturnIndirect)
            {
                var indirectParam = new Parameter()
                    {
                        Kind = ParameterKind.IndirectReturnType,
                        QualifiedType = function.ReturnType,
                        Name = "return",
                    };

                function.Parameters.Insert(0, indirectParam);
                function.ReturnType = new QualifiedType(new BuiltinType(
                    PrimitiveType.Void));
            }

            if (function.HasThisReturn)
            {
                // This flag should only be true on methods.
                var method = (Method) function;
                var classType = new QualifiedType(new TagType(method.Namespace),
                    new TypeQualifiers {IsConst = true});
                function.ReturnType = new QualifiedType(new PointerType(classType));
            }

            // TODO: Handle indirect parameters

            return true;
        }
    }
}
