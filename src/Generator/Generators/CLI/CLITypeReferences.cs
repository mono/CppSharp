using System.Collections.Generic;
using System.IO;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.AST;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    public class CLITypeReference : TypeReference
    {
        public Include Include;

        public override string ToString()
        {
            if(Include.InHeader)
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

        public CLITypeReferenceCollector(ITypeMapDatabase typeMapDatabase, DriverOptions driverOptions)
        {
            TypeMapDatabase = typeMapDatabase;
            DriverOptions = driverOptions;
            typeReferences = new Dictionary<Declaration,CLITypeReference>();
        }

        public CLITypeReference GetTypeReference(Declaration decl)
        {
            if(typeReferences.ContainsKey(decl))
                return typeReferences[decl];

            var @ref = new CLITypeReference { Declaration = decl };
            typeReferences.Add(decl, @ref);

            return @ref;
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
            if(decl.Namespace == null)
                return;

            // Find a type map for the declaration and use it if it exists.
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(record.Value, out typeMap))
            {
                typeMap.Declaration = record.Value;
                typeMap.CLITypeReference(this, record);
                return;
            }

            var translationUnit = decl.Namespace.TranslationUnit;

            if (translationUnit.IsSystemHeader)
                return;

            if (!decl.IsGenerated)
                return;

            if(IsBuiltinTypedef(decl))
                return;

            var typeRef = GetTypeReference(decl);
            if (typeRef.Include.TranslationUnit == null)
            {
                typeRef.Include = new Include
                    {
                        File = GetIncludePath(translationUnit),
                        TranslationUnit = translationUnit,
                        Kind = translationUnit.IsGenerated
                            ? Include.IncludeKind.Quoted
                            : Include.IncludeKind.Angled,
                    };
            }

            typeRef.Include.InHeader |= IsIncludeInHeader(record);
        }

        private string GetIncludePath(TranslationUnit translationUnit)
        {
            if (!DriverOptions.UseHeaderDirectories)
                return translationUnit.FileName;

            var rel = PathHelpers.GetRelativePath(
                TranslationUnit.FileRelativeDirectory,
                translationUnit.FileRelativeDirectory);

            if (string.IsNullOrEmpty(rel))
                return translationUnit.FileName;

            return Path.Combine(rel, translationUnit.FileName);
        }

        private bool IsBuiltinTypedef(Declaration decl)
        {
            var typedefDecl = decl as TypedefDecl;
            if(typedefDecl == null) return false;
            if(typedefDecl.Type is BuiltinType) return true;

            var typedefType = typedefDecl.Type as TypedefType;
            if(typedefType == null) return false;
            if(typedefType.Declaration == null) return false;

            return typedefType.Declaration.Type is BuiltinType;
        }

        private bool IsIncludeInHeader(ASTRecord<Declaration> record)
        {
            if (TranslationUnit == record.Value.Namespace.TranslationUnit)
                return false;

            return record.IsBaseClass() || record.IsFieldValueType() || record.IsDelegate();
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (decl.Namespace != null && decl.Namespace.TranslationUnit.IsSystemHeader)
                return false;

            return decl.IsDeclared;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            if (@class.IsIncomplete && @class.CompleteDeclaration != null)
                @class = (Class) @class.CompleteDeclaration;

            var keywords = @class.IsValueType? "value struct" : "ref class";
            var @ref = string.Format("{0} {1};", keywords, @class.Name);
            
            GetTypeReference(@class).FowardReference = @ref;

            return false;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            var @base = "";
            if(!@enum.Type.IsPrimitiveType(PrimitiveType.Int))
                @base = string.Format(" : {0}", @enum.Type);

            var @ref = string.Format("enum struct {0}{1};", @enum.Name, @base);

            GetTypeReference(@enum).FowardReference = @ref;

            return false;
        }
    }
}