using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Generators.Registrable.Lua.Sol
{
    public class LuaSolHeaders : LuaSolSources
    {
        public LuaSolHeaders(LuaSolGenerator generator, IEnumerable<TranslationUnit> units)
            : base(generator, units)
        {
        }

        public override string FileExtension => "h";

        protected override bool TemplateAllowed { get { return true; } }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock();
            WriteLine("#pragma once");
            PopBlock(NewLineKind.BeforeNextBlock);

            TranslationUnit.Visit(this);
        }

        #region TranslationUnit

        public override void GenerateTranslationUnitNamespaceBegin(TranslationUnit translationUnit)
        {
            PushBlock(BlockKind.Namespace);
            WriteLine($"namespace {TranslationUnit.Module.OutputNamespace} {{");
        }

        public override void GenerateTranslationUnitNamespaceEnd(TranslationUnit translationUnit)
        {
            WriteLine($"}}  // namespace {TranslationUnit.Module.OutputNamespace}");
            PopBlock();
        }

        public virtual void GenerateTranslationUnitRegistrationFunctionDeclaration(TranslationUnit translationUnit)
        {
            NewLine();
            WriteLine(GetTranslationUnitRegistrationFunctionSignature(translationUnit));
            NewLine();
        }

        public override void GenerateTranslationUnit(TranslationUnit translationUnit)
        {
            GenerateTranslationUnitNamespaceBegin(translationUnit);
            GenerateTranslationUnitRegistrationFunctionDeclaration(translationUnit);
            GenerateTranslationUnitNamespaceEnd(translationUnit);
        }

        public override bool CanGenerateTranslationUnit(TranslationUnit unit)
        {
            if (AlreadyVisited(unit))
            {
                return false;
            }
            return true;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!CanGenerateTranslationUnit(unit))
            {
                return false;
            }

            GenerateTranslationUnit(unit);

            return true;
        }

        #endregion

        public virtual void GenerateMain()
        {
            VisitNamespace(TranslationUnit);
        }

        public virtual void GenerateIncludes()
        {
            if (Generator.GeneratorOptions.BaseInclude != null)
            {
                WriteLineIndent(Generator.GeneratorOptions.BaseInclude.ToString());
            }
        }
    }
}
