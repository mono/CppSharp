using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.Cpp;

namespace CppSharp.Generators.C
{
    /// <summary>
    /// N-API generator responsible for driving the generation of binding files.
    /// N-API documentation: https://nodejs.org/api/n-api.html
    /// </summary>
    public class NAPIGenerator : CppGenerator
    {
        public NAPIGenerator(BindingContext context) : base(context)
        {
        }

        public override bool SetupPasses()
        {
            var typeCheck = new NAPITypeCheck();
            Context.TranslationUnitPasses.AddPass(typeCheck);

            return true;
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

            var helpers = GenerateHelpers();
            OnUnitGenerated(helpers);
            outputs.Add(helpers);

            return outputs;
        }

        public GeneratorOutput GenerateHelpers()
        {
            var helpersGen = new NAPIHelpers(Context);
            helpersGen.Process();

            var output = new GeneratorOutput
            {
                TranslationUnit = new TranslationUnit { FilePath = "NAPIHelpers.h" },
                Outputs = new List<CodeGenerator> { helpersGen }
            };

            return output;
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var outputs = new List<CodeGenerator>();

            var source = new NAPISources(Context, units);
            outputs.Add(source);

            return outputs;
        }

        public override GeneratorOutput GenerateModule(Module module)
        {
            if (module == Context.Options.SystemModule)
                return null;

            var moduleGen = new NAPIModule(Context, module);

            var output = new GeneratorOutput
            {
                TranslationUnit = new TranslationUnit
                {
                    FilePath = $"{module.LibraryName}.cpp",
                    Module = module
                },
                Outputs = new List<CodeGenerator> { moduleGen }
            };

            output.Outputs[0].Process();

            return output;
        }
    }
}