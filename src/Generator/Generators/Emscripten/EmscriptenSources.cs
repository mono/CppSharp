using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.Cpp;

namespace CppSharp.Generators.Emscripten
{
    public class EmscriptenCodeGenerator : MethodGroupCodeGenerator
    {
        protected EmscriptenCodeGenerator(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public virtual MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalManagedToNativePrinter(MarshalContext ctx)
        {
            return new EmscriptenMarshalManagedToNativePrinter(ctx);
        }

        public virtual MarshalPrinter<MarshalContext, CppTypePrinter> GetMarshalNativeToManagedPrinter(MarshalContext ctx)
        {
            return new EmscriptenMarshalNativeToManagedPrinter(ctx);
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return true;
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return true;
        }
    }

    /// <summary>
    /// Generates Emscripten C/C++ source files.
    /// Embind documentation: https://emscripten.org/docs/porting/connecting_cpp_and_javascript/embind.html
    /// </summary>
    public class EmscriptenSources : EmscriptenCodeGenerator
    {
        public override string FileExtension => "cpp";

        public EmscriptenSources(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            {
                WriteInclude(new CInclude()
                {
                    File = "emscripten/bind.h",
                    Kind = CInclude.IncludeKind.Angled
                });

                foreach (var unit in TranslationUnits)
                {
                    WriteInclude(unit.IncludePath, CInclude.IncludeKind.Angled);
                }
            }
            PopBlock(NewLineKind.Always);
            
            var name = GetTranslationUnitName(TranslationUnit);
            WriteLine($"extern \"C\" void embind_init_{name}()");
            WriteOpenBraceAndIndent();
            VisitNamespace(TranslationUnit);
            UnindentAndWriteCloseBrace();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.IsIncomplete)
                return true;

            PushBlock();
            Write($"emscripten::class_<{@class.QualifiedOriginalName}");
            if (@class.HasBaseClass)
                Write($", emscripten::base<{@class.BaseClass.QualifiedOriginalName}>");
            WriteLine($">(\"{@class.Name}\")");
            
            VisitClassDeclContext(@class);
            
            WriteLineIndent(";");
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override void VisitClassConstructors(IEnumerable<Method> ctors)
        {
            var overloadCheck = new HashSet<int>();
            foreach (var ctor in ctors)
            {
                if (overloadCheck.Contains(ctor.Parameters.Count))
                {
                    Console.WriteLine($"Ignoring overloaded ctor: {ctor.QualifiedOriginalName}");
                    continue;
                }

                var cppTypePrinter = new CppTypePrinter(Context)
                {
                    PrintFlavorKind = CppTypePrintFlavorKind.Cpp
                };
                cppTypePrinter.PushContext(TypePrinterContextKind.Native);

                var parameters = ctor.Parameters.Select(p => p.Type.Visit(cppTypePrinter).Type);
                WriteLineIndent($".constructor<{string.Join(", ", parameters)}>()");

                overloadCheck.Add(ctor.Parameters.Count);
            }
        }

        public override bool VisitMethodDecl(Method method)
        {
            Indent();
            var ret = VisitFunctionDecl(method);
            Unindent();
            return ret;
        }

        public override bool VisitFieldDecl(Field field)
        {
            WriteLineIndent($".field(\"{field.Name}\", &{field.Class.QualifiedOriginalName}::{field.OriginalName})");
            return true;
        }

        public override void GenerateMethodGroup(List<Method> @group)
        {
            if (@group.Count > 1)
            {
                Console.WriteLine($"Ignoring method group: {@group.First().QualifiedOriginalName}");
                return;
            }

            base.GenerateMethodGroup(@group);
        }

        public override void GenerateFunctionGroup(List<Function> @group)
        {
            if (@group.Count > 1)
            {
                Console.WriteLine($"Ignoring function group: {@group.First().QualifiedOriginalName}");
                return;
            }

            base.GenerateFunctionGroup(@group);
        }

        public override void VisitDeclContextFunctions(DeclarationContext context)
        {
            PushBlock();
            base.VisitDeclContextFunctions(context);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            var prefix = function is Method ? ".function" : "emscripten::function";
            Write($"{prefix}(\"{function.Name}\", &{function.QualifiedOriginalName}");
                      
            var hasPointers = function.ReturnType.Type.IsPointer() ||
                              function.Parameters.Any(p => p.Type.IsPointer());
            if (hasPointers)
                Write(", emscripten::allow_raw_pointers()");

            WriteLine(function is not Method ? ");" : $")");
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return false;

            PushBlock();

            WriteLine($"emscripten::enum_<{@enum.Name}>(\"{@enum.QualifiedOriginalName}\")");
            foreach (var item in @enum.Items)
            {
                WriteLineIndent(@enum.IsScoped
                    ? $".value(\"{item.Name}\", {@enum.QualifiedOriginalName}::{item.OriginalName})"
                    : $".value(\"{item.Name}\", {item.QualifiedOriginalName})");
            }
            WriteLineIndent(";");
            
            PopBlock(NewLineKind.BeforeNextBlock);
            return true;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }
    }
}
