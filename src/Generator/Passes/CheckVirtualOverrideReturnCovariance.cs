using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks if a pair of types are covariant according to the C++ standard.
    /// Excerpt from 10.3.7 (Virtual Functions):
    /// 
    /// "The return type of an overriding function shall be either identical
    /// to the return type of the overridden function or covariant with the
    /// classes of the functions. If a function D::f overrides a function
    /// B::f, the return types of the functions are covariant if they satisfy
    /// the following criteria:
    /// 
    ///  - both are pointers to classes, both are lvalue references to classes,
    ///    or both are rvalue references to classes
    /// 
    ///  - the class in the return type of B::f is the same class as the class
    ///    in the return type of D::f, or is an unambiguous and accessible
    ///    direct or indirect base class of the class in the return type of D::f
    /// 
    ///  -  both pointers or references have the same cv-qualification and the
    ///     class type in the return type of D::f has the same cv-qualification
    ///     as or less cv-qualification than the class type in the return type of
    ///     B::f."
    /// </summary>
    public class CovariantTypeComparer : TranslationUnitPass
    {
        public QualifiedType currentType;
        public Declaration currentDecl;

        public CovariantTypeComparer(QualifiedType type)
        {
            currentType = type;
        }

        public override bool VisitPointerType(PointerType type,
            TypeQualifiers quals)
        {
            if (!currentType.Qualifiers.Equals(quals))
                return false;

            var currentPointer = currentType.Type as PointerType;
            if (currentPointer == null)
                return false;

            currentType = currentPointer.QualifiedPointee;
            var pointee = type.QualifiedPointee;

            if (!currentType.Qualifiers.Equals(pointee.Qualifiers))
                return false;

            // Strip any typedefs that might be sugaring the type.
            currentType = new QualifiedType(currentType.Type.Desugar());
            var stripPointee = pointee.Type.Desugar();

            return stripPointee.Visit(this);
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (!currentType.Qualifiers.Equals(quals))
                return false;

            var tagType = currentType.Type as TagType;
            if (tagType == null)
                return false;

            currentDecl = tagType.Declaration;
            return tag.Declaration.Visit(this);
        }

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template,
            TypeQualifiers quals)
        {
            if (!currentType.Qualifiers.Equals(quals))
                return false;

            var currentTemplateType = currentType.Type as TemplateSpecializationType;
            if (currentTemplateType == null)
                return false;

            return currentTemplateType.Equals(template);
        }

        static bool IsDescendentOf(Class @class, Class parent)
        {
            return @class == parent ||
                (@class.HasBaseClass && IsDescendentOf(@class.BaseClass, parent));
        }

        public override bool VisitClassDecl(Class @class)
        {
            var currentClass = currentDecl as Class;
            return IsDescendentOf(currentClass, @class);
        }
    }

    /// <summary>
    /// This pass checks covariance in virtual override overloads.
    /// 
    /// struct A { virtual A* foo() = 0; };
    /// struct B : public A { virtual B* foo() override; };
    /// 
    /// The overriden method in B uses a subtype of A, which is not allowed in
    /// C#, so we need to fix it to use the same type as the overriden method.
    /// </summary>
    public class CheckVirtualOverrideReturnCovariance : TranslationUnitPass
    {
        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            if (!method.IsOverride)
                return false;

            var overridenMethod = GetOverridenBaseMethod(method);
            if (overridenMethod == null)
                return false;

            // Fix-up any types that are not equal to the overriden base.
            if (!method.ReturnType.Equals(overridenMethod.ReturnType))
            {
                method.ReturnType = overridenMethod.ReturnType;

                Diagnostics.Debug(
                    "{0} return type is co-variant with overriden base",
                    method.QualifiedOriginalName);
            }

            return false;
        }

        public Method GetOverridenBaseMethod(Method method)
        {
            if (!method.IsOverride)
                return null;

            var @class = (Class)method.Namespace;
            if (!@class.HasBaseClass)
                return null;

            var baseClass = @class.BaseClass;
            var baseMethod = baseClass.Methods.Find(m => IsCompatibleOverload(method, m));

            return baseMethod;
        }

        public bool IsCompatibleOverload(Method m1, Method m2)
        {
            if (m1.Name != m2.Name)
                return false;

            if (m1.IsConst != m2.IsConst)
                return false;

            if (m1.Parameters.Count != m2.Parameters.Count)
                return false;

            for (var i = 0; i < m1.Parameters.Count; ++i)
            {
                var m1Param = m1.Parameters[i];
                var m2Param = m2.Parameters[i];

                if (!m1Param.QualifiedType.Equals(m2Param.QualifiedType))
                    return false;
            }

            return m1.ReturnType.Equals(m2.ReturnType)
                || IsCovariantType(m1.ReturnType, m2.ReturnType);
        }

        public static bool IsCovariantType(QualifiedType t1, QualifiedType t2)
        {
            var comparer = new CovariantTypeComparer(t1);
            return t2.Visit(comparer);
        }
    }
}
