using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public static class FunctionExtensions
    {
        public static IList<Parameter> GatherInternalParams(this Function function,
            bool isItaniumLikeAbi, bool universalDelegate = false)
        {
            var @params = new List<Parameter>();

            var method = (function.OriginalFunction ?? function) as Method;
            var isInstanceMethod = method != null && !method.IsStatic;

            var pointer = new QualifiedType(new PointerType(new QualifiedType(new BuiltinType(PrimitiveType.Void))));

            if (isInstanceMethod &&
                (!isItaniumLikeAbi || !function.HasIndirectReturnTypeParameter))
            {
                @params.Add(new Parameter
                {
                    QualifiedType = pointer,
                    Name = "__instance",
                    Namespace = function
                });
            }

            var i = 0;
            foreach (var param in function.Parameters.Where(
                p => p.Kind != ParameterKind.OperatorParameter && p.Kind != ParameterKind.Extension))
            {
                @params.Add(new Parameter
                {
                    QualifiedType = universalDelegate &&
                            (param.Kind == ParameterKind.IndirectReturnType ||
                             param.Type.Desugar().IsPointerTo(out FunctionType functionType)) ?
                            pointer : param.QualifiedType,
                    Kind = param.Kind,
                    Usage = param.Usage,
                    Name = universalDelegate ? $"arg{++i}" : param.Name,
                    Namespace = param.Namespace
                });

                if (param.Kind == ParameterKind.IndirectReturnType &&
                    isInstanceMethod && isItaniumLikeAbi)
                {
                    @params.Add(new Parameter
                    {
                        QualifiedType = pointer,
                        Name = "__instance",
                        Namespace = function
                    });
                }
            }

            if (method != null && method.IsConstructor)
            {
                var @class = (Class)method.Namespace;
                if (!isItaniumLikeAbi && @class.Layout.HasVirtualBases)
                {
                    @params.Add(new Parameter
                    {
                        QualifiedType = new QualifiedType(new BuiltinType(PrimitiveType.Int)),
                        Name = "__forBases",
                        Namespace = function
                    });
                }
            }

            return @params;
        }

        public static bool IsGeneratedOverride(this Method method)
        {
            // Check if overriding a function from a secondary base.
            var @class = method.Namespace as Class;
            Method rootBaseMethod;
            return method.IsOverride &&
                (rootBaseMethod = @class.GetBaseMethod(method)) != null &&
                rootBaseMethod.IsGenerated && rootBaseMethod.IsVirtual;
        }

        public static bool CanOverride(this Method @override, Method method)
        {
            return (method.OriginalName == @override.OriginalName &&
                method.IsVirtual == @override.IsVirtual &&
                method.OriginalReturnType.ResolvesTo(@override.OriginalReturnType) &&
                method.Parameters.Where(p => p.Kind != ParameterKind.IndirectReturnType).SequenceEqual(
                    @override.Parameters.Where(p => p.Kind != ParameterKind.IndirectReturnType),
                    ParameterTypeComparer.Instance)) ||
                (@override.IsDestructor && method.IsDestructor && method.IsVirtual);
        }

        public static bool NeedsSymbol(this Method method)
        {
            Class @class = (Class)(method.OriginalFunction ?? method).Namespace;
            // virtual functions cannot really be inlined and
            // we don't need their symbols anyway as we call them through the v-table
            return (!method.IsVirtual && !method.IsSynthesized &&
                !method.IsDefaultConstructor && !method.IsCopyConstructor && !method.IsDestructor) ||
                (method.IsDefaultConstructor && @class.HasNonTrivialDefaultConstructor) ||
                (method.IsCopyConstructor && @class.HasNonTrivialCopyConstructor) ||
                (method.IsDestructor && !method.IsVirtual && @class.HasNonTrivialDestructor);
        }
    }
}
