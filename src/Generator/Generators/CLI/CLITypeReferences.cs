using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.AST;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    public class CLITypeReference : TypeReference
    {
        public Include Include;

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(FowardReference))
                return FowardReference;

            return Include.ToString();
        }
    }

    public class CLITypeReferenceCollector : AstVisitor
    {
        private readonly ITypeMapDatabase TypeMapDatabase;
        private TranslationUnit TranslationUnit;

        private Dictionary<Declaration, CLITypeReference> typeReferences;
        public IEnumerable<CLITypeReference> TypeReferences
        {
            get { return typeReferences.Values; }
        }

        public CLITypeReferenceCollector(ITypeMapDatabase typeMapDatabase)
        {
            TypeMapDatabase = typeMapDatabase;
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
                ProcessTypeMap(record);
            }
        }

        private void ProcessTypeMap(ASTRecord<Declaration> record)
        {
            TypeMap typeMap;
            if (!TypeMapDatabase.FindTypeMap(record.Value, out typeMap))
                return;


            typeMap.Declaration = record.Value;
            typeMap.CLITypeReference(this, record);
        }

        private void GenerateInclude(ASTRecord<Declaration> record)
        {
            var decl = record.Value;
            if(decl.Namespace == null)
                return;

            var declFile = decl.Namespace.TranslationUnit.FileName;

            if(decl.Namespace.TranslationUnit.IsSystemHeader)
                return;

            if(decl.Ignore)
                return;

            if(IsBuiltinTypedef(decl))
                return;

            var typeRef = GetTypeReference(decl);
            typeRef.Include = new Include()
            {
                File = declFile,
                TranslationUnit = decl.Namespace.TranslationUnit,
                Kind = Include.IncludeKind.Quoted,
                InHeader = IsIncludeInHeader(record)
            };
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
            return record.IsBaseClass() || record.IsFieldValueType();
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            return ShouldVisitDecl(decl);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if(!ShouldVisitDecl(@class))
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
            if(!ShouldVisitDecl(@enum))
                return false;

            var @base = "";
            if(!@enum.Type.IsPrimitiveType(PrimitiveType.Int32)) 
                @base = string.Format(" : {0}", @enum.Type);

            var @ref = string.Format("enum struct {0}{1};", @enum.Name, @base);

            GetTypeReference(@enum).FowardReference = @ref;

            return false;
        }

        private bool ShouldVisitDecl(Declaration decl)
        {
            if(decl.Namespace != null && decl.Namespace.TranslationUnit.IsSystemHeader)
                return false;

            if (decl.Ignore)
                return false;

            return true;
        }
    }
}