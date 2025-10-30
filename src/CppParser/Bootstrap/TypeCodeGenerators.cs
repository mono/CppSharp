using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Generators.CSharp;

using static CppSharp.CodeGeneratorHelpers;

namespace CppSharp
{
    class TypeDeclarationsCodeGenerator : StmtDeclarationsCodeGenerator
    {
        public TypeDeclarationsCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override void GenerateIncludes()
        {
            WriteInclude("Sources.h", CInclude.IncludeKind.Quoted);
            WriteInclude("string", CInclude.IncludeKind.Angled);
            WriteInclude("memory", CInclude.IncludeKind.Angled);
            WriteInclude("vector", CInclude.IncludeKind.Angled);
        }

        public override void GenerateForwardDecls()
        {
            WriteLine("class Decl;");
            WriteLine("class TemplateArgument;");
            WriteLine("class TemplateParameterList;");
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.IsGenerated)
                return false;

            GenerateClassSpecifier(@class);
            NewLine();

            WriteOpenBraceAndIndent();

            PushBlock();
            VisitDeclContext(@class);
            PopBlock(NewLineKind.Always);

            WriteLine($"public:");
            Indent();

            WriteLine($"{@class.Name}();");

            if (@class.Name == "Type")
                WriteLine("TypeClass typeClass;");

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                GenerateMethod(method);
            }

            foreach (var property in @class.Properties)
            {
                if (SkipProperty(property))
                    continue;

                GenerateProperty(property);
            }

            Unindent();
            UnindentAndWriteCloseBrace();

