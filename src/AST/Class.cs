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

    public enum TagKind
    {
        Struct,
        Interface,
        Union,
        Class,
        Enum
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
    public class BaseClassSpecifier : DeclarationBase
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
                Type.TryGetClass(out var @class);
                return @class;
            }
        }

        public bool IsClass => Type.IsClass();
    }

    public enum ClassType
    {
        ValueType,
        RefType,
        Interface
    }

    // Represents a C++ record decl.
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

        public TagKind TagKind { get; set; }

        // True if the class is final / sealed.
        public bool IsFinal { get; set; }

        private bool? isOpaque;

        public bool IsInjected { get; set; }

        // True if the type is to be treated as opaque.
        public bool IsOpaque
        {
            get => isOpaque ?? (IsIncomplete && CompleteDeclaration == null);
            set => isOpaque = value;
        }

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
            IsFinal = false;
            IsPOD = false;
            Type = ClassType.RefType;
            Layout = new ClassLayout();
            templateParameters = new List<Declaration>();
            specializations = new List<ClassTemplateSpecialization>();
        }

        public bool HasBase => Bases.Count > 0;

        public bool HasBaseClass => BaseClass != null;

        public Class BaseClass
        {
            get
            {
                foreach (var @base in Bases)
                {
                    if (@base.IsClass && @base.Class.IsGenerated)
                        return @base.Class;
                }

                return null;
            }
        }

        public bool HasNonIgnoredBase =>
            HasBaseClass && !IsValueType
                         && BaseClass != null
                         && !BaseClass.IsValueType
                         && BaseClass.IsGenerated;

        public bool NeedsBase => HasNonIgnoredBase && IsGenerated;

        // When we have an interface, this is the class mapped to that interface.
        public Class OriginalClass { get; set; }

        public bool IsValueType => Type == ClassType.ValueType || IsUnion;

        public bool IsRefType => Type == ClassType.RefType && !IsUnion;

        public bool IsInterface => Type == ClassType.Interface;

        public bool HasUnionFields { get; set; }

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

        /// <summary>
        /// If this class is a template, this list contains all of its template parameters.
        /// <para>
        /// <see cref="ClassTemplate"/> cannot be relied upon to contain all of them because
        /// ClassTemplateDecl in Clang is not a complete declaration, it only serves to forward template classes.
        /// </para>
        /// </summary>
        public List<Declaration> TemplateParameters
        {
            get
            {
                if (!IsDependent)
                    throw new InvalidOperationException(
                        "Only dependent classes have template parameters.");
                return templateParameters;
            }
        }

        /// <summary>
        /// If this class is a template, this list contains all of its specializations.
        /// <see cref="ClassTemplate"/> cannot be relied upon to contain all of them because
        /// ClassTemplateDecl in Clang is not a complete declaration, it only serves to forward template classes.
        /// </summary>
        public List<ClassTemplateSpecialization> Specializations
        {
            get
            {
                if (!IsDependent)
                    throw new InvalidOperationException(
                        "Only dependent classes have specializations.");
                return specializations;
            }
        }

        public bool IsTemplate => IsDependent && TemplateParameters.Count > 0;

        public override IEnumerable<Function> FindOperator(CXXOperatorKind kind)
        {
            return Methods.Where(m => m.OperatorKind == kind);
        }

        public override IEnumerable<Function> GetOverloads(Function function)
        {
            if (function.IsOperator)
                return Methods.Where(fn => fn.OperatorKind == function.OperatorKind);

            var overloads = Methods.Where(m => m.Name == function.Name)
                .Union(Declarations.Where(d => d is Function && d.Name == function.Name))
                .Cast<Function>();

            overloads = overloads.Union(base.GetOverloads(function));

            return overloads;
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

        public Variable FindVariable(string name)
        {
            return Variables.FirstOrDefault(m => m.Name == name);
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitClassDecl(this);
        }

        private List<Declaration> templateParameters;
        private List<ClassTemplateSpecialization> specializations;
    }
}
