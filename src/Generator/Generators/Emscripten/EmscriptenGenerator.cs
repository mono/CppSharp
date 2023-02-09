using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators.Cpp;

namespace CppSharp.Generators.Emscripten
{
    /// <summary>
    /// Emscripten generator responsible for driving the generation of binding files.
    /// Embind documentation: https://emscripten.org/docs/porting/connecting_cpp_and_javascript/embind.html
    /// </summary>
    public class EmscriptenGenerator : CppGenerator
    {
        public EmscriptenGenerator(BindingContext context) : base(context)
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
            }

            return outputs;
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var header = new EmscriptenHeaders(Context, units);
            outputs.Add(header);

            var source = new EmscriptenSources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override GeneratorOutput GenerateModule(Module module)
        {
            if (module == Context.Options.SystemModule)
                return null;

            var moduleGen = new EmscriptenModule(Context, module);

            var output = new GeneratorOutput
            {
                TranslationUnit = new TranslationUnit
                {
                    FilePath = $"{module.LibraryName}_embind_module.cpp",
                    Module = module
                },
                Outputs = new List<CodeGenerator> { moduleGen }
            };

            output.Outputs[0].Process();

            return output;
        }
    }
}
