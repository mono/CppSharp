using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;
using Attribute = CppSharp.AST.Attribute;
using Type = CppSharp.AST.Type;

namespace CppSharp.Tests
{
    public class CSharpTestsGenerator : GeneratorTest
    {
        public CSharpTestsGenerator(GeneratorKind kind)
            : base("CSharp", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Context.TranslationUnitPasses.AddPass(new TestAttributesPass());
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("TestCopyConstructorVal");
            ctx.SetClassAsValueType("QGenericArgument");
            ctx.SetClassAsValueType("StructWithPrivateFields");
            ctx.SetClassAsValueType("QPoint");
            ctx.SetClassAsValueType("QSize");
            ctx.SetClassAsValueType("QRect");
            ctx.SetClassAsValueType("StructTestArrayTypeFromTypedef");
            ctx.IgnoreClassWithName("IgnoredTypeInheritingNonIgnoredWithNoEmptyCtor");

            var macroRegex = new Regex(@"(MY_MACRO_TEST_.*)");
            List<string> list = new List<string>();
            foreach (TranslationUnit unit in ctx.TranslationUnits)
            {
              if (unit.FilePath == "<invalid>" || unit.FileName == "CSharp.h")
                foreach (var macro in unit.PreprocessedEntities.OfType<MacroDefinition>())
                {
                  Match match = macroRegex.Match(macro.Name);
                  if (match.Success) list.Add(macro.Name);
                }
            }
            var enumTest = ctx.GenerateEnumFromMacros("MyMacroTestEnum", list.ToArray());
              enumTest.Namespace = new Namespace() {Name = "MacroTest"};
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CSharpTestsGenerator(GeneratorKind.CSharp));
        }
    }

    public class TestAttributesPass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (AlreadyVisited(function) || function.Name != "obsolete")
                return false;

            var attribute = new Attribute
            {
                Type = typeof(ObsoleteAttribute),
                Value = string.Format("\"{0} is obsolete.\"", function.Name)
            };

            function.Attributes.Add(attribute);

            return base.VisitFunctionDecl(function);
        }
    }

    #region Type Maps

    [TypeMap("QFlags")]
    public class QFlags : TypeMap
    {
        public override string CSharpConstruct()
        {
            return string.Empty;
        }

        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return GetEnumType(ctx.Type);
        }

        public override string CSharpSignature(TypePrinterContext ctx)
        {
            return CSharpSignatureType(ctx).ToString();
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            if (ctx.Parameter.Type.Desugar().IsAddress())
                ctx.Return.Write("new global::System.IntPtr(&{0})", ctx.Parameter.Name);
            else
                ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            if (ctx.ReturnType.Type.Desugar().IsAddress())
            {
                var finalType = ctx.ReturnType.Type.GetFinalPointee() ?? ctx.ReturnType.Type;
                var enumType = GetEnumType(finalType);
                ctx.Return.Write("*({0}*) {1}", enumType, ctx.ReturnVarName);
            }
            else
            {
                ctx.Return.Write(ctx.ReturnVarName);
            }
        }

        private static Type GetEnumType(Type mappedType)
        {
            var type = mappedType.Desugar();
            ClassTemplateSpecialization classTemplateSpecialization;
            var templateSpecializationType = type as TemplateSpecializationType;
            if (templateSpecializationType != null)
            {
                classTemplateSpecialization = templateSpecializationType.GetClassTemplateSpecialization();
                return classTemplateSpecialization.Arguments[0].Type.Type;
            }
            var declaration = ((TagType) type).Declaration;
            if (declaration.IsDependent)
                return new TagType(((Class) declaration).TemplateParameters[0]);
            classTemplateSpecialization = (ClassTemplateSpecialization) declaration;
            return classTemplateSpecialization.Arguments[0].Type.Type;
        }
    }

    [TypeMap("DefaultZeroMappedToEnum")]
    public class DefaultZeroMappedToEnum : TypeMap
    {
        public override string CSharpConstruct()
        {
            return string.Empty;
        }

        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new TagType(new Enumeration());
        }

        public override string CSharpSignature(TypePrinterContext ctx)
        {
            return "Flags";
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("QList")]
    public class QList : TypeMap
    {
        public override bool IsIgnored
        {
            get
            {
                var type = (TemplateSpecializationType)Type;
                var pointeeType = type.Arguments[0].Type;
                var checker = new TypeIgnoreChecker(TypeMapDatabase);
                pointeeType.Visit(checker);
                return checker.IsIgnored;
            }
        }

        public override string CSharpSignature(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Native)
            {
                var type = (TemplateSpecializationType) ctx.Type.Desugar();
                var specialization = type.GetClassTemplateSpecialization();
                var typePrinter = new CSharpTypePrinter(null);
                typePrinter.PushContext(TypePrinterContextKind.Native);
                return string.Format($"{specialization.Visit(typePrinter)}{(Type.IsAddress() ? "*" : string.Empty)}", specialization.Visit(typePrinter),
                    Type.IsAddress() ? "*" : string.Empty);
            }

            return string.Format("System.Collections.Generic.{0}<{1}>",
                ctx.MarshalKind == MarshalKind.DefaultExpression ? "List" : "IList",
                ctx.GetTemplateParameterList());
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            // pointless, put just so that the generated code compiles
            var type = (TemplateSpecializationType) ctx.Parameter.Type.Desugar();
            var specialization = type.GetClassTemplateSpecialization();
            var typePrinter = new CSharpTypePrinter(null);
            typePrinter.PushContext(TypePrinterContextKind.Native);
            ctx.Return.Write("new {0}()", specialization.Visit(typePrinter));
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("TypeMappedWithOperator")]
    public class TypeMappedWithOperator : TypeMap
    {
        public override string CSharpSignature(TypePrinterContext ctx)
        {
            // doesn't matter, we just need it to compile
            return "int";
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    #endregion
}

