using CppSharp.Generators.C;
using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public abstract class RegistrableGeneratorOptions
    {
        public delegate string Delegate(string name);

        protected Generator generator;
        public virtual ISet<CInclude> CommonIncludes { get; }
        public virtual string OutputSubDir { get; }
        public Delegate BindingIdNamePredicate { get; }
        public Delegate BindingIdValuePredicate { get; }
        public Delegate BindingNamePredicate { get; }

        public RegistrableGeneratorOptions()
        {
            CommonIncludes = new HashSet<CInclude>();
            OutputSubDir = null;
            BindingIdNamePredicate = DefaultBindingIdNamePredicate();
            BindingIdValuePredicate = DefaultBindingIdValuePredicate();
            BindingNamePredicate = DefaultBindingNamePredicate();
        }

        public virtual Delegate DefaultBindingIdNamePredicate()
        {
            return (string name) =>
            {
                return $"_cppbind_id_{name}";
            };
        }

        public virtual Delegate DefaultBindingIdValuePredicate()
        {
            return (string name) =>
            {
                return $"typeid({name}).name()";
            };
        }

        public virtual Delegate DefaultBindingNamePredicate()
        {
            return (string name) =>
            {
                return $"_cppbind_{name}";
            };
        }
    }
}
