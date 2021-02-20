using System.Collections.Generic;

namespace CppSharp.AST
{
    public class Event : DeclarationContext, ITypedDecl
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitEvent(this);
        }

        public Type Type => QualifiedType.Type;
        public QualifiedType QualifiedType { get; set; }

        public List<Parameter> Parameters { get; } = new List<Parameter>();

        public Declaration OriginalDeclaration { get; set; }

        public int GlobalId { get; set; }
    }
}
