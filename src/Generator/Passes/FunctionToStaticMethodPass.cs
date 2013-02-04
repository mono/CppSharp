using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cxxi.Passes
{
    /// <summary>
    /// This pass will try to hoist functions into classes so they
    /// work just like static methods.
    /// </summary>
    public class FunctionToStaticMethodPass : TranslationUnitPass
    {
        /// <summary>
        /// Processes a function declaration.
        /// </summary>
        public override bool ProcessFunction(Function function)
        {
            if (function.Ignore)
                return false;

            var types = SplitCamelCase(function.Name);
            if (types.Length == 0)
                return false;

            var @class = Library.FindCompleteClass(types[0]);
            if (@class == null)
                return false;

            // Clean up the name of the function now that it will be a static method.
            function.Name = function.Name.Substring(@class.Name.Length);
            function.ExplicityIgnored = true;

            // Create a new fake method so it acts as a static method.
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
                IsStatic = true,
                Conversion = MethodConversionKind.FunctionToStaticMethod
            };

            @class.Methods.Add(method);

            Console.WriteLine("Static method: {0}::{1}", @class.Name,
                function.Name);

            return true;
        }

        public static string[] SplitCamelCase(string input)
        {
            var str = Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled);
            return str.Trim().Split();
        }
    }

    public static class FunctionToStaticMethodExtensions
    {
        public static void FunctionToStaticMethod(this PassBuilder builder)
        {
            var pass = new FunctionToStaticMethodPass();
            builder.AddPass(pass);
        }
    }
}
