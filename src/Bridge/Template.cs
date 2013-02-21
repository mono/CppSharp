using System.Collections.Generic;

namespace Cxxi
{
    public struct TemplateParameter
    {
        public string Name;
    }

    public abstract class Template : Declaration
    {
        protected Template(Declaration decl)
        {
            TemplatedDecl = decl;
        }

        public Declaration TemplatedDecl;

        public List<TemplateParameter> Parameters;
    }

    public class ClassTemplate : Template
    {
        public ClassTemplate(Declaration decl)
            : base(decl)
        {
        }

        public Class TemplatedClass
        {
          get { return TemplatedDecl as Class; }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassTemplateDecl(this);
        }
    }

    public class ClassTemplateSpecialization : Class
    {
        public  ClassTemplate TemplatedDecl;
    }

    public class ClassTemplatePartialSpecialization : ClassTemplateSpecialization
    {
    }

    public class FunctionTemplate : Template
    {
        public FunctionTemplate(Declaration decl)
            : base(decl)
        {
        }

        public Function TemplatedFunction
        {
            get { return TemplatedDecl as Function; }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionTemplateDecl(this);
        }
    }
}
