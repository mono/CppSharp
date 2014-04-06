using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

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
                if (Type.IsTagDecl(out @class))
                    return @class;

                var type = Type.Desugar() as TemplateSpecializationType;
                if (type == null)
                {
                    TypedefType typedef;
                    if (Type.IsPointerTo(out typedef))
                    {
                        type = (TemplateSpecializationType) typedef.Desugar();
                    }
                    else
                    {
                        Type.IsPointerTo(out type);
                    }
                }
                var templatedClass = ((ClassTemplate) type.Template).TemplatedClass;
                return templatedClass.CompleteDeclaration == null ?
                    templatedClass : (Class) templatedClass.CompleteDeclaration;
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
    }

    public enum ClassType
    {
        ValueType,
        RefType,
        Interface
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

        // True if the class has a non trivial default constructor.
        public bool HasNonTrivialDefaultConstructor;

        // True if the class has a non trivial copy constructor.
        public bool HasNonTrivialCopyConstructor;

        // True if the class has a non trivial destructor.
        public bool HasNonTrivialDestructor;

        // True if the class represents a static class.
        public bool IsStatic;

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
                    if (@base.IsClass && !@base.Class.ExplicityIgnored)
                        return @base.Class;
                }

                return null;
            }
        }

        public Class OriginalClass { get; set; }

        public bool IsValueType
        {
            get { return Type == ClassType.ValueType || IsUnion; }
        }

        public bool IsRefType
        {
            get { return Type == ClassType.RefType && !IsUnion; }
        }

        public bool IsInterface
        {
            get { return Type == ClassType.Interface; }
        }

        public IEnumerable<Method> Constructors
        {
            get
            {
                return Methods.Where(
                    method => method.IsConstructor || method.IsCopyConstructor);
            }
        }

        public IEnumerable<Method> Destructors
        {
            get
            {
                return Methods.Where(method => method.IsDestructor);
            }
        }

        public IEnumerable<Method> Operators
        {
            get
            {
                return Methods.Where(method => method.IsOperator);
            }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassDecl(this);
        }
    }
}