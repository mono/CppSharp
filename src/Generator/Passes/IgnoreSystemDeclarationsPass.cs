using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class IgnoreSystemDeclarationsPass : TranslationUnitPass
    {
        public IgnoreSystemDeclarationsPass()
            => VisitOptions.ResetFlags(VisitFlags.NamespaceEnums);

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

            @class.ExplicitlyIgnore();

            if (!@class.IsDependent || @class.Specializations.Count == 0)
                return false;

            foreach (var specialization in @class.Specializations)
                specialization.ExplicitlyIgnore();

            // we only need a few members for marshalling so strip the rest
            switch (@class.Name)
            {
                case "basic_string":
                    @class.GenerationKind = GenerationKind.Generate;
                    foreach (var specialization in from s in @class.Specializations
                                                   let arg = s.Arguments[0].Type.Type.Desugar()
                                                   where arg.IsPrimitiveType(PrimitiveType.Char) || arg.IsPrimitiveType(PrimitiveType.WideChar)
                                                   select s)
                    {
                        specialization.GenerationKind = GenerationKind.Generate;
                        foreach (var method in specialization.Methods)
                        {
                            if (method.OriginalName == "assign" || method.OriginalName == "data")
                            {
                                if (specialization.Arguments[0].Type.Type.Desugar().IsPrimitiveType(PrimitiveType.WideChar))
                                    method.Name = method.Name + "W";
                                else if (specialization.Arguments[0].Type.Type.Desugar().IsPrimitiveType(PrimitiveType.Char))
                                    method.Name = method.Name + "A";

                                method.GenerationKind = GenerationKind.Generate;
                                method.Namespace.GenerationKind = GenerationKind.Generate;
                                method.InstantiatedFrom.GenerationKind = GenerationKind.Generate;
                                method.InstantiatedFrom.Namespace.GenerationKind = GenerationKind.Generate;
                            }
                        }
                    }
                    break;
                case "allocator":
                case "char_traits":
                    @class.GenerationKind = GenerationKind.Generate;
                    foreach (var specialization in from s in @class.Specializations
                                                   let arg = s.Arguments[0].Type.Type.Desugar()
                                                   where arg.IsPrimitiveType(PrimitiveType.Char) || arg.IsPrimitiveType(PrimitiveType.WideChar)
                                                   select s)
                    {
                        specialization.GenerationKind = GenerationKind.Generate;
                    }
                    break;
            }
            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            if (@enum.TranslationUnit.IsSystemHeader)
                @enum.ExplicitlyIgnore();

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
