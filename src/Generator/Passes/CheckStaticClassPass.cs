using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for classes that should be bound as static classes.
    /// </summary>
    public class CheckStaticClassPass : TranslationUnitPass
    {
        public CheckStaticClassPass()
            => VisitOptions.ResetFlags(VisitFlags.ClassMethods);

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            if (Options.IsCSharpGenerator)
            {
                // C# cannot have protected members in static classes.
                if (IsProtectedClassMember(decl) && decl.IsGenerated)
                    SetDeclarationAccessToPrivate(decl);
            }

            return true;
        }

        static bool IsProtectedClassMember(Declaration decl)
        {
            if (decl.Access != AccessSpecifier.Protected)
                return false;

            var @class = decl.Namespace as Class;
            return @class != null && @class.IsStatic;
        }

        static void SetDeclarationAccessToPrivate(Declaration decl)
        {
            // By setting it to private it will appear
            // as an internal in the final C# wrapper.
            decl.Access = AccessSpecifier.Private;

            // We need to explicity set the generation else the
            // now private declaration will get filtered out later.
            decl.GenerationKind = GenerationKind.Generate;
        }

        static bool ReturnsClassInstance(Function function)
        {
            var returnType = function.ReturnType.Type.Desugar();

            var tag = returnType as TagType;
            if (tag == null)
                returnType.IsPointerTo(out tag);

            if (tag == null)
                return false;

            var @class = (Class)function.Namespace;
            var decl = tag.Declaration;

            if (!(decl is Class))
                return false;

            return @class.QualifiedOriginalName == decl.QualifiedOriginalName;
        }

        public override bool VisitClassDecl(Class @class)
        {
            // If the class is to be used as an opaque type, then it cannot be
            // bound as static.
            if (@class.IsOpaque)
                return false;

            if (@class.IsDependent)
                return false;

            // Polymorphic classes are currently not supported.
            // TODO: We could support this if the base class is also static, since it's composition then.
            if (@class.IsPolymorphic)
                return false;

            // Make sure we have at least one accessible static method or field
            if (!@class.Methods.Any(m => m.Kind == CXXMethodKind.Normal && m.Access != AccessSpecifier.Private && m.IsStatic)
                && @class.Variables.All(v => v.Access == AccessSpecifier.Private))
                return false;

            // Check for any non-static fields or methods, in which case we
            // assume the class is not meant to be static.
            // Note: Static fields are represented as variables in the AST.
            if (@class.Fields.Count != 0)
                return false;

            if (@class.Methods.Any(m => m.Kind == CXXMethodKind.Normal && !m.IsStatic))
                return false;

            if (@class.Constructors.Any(m =>
                {
                    // Implicit constructors are not user-defined, so assume this was unintentional.
                    if (m.IsImplicit)
                        return false;

                    // Ignore deleted constructors.
                    if (m.IsDeleted)
                        return false;

                    // If the class has a copy or move constructor, it cannot be static.
                    if (m.IsCopyConstructor || m.IsMoveConstructor)
                        return true;

                    // If the class has any (user defined) non-private constructors then it cannot be static
                    return m.Access != AccessSpecifier.Private;
                }))
            {
                return false;
            }

            // Check for any static function that return a pointer to the class.
            // If one exists, we assume it's a factory function and the class is
            // not meant to be static. It's a simple heuristic, but it should be
            // good enough for the time being.
            if (@class.Functions.Any(m => !m.IsOperator && ReturnsClassInstance(m)) ||
                @class.Methods.Any(m => !m.IsOperator && ReturnsClassInstance(m)))
                return false;

            // TODO: We should take C++ friends into account here, they might allow
            // a class to be instantiated even it if's not possible to instantiate
            // it using just its regular members.

            // If all the above constraints hold, then we assume the class can be
            // static.
            @class.IsStatic = true;

            // Ignore the special methods for static classes.
            foreach (var ctor in @class.Constructors)
                ctor.GenerationKind = GenerationKind.Internal;

            foreach (var dtor in @class.Destructors)
                dtor.GenerationKind = GenerationKind.Internal;

            return base.VisitClassDecl(@class);
        }
    }
}
