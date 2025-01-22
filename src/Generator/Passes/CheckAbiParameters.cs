using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
using System.Linq;

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
    /// Furthermore, there's at least one ABI (System V) that gives to empty structs
    /// size 1 in C++ and size 0 in C. The former causes crashes in older versions of Mono.
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

            function.ReturnType.Type.Desugar().TryGetDeclaration(out Class returnTypeDecl);

            var isReturnIndirect = function.IsReturnIndirect || (
                    Context.ParserOptions.IsMicrosoftAbi &&
                    function is Method m && m.IsNativeMethod() &&
                    !function.ReturnType.Type.Desugar().IsAddress() &&
                    returnTypeDecl.IsPOD &&
                    returnTypeDecl.Layout.Size <= 8);

            var triple = Context.ParserOptions.TargetTriple;
            var isArm64 = triple.Contains("arm64") || triple.Contains("aarch64");

            if (isReturnIndirect && !isArm64)
            {
                var indirectParam = new Parameter()
                {
                    Kind = ParameterKind.IndirectReturnType,
                    QualifiedType = function.ReturnType,
                    Name = "return",
                    Namespace = function
                };

                function.Parameters.Insert(0, indirectParam);
                function.ReturnType = new QualifiedType(new BuiltinType(
                    PrimitiveType.Void));
            }

            // .NET cannot deal with non-POD with size <= 16 types being passed by value.
            // https://github.com/dotnet/runtime/issues/106471
            if (isArm64 && NeedsArm64IndirectReturn(returnTypeDecl))
            {
                Diagnostics.Error($"Non-POD return type {returnTypeDecl.Name} with size <= 16 is not supported in ARM64 target.");
            }

            var method = function as Method;

            if (function.HasThisReturn)
            {
                // This flag should only be true on methods.
                var classType = new QualifiedType(new TagType(method.Namespace),
                    new TypeQualifiers { IsConst = true });
                function.ReturnType = new QualifiedType(new PointerType(classType));
            }

            // Deleting destructors (default in v-table) accept an i32 bitfield as a
            // second parameter in MS ABI.
            if (method is { IsDestructor: true } && method.IsVirtual && Context.ParserOptions.IsMicrosoftAbi)
            {
                method.Parameters.Add(new Parameter
                {
                    Kind = ParameterKind.ImplicitDestructorParameter,
                    QualifiedType = new QualifiedType(new BuiltinType(PrimitiveType.Int)),
                    Name = "delete",
                    Namespace = method
                });
            }

            foreach (var param in function.Parameters)
            {
                param.QualifiedType.Type.Desugar().TryGetDeclaration(out Class paramTypeDecl);
                if (isArm64 && NeedsArm64IndirectReturn(paramTypeDecl))
                {
                    Diagnostics.Error($"Non-POD parameter type {paramTypeDecl.Name} with size <= 16 is not supported in ARM64 target.");
                }

                if (param.IsIndirect)
                    param.QualifiedType = new QualifiedType(new PointerType(param.QualifiedType));
            }

            return true;
        }

        public static bool NeedsArm64IndirectReturn(Class @class)
        {
            if (@class == null)
                return false;
            return (@class.HasNonTrivialCopyConstructor || @class.HasNonTrivialDestructor || @class.IsDynamic) && @class.Layout.Size <= 16;
        }
    }
}
