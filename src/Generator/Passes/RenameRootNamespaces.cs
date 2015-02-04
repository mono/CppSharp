using CppSharp.AST;
using System;
using System.Collections.Generic;

namespace CppSharp.Passes
{
    public class RenameRootNamespacesPass : TranslationUnitPass
    {

        public override bool VisitTranslationUnit(TranslationUnit tunit)
        {
            if (base.VisitTranslationUnit(tunit))
            {
                var fname = tunit.TranslationUnit.FileName;
                if (!Driver.DependencyNamespaces.ContainsKey(fname) && tunit.GenerationKind == GenerationKind.Generate)
                {
                    Driver.DependencyNamespaces.Add(fname, new Driver.TransUnitInfo
                    {
                        transUnit = fname,
                        rootNamespaceName = Driver.Options.OutputNamespace,
                    });
                }
                else
                {
                    tunit.Name = Driver.DependencyNamespaces[fname].rootNamespaceName;
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