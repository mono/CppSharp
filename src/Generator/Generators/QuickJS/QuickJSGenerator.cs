using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.Cpp;

namespace CppSharp.Generators.C
{
    /// <summary>
    /// QuickJS generator responsible for driving the generation of binding files.
    /// QuickJS documentation: https://bellard.org/quickjs/
    /// </summary>
    public class QuickJSGenerator : CppGenerator
    {
        public QuickJSGenerator(BindingContext context) : base(context)
        {
        }

        public override List<GeneratorOutput> Generate()
        {
            var outputs = base.Generate();

            foreach (var module in Context.Options.Modules)
            {
                if (module == Context.Options.SystemModule)
                    continue;

                var output = GenerateModule(module);
                if (output != null)
                {
                    OnUnitGenerated(output);
                    outputs.Add(output);
                }

                var premake = GeneratePremake(module);
                if (premake != null)
                {
                    OnUnitGenerated(premake);
                    outputs.Add(premake);
                }
            }

            return outputs;
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new QuickJSHeaders(Context, units);
            outputs.Add(header);

            var source = new QuickJSSources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override GeneratorOutput GenerateModule(Module module)
        {
            if (module == Context.Options.SystemModule)
                return null;

            var moduleGen = new QuickJSModule(Context, module);

            var output = new GeneratorOutput
            {
                TranslationUnit = new TranslationUnit
                {
                    FilePath = $"{module.LibraryName}_qjs_module.cpp",
                    Module = module
                },
                Outputs = new List<CodeGenerator> { moduleGen }
            };

            output.Outputs[0].Process();

            return output;
        }

        public GeneratorOutput GeneratePremake(Module module)
        {
            if (module == Context.Options.SystemModule)
                return null;

            var premakeGen = new QuickJSPremakeBuildGenerator(Context, module);

            var output = new GeneratorOutput
            {
                TranslationUnit = new TranslationUnit
                {
                    FilePath = "premake5.lua",
                    Module = module
                },
                Outputs = new List<CodeGenerator> { premakeGen }
            };

            output.Outputs[0].Process();

            return output;
        }
    }

    class QuickJSPremakeBuildGenerator : CodeGenerator
    {
        readonly Module module;

        public QuickJSPremakeBuildGenerator(BindingContext context, Module module)
            : base(context, (TranslationUnit)null)
        {
            this.module = module;
        }

        public override string FileExtension => "lua";

        public override void Process()
        {
            /*
            var qjsPath = @"/Users/joao/Dev/CppSharp/examples/wxSharp/QuickJS";
            WriteLine($"qjs_inc_dir = \"{qjsPath}\"");
            WriteLine($"qjs_lib_dir = \"{qjsPath}\"");

            WriteLine("workspace \"qjs\"");
            WriteLineIndent("configurations { \"release\" }");
            */

            WriteLine($"project \"{module.LibraryName}\"");
            Indent();

            WriteLine("kind \"SharedLib\"");
            WriteLine("language \"C++\"");
            WriteLine("files { \"*.cpp\" }");
            WriteLine("includedirs { qjs_inc_dir }");
            WriteLine("libdirs { qjs_lib_dir }");
            WriteLine("filter { \"kind:StaticLib\" }");
            WriteLineIndent("links { \"quickjs\" }");
            WriteLine("filter { \"kind:SharedLib\" }");
            WriteLineIndent("defines { \"JS_SHARED_LIBRARY\" }");
            WriteLine("filter { \"kind:SharedLib\", \"system:macosx\" }");
            WriteLineIndent("linkoptions { \"-undefined dynamic_lookup\" }");

            Unindent();
        }
    }

    /* TODO: Write a build.sh aswell.
     *  set -ex
        ../../../../../build/premake5-osx gmake
        make clean
        make
    */
}
