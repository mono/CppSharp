using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.Generators.Registrable
{
    public class InfoEntry : IEquatable<InfoEntry>
    {
        public static readonly HashSet<InfoEntry> Registered = new();

        public string Name { get; }

        public InfoEntry(string name)
        {
            if (Registered.Any(kind => kind.Name == name))
            {
                throw new Exception($"InfoEntry has an already registered name: {Name}");
            }
            Name = name;
            Registered.Add(this);
        }

        public static bool operator ==(InfoEntry obj1, InfoEntry obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }
            if (obj1 is null)
            {
                return false;
            }
            if (obj2 is null)
            {
                return false;
            }
            return obj1.Equals(obj2);
        }

        public static bool operator !=(InfoEntry obj1, InfoEntry obj2) => !(obj1 == obj2);

        public bool Equals(InfoEntry other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return Name.Equals(other.Name);
        }

        public override bool Equals(object obj) => Equals(obj as InfoEntry);

        public override int GetHashCode()
        {
            unchecked
            {
                return Name.GetHashCode();
            }
        }
    }
}
