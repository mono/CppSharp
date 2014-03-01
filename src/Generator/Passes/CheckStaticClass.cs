using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for classes that should be bound as static classes.
    /// </summary>
    public class CheckStaticClass : TranslationUnitPass
    {
        static bool ReturnsClassInstance(Function function)
        {
            var returnType = function.ReturnType.Type.Desugar();

            TagType tag;
            if (!returnType.IsPointerTo(out tag))
                return false;

            var @class = (Class) function.Namespace;
            var decl = tag.Declaration;

            if (decl is Class)
                return false;

            return @class.QualifiedOriginalName == decl.QualifiedOriginalName;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            // If the class has any non-private constructors then it cannot
            // be bound as a static class and we bail out early.
            if (@class.Constructors.Any(m =>
                !(m.IsCopyConstructor || m.IsMoveConstructor)
                && m.Access != AccessSpecifier.Private))
                return false;

            // Check for any non-static fields or methods, in which case we
            // assume the class is not meant to be static.
            // Note: Static fields are represented as variables in the AST.
            if (@class.Fields.Any() ||
                @class.Methods.Any(m => m.Kind == CXXMethodKind.Normal
                && !m.IsStatic))
                return false;

            // Check for any static function that return a pointer to the class.
            // If one exists, we assume it's a factory function and the class is
            // not meant to be static. It's a simple heuristic but it should be
            // good enough for the time being.
            if (@class.Functions.Any(ReturnsClassInstance))
                return false;

            // TODO: We should take C++ friends into account here, they might allow
            // a class to be instantiated even it if's not possible to instantiate
            // it using just its regular members.

            // If all the above constraints hold, then we assume the class can be
            // static.
            @class.IsStatic = true;

            // Ignore the special methods for static classes.
            foreach (var ctor in @class.Constructors)
                ctor.IsGenerated = false;

            foreach (var dtor in @class.Destructors)
                dtor.IsGenerated = false;

            return true;
        }
    }
}
