using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Generators
{
    /// <summary>
    /// Kinds of language generators.
    /// </summary>
    public class GeneratorKind : IEquatable<GeneratorKind>
    {
        private static HashSet<string> s_registeredIDSet = new();

        public string ID { get; }

        public GeneratorKind(string id)
        {
            if (s_registeredIDSet.Contains(id))
            {
                throw new Exception($"GeneratorKind has an already registered ID: {ID}");
            }
            ID = id;
            s_registeredIDSet.Add(id);
        }

        public static bool operator ==(GeneratorKind obj1, GeneratorKind obj2)
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

        public static bool operator !=(GeneratorKind obj1, GeneratorKind obj2) => !(obj1 == obj2);

        public bool Equals(GeneratorKind? other)
        {
            if (other is null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return ID.Equals(other.ID);
        }

        public override bool Equals(object? obj) => Equals(obj as GeneratorKind);

        public override int GetHashCode()
        {
            unchecked
            {
                return ID.GetHashCode();
            }
        }

        public const string CLI_ID = "CLI";
        public static readonly GeneratorKind CLI = new(CLI_ID);

        public const string CSharp_ID = "CSharp";
        public static readonly GeneratorKind CSharp = new(CSharp_ID);

        public const string C_ID = "C";
        public static readonly GeneratorKind C = new(C_ID);

        public const string CPlusPlus_ID = "CPlusPlus";
        public static readonly GeneratorKind CPlusPlus = new(CPlusPlus_ID);

        public const string Emscripten_ID = "Emscripten";
        public static readonly GeneratorKind Emscripten = new(Emscripten_ID);

        public const string ObjectiveC_ID = "ObjectiveC";
        public static readonly GeneratorKind ObjectiveC = new(ObjectiveC_ID);

        public const string Java_ID = "Java";
        public static readonly GeneratorKind Java = new(Java_ID);

        public const string Swift_ID = "Swift";
        public static readonly GeneratorKind Swift = new(Swift_ID);

        public const string QuickJS_ID = "QuickJS";
        public static readonly GeneratorKind QuickJS = new(QuickJS_ID);

        public const string NAPI_ID = "NAPI";
        public static readonly GeneratorKind NAPI = new(NAPI_ID);

        public const string TypeScript_ID = "TypeScript";
        public static readonly GeneratorKind TypeScript = new(TypeScript_ID);
    }
}