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
        Public,
        Internal
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
        public BaseClassSpecifier()
        {
        }

        public BaseClassSpecifier(BaseClassSpecifier other)
        {
            Access = other.Access;
            IsVirtual = other.IsVirtual;
            Type = other.Type;
            Offset = other.Offset;
        }

        public AccessSpecifier Access { get; set; }
        public bool IsVirtual { get; set; }
        public Type Type { get; set; }
        public int Offset { get; set; }

        public Class Class
        {
            get
            {
                Class @class;
                Type.TryGetClass(out @class);
                return @class;
            }
        }

        public bool IsClass
        {
            get
            {
                return Type.IsClass();
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

        public Class(Class @class)
            : base(@class)
        {
            Bases = new List<BaseClassSpecifier>(@class.Bases);
            Fields = new List<Field>(@class.Fields);
            Properties = new List<Property>(@class.Properties);
            Methods = new List<Method>(@class.Methods);
            Specifiers = new List<AccessSpecifierDecl>(@class.Specifiers);
            IsPOD = @class.IsPOD;
            Type = @class.Type;
            Layout = new ClassLayout(@class.Layout);
            IsAbstract = @class.IsAbstract;
            IsUnion = @class.IsUnion;
            IsOpaque = @class.IsOpaque;
            IsDynamic = @class.IsDynamic;
            IsPolymorphic = @class.IsPolymorphic;
            HasNonTrivialDefaultConstructor = @class.HasNonTrivialDefaultConstructor;
            HasNonTrivialCopyConstructor = @class.HasNonTrivialCopyConstructor;
            HasNonTrivialDestructor = @class.HasNonTrivialDestructor;
            IsStatic = @class.IsStatic;
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
                    if (@base.IsClass && @base.Class.IsDeclared)
                        return @base.Class;
                }

                return null;
            }
        }

        public bool HasNonIgnoredBase
        {
            get
            {
                return HasBaseClass && !IsValueType
                       && Bases[0].Class != null
                       && !Bases[0].Class.IsValueType
                       && Bases[0].Class.GenerationKind != GenerationKind.None;
            }
        }

        public bool NeedsBase
        {
            get { return HasNonIgnoredBase && IsGenerated; }
        }

        // When we have an interface, this is the class mapped to that interface.
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

        public bool IsAbstractImpl
        {
            get { return Methods.Any(m => m.SynthKind == FunctionSynthKind.AbstractImplCall); }
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

        public override IEnumerable<Function> FindOperator(CXXOperatorKind kind)
        {
            return Methods.Where(m => m.OperatorKind == kind);
        }

        public override IEnumerable<Function> GetOverloads(Function function)
        {
            if (function.IsOperator)
                return Methods.Where(fn => fn.OperatorKind == function.OperatorKind);

            var methods = Methods.Where(m => m.Name == function.Name).ToList();
            if (methods.Count != 0)
                return methods;

            return base.GetOverloads(function);
        }

        public Method FindMethod(string name)
        {
            return Methods
                .Concat(Templates.OfType<FunctionTemplate>()
                    .Select(t => t.TemplatedFunction)
                    .OfType<Method>())
                .FirstOrDefault(m => m.Name == name);
        }

        public Method FindMethodByUSR(string usr)
        {
            return Methods
                .Concat(Templates.OfType<FunctionTemplate>()
                    .Select(t => t.TemplatedFunction)
                    .OfType<Method>())
                .FirstOrDefault(m => m.USR == usr);
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassDecl(this);
        }
    }
}
