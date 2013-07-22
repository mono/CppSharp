using System.Collections.Generic;

namespace CppSharp.AST
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
            Parameters = new List<TemplateParameter>();
        }

        public Declaration TemplatedDecl;

        public List<TemplateParameter> Parameters;

        public override string ToString()
        {
            return TemplatedDecl.ToString();
        }
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

        public override string Name
        {
            get 
            { 
                if(TemplatedDecl != null) 
                    return TemplatedClass.Name;
               return base.Name;
            }
            set 
            { 
                if(TemplatedDecl != null) 
                    TemplatedClass.Name = value;
                else
                    base.Name = value;
            }
        }

        public override string OriginalName
        {
            get 
            { 
                if(TemplatedDecl != null) 
                    return TemplatedClass.OriginalName;
               return base.OriginalName;
            }
            set 
            { 
                if(TemplatedDecl != null) 
                    TemplatedClass.OriginalName = value;
                else
                    base.OriginalName = value;
            }
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
