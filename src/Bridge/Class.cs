using System.Collections.Generic;

namespace Cxxi
{
    // A C++ access specifier.
    public enum AccessSpecifier
    {
        Private,
        Protected,
        Public
    }

    // Represents a base class of a C++ class.
    public class BaseClassSpecifier
    {
        public Class Class { get; set; }
        public AccessSpecifier Access { get; set; }
        public bool IsVirtual { get; set; }

        BaseClassSpecifier(Class @class, AccessSpecifier access,
            bool isVirtual = false)
        {
            Class = @class;
            Access = access;
            IsVirtual = isVirtual;
        }
    }

    // Represents a C++ virtual function table.
    public class VFTable
    {

    }

    // Represents a C++ virtual base table.
    public class VBTable
    {

    }

    // Represents ABI-specific layout details for a class.
    public class ClassLayout
    {
        public CppAbi ABI { get; set; }
        public bool HasOwnVFTable { get; set; }
        public VFTable VirtualFunctions { get; set; }
        public VBTable VirtualBases { get; set; }
    }

    public enum ClassType
    {
        ValueType,
        RefType,
    }

    // Represents a C++ record Decl.
    public class Class : Declaration
    {
        public List<BaseClassSpecifier> Bases;
        public List<Class> NestedClasses;
        public List<Enumeration> NestedEnums;
        public List<Field> Fields;
        public List<Property> Properties;
        public List<Method> Methods;

        // True if the record is a POD (Plain Old Data) type.
        public bool IsPOD;

        // Semantic type of the class.
        public ClassType Type;

        // ABI-specific class layout.
        public List<ClassLayout> Layouts { get; set; }

        // True if class provides pure virtual methods.
        public bool IsAbstract;

        // True if the type is to be treated as a union.
        public bool IsUnion;

        // True if the type is to be treated as opaque.
        public bool IsOpaque;

        public Class()
        {
            Bases = new List<BaseClassSpecifier>();
            Fields = new List<Field>();
            Properties = new List<Property>();
            Methods = new List<Method>();
            NestedClasses = new List<Class>();
            NestedEnums = new List<Enumeration>();
            IsAbstract = false;
            IsUnion = false;
            IsOpaque = false;
            IsPOD = false;
            Type = ClassType.RefType;
        }

        public bool HasBase
        {
            get { return Bases.Count > 0; }
        }

        public bool IsValueType
        {
            get { return Type == ClassType.ValueType; }
        }

        public bool IsRefType
        {
            get { return Type == ClassType.RefType; }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassDecl(this);
        }
    }
}