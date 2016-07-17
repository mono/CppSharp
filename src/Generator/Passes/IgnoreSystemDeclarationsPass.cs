using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class IgnoreSystemDeclarationsPass : TranslationUnitPass
    {
        public IgnoreSystemDeclarationsPass()
        {
            Options.VisitClassBases = false;
            Options.VisitClassFields = false;
            Options.VisitClassMethods = false;
            Options.VisitClassProperties = false;
            Options.VisitFunctionParameters = false;
            Options.VisitFunctionReturnType = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceEvents = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitNamespaceTypedefs = false;
            Options.VisitNamespaceVariables = false;
            Options.VisitTemplateArguments = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!unit.IsValid)
                return false;

            if (unit.IsSystemHeader)
                unit.ExplicitlyIgnore();

            if (ClearVisitedDeclarations)
                Visited.Clear();

            VisitDeclarationContext(unit);

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (decl.Namespace != null && decl.TranslationUnit.IsSystemHeader)
                decl.ExplicitlyIgnore();
            return base.VisitDeclaration(decl);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || !@class.IsDependent ||
                Driver.Options.IsCLIGenerator || !@class.IsSupportedStdType())
                return false;

            // we only need a few members for marshalling so strip the rest
            switch (@class.Name)
            {
                case "basic_string":
                    foreach (var specialization in @class.Specializations.Where(
                        s => s.IsSupportedStdSpecialization()))
                    {
                        MarkForGeneration(specialization);
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
                    foreach (var specialization in @class.Specializations.Where(
                        s => s.IsSupportedStdSpecialization()))
                    {
                        MarkForGeneration(specialization);
                        foreach (var method in specialization.Methods.Where(m => !m.IsConstructor || m.Parameters.Any()))
                            method.ExplicitlyIgnore();
                    }
                    break;
            }
            return true;
        }

        private static void MarkForGeneration(ClassTemplateSpecialization specialization)
        {
            specialization.GenerationKind = GenerationKind.Generate;
            Declaration declaration = specialization.TemplatedDecl.TemplatedDecl;
            while (declaration != null)
            {
                declaration.GenerationKind = GenerationKind.Generate;
                declaration = declaration.Namespace;
            }
        }
    }
}
