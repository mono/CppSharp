using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.AST;
using CppSharp.Types;

namespace CppSharp.Generators.CLI
{
    public class CLITypeReference
    {
        public Include Include;
        public string FowardReference;

        public override string ToString()
        {
            return FowardReference;
        }
    }

    public class CLITypeReferenceCollector : AstVisitor
    {
        private Dictionary<Declaration, CLITypeReference> typeReferences;
        private readonly ITypeMapDatabase TypeMapDatabase;
        private TranslationUnit TranslationUnit;

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

            var @ref = new CLITypeReference();
            typeReferences.Add(decl, @ref);
            return @ref;
        }

        public void Process(Namespace @namespace)
        {
            var collector = new RecordCollector(@namespace.TranslationUnit);
            @namespace.Visit(collector);

            TranslationUnit = @namespace.TranslationUnit;

            foreach (var record in collector.Declarations)
            {
                if (record.Value is Namespace)
                    continue;

                var declNamespace = record.Value.Namespace;

                var isSameNamespace = declNamespace == @namespace;
                if (declNamespace != null)
                    isSameNamespace |= declNamespace.QualifiedName == @namespace.QualifiedName;

                if (!isSameNamespace)
                    continue;

                record.Value.Visit(this);
                GenerateInclude(record);
                ProcessTypeMap(record);
            }
        }

        private void ProcessTypeMap(ASTRecord<Declaration> record)
        {
            TypeMap typeMap;
            if (!TypeMapDatabase.FindTypeMap(record.Value, out typeMap)) return;

            // Typemap must explicitly set the include file when one is required.
            GetTypeReference(record.Value).Include.File = "";

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

            if (declFile.Contains("String"))
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