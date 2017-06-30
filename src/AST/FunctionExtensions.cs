﻿using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public static class FunctionExtensions
    {
        public static IEnumerable<Parameter> GatherInternalParams(this Function function,
            bool isItaniumLikeAbi, bool universalDelegate = false)
        {
            var @params = new List<Parameter>();

            var method = (function.OriginalFunction ?? function) as Method;
            var isInstanceMethod = method != null && !method.IsStatic;

            var pointer = new QualifiedType(new PointerType(new QualifiedType(new BuiltinType(PrimitiveType.Void))));

            if (isInstanceMethod && !isItaniumLikeAbi)
            {
                @params.Add(new Parameter
                    {
                        QualifiedType = pointer,
                        Name = "instance"
                    });
            }

            if (!function.HasIndirectReturnTypeParameter &&
                isInstanceMethod && isItaniumLikeAbi)
            {
                @params.Add(new Parameter
                    {
                        QualifiedType = pointer,
                        Name = "instance"
                    });
            }

            var i = 0;
            foreach (var param in function.Parameters.Where(
                p => p.Kind != ParameterKind.OperatorParameter && p.Kind != ParameterKind.Extension))
            {
                @params.Add(new Parameter
                    {
                        QualifiedType = universalDelegate && param.Kind == ParameterKind.IndirectReturnType ?
                            pointer : param.QualifiedType,
                        Kind = param.Kind,
                        Usage = param.Usage,
                        Name = universalDelegate ? "arg" + ++i : param.Name
                    });

                if (param.Kind == ParameterKind.IndirectReturnType &&
                    isInstanceMethod && isItaniumLikeAbi)
                {
                    @params.Add(new Parameter
                        {
                            QualifiedType = pointer,
                            Name = "instance"
                        });
                }
            }

            if (method != null && method.IsConstructor)
            {
                var @class = (Class) method.Namespace;
                if (!isItaniumLikeAbi && @class.Layout.HasVirtualBases)
                {
                    @params.Add(new Parameter
                        {
                            QualifiedType = new QualifiedType(new BuiltinType(PrimitiveType.Int)),
                            Name = "__forBases"
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
                (rootBaseMethod = @class.GetBaseMethod(method, true)) != null &&
                rootBaseMethod.IsGenerated && rootBaseMethod.IsVirtual;
        }

        public static bool CanOverride(this Method @override, Method method)
        {
            return (method.OriginalName == @override.OriginalName &&
                method.OriginalReturnType.ResolvesTo(@override.OriginalReturnType) &&
                method.Parameters.SequenceEqual(@override.Parameters, ParameterTypeComparer.Instance)) ||
                (@override.IsDestructor && method.IsDestructor && method.IsVirtual);
        }
    }
}
