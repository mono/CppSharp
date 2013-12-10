using System.Collections.Generic;

namespace CppSharp.AST
{
    public class Event : Declaration, ITypedDecl
    {
        public Event()
        {
            Parameters = new List<Parameter>();
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitEvent(this);
        }

        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }

        public List<Parameter> Parameters;

        public Declaration OriginalDeclaration { get; set; }
    }
}
