using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    // A C++ access specifier.
    public enum AccessSpecifier
    {
        Private,
        Protected,
        Public
    }

    // A C++ access specifier declaration.
    public class AccessSpecifierDecl : Declaration
    {
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            throw new NotImplementedException();
        }
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
        public List<AccessSpecifierDecl> Specifiers;

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

        // True if the class is dynamic.
        public bool IsDynamic;

        // True if the class is polymorphic.
        public bool IsPolymorphic;

        // True if the class has a non trivial copy constructor.
        public bool HasNonTrivialCopyConstructor;

        public Class()
        {
            Bases = new List<BaseClassSpecifier>();
            Fields = new List<Field>();
            Properties = new List<Property>();
            Methods = new List<Method>();
            Specifiers = new List<AccessSpecifierDecl>();
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
            get { return Type == ClassType.ValueType || IsUnion; }
        }

        public bool IsRefType
        {
            get { return Type == ClassType.RefType && !IsUnion; }
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
            foreach (var elem in func(this))
                yield return elem;

            foreach (var @base in Bases)
            {
                if (!@base.IsClass) continue;
                foreach(var elem in @base.Class.FindHierarchy<T>(func))
                    yield return elem;
            }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassDecl(this);
        }
    }
}