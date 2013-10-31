using System;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass will try to hoist functions as class static methods.
    /// </summary>
    public class FunctionToStaticMethodPass : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (function.Ignore)
                return false;

            var types = StringHelpers.SplitCamelCase(function.Name);
            if (types.Length == 0)
                return false;

            var @class = AstContext.FindCompleteClass(types[0]);
            if (@class == null)
                return false;

            // Clean up the name of the function now that it will be a static method.
            var name = function.Name.Substring(@class.Name.Length);
            function.ExplicityIgnored = true;

            // Create a new fake method so it acts as a static method.
            var method = new Method()
            {
                Namespace = @class,
                OriginalNamespace = function.Namespace,
                Name = name,
                OriginalName = function.OriginalName,
                Mangled = function.Mangled,
                Access = AccessSpecifier.Public,
                Kind = CXXMethodKind.Normal,
                ReturnType = function.ReturnType,
                Parameters = function.Parameters,
                CallingConvention = function.CallingConvention,
                IsVariadic = function.IsVariadic,
                IsInline = function.IsInline,
                IsStatic = true,
                Conversion = MethodConversionKind.FunctionToStaticMethod
            };

            @class.Methods.Add(method);

            Log.EmitMessage("Static method: {0}::{1}", @class.Name,
                function.Name);

            return true;
        }
    }
}
