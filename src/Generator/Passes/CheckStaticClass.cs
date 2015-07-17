using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for classes that should be bound as static classes.
    /// </summary>
    public class CheckStaticClass : TranslationUnitPass
    {
        public CheckStaticClass()
        {
            Options.VisitClassBases = false;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            if (Driver.Options.IsCSharpGenerator)
            {
                // C# cannot have protected members in static classes.
                var @class = decl.Namespace as Class;
                if (    decl.Access == AccessSpecifier.Protected && 
                        decl.GenerationKind == GenerationKind.Generate &&
                        @class != null && 
                        @class.IsStatic)
                {
                    // By setting it to private it will appear
                    // as an internal in the final C# wrapper.
                    decl.Access = AccessSpecifier.Private;

                    // We need to explicity set the generation else the
                    // now private declaration will get filtered out later.
                    decl.GenerationKind = GenerationKind.Generate;
                }
            }

            return true;
        }

        static bool ReturnsClassInstance(Function function)
        {
            var returnType = function.ReturnType.Type.Desugar();

            var tag = returnType as TagType;
            if (tag == null)
                returnType.IsPointerTo(out tag);

            if (tag == null)
                return false;

            var @class = (Class) function.Namespace;
            var decl = tag.Declaration;

            if (!(decl is Class))
                return false;

            return @class.QualifiedOriginalName == decl.QualifiedOriginalName;
        }

        public override bool VisitClassDecl(Class @class)
        {
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
            if (@class.Functions.Any(ReturnsClassInstance) ||
                @class.Methods.Any(ReturnsClassInstance))
                return false;

            // If the class is to be used as an opaque type, then it cannot be
            // bound as static.
            if (@class.IsOpaque)
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
