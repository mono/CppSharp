using CppSharp.AST;
using System.Collections.Generic;
using CppSharp.AST.Extensions;

namespace CppSharp.Generators.CSharp
{
    internal class CSharpStaticVariableWithInitializerGenerator : TextGenerator
    {
        private readonly BindingContext context;
        private readonly Variable variable;
        private readonly CSharpTypePrinter typePrinter;

        public CSharpStaticVariableWithInitializerGenerator(BindingContext context, Variable variable, CSharpTypePrinter typePrinter)
        {
            this.context = context;
            this.variable = variable;
            this.typePrinter = typePrinter;
        }

        public static bool CanGenerate(Variable variable)
        {
            if (variable.Initializer == null || string.IsNullOrWhiteSpace(variable.Initializer.String))
                return false;

            var arrayType = variable.Type.Desugar() as ArrayType;
            var type = (arrayType?.Type ?? variable.Type).Desugar();
            var isTypeSupported =
                type.IsPrimitiveType() ||
                type.IsPointerToPrimitiveType(PrimitiveType.Char) ||
                type.IsPointerToPrimitiveType(PrimitiveType.WideChar) ||
                (type.TryGetClass(out Class c) && c.IsValueType);

            return isTypeSupported;
        }

        public string Generate()
        {
            typePrinter.PushMarshalKind(MarshalKind.ReturnVariableArray);
            var variableType = variable.Type.Visit(typePrinter);
            typePrinter.PopMarshalKind();

            Write($"public static {variableType} {variable.Name} {{ get; }} = ");

            if (variable.Type.Desugar() as ArrayType != null)
                GenerateArrayInitializerExpression();
            else
                GenerateSimpleInitializerExpression();

            WriteLine(";");
            return ToString();
        }

        private void GenerateSimpleInitializerExpression()
        {
            var systemType = Internal.ExpressionHelper.GetSystemType(context, variable.Type.Desugar());
            var initializerString = variable.Initializer.String;

            if (Internal.ExpressionHelper.TryParseExactLiteralExpression(ref initializerString, systemType))
            {
                Write(initializerString);
                return;
            }

            Write($"unchecked(({variable.Type}){initializerString})");
        }

        private void GenerateArrayInitializerExpression()
        {
            var arrayType = variable.Type.Desugar() as ArrayType;
            var systemType = Internal.ExpressionHelper.GetSystemType(context, arrayType.Type.Desugar());
            Write($"new {arrayType.Type}[{arrayType.Size}] {{");

            List<string> elements = Internal.ExpressionHelper.SplitInitListExpr(variable.Initializer.String);

            while (elements.Count < arrayType.Size)
                elements.Add(systemType == typeof(string) ? "\"\"" : null);

            for (int i = 0; i < elements.Count; ++i)
            {
                var e = elements[i];

                if (e == null)
                    Write("default");
                else
                {
                    if (!Internal.ExpressionHelper.TryParseExactLiteralExpression(ref e, systemType))
                        Write($"unchecked(({arrayType.Type}){e})");
                    else
                        Write(e);
                }

                if (i + 1 != elements.Count)
                    Write(", ");
            }

            Write(" }");
        }
    }
}