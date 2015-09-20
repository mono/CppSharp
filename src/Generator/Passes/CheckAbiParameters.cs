using System.Linq;
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
        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            if (HasFieldsOrVirtuals(@class))
                return false;

            @class.Layout.Size = @class.Layout.DataSize = 0;

            return true;
        }

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

            var method = function as Method;

            if (function.HasThisReturn)
            {
                // This flag should only be true on methods.
                var classType = new QualifiedType(new TagType(method.Namespace),
                    new TypeQualifiers {IsConst = true});
                function.ReturnType = new QualifiedType(new PointerType(classType));
            }

            // Deleting destructors (default in v-table) accept an i32 bitfield as a
            // second parameter.in MS ABI.
            if (method != null && method.IsDestructor && Driver.Options.IsMicrosoftAbi)
            {
                method.Parameters.Add(new Parameter
                {
                    Kind = ParameterKind.ImplicitDestructorParameter,
                    QualifiedType = new QualifiedType(new BuiltinType(PrimitiveType.Int)),
                    Name = "delete"
                });
            }

            // TODO: Handle indirect parameters

            return true;
        }

        private static bool HasFieldsOrVirtuals(Class @class)
        {
            if (@class.Fields.Count > 0 || @class.IsDynamic)
                return true;
            return @class.Bases.Any(@base => @base.IsClass && @base.Class != @class &&
                HasFieldsOrVirtuals(@base.Class));
        }
    }
}
