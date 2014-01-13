#if !OLD_PARSER

using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.Parser.AST;

namespace CppSharp
{
    /// <summary>
    /// This class converts from the C++ parser AST bindings to the
    /// AST defined in C#.
    /// </summary>
    public class ASTConverter
    {
        ASTContext Context { get; set; }

        public ASTConverter(ASTContext context)
        {
            Context = context;
        }

        public AST.ASTContext Convert()
        {
            var _ctx = new AST.ASTContext();

            for (uint i = 0; i < Context.TranslationUnitsCount; ++i)
            {
                var unit = Context.getTranslationUnits(i);
                _ctx.TranslationUnits.Add(VisitTranslationUnit(unit));
            }

            return _ctx;
        }

        #region Declarations

        AST.TranslationUnit VisitTranslationUnit(TranslationUnit unit)
        {
            var _unit = new AST.TranslationUnit { FilePath = unit.FileName };
            VisitDeclaration(unit, _unit);
            VisitDeclContext(unit, _unit);

            return _unit;
        }

        AST.Namespace VisitNamespace(Namespace @namespace)
        {
            var _namespace = new AST.Namespace();
            VisitDeclaration(@namespace, _namespace);
            VisitDeclContext(@namespace, _namespace);

            return _namespace;
        }

        void VisitDeclContext(DeclarationContext ctx, AST.DeclarationContext _ctx)
        {
            for (uint i = 0; i < ctx.NamespacesCount; ++i)
            {
                var childNamespace = ctx.getNamespaces(i);
                var _childNamespace = VisitNamespace(childNamespace);
                _ctx.Namespaces.Add(_childNamespace);
            }

            for (uint i = 0; i < ctx.ClassesCount; ++i)
            {
                var @class = ctx.getClasses(i);
                var _class = VisitClass(@class);
                _ctx.Classes.Add(_class);
            }
        }

        AST.Class VisitClass(Parser.AST.Class @class)
        {
            var _class = new AST.Class();
            VisitDeclaration(@class, _class);

            for (uint i = 0; i < @class.BasesCount; ++i)
            {
                var @base = @class.getBases(i);
                var _base = VisitBaseClassSpecifier(@base);
                _class.Classes.Add(_class);
            }

            for (uint i = 0; i < @class.FieldsCount; ++i)
            {
                var field = @class.getFields(i);
                var _field = VisitField(field);
                _class.Fields.Add(_field);
            }

            for (uint i = 0; i < @class.MethodsCount; ++i)
            {
                var method = @class.getMethods(i);
                var _method = VisitMethod(method);
                _class.Methods.Add(_method);
            }

            for (uint i = 0; i < @class.SpecifiersCount; ++i)
            {
                var spec = @class.getSpecifiers(i);
                var _spec = VisitAccessSpecifierDecl(spec);
                _class.Specifiers.Add(_spec);
            }

            _class.IsPOD = @class.IsPOD;
            _class.IsAbstract = @class.IsAbstract;
            _class.IsUnion = @class.IsUnion;
            _class.IsDynamic = @class.IsDynamic;
            _class.IsPolymorphic = @class.IsPolymorphic;
            _class.HasNonTrivialDefaultConstructor = @class.HasNonTrivialDefaultConstructor;
            _class.HasNonTrivialCopyConstructor = @class.HasNonTrivialCopyConstructor;

            _class.Layout = VisitClassLayout(@class.Layout);

            return _class;
        }

        AST.ClassLayout VisitClassLayout(Parser.AST.ClassLayout layout)
        {
            var _layout = new AST.ClassLayout();

            return _layout;
        }

        AST.Method VisitMethod(Parser.AST.Method method)
        {
            var _method = new AST.Method();
            VisitDeclaration(method, _method);

            return _method;
        }

        AST.Field VisitField(Parser.AST.Field field)
        {
            var _field = new AST.Field();
            VisitDeclaration(field, _field);

            return _field;
        }

        void VisitDeclaration(Declaration decl, AST.Declaration _decl)
        {
            _decl.Access = VisitAccessSpecifier(decl.Access);
            _decl.Name = decl.Name;
            _decl.DebugText = decl.DebugText;
            _decl.IsIncomplete = decl.IsIncomplete;
            _decl.IsDependent = decl.IsDependent;
            _decl.DefinitionOrder = decl.DefinitionOrder;

            for (uint i = 0; i < decl.PreprocessedEntitiesCount; ++i)
            {
                var entity = decl.getPreprocessedEntities(i);
                var _entity = VisitPreprocessedEntity(entity);
                _decl.PreprocessedEntities.Add(_entity);
            }

            _decl.OriginalPtr = decl.OriginalPtr;

        }

        AST.PreprocessedEntity VisitPreprocessedEntity(Parser.AST.PreprocessedEntity entity)
        {
            var _entity = new AST.MacroDefinition();
            VisitDeclaration(entity, _entity);

            return _entity;
        }

        AST.BaseClassSpecifier VisitBaseClassSpecifier(BaseClassSpecifier @base)
        {
            var _base = new AST.BaseClassSpecifier
                {
                    IsVirtual = @base.IsVirtual,
                    Access = VisitAccessSpecifier(@base.Access)
                };

            return _base;
        }

        AST.AccessSpecifierDecl VisitAccessSpecifierDecl(Parser.AST.AccessSpecifierDecl access)
        {
            var _access = new AST.AccessSpecifierDecl();
            VisitDeclaration(access, _access);

            return _access;
        }

        AST.AccessSpecifier VisitAccessSpecifier(Parser.AST.AccessSpecifier access)
        {
            switch (access)
            {
            case Parser.AST.AccessSpecifier.Private:
                return AST.AccessSpecifier.Private;
            case Parser.AST.AccessSpecifier.Protected:
                return AST.AccessSpecifier.Protected;
            case Parser.AST.AccessSpecifier.Public:
                return AST.AccessSpecifier.Public;
            }

            throw new ArgumentOutOfRangeException();
        }

        #endregion

        #region Types

        #endregion
    }
}

#endif