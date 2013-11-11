using System;
using System.Collections.Generic;
using CppSharp.AST;

#if !OLD_PARSER

namespace CppSharp.Parser
{
    /// <summary>
    /// Converts from native parser ASTs to managed ASTs.
    /// </summary>
    public class ASTConverter
    {
        public AST.ASTContext OriginalContext;
        public ASTContext NewContext;

        public ASTConverter(AST.ASTContext context)
        {
            OriginalContext = context;
        }

        public ASTContext Convert()
        {
            NewContext = new ASTContext();

            foreach (var unit in OriginalContext.TranslationUnits)
            {
                var newUnit = ConvertTranslationUnit(unit);
                NewContext.TranslationUnits.Add(newUnit);
            }

            return NewContext;
        }

        public TranslationUnit ConvertTranslationUnit(AST.TranslationUnit unit)
        {
            var newUnit = ConvertDeclaration<TranslationUnit>(unit);
            newUnit.FilePath = unit.FileName;
            
            foreach (var macro in unit.Macros)
            {
                var newMacro = ConvertMacroDefinition(macro);
                newUnit.Macros.Add(newMacro);
            }

            return newUnit;
        }

        public T ConvertDeclaration<T>(AST.Declaration decl)
            where T : Declaration, new()
        {
            var newDecl = new T
                {
                    Access = ConvertAccessSpecifier(decl.Access),
                    //Comment = ConvertRawComment(),
                    //CompleteDeclaration =
                    //    ConvertDeclaration<Declaration>(decl.CompleteDeclaration)
                    DebugText = decl.DebugText,
                    DefinitionOrder = decl.DefinitionOrder,
                    IsDependent = decl.IsDependent,
                    IsIncomplete = decl.IsIncomplete,
                    Name = decl.Name,
                    PreprocessedEntities =
                        ConvertPreprocessedEntities(decl.PreprocessedEntities),
                    Namespace = ConvertNamespace(decl._Namespace),
                };

            return newDecl;
        }

        IList<PreprocessedEntity>
        ConvertPreprocessedEntities(IEnumerable<AST.PreprocessedEntity> entities)
        {
            var newEntities = new List<PreprocessedEntity>();
            foreach (var entity in entities)
            {
                //var newEntity = ConvertDeclaration<PreprocessedEntity>(entity);
                //newEntities.Add(newEntity);
            }

            return newEntities;
        }

        DeclarationContext ConvertNamespace(AST.DeclarationContext context)
        {
            var newNamespace = new Namespace();

            return newNamespace;
        }

        AccessSpecifier ConvertAccessSpecifier(AST.AccessSpecifier access)
        {
            switch (access)
            {
                case AST.AccessSpecifier.Private:
                    return AccessSpecifier.Private;
                case AST.AccessSpecifier.Protected:
                    return AccessSpecifier.Protected;
                case AST.AccessSpecifier.Public:
                    return AccessSpecifier.Public;
            }

            throw new NotSupportedException();
        }

        MacroDefinition ConvertMacroDefinition(AST.MacroDefinition macro)
        {
            var newMacro = ConvertDeclaration<MacroDefinition>(macro);
            newMacro.Expression = macro.Expression;

            return newMacro;
        }
    }
}

#endif