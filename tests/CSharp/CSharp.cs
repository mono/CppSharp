﻿using System;
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

        public override void Setup(Driver driver)
        {
            base.Setup(driver);

            driver.ParserOptions.UnityBuild = true;
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Context.TranslationUnitPasses.AddPass(new TestAttributesPass());
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
            driver.Options.GenerateClassTemplates = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("TestCopyConstructorVal");
            ctx.SetClassAsValueType("QGenericArgument");
            ctx.SetClassAsValueType("StructWithPrivateFields");
            ctx.SetClassAsValueType("QPoint");
            ctx.SetClassAsValueType("QSize");
            ctx.SetClassAsValueType("QRect");
            ctx.SetClassAsValueType("CSharp");
            ctx.SetClassAsValueType("StructTestArrayTypeFromTypedef");
            ctx.IgnoreClassWithName("IgnoredTypeInheritingNonIgnoredWithNoEmptyCtor");
            ctx.IgnoreClassWithName("Ignored");

            var macroRegex = new Regex("(MY_MACRO_TEST_.*)");
            var list = (from unit in ctx.TranslationUnits
                        where !unit.IsValid || unit.FileName == "CSharp.h"
                        from macro in unit.PreprocessedEntities.OfType<MacroDefinition>()
                        where macroRegex.IsMatch(macro.Name)
                        select macro.Name).ToList();
            var enumTest = ctx.GenerateEnumFromMacros("MyMacroTestEnum", list.ToArray());

            ctx.GenerateEnumFromMacros("MyMacroTest2Enum", "MY_MACRO_TEST2_.*");

            enumTest.Namespace = new Namespace()
                {
                    Name = "MacroTest",
                    Namespace = ctx.TranslationUnits.First(u => u.IsValid && !u.IsSystemHeader)
                };
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
    [TypeMap("boolean_t")]
    public class BooleanTypeMap : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new BuiltinType(PrimitiveType.Bool);
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

        public override bool IsIgnored => Type.IsDependent;

        private static Type GetEnumType(Type mappedType)
        {
            var type = mappedType.Desugar();
            ClassTemplateSpecialization classTemplateSpecialization;
            var templateSpecializationType = type as TemplateSpecializationType;
            if (templateSpecializationType != null)
                return templateSpecializationType.Arguments[0].Type.Type;
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
            return new TagType(flags ?? (flags = Context.ASTContext.FindEnum("Flags").First()));
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }

        private Enumeration flags;
    }

    [TypeMap("QList")]
    public class QList : TypeMap
    {
        public override bool IsIgnored
        {
            get
            {
                var type = (TemplateSpecializationType) Type;
                var pointeeType = type.Arguments[0].Type;
                var checker = new TypeIgnoreChecker(TypeMapDatabase);
                pointeeType.Visit(checker);
                return checker.IsIgnored;
            }
        }

        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Native)
            {
                var type = (TemplateSpecializationType) ctx.Type.Desugar();
                var specialization = type.GetClassTemplateSpecialization();
                var typePrinter = new CSharpTypePrinter(null);
                typePrinter.PushContext(TypePrinterContextKind.Native);
                return new CustomType(string.Format($@"{
                    specialization.Visit(typePrinter)}{
                    (Type.IsAddress() ? "*" : string.Empty)}", specialization.Visit(typePrinter),
                    Type.IsAddress() ? "*" : string.Empty));
            }

            return new CustomType(
                $@"System.Collections.Generic.{
                    (ctx.MarshalKind == MarshalKind.DefaultExpression ? "List" : "IList")}<{
                    ctx.GetTemplateParameterList()}>");
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
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            // doesn't matter, we just need it to compile
            return new BuiltinType(PrimitiveType.Int);
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

    [TypeMap("QString")]
    public class QString : TypeMap
    {
        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            if (ctx.Kind == TypePrinterContextKind.Native)
            {
                return new CustomType($@"global::CSharp.QString.{
                    Helpers.InternalStruct}{
                    (ctx.Type.IsAddress() ? "*" : string.Empty)}");
            }
            return new CILType(typeof(string));
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Type.Desugar().IsAddress() ?
                "global::System.IntPtr.Zero" : "\"test\"");
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write("\"test\"");
        }
    }

    #endregion
}

