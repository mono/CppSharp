using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp
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
        public AccessSpecifier Access { get; set; }
        public bool IsVirtual { get; set; }
        public Type Type { get; set; }

        public Class Class
        {
            get
            {
                Class @class;
                if (!Type.IsTagDecl(out @class))
                    throw new NotSupportedException();

                return @class;
            }
        }

        public bool IsClass
        {
            get
            {
                Class @class;
                return Type.IsTagDecl(out @class);
            }
        }

        public BaseClassSpecifier()
        {
            
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
        public VFTable VirtualFunctions { get; set; }
        public VBTable VirtualBases { get; set; }

        public bool HasOwnVFTable;
        public bool HasVirtualBases;

        public int Alignment;
        public int Size;
        public int DataSize;
    }

    public enum ClassType
    {
        ValueType,
        RefType,
    }

    // Represents a C++ record Decl.
    public class Class : DeclarationContext
    {
        public List<BaseClassSpecifier> Bases;
        public List<Field> Fields;
        public List<Property> Properties;
        public List<Method> Methods;

        // True if the record is a POD (Plain Old Data) type.
        public bool IsPOD;

        // Semantic type of the class.
        public ClassType Type;

        // ABI-specific class layout.
        public ClassLayout Layout;

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
            IsAbstract = false;
            IsUnion = false;
            IsOpaque = false;
            IsPOD = false;
            Type = ClassType.RefType;
            Layout = new ClassLayout();
        }

        public bool HasBase
        {
            get { return Bases.Count > 0; }
        }

        public bool HasBaseClass
        {
            get { return BaseClass != null; }
        }

        public Class BaseClass
        {
            get
            {
                foreach (var @base in Bases)
                {
                    if (@base.IsClass && !@base.Class.Ignore)
                        return @base.Class;
                }

                return null;
            }
        }

        public bool IsValueType
        {
            get { return Type == ClassType.ValueType; }
        }

        public bool IsRefType
        {
            get { return Type == ClassType.RefType; }
        }

        public IEnumerable<Method> Constructors
        {
            get
            {
                return Methods.Where(
                    method => method.IsConstructor || method.IsCopyConstructor);
            }
        }

        public IEnumerable<Method> Operators
        {
            get
            {
                return Methods.Where(method => method.IsOperator);
            }
        }

        public IEnumerable<Method> FindOperator(CXXOperatorKind kind)
        {
            return Operators.Where(method => method.OperatorKind == kind);
        }

        public IEnumerable<Method> FindMethodByOriginalName(string name)
        {
            return Methods.Where(method => method.OriginalName == name);
        }

        public IEnumerable<Variable> FindVariableByOriginalName(string originalName)
        {
            return Variables.Where(v => v.OriginalName == originalName);
        }

        public override IEnumerable<Function> GetFunctionOverloads(Function function)
        {
            return Methods.Where(method => method.Name == function.Name)
                .Cast<Function>();
        }

        public IEnumerable<T> FindHierarchy<T>(Func<Class, IEnumerable<T>> func)
            where T : Declaration
        {
            foreach (var @base in Bases)
            {
                if (!@base.IsClass) continue;
                foreach(var elem in @base.Class.FindHierarchy<T>(func))
                    yield return elem;
            }

            foreach (var elem in func(this))
                yield return elem;
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassDecl(this);
        }
    }
}