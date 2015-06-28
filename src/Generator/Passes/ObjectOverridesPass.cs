using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Passes;

namespace CppSharp
{
    // This pass adds Equals and GetHashCode methods to classes.
    // It will also add a ToString method if the insertion operator (<<)
    // of the class is overloaded.
    // Note that the OperatorToClassPass needs to run first in order for
    // this to work.
    public class ObjectOverridesPass : TranslationUnitPass
    {
        private bool needsStreamInclude;
        private void OnUnitGenerated(GeneratorOutput output)
        {
            needsStreamInclude = false;
            foreach (var template in output.Templates)
            {
                foreach (var block in template.FindBlocks(CLIBlockKind.MethodBody))
                {
                    var method = block.Declaration as Method;
                    VisitMethod(method, block);
                }
                if (needsStreamInclude)
                {
                    var sourcesTemplate = template as CLISourcesTemplate;
                    if (sourcesTemplate != null)
                    {
                        foreach (var block in sourcesTemplate.FindBlocks(CLIBlockKind.Includes))
                        {
                            block.WriteLine("#include <sstream>");
                            block.WriteLine("");
                            break;
                        }
                        break;
                    }
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

            if (method.Name == "ToString" && method.Parameters.Count == 0)
                GenerateToString(@class, block, method);
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
            block.Write("return __Instance == obj->__Instance;");
        }

        void GenerateToString(Class @class, Block block, Method method)
        {
            needsStreamInclude = true;
            block.WriteLine("std::ostringstream os;");
            block.WriteLine("os << *NativePtr;");
            block.Write("return clix::marshalString<clix::E_UTF8>(os.str());");
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

            // We can't handle value types yet
            // The generated code assumes that a NativePtr is available
            if (@class.IsValueType)
                return false;

            foreach (var method in @class.Methods)
            {
                if (!IsInsertionOperator(method))
                    continue;

                // Create the ToString method
                var stringType = GetType("std::string");
                if (stringType == null)
                    stringType = new CILType(typeof(string));
                var toStringMethod = new Method()
                {
                    Name = "ToString",
                    Namespace = @class,
                    ReturnType = new QualifiedType(stringType),
                    SynthKind = FunctionSynthKind.ComplementOperator,
                    IsOverride = true,
                    IsProxy = true
                };

                @class.Methods.Add(toStringMethod);

                Driver.Diagnostics.Debug("Function converted to ToString: {0}::{1}",
                    @class.Name, method.Name);

                break;
            }

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
                SynthKind = FunctionSynthKind.ComplementOperator,
                IsOverride = true,
                IsProxy = true
            };
            @class.Methods.Add(methodEquals);

            var methodHashCode = new Method
            {
                Name = "GetHashCode",
                Namespace = @class,
                ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Int)),
                SynthKind = FunctionSynthKind.ComplementOperator,
                IsOverride = true,
                IsProxy = true
            };

            @class.Methods.Add(methodHashCode);
            return true;
        }

        private Dictionary<string, AST.Type> typeCache;
        protected AST.Type GetType(string typeName)
        {
            if (typeCache == null)
                typeCache = new Dictionary<string, AST.Type>();
            AST.Type result;
            if (!typeCache.TryGetValue(typeName, out result))
            {
                var typeDef = Driver.ASTContext.FindTypedef(typeName)
                    .FirstOrDefault();
                if (typeDef != null)
                    result = new TypedefType() { Declaration = typeDef };
                typeCache.Add(typeName, result);
            }
            return result;
        }

        private bool IsInsertionOperator(Method method)
        {
            // Do some basic check
            if (!method.IsOperator)
                return false;
            if (method.OperatorKind != CXXOperatorKind.LessLess)
                return false;
            if (method.Parameters.Count != 2)
                return false;

            // Check that first parameter is a std::ostream&
            var fstType = method.Parameters[0].Type as PointerType;
            if (fstType == null)
                return false;
            var oStreamType = GetType("std::ostream");
            if (oStreamType == null)
                return false;
            if (!oStreamType.Equals(fstType.Pointee))
                return false;

            // Check that second parameter is a const CLASS&
            var sndType = method.Parameters[1].Type as PointerType;
            if (sndType == null)
                return false;
            if (!sndType.QualifiedPointee.Qualifiers.IsConst)
                return false;
            var @class = method.Namespace as Class;
            var classType = new TagType(@class);
            if (!classType.Equals(sndType.Pointee))
                return false;

            return true;
        }
    }
}
