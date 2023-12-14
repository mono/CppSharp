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

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock();
            WriteLine("#pragma once");
            PopBlock(NewLineKind.BeforeNextBlock);

            //NewLine();
            //PushBlock(BlockKind.Includes);
            //GenerateIncludes();
            //PopBlock(NewLineKind.BeforeNextBlock);

            TranslationUnit.Visit(this);

            //PushBlock(BlockKind.Footer);
            //PopBlock();

            //PushBlock(BlockKind.Class);
            //PopBlock(NewLineKind.IfNotEmpty);

            //RegistrableGeneratorContext mycontext = new RegistrableGeneratorContext();
            //string a = (string)mycontext[new InfoEntry("")].Pop();
        }

        #region TranslationUnit

        public virtual void GenerateTranslationUnitNamespaceBegin(TranslationUnit translationUnit)
        {
            PushBlock(BlockKind.Namespace);
            WriteLine($"namespace {TranslationUnit.Module.OutputNamespace} {{");
        }

        public virtual void GenerateTranslationUnitNamespaceEnd(TranslationUnit translationUnit)
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

        public virtual void GenerateTranslationUnit(TranslationUnit translationUnit)
        {
            GenerateTranslationUnitNamespaceBegin(translationUnit);
            GenerateTranslationUnitRegistrationFunctionDeclaration(translationUnit);
            GenerateTranslationUnitNamespaceEnd(translationUnit);
        }

        public virtual bool CanGenerateTranslationUnit(TranslationUnit unit)
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

        //

        public virtual void GenerateMain()
        {
            VisitNamespace(TranslationUnit);
        }

        public virtual void GenerateIncludes()
        {
            foreach (var include in Generator.GeneratorOptions.CommonIncludes)
            {
                WriteLineIndent(include.ToString());
            }
        }

        //public override bool VisitNamespace(Namespace @namespace)
        //{
        //    base.VisitNamespace(@namespace);
        //    return true;
        //}

        public override bool VisitMethodDecl(Method method)
        {
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            //if (FunctionIsTemplate(function))
            //{
            //    Console.WriteLine("test");
            //}
            return true;
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            return true;
        }

        public override bool VisitTypeAliasTemplateDecl(TypeAliasTemplate typeAliasTemplate)
        {
            return true;
        }

        public override bool VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            return true;
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return true;
        }

        public static bool FunctionIsTemplate(Function function)
        {
            foreach (var template in function.Namespace.Templates)
            {
                if (template.TemplatedDecl == function)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
