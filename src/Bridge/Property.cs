using System;
using System.Collections.Generic;

namespace CppSharp
{
    /// <summary>
    /// Represents a C++ property.
    /// </summary>
    public class Property : Declaration
    {
        public Property(string name, Declaration type)
        {
            Name = name;
            Type = type;
        }

        public Declaration Type
        {
            get;
            set;
        }

        public Method GetMethod
        {
            get;
            set;
        }

        public Method SetMethod
        {
            get;
            set;
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            throw new NotImplementedException();
        }
    }
}