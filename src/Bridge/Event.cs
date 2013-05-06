using System.Collections.Generic;

namespace CppSharp
{
    public class Event : Declaration, ITypedDecl
    {
        public Event()
        {
            Parameters = new List<QualifiedType>();
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitEvent(this);
        }

        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }

        public List<QualifiedType> Parameters;
    }
}
