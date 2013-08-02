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
        void OnUnitGenerated(GeneratorOutput output)
        {
            foreach (var template in output.Templates)
            {
                foreach (var block in template.FindBlocks(CLIBlockKind.MethodBody))
                {
                    var method = block.Declaration as Method;
                    if (!method.IsSynthetized)
                        continue;

                    var @class = (Class) method.Namespace;

                    switch (method.Name)
                    {
                    case "GetHashCode":
                        block.Write("return (int)NativePtr;");
                        break;
                    case "Equals":
                        var cliTypePrinter = new CLITypePrinter(Driver);
                        var classCliType = @class.Visit(cliTypePrinter);
                        block.WriteLine("if (!object) return false;");
                        block.WriteLine("auto obj = dynamic_cast<{0}>({1});",
                            classCliType, method.Parameters[0].Name);
                        block.WriteLine("if (!obj) return false;");
                        block.Write("return Instance == obj->Instance;");
                        break;
                    }
                }
            }
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

            if (AlreadyVisited(@class))
                return true;

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
