using System;

namespace Cxxi.Passes
{
    /// <summary>
    /// This pass will try to hoist functions into classes so they
    /// work just like instance methods.
    /// </summary>
    public class FunctionToInstanceMethodPass : TranslationUnitPass
    {
        /// <summary>
        /// Processes a function declaration.
        /// </summary>
        public override bool VisitFunctionDecl(Function function)
        {
            if (function.Ignore)
                return false;

            // Check if this function can be converted.
            if (function.Parameters.Count == 0)
                return false;

            var classParam = function.Parameters[0];

            Class @class;
            if (!GetClassParameter(classParam, out @class))
                return false;

            // If we reach here, it means the first parameter is of class type.
            // This means we can change the function to be an instance method.

            // Clean up the name of the function now that it will be an instance method.
            if (!function.Name.StartsWith(@class.Name))
                return false;

            function.Name = function.Name.Substring(@class.Name.Length);
            function.ExplicityIgnored = true;

            // Create a new fake method so it acts as an instance method.
            var method = new Method()
                {
                    Namespace = @class.Namespace,
                    Name = function.Name,
                    OriginalName = function.OriginalName,
                    Access = AccessSpecifier.Public,
                    Kind = CXXMethodKind.Normal,
                    ReturnType = function.ReturnType,
                    Parameters = function.Parameters,
                    CallingConvention = function.CallingConvention,
                    IsVariadic = function.IsVariadic,
                    IsInline = function.IsInline,
                    Conversion = MethodConversionKind.FunctionToInstanceMethod
                };

            @class.Methods.Add(method);

            Console.WriteLine("Instance method: {0}::{1}", @class.Name,
                function.Name);

            return true;
        }

        private static bool GetClassParameter(Parameter classParam, out Class @class)
        {
            TagType tag;
            if (classParam.Type.IsPointerTo(out tag))
            {
                @class = tag.Declaration as Class;
                return true;
            }

            if (classParam.Type.IsTagDecl(out @class))
                return true;

            return false;
        }
    }

    public static class FunctionToInstanceMethodExtensions
    {
        public static void FunctionToInstanceMethod(this PassBuilder builder)
        {
            var pass = new FunctionToInstanceMethodPass();
            builder.AddPass(pass);
        }
    }
}