            return true;
        }

        protected void GenerateMethod(Method method)
        {
            // Validate all method parameters first
            foreach (var param in method.Parameters)
            {
                ValidateTypeMethodParameter(param);
            }

            var iteratorType = GetIteratorType(method);
            var iteratorTypeName = GetIteratorTypeName(iteratorType, CodeGeneratorHelpers.CppTypePrinter);

            // Handle special cases for type-specific methods
            if (method.ReturnType.Type.IsPointer())
            {
                var returnTypeName = ValidateTypeMethodReturnType(method.ReturnType.Type);
                WriteLine($"{returnTypeName} Get{method.Name}(uint i);");
            }
            else
                WriteLine($"{iteratorTypeName} Get{method.Name}(uint i);");

            WriteLine($"uint Get{method.Name}Count();");
        }

        private string ValidateTypeMethodReturnType(AST.Type type)
        {
            if (type.IsPointerTo(out TagType tagType))
            {
                var pointeeType = tagType.Declaration?.Visit(CodeGeneratorHelpers.CppTypePrinter).Type ?? "void";
                if (pointeeType.Contains("Type") || pointeeType.Contains("QualType"))
                    return $"AST::{pointeeType}*";
                return $"{pointeeType}*";
            }

            if (type is TemplateSpecializationType template)
            {
                if (template.Template.TemplatedDecl.Name.Contains("Type"))
                    return $"AST::{template.Template.TemplatedDecl.Name}";
            }

            return type.Visit(CodeGeneratorHelpers.CppTypePrinter).ToString();
        }

        private void ValidateTypeMethodParameter(Parameter param)
        {
            // Handle pointer types
            if (param.Type.IsPointer())
            {
                var pointee = param.Type.GetFinalPointee();
                if (pointee is ArrayType)
                {
                    param.ExplicitlyIgnore();
                    return;
                }

                // Special handling for type-related pointers
                if (pointee is TagType tagType &&
                    tagType.Declaration?.Name.Contains("Type") == true)
                {
                    // These are valid, don't ignore
                    return;
                }
            }

            // Handle template parameters specific to types
            if (param.Type is TemplateSpecializationType template)
            {
                var templateName = template.Template.TemplatedDecl.Name;
                if (!templateName.Contains("Type") &&
                    !templateName.Contains("QualType"))
                {
                    param.ExplicitlyIgnore();
                }
            }
        }

        protected void GenerateProperty(Property property)
        {
            var typeName = GetDeclTypeName(property);
            var fieldName = GetDeclName(property);

            // Handle special type properties
            if (typeName.Contains("QualType"))
            {
                WriteLine($"AST::{typeName} {fieldName};");

                if (property.GetMethod != null)
                    WriteLine($"AST::{typeName} Get{FirstLetterToUpperCase(fieldName)}() const;");
                if (property.SetMethod != null)
                    WriteLine($"void Set{FirstLetterToUpperCase(fieldName)}(AST::{typeName} value);");
            }
            else if (property.Type.IsPointerTo(out TagType tagType))
            {
                var pointeeType = tagType.Declaration?.Visit(CodeGeneratorHelpers.CppTypePrinter).Type ?? typeName;
                if (pointeeType.Contains("Type") || pointeeType.Contains("QualType"))
                {
                    WriteLine($"AST::{pointeeType}* {fieldName};");

                    if (property.GetMethod != null)
                        WriteLine($"AST::{pointeeType}* Get{FirstLetterToUpperCase(fieldName)}() const;");
                    if (property.SetMethod != null)
                        WriteLine($"void Set{FirstLetterToUpperCase(fieldName)}(AST::{pointeeType}* value);");
                }
                else
                {
                    WriteLine($"{pointeeType}* {fieldName};");

                    if (property.GetMethod != null)
                        WriteLine($"{pointeeType}* Get{FirstLetterToUpperCase(fieldName)}() const;");
                    if (property.SetMethod != null)
                        WriteLine($"void Set{FirstLetterToUpperCase(fieldName)}({pointeeType}* value);");
                }
            }
            // Handle template specialization types
            else if (property.Type is TemplateSpecializationType templateType)
            {
                var templateName = templateType.Template.TemplatedDecl.Name;
                WriteLine($"AST::{templateName} {fieldName};");

                if (property.GetMethod != null)
                    WriteLine($"AST::{templateName} Get{FirstLetterToUpperCase(fieldName)}() const;");
                if (property.SetMethod != null)
                    WriteLine($"void Set{FirstLetterToUpperCase(fieldName)}(AST::{templateName} value);");
            }
            else
            {
                WriteLine($"{typeName} {fieldName};");

                if (property.GetMethod != null)
                    WriteLine($"{typeName} Get{FirstLetterToUpperCase(fieldName)}() const;");
                if (property.SetMethod != null)
                    WriteLine($"void Set{FirstLetterToUpperCase(fieldName)}({typeName} value);");
            }
        }
    }

    class TypeDefinitionsCodeGenerator : StmtDefinitionsCodeGenerator
    {
        public TypeDefinitionsCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override void GenerateIncludes()
        {
            GenerateCommonIncludes();
            WriteInclude("Type.h", CInclude.IncludeKind.Quoted);
        }

        protected void GenerateMethodDefinition(Class @class, Method method)
        {
            var iteratorType = GetIteratorType(method);
            var iteratorTypeName = GetIteratorTypeName(iteratorType, CodeGeneratorHelpers.CppTypePrinter);

            WriteLine($"uint {GetQualifiedName(@class)}::Get{method.Name}Count()");
            WriteOpenBraceAndIndent();
            WriteLine($"return {method.Name}().size();");
            UnindentAndWriteCloseBrace();
            NewLine();

            // Handle pointer types specially
            if (method.ReturnType.Type.IsPointerTo(out TagType tagType))
            {
                var pointeeType = tagType.Declaration?.Name ?? iteratorTypeName;
                WriteLine($"AST::{pointeeType}* {GetQualifiedName(@class)}::Get{method.Name}(uint i)");
            }
            else
                WriteLine($"{iteratorTypeName} {GetQualifiedName(@class)}::Get{method.Name}(uint i)");

            WriteOpenBraceAndIndent();
            WriteLine($"auto elements = {method.Name}();");
            WriteLine($"auto it = elements.begin();");
            WriteLine($"std::advance(it, i);");
            WriteLine($"return *it;");
            UnindentAndWriteCloseBrace();
            NewLine();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.IsGenerated)
                return false;

            WriteLine($"{@class.Name}::{@class.Name}()");
            var isBaseType = @class.Name == "Type";
            var typeMember = isBaseType ? "typeClass" : @class.BaseClass.Name;
            var typeClass = @class.Name == "Type" ? "None" : @class.Name;
            WriteLineIndent($": {typeMember}(TypeClass::{typeClass})");
            GenerateMemberInits(@class);
            WriteOpenBraceAndIndent();
            UnindentAndWriteCloseBrace();
            NewLine();

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                GenerateMethodDefinition(@class, method);
            }

            return true;
        }
    }

    class TypeASTConverterCodeGenerator : ASTConverterCodeGenerator
    {
        public TypeASTConverterCodeGenerator(BindingContext context, IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override string BaseTypeName => "Type";

        public override bool IsAbstractASTNode(Class kind)
        {
            return IsAbstractType(kind);
        }

        protected override void GenerateVisitorSwitch(IEnumerable<string> classes)
        {
            WriteLine($"switch({ParamName}.DeclKind)");
            WriteOpenBraceAndIndent();

            GenerateSwitchCases(classes);

            UnindentAndWriteCloseBrace();
        }

        public void GenerateSwitchCases(IEnumerable<string> classes)
        {
            foreach (var className in classes)
            {
                WriteLine($"case Parser.AST.TypeClass.{className}:");
                WriteLineIndent($"return Visit{className}({ParamName} as Parser.AST.{className}Type);");
            }

            WriteLine("default:");
            WriteLineIndent($"throw new System.NotImplementedException(" +
                $"{ParamName}.TypeClass.ToString());");
        }
    }
}