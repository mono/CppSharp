using CppSharp.AST;
using CppSharp;
using System;
using System.Collections.Generic;

namespace CppSharp.Passes
{
    public class ExportDeclsPass : TranslationUnitPass
    {

        void AddToExports(Declaration decl)
        {
            if (Driver.Options.ImportNames.ContainsKey(decl.QualifiedLogicalOriginalName))
            {
                var importedName = Driver.Options.ImportNames[decl.QualifiedLogicalOriginalName];
            }
            try
            {
                var convertedQualName = Driver.Options.OutputNamespace;
                if (decl.QualifiedLogicalName != "")
                    convertedQualName += "::" + decl.QualifiedLogicalName;
                var info = new DeclInfo
                {
                    originalQualName = decl.QualifiedLogicalOriginalName,
                    convertedQualName = convertedQualName,
                    kind = decl.GetType().ToString(),
                    transUnit = decl.TranslationUnit.FileName,
                };
                Driver.Options.ExportNames.Add(decl.QualifiedLogicalOriginalName, info);
            }
            catch (ArgumentException)
            {
            }

        }

        override public bool VisitDeclarationContext(DeclarationContext context)
        {
            if (base.VisitDeclarationContext(context))
            {

                foreach (var @enum in context.Enums)
                {
                    if (!@enum.IsGenerated || @enum.IsIncomplete) continue;
                    AddToExports(@enum);
                }

                foreach (var typedef in context.Typedefs)
                    AddToExports(typedef);

                foreach (var @class in context.Classes)
                {
                    if (@class.IsIncomplete) continue;
                    AddToExports(@class);
                }

                foreach (var function in context.Functions)
                {
                    if (!function.IsGenerated && !function.IsInternal) continue;
                    AddToExports(function);
                }

                foreach (var @event in context.Events)
                {
                    if (!@event.IsGenerated) continue;
                    AddToExports(@event);
                }

                foreach (var childNamespace in context.Namespaces)
                {
                    AddToExports(childNamespace);
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