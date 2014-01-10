using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Passes;

namespace CppSharp
{
    public class ObjectOverridesPass : TranslationUnitPass
    {
        private void OnUnitGenerated(GeneratorOutput output)
        {
            foreach (var template in output.Templates)
            {
                foreach (var block in template.FindBlocks(CLIBlockKind.MethodBody))
                {
                    var method = block.Declaration as Method;
                    VisitMethod(method, block);
                }
            }
        }

        private void VisitMethod(Method method, Block block)
        {
            if (!method.IsSynthetized)
                return;

            var @class = (Class)method.Namespace;

            if (method.Name == "GetHashCode" && method.Parameters.Count == 0)
                GenerateGetHashCode(block);

            if (method.Name == "Equals" && method.Parameters.Count == 1)
                GenerateEquals(@class, block, method);
        }

        void GenerateGetHashCode(Block block)
        {
            block.Write("return (int)NativePtr;");
        }

        void GenerateEquals(Class @class, Block block, Method method)
        {
            var cliTypePrinter = new CLITypePrinter(Driver);
            var classCliType = @class.Visit(cliTypePrinter);

            block.WriteLine("if (!object) return false;");
            block.WriteLine("auto obj = dynamic_cast<{0}>({1});",
                classCliType, method.Parameters[0].Name);
            block.NewLine();

            block.WriteLine("if (!obj) return false;");
            block.Write("return Instance == obj->Instance;");
        }

        private bool isHooked;
        public override bool VisitClassDecl(Class @class)
        {
            // FIXME: Add a better way to hook the event
            if (!isHooked)
            {
                Driver.Generator.OnUnitGenerated += OnUnitGenerated;
                isHooked = true;
            }

            if (!VisitDeclaration(@class))
                return false;

            if (@class.IsValueType)
                return false;

            var methodEqualsParam = new Parameter
            {
                Name = "object",
                QualifiedType = new QualifiedType(new CILType(typeof(Object))),
            };

            var methodEquals = new Method
            {
                Name = "Equals",
                Namespace = @class,
                ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Bool)),
                Parameters = new List<Parameter> { methodEqualsParam },
                IsSynthetized = true,
                IsOverride = true,
                IsProxy = true
            };
             @class.Methods.Add(methodEquals);
             
            var methodHashCode = new Method
            {
                Name = "GetHashCode",
                Namespace = @class,
                ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Int32)),
                IsSynthetized = true,
                IsOverride = true,
                IsProxy = true
            };

            @class.Methods.Add(methodHashCode);
            return true;
        }
    }
}
