using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MarshalStructAsValTypeIfPossiblePass : TranslationUnitPass
    {
        
        public override bool VisitClassDecl(Class @class)
        {
            @class.Type = CheckClassIsStructible(@class, Driver) ? ClassType.ValueType : @class.Type;
            return base.VisitClassDecl(@class);
        }

        private bool CheckClassIsStructible(Class @class, Driver Driver)
        {
            if (@class.IsValueType)
                return true;

            if (!@class.IsStruct)
                return false;
            if (@class.IsInterface || @class.IsStatic || @class.IsAbstract)
                return false;
            if (@class.Declarations.Any(decl => decl.Access == AccessSpecifier.Protected))
                return false;
            if (@class.Methods.Any(m => m.IsVirtual))
                return false;
            if (Driver.Options.IsCLIGenerator && @class.HasBaseClass && @class.BaseClass.IsRefType)
                return false;

            var allTrUnits = Driver.ASTContext.TranslationUnits;
            foreach (var trUnit in allTrUnits)
            {
                foreach (var cls in trUnit.Classes)
                {
                    if (cls.Bases.Any(clss => clss.IsClass && clss.Class == @class))
                        return false;
                }
            }

            return true;
        }
    }
}
