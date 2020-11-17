﻿using System.Collections.Generic;
using System.IO;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.AST;
using CppSharp.Generators.C;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    public class CLITypeReference : TypeReference
    {
        public CInclude Include;

        public override string ToString()
        {
            if (Include.InHeader)
                return Include.ToString();

            if (!string.IsNullOrWhiteSpace(FowardReference))
                return FowardReference;

            return Include.ToString();
        }
    }

    public class CLITypeReferenceCollector : AstVisitor
    {
        private readonly ITypeMapDatabase TypeMapDatabase;
        private readonly DriverOptions DriverOptions;
        private TranslationUnit TranslationUnit;

        private Dictionary<Declaration, CLITypeReference> typeReferences;
        public IEnumerable<CLITypeReference> TypeReferences
        {
            get { return typeReferences.Values; }
        }

        public HashSet<Declaration> GeneratedDeclarations;

        public CLITypeReferenceCollector(ITypeMapDatabase typeMapDatabase, DriverOptions driverOptions)
        {
            TypeMapDatabase = typeMapDatabase;
            DriverOptions = driverOptions;
            typeReferences = new Dictionary<Declaration, CLITypeReference>();
            GeneratedDeclarations = new HashSet<Declaration>();
        }

        public CLITypeReference GetTypeReference(Declaration decl)
        {
            if (typeReferences.ContainsKey(decl))
                return typeReferences[decl];

            var translationUnit = decl.Namespace.TranslationUnit;

            if (ShouldIncludeTranslationUnit(translationUnit) && decl.IsGenerated && !IsBuiltinTypedef(decl))
            {
                var @ref = new CLITypeReference { Declaration = decl };

                @ref.Include = new CInclude
                {
                    File = GetIncludePath(translationUnit),
                    TranslationUnit = translationUnit,
                    Kind = translationUnit.IsGenerated ? CInclude.IncludeKind.Quoted : CInclude.IncludeKind.Angled
                };

                typeReferences.Add(decl, @ref);

                return @ref;
            }

            return null;
        }

        static Namespace GetEffectiveNamespace(Declaration decl)
        {
            if (decl == null || decl.Namespace == null)
                return null;

            var @namespace = decl.Namespace as Namespace;
            if (@namespace != null)
                return @namespace;

            return GetEffectiveNamespace(@namespace);
        }

        public void Process(Namespace @namespace, bool filterNamespaces = false)
        {
            TranslationUnit = @namespace.TranslationUnit;

            var collector = new RecordCollector(TranslationUnit);
            @namespace.Visit(collector);

            foreach (var record in collector.Declarations)
            {
                if (record.Value is Namespace)
                    continue;

                if (record.Value.IsDependent)
                    continue;

                if (filterNamespaces)
                {
                    var declNamespace = GetEffectiveNamespace(record.Value);

                    var isSameNamespace = declNamespace == @namespace;
                    if (declNamespace != null)
                        isSameNamespace |= declNamespace.QualifiedName == @namespace.QualifiedName;

                    if (!isSameNamespace)
                        continue;
                }

                record.Value.Visit(this);
                GenerateInclude(record);
            }
        }

        private void GenerateInclude(ASTRecord<Declaration> record)
        {
            var decl = record.Value;
            if (decl.Namespace == null)
                return;

            var typedefType = record.Value as TypedefNameDecl;

            // Find a type map for the declaration and use it if it exists.
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(record.Value, out typeMap) 
                || (typedefType != null && TypeMapDatabase.FindTypeMap(typedefType.Type.Desugar(), out typeMap)))
            {
                typeMap.CLITypeReference(this, record);
                return;
            }

            var typeRef = GetTypeReference(decl);
            if(typeRef != null)
            {
                typeRef.Include.InHeader |= IsIncludeInHeader(record);
            }
        }

        private bool ShouldIncludeTranslationUnit(TranslationUnit unit)
        {
            return !unit.IsSystemHeader && unit.IsValid && !unit.Ignore;
        }

        private string GetIncludePath(TranslationUnit translationUnit)
        {
            if (!DriverOptions.UseHeaderDirectories && DriverOptions.GenerateName != null)
            {
                var extension = Path.GetExtension(TranslationUnit.FileName);
                return $"{DriverOptions.GenerateName(translationUnit)}{extension}";
            }
            else if (DriverOptions.UseHeaderDirectories)
            {
                var path = Path.Combine(translationUnit.FileRelativeDirectory, translationUnit.FileName);
                return path;
            }

            return translationUnit.FileName;
        }

        private bool IsBuiltinTypedef(Declaration decl)
        {
            var typedefDecl = decl as TypedefDecl;
            if (typedefDecl == null) return false;
            if (typedefDecl.Type is BuiltinType) return true;

            var typedefType = typedefDecl.Type as TypedefType;
            if (typedefType == null) return false;
            if (typedefType.Declaration == null) return false;

            return typedefType.Declaration.Type is BuiltinType;
        }

        public bool IsIncludeInHeader(ASTRecord<Declaration> record)
        {
            if (TranslationUnit == record.Value.Namespace.TranslationUnit)
                return false;

            return record.IsBaseClass() || record.IsFieldValueType() || record.IsDelegate()
                || record.IsEnumNestedInClass();
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            return decl.IsDeclared &&
                (decl.Namespace == null || !decl.TranslationUnit.IsSystemHeader);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            if (@class.IsIncomplete && @class.CompleteDeclaration != null)
                @class = (Class) @class.CompleteDeclaration;

            if (@class.TranslationUnit == TranslationUnit)
                GeneratedDeclarations.Add(@class);

            string keywords;
            if (DriverOptions.IsCLIGenerator)
                keywords = @class.IsValueType ? "value struct" : "ref class";
            else
                keywords = @class.IsValueType ? "struct" : "class";

            var @ref = $"{keywords} {@class.Name};";

            var typeRef = GetTypeReference(@class);

            if (typeRef != null)
            {
                typeRef.FowardReference = @ref;
            }

            return false;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            var @base = "";
            if (!@enum.Type.IsPrimitiveType(PrimitiveType.Int))
                @base = $" : {@enum.Type}";

            var isCLIGenerator = DriverOptions.GeneratorKind == GeneratorKind.CLI;
            var enumKind = @enum.IsScoped || isCLIGenerator ? "enum class" : "enum";

            var @ref = $"{enumKind} {@enum.Name}{@base};";

            var typeRef = GetTypeReference(@enum);

            if (typeRef != null)
            {
                typeRef.FowardReference = @ref;
            }

            return false;
        }
    }
}