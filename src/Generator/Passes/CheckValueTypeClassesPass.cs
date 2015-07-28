using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class CheckValueTypeClassesPass : TranslationUnitPass
    {
        public CheckValueTypeClassesPass()
        {
        }

        public override bool VisitClassDecl(Class @class)
        {
            @class.Type = CheckClassIsStructible(@class, Driver) ? ClassType.ValueType : @class.Type;
            return base.VisitClassDecl(@class);
        }

        private bool CheckClassIsStructible(Class @class, Driver Driver)
        {
            if (@class.IsUnion || @class.Namespace.Templates.Any(tmp => tmp.Name.Equals(@class.Name)))
                return false;
            if (@class.IsInterface || @class.IsStatic || @class.IsAbstract)
                return false;
            if (@class.Declarations.Any(decl => decl.Access == AccessSpecifier.Protected))
                return false;
            if (@class.IsDynamic)
                return false;
            if (@class.HasBaseClass && @class.BaseClass.IsRefType)
                return false;

            var allTrUnits = Driver.ASTContext.TranslationUnits;
            if (allTrUnits.Any(trUnit => trUnit.Classes.Any(
                                  cls => cls.Bases.Any(clss => clss.IsClass && clss.Class == @class))))
                return false;

            return @class.IsPOD;
        }
    }
}