using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace CppSharp.Passes
{
    public class ImportDeclsPass : TranslationUnitPass
    {
        public override bool VisitDeclaration(Declaration decl)
        {
            if (base.VisitDeclaration(decl))
            {
                if (Driver.Options.ImportNames.ContainsKey(decl.Name))
                {
                    var import = Driver.Options.ImportNames[decl.Name];
                    if (import.kind == decl.GetType().ToString() && import.originalQualName == decl.QualifiedLogicalOriginalName)
                    {
                        var names = import.convertedQualName.Split(new string[] { "::" }, StringSplitOptions.None);
                        var ctx = decl;
                        while (ctx.Namespace != null)
                        {
                            ctx = ctx.Namespace;
                        }
                        ctx.Name = names[0]; // Change the name of outernmost namespace
                    }

                }

                return true;
            }
            else
            {
                return false;
            }
        }

    }
}