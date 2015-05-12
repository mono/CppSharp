using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass implements checks for helper macros in declarations.
    /// The supported macros are listed below with a "CS" prefix which
    /// stands for CppSharp. Using custom prefixes is also supported by
    /// passing the value to the constructor of the pass.
    /// 
    ///     CS_IGNORE_FILE (translation units)
    ///         Used to ignore whole translation units.
    /// 
    ///     CS_IGNORE (declarations)
    ///         Used to ignore declarations from being processed.
    /// 
    ///     CS_IGNORE_GEN (declarations)
    ///         Used to ignore declaration from being generated.
    /// 
    ///     CS_IGNORE_FILE (.h)
    ///         Used to ignore all declarations of one header.
    /// 
    ///     CS_VALUE_TYPE (classes and structs)
    ///         Used to flag that a class or struct is a value type.
    /// 
    ///     CS_IN_OUT / CS_OUT (parameters)
    ///         Used in function parameters to specify their usage kind.
    /// 
    ///     CS_FLAGS (enums)
    ///         Used to specify that enumerations represent bitwise flags.
    /// 
    ///     CS_READONLY (fields and properties)
    ///         Used in fields and properties to specify read-only semantics.
    /// 
    ///     CS_EQUALS / CS_HASHCODE (methods)
    ///         Used to flag method as representing the .NET Equals or
    ///         Hashcode methods.
    /// 
    ///     CS_CONSTRAINT(TYPE [, TYPE]*) (templates)
    ///         Used to define constraint of generated generic type or generic method.
    /// 
    ///     CS_INTERNAL (methods)
    ///         Used to flag a method as internal to an assembly. So, it is
    ///         not accessible outside that assembly.
    /// 
    /// There isn't a standardized header provided by CppSharp so you will
    /// have to define these on your own.
    /// </summary>
    public class CheckMacroPass : TranslationUnitPass
    {
        private readonly string Prefix;

        public CheckMacroPass(string prefix = "CS")
        {
            Prefix = prefix;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (AlreadyVisited(decl))
                return false;

            if (decl is DeclarationContext && !(decl is Class))
                return true;

            var expansions = decl.PreprocessedEntities.OfType<MacroExpansion>();

            CheckIgnoreMacros(decl, expansions);
            return true;
        }

        void CheckIgnoreMacros(Declaration decl, IEnumerable<MacroExpansion> expansions)
        {
            if (expansions.Any(e => e.Text == Prefix + "_IGNORE" &&
                                    e.MacroLocation != MacroLocation.ClassBody &&
                                    e.MacroLocation != MacroLocation.FunctionBody &&
                                    e.MacroLocation != MacroLocation.FunctionParameters))
            {
                Log.Debug("Decl '{0}' was ignored due to ignore macro",
                    decl.Name);
                decl.ExplicitlyIgnore();
            }

            if (expansions.Any(e => e.Text == Prefix + "_IGNORE_GEN" &&
                                    e.MacroLocation != MacroLocation.ClassBody &&
                                    e.MacroLocation != MacroLocation.FunctionBody &&
                                    e.MacroLocation != MacroLocation.FunctionParameters))
                decl.GenerationKind = GenerationKind.Internal;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            var expansions = unit.PreprocessedEntities.OfType<MacroExpansion>();

            if (expansions.Any(e => e.Text == Prefix + "_IGNORE_FILE"))
            {
                unit.ExplicitlyIgnore();
            }

            return base.VisitTranslationUnit(unit);
        }

        public override bool VisitClassDecl(Class @class)
        {
            var expansions = @class.PreprocessedEntities.OfType<MacroExpansion>();

            if (expansions.Any(e => e.Text == Prefix + "_VALUE_TYPE"))
                @class.Type = ClassType.ValueType;

            // If the class is a forward declaration, then we process the macro expansions
            // of the complete class as if they were specified on the forward declaration.
            if (@class.CompleteDeclaration != null)
            {
                var completeExpansions = @class.CompleteDeclaration.PreprocessedEntities
                    .OfType<MacroExpansion>();
                CheckIgnoreMacros(@class, completeExpansions);
            }

            return base.VisitClassDecl(@class);
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (!VisitDeclaration(parameter))
                return false;

            var expansions = parameter.PreprocessedEntities.OfType<MacroExpansion>();

            if (expansions.Any(e => e.Text == Prefix + "_OUT"))
                parameter.Usage = ParameterUsage.Out;

            if (expansions.Any(e => e.Text == Prefix + "_IN_OUT"))
                parameter.Usage = ParameterUsage.InOut;

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            var expansions = @enum.PreprocessedEntities.OfType<MacroExpansion>();

            if (expansions.Any(e => e.Text == Prefix + "_FLAGS"))
                @enum.SetFlags();

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            var expansions = method.PreprocessedEntities.OfType<MacroExpansion>();

            if (expansions.Any(e => e.Text == Prefix + "_HASHCODE"
                || e.Text == Prefix + "_EQUALS"))
                method.ExplicitlyIgnore();

            if (expansions.Any(e => e.Text == Prefix + "_INTERNAL"))
                method.Access = AccessSpecifier.Internal;

            return base.VisitMethodDecl(method);
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            var expansions = field.PreprocessedEntities.OfType<MacroExpansion>();

            if (expansions.Any(e => e.Text == Prefix + "_READONLY"))
            {
                var quals = field.QualifiedType.Qualifiers;
                quals.IsConst = true;

                var qualType  = field.QualifiedType;
                qualType.Qualifiers = quals;

                field.QualifiedType = qualType;
            }

            return true;
        }

        public override bool VisitProperty(Property property)
        {
            var expansions = property.PreprocessedEntities.OfType<MacroExpansion>();

            if (expansions.Any(e => e.Text == Prefix + "_READONLY"))
            {
                var setMethod = property.SetMethod;

                if (setMethod != null)
                    property.SetMethod.ExplicitlyIgnore();
            }

            return base.VisitProperty(property);
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            var expansions = template.PreprocessedEntities.OfType<MacroExpansion>();

            var expansion = expansions.FirstOrDefault(e => e.Text.StartsWith(Prefix + "_CONSTRAINT"));
            if (expansion != null)
            {
                var args = GetArguments(expansion.Text);
                for (var i = 0; i < template.Parameters.Count && i < args.Length; ++i)
                {
                    var param = template.Parameters[i];
                    param.Constraint = args[i];
                    template.Parameters[i] = param;
                }
            }

            return base.VisitClassTemplateDecl(template);
        }

        public override bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            var expansions = template.PreprocessedEntities.OfType<MacroExpansion>();

            var expansion = expansions.FirstOrDefault(e => e.Text.StartsWith(Prefix + "_CONSTRAINT"));
            if (expansion != null)
            {
                var args = GetArguments(expansion.Text);
                for (var i = 0; i < template.Parameters.Count && i < args.Length; ++i)
                {
                    var param = template.Parameters[i];
                    param.Constraint = args[i];
                    template.Parameters[i] = param;
                }
            }

            return base.VisitFunctionTemplateDecl(template);
        }

        private static string[] GetArguments(string str)
        {
            str = str.Substring(str.LastIndexOf('('));
            str = str.Trim('(', ')');
            return str.Split(',');
        }
    }
}
