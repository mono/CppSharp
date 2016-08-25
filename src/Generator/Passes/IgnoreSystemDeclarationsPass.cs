using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class IgnoreSystemDeclarationsPass : TranslationUnitPass
    {
        public IgnoreSystemDeclarationsPass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassMethods = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!unit.IsValid)
                return false;

            if (ClearVisitedDeclarations)
                Visited.Clear();

            VisitDeclarationContext(unit);

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || Options.IsCLIGenerator)
                return false;

            if (!@class.TranslationUnit.IsSystemHeader)
                return false;

            if (!@class.IsExplicitlyGenerated)
                @class.ExplicitlyIgnore();

            if (!@class.IsDependent)
                return false;

            // we only need a few members for marshalling so strip the rest
            switch (@class.Name)
            {
                case "basic_string":
                    foreach (var specialization in @class.Specializations.Where(s => !s.Ignore))
                    {
                        foreach (var method in specialization.Methods.Where(m => m.OriginalName != "c_str"))
                            method.ExplicitlyIgnore();
                        var l = specialization.Methods.Where(m => m.IsConstructor && m.Parameters.Count == 2).ToList();
                        var ctor = specialization.Methods.Single(m => m.IsConstructor && m.Parameters.Count == 2 &&
                            m.Parameters[0].Type.Desugar().IsPointerToPrimitiveType(PrimitiveType.Char) &&
                            !m.Parameters[1].Type.Desugar().IsPrimitiveType());
                        ctor.GenerationKind = GenerationKind.Generate;
                        foreach (var parameter in ctor.Parameters)
                            parameter.DefaultArgument = null;
                    }
                    break;
                case "allocator":
                    foreach (var specialization in @class.Specializations.Where(s => !s.Ignore))
                        foreach (var method in specialization.Methods.Where(m => !m.IsConstructor || m.Parameters.Any()))
                            method.ExplicitlyIgnore();
                    break;
            }
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            if (function.TranslationUnit.IsSystemHeader)
                function.ExplicitlyIgnore();

            return true;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (!base.VisitTypedefDecl(typedef))
                return false;

            if (typedef.TranslationUnit.IsSystemHeader)
                typedef.ExplicitlyIgnore();

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!base.VisitDeclaration(variable))
                return false;

            if (variable.TranslationUnit.IsSystemHeader)
                variable.ExplicitlyIgnore();

            return true;
        }
    }
}
