using System;
using System.Collections.Generic;

namespace Cxxi
{
    /// <summary>
    /// Represents a C++ property.
    /// </summary>
    public class Property
    {
        public Property(string name, Declaration type)
        {
            Name = name;
            Type = type;
        }

        public string Name
        {
            get;
            set;
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
    }
}