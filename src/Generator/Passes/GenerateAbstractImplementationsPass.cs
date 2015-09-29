using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass generates internal classes that implement abstract classes.
    /// When the return type of a function is abstract, these internal
    /// classes provide since the real type cannot be resolved while binding
    /// an allocatable class that supports proper polymorphism.
    /// </summary>
    public class GenerateAbstractImplementationsPass : TranslationUnitPass
    {
        /// <summary>
        /// Collects all internal implementations in a unit to be added at
        /// the end because the unit cannot be changed while it's being
        /// iterated though.
        /// </summary>
        private readonly List<Class> internalImpls = new List<Class>();

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            var result = base.VisitTranslationUnit(unit);
            foreach (var internalImpl in internalImpls)
                if (internalImpl.Namespace != null)
                    internalImpl.Namespace.Classes.Add(internalImpl);
                else
                    unit.Classes.AddRange(internalImpls);

            internalImpls.Clear();
            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || @class.Ignore)
                return false;

            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            if (@class.IsAbstract)
            {
                foreach (var ctor in from ctor in @class.Constructors
                                     where ctor.Access == AccessSpecifier.Public
                                     select ctor)
                    ctor.Access = AccessSpecifier.Protected;
                internalImpls.Add(AddInternalImplementation(@class));
            }

            return @class.IsAbstract;
        }

        private static Class AddInternalImplementation(Class @class)
        {
            var internalImpl = GetInternalImpl(@class);

            var abstractMethods = GetRelevantAbstractMethods(@class);

            internalImpl.Methods.AddRange(
                from method in abstractMethods
                select new Method(method)
                {
                    Namespace = internalImpl,
                    OriginalFunction = method,
                    IsPure = false,
                    IsOverride = true,
                    SynthKind = FunctionSynthKind.AbstractImplCall
                });

            internalImpl.Layout = @class.Layout;

            return internalImpl;
        }

        private static Class GetInternalImpl(Declaration @class)
        {
            var internalImpl = new Class
                                {
                                    Name = @class.Name + "Internal",
                                    Access = AccessSpecifier.Private,
                                    Namespace = @class.Namespace
                                };

            var @base = new BaseClassSpecifier { Type = new TagType(@class) };
            internalImpl.Bases.Add(@base);

            return internalImpl;
        }

        private static IEnumerable<Method> GetRelevantAbstractMethods(Class @class)
        {
            var abstractMethods = GetAbstractMethods(@class);
            var overriddenMethods = GetOverriddenMethods(@class);
            var paramTypeCmp = new ParameterTypeComparer();

            for (var i = abstractMethods.Count - 1; i >= 0; i--)
            {
                var @abstract = abstractMethods[i];
                var @override = overriddenMethods.Find(m => m.Name == @abstract.Name && 
                    m.ReturnType == @abstract.ReturnType && 
                    m.Parameters.SequenceEqual(@abstract.Parameters, paramTypeCmp));
                if (@override != null)
                {
                    if (@abstract.IsOverride)
                    {
                        var abstractMethod = abstractMethods[i];
                        bool found;
                        var rootBaseMethod = abstractMethod;
                        do
                        {
                            rootBaseMethod = @class.GetBaseMethod(rootBaseMethod, false, true);
                            if (found = (rootBaseMethod == @override))
                                break;
                        } while (rootBaseMethod != null);
                        if (!found)
                            abstractMethods.RemoveAt(i);
                    }
                    else
                    {
                        abstractMethods.RemoveAt(i);                        
                    }
                }
            }

            return abstractMethods;
        }

        private static List<Method> GetAbstractMethods(Class @class)
        {
            var abstractMethods = @class.Methods.Where(m => m.IsPure).ToList();
            foreach (var @base in @class.Bases)
                abstractMethods.AddRange(GetAbstractMethods(@base.Class));

            return abstractMethods;
        }

        private static List<Method> GetOverriddenMethods(Class @class)
        {
            var overriddenMethods = @class.Methods.Where(m => m.IsOverride && !m.IsPure).ToList();
            foreach (var @base in @class.Bases)
                overriddenMethods.AddRange(GetOverriddenMethods(@base.Class));

            return overriddenMethods;
        }
    }
}
