using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Passes;

namespace CppSharp.Generators.Cpp
{
    public class NAPICodeGenerator : CCodeGenerator
    {
        public override string FileExtension => "cpp";

        public NAPICodeGenerator(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override void VisitDeclContextFunctions(DeclarationContext context)
        {
            var functions = context.Functions.Where(f => !ASTUtils.CheckIgnoreFunction(f)).ToList();
            var unique = functions.GroupBy(m => m.Name);
            foreach (var group in unique)
                GenerateFunctionGroup(group.ToList());
        }

        public virtual void GenerateFunctionGroup(List<Function> group)
        {
            foreach (var function in group)
            {
                function.Visit(this);
                return;
            }
        }

        public override bool VisitClassDecl(Class @class)
        {
            return VisitClassDeclContext(@class);
        }

        public override void VisitClassConstructors(Class @class)
        {
            var constructors = @class.Constructors.Where(c => c.IsGenerated && !c.IsCopyConstructor)
                .ToList();
            if (!constructors.Any())
                return;

            GenerateMethodGroup(constructors);
        }

        public static bool ShouldGenerate(Function function)
        {
            if (!function.IsGenerated)
                return false;

            if (function is Method method)
            {
                if (method.IsConstructor || method.IsDestructor)
                    return false;

                if (method.IsOperator)
                    if (method.OperatorKind == CXXOperatorKind.Conversion ||
                        method.OperatorKind == CXXOperatorKind.Equal)
                        return false;
            }

            return true;
        }

        public override void VisitClassMethods(Class @class)
        {
            var methods = @class.Methods.Where(ShouldGenerate);
            var uniqueMethods = methods.GroupBy(m => m.Name);
            foreach (var group in uniqueMethods)
                GenerateMethodGroup(group.ToList());
        }

        public virtual void GenerateMethodGroup(List<Method> @group)
        {
            foreach (var method in @group)
            {
                method.Visit(this);
                return;
            }
        }
    }

    /// <summary>
    /// Generates a common Node N-API C/C++ common files.
    /// N-API documentation: https://nodejs.org/api/n-api.html
    /// </summary>
    public class NAPIHelpers : CppHeaders
    {
        public NAPIHelpers(BindingContext context)
            : base(context, null)
        {
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            WriteLine("#pragma once");
            NewLine();

            WriteInclude("math.h", CInclude.IncludeKind.Angled);
            WriteInclude("limits.h", CInclude.IncludeKind.Angled);
            NewLine();

            GenerateHelpers();
            return;
        }

        private void GenerateHelpers()
        {
            WriteLine(@"#define NAPI_CALL(env, call)                                      \
  do {                                                            \
    napi_status status = (call);                                  \
    if (status != napi_ok) {                                      \
      const napi_extended_error_info* error_info = NULL;          \
      napi_get_last_error_info((env), &error_info);               \
      bool is_pending;                                            \
      napi_is_exception_pending((env), &is_pending);              \
      if (!is_pending) {                                          \
        const char* message = (error_info->error_message == NULL) \
            ? ""empty error message""                             \
            : error_info->error_message;                          \
        napi_throw_error((env), NULL, message);                   \
        return NULL;                                              \
      }                                                           \
    }                                                             \
  } while(0)");
            NewLine();

            WriteLine(@"#define NAPI_CALL_NORET(env, call)                                \
  do {                                                            \
    napi_status status = (call);                                  \
    if (status != napi_ok) {                                      \
      const napi_extended_error_info* error_info = NULL;          \
      napi_get_last_error_info((env), &error_info);               \
      bool is_pending;                                            \
      napi_is_exception_pending((env), &is_pending);              \
      if (!is_pending) {                                          \
        const char* message = (error_info->error_message == NULL) \
            ? ""empty error message""                             \
            : error_info->error_message;                          \
        napi_throw_error((env), NULL, message);                   \
        return;                                                   \
      }                                                           \
    }                                                             \
  } while(0)");
            NewLine();

            WriteLine(@"static int napi_is_int32(napi_env env, napi_value value, int* integer) {
    double temp = 0;
    if (
        // We get the value as a double so we can check for NaN, Infinity and float:
        // https://github.com/nodejs/node/issues/26323
        napi_get_value_double(env, value, &temp) != napi_ok ||
        // Reject NaN:
        isnan(temp) ||
        // Reject Infinity and avoid undefined behavior when casting double to int:
        // https://groups.google.com/forum/#!topic/comp.lang.c/rhPzd4bgKJk
        temp < INT_MIN ||
        temp > INT_MAX ||
        // Reject float by casting double to int:
        (double) ((int) temp) != temp
    ) {
        //napi_throw_error(env, NULL, ""argument must be an integer"");
        return 0;
    }
    if (integer)
        *integer = (int) temp;
    return 1;
}");
            NewLine();

            WriteLine(@"static int napi_is_uint32(napi_env env, napi_value value, int* integer) {
    double temp = 0;
    if (
        // We get the value as a double so we can check for NaN, Infinity and float:
        // https://github.com/nodejs/node/issues/26323
        napi_get_value_double(env, value, &temp) != napi_ok ||
        // Reject NaN:
        isnan(temp) ||
        // Reject Infinity and avoid undefined behavior when casting double to int:
        // https://groups.google.com/forum/#!topic/comp.lang.c/rhPzd4bgKJk
        temp < 0 ||
        temp > ULONG_MAX ||
        // Reject float by casting double to int:
        (double) ((unsigned long) temp) != temp
    ) {
        //napi_throw_error(env, NULL, ""argument must be an integer"");
        return 0;
    }
    if (integer)
        *integer = (int) temp;
    return 1;
}");
            NewLine();

            WriteLine(@"#define NAPI_IS_BOOL(valuetype) (valuetype == napi_boolean)");
            WriteLine(@"#define NAPI_IS_NULL(valuetype) (valuetype == napi_null)");
            WriteLine(@"#define NAPI_IS_NUMBER(valuetype) (valuetype == napi_number)");
            WriteLine(@"#define NAPI_IS_BIGINT(valuetype) (valuetype == napi_bigint)");
            WriteLine(@"#define NAPI_IS_INT32(valuetype, value) (NAPI_IS_NUMBER(valuetype) && napi_is_int32(env, value, nullptr))");
            WriteLine(@"#define NAPI_IS_UINT32(valuetype, value) (NAPI_IS_NUMBER(valuetype) && napi_is_uint32(env, value, nullptr))");
            WriteLine(@"#define NAPI_IS_INT64(valuetype, value) (NAPI_IS_BIGINT(valuetype))");
            WriteLine(@"#define NAPI_IS_UINT64(valuetype, value) (NAPI_IS_BIGINT(valuetype))");
            WriteLine(@"#define NAPI_IS_ARRAY(valuetype) (valuetype == napi_object)");
            WriteLine(@"#define NAPI_IS_OBJECT(valuetype) (valuetype == napi_object)");

            NewLine();
        }
    }
}