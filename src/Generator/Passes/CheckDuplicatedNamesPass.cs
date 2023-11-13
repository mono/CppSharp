using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Types;

namespace CppSharp.Passes
{
    class DeclarationName
    {
        private readonly Dictionary<Function, int> functions;
        private int Count;

        public DeclarationName()
        {
            functions = new Dictionary<Function, int>();
        }

        public bool UpdateName(Declaration decl)
        {
            if (decl is Function function)
            {
                return UpdateName(function);
            }

            var isIndexer = decl is Property property && property.Parameters.Count > 0;
            if (isIndexer)
            {
                return false;
            }

            var count = Count++;
            if (count <= 1)
                return false;

            decl.Name += count.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private bool UpdateName(Function function)
        {
            if (Count == 0)
                Count++;

            var duplicate = functions.Keys.FirstOrDefault(f =>
                function.SynthKind != FunctionSynthKind.DefaultValueOverload &&
                f.Parameters.Where(p => p.Kind == ParameterKind.Regular ||
                    p.Kind == ParameterKind.Extension).SequenceEqual(
                    function.Parameters.Where(p => p.Kind == ParameterKind.Regular ||
                        p.Kind == ParameterKind.Extension),
                    ParameterTypeComparer.Instance));

            if (duplicate == null)
            {
                functions.Add(function, 0);
                return false;
            }

            var methodCount = ++functions[duplicate];

            if (Count < methodCount + 1)
                Count = methodCount + 1;

            var method = function as Method;
            if (function.IsOperator)
            {
                // TODO: turn into a method; append the original type (say, "signed long")
                // of the last parameter to the type so that the user knows which overload is called
                function.ExplicitlyIgnore();
            }
            else if (method != null && method.IsConstructor)
            {
                function.ExplicitlyIgnore();
            }
            else
                function.Name += methodCount.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        public static string FixSignatureForConversions(Function function, string signature)
        {
            switch (function.OperatorKind)
            {
                // C# does not allow an explicit and an implicit operator from the same type
                case CXXOperatorKind.Conversion:
                    signature = signature.Replace("implicit ", "conversion ");
                    break;
                case CXXOperatorKind.ExplicitConversion:
                    signature = signature.Replace("explicit ", "conversion ");
                    break;
            }
            return signature;
        }

        public class ParameterTypeComparer : IEqualityComparer<Parameter>
        {
            public static readonly ParameterTypeComparer Instance =
                new ParameterTypeComparer();

            public static TypeMapDatabase TypeMaps { get; set; }
            public static GeneratorKind GeneratorKind { get; set; }

            public bool Equals(Parameter x, Parameter y)
            {
                Type left = x.Type.Desugar(resolveTemplateSubstitution: false);
                Type right = y.Type.Desugar(resolveTemplateSubstitution: false);
                if (left.Equals(right))
                    return true;

                if (CheckForSpecializations(left, right))
                    return true;

                // TODO: some target languages might make a difference between values and pointers
                Type leftPointee = left.GetPointee();
                Type rightPointee = right.GetPointee();

                if (CheckForSpecializations(leftPointee, rightPointee))
                    return true;

                return leftPointee != null && rightPointee != null &&
                    leftPointee.GetMappedType(TypeMaps, GeneratorKind).Equals(
                    rightPointee.GetMappedType(TypeMaps, GeneratorKind));
            }

            private static bool CheckForSpecializations(Type leftPointee, Type rightPointee)
            {
                if (!leftPointee.TryGetDeclaration(out Class leftClass) ||
                    !rightPointee.TryGetDeclaration(out Class rightClass))
                    return false;

                var leftSpecialization = leftClass as ClassTemplateSpecialization ??
                    leftClass.Namespace as ClassTemplateSpecialization;
                var rightSpecialization = rightClass as ClassTemplateSpecialization ??
                    rightClass.Namespace as ClassTemplateSpecialization;

                return leftSpecialization != null && rightSpecialization != null &&
                    leftSpecialization.TemplatedDecl.TemplatedDecl.Equals(
                        rightSpecialization.TemplatedDecl.TemplatedDecl) &&
                    leftSpecialization.Arguments.SequenceEqual(
                        rightSpecialization.Arguments, TemplateArgumentComparer.Instance) &&
                    leftClass.OriginalName == rightClass.OriginalName;
            }

            public int GetHashCode(Parameter obj)
            {
                return obj.Type.GetHashCode();
            }

            public static TypePrinter TypePrinter { get; set; }
        }

        public class TemplateArgumentComparer : IEqualityComparer<TemplateArgument>
        {
            public static readonly TemplateArgumentComparer Instance =
                new TemplateArgumentComparer();

            public bool Equals(TemplateArgument x, TemplateArgument y)
            {
                if (x.Kind != TemplateArgument.ArgumentKind.Type ||
                    y.Kind != TemplateArgument.ArgumentKind.Type)
                    return x.Equals(y);
                Type left = x.Type.Type.GetMappedType(ParameterTypeComparer.TypeMaps,
                    ParameterTypeComparer.GeneratorKind);
                Type right = y.Type.Type.GetMappedType(ParameterTypeComparer.TypeMaps,
                    ParameterTypeComparer.GeneratorKind);
                // consider Type and const Type the same
                if (left.IsReference() && !left.IsPointerToPrimitiveType())
                    left = left.GetPointee();
                if (right.IsReference() && !right.IsPointerToPrimitiveType())
                    right = right.GetPointee();
                return left.Equals(right);
            }

            public int GetHashCode(TemplateArgument obj)
            {
                return obj.GetHashCode();
            }
        }
    }

    public class CheckDuplicatedNamesPass : TranslationUnitPass
    {
        private readonly IDictionary<string, DeclarationName> names = new Dictionary<string, DeclarationName>();

        public override bool VisitASTContext(ASTContext context)
        {
            DeclarationName.ParameterTypeComparer.TypePrinter = Options.GeneratorKind.CreateTypePrinter(Context);
            DeclarationName.ParameterTypeComparer.TypeMaps = Context.TypeMaps;
            DeclarationName.ParameterTypeComparer.GeneratorKind = Options.GeneratorKind;
            return base.VisitASTContext(context);
        }

        public override bool VisitProperty(Property decl)
        {
            if (!VisitDeclaration(decl))
                return false;

            if (decl.ExplicitInterfaceImpl == null)
                CheckDuplicate(decl);

            return false;
        }

        public override bool VisitFunctionDecl(Function decl)
        {
            if (!VisitDeclaration(decl))
                return false;

            if (ASTUtils.CheckIgnoreFunction(decl))
                return false;

            CheckDuplicate(decl);
            return false;
        }

        public override bool VisitMethodDecl(Method decl)
        {
            if (!VisitDeclaration(decl))
                return false;

            if (ASTUtils.CheckIgnoreMethod(decl))
                return false;

            if (decl.ExplicitInterfaceImpl == null)
                CheckDuplicate(decl);

            return false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            if (@class.IsIncomplete)
                return false;

            // DeclarationName should always process methods first, 
            // so we visit methods first.
            foreach (var method in @class.Methods)
                VisitMethodDecl(method);

            foreach (var function in @class.Functions)
                VisitFunctionDecl(function);

            if (!@class.IsDependent)
            {
                foreach (var fields in @class.Layout.Fields.GroupBy(
                    f => f.Name).Select(g => g.ToList()))
                {
                    for (var i = 1; i < fields.Count; i++)
                    {
                        var name = fields[i].Name;
                        fields[i].Name = (string.IsNullOrEmpty(name) ? "__" : name) + i;
                    }
                }
            }

            foreach (var property in @class.Properties)
                VisitProperty(property);

            return false;
        }

        private void CheckDuplicate(Declaration decl)
        {
            if (decl.IsDependent || !decl.IsGenerated)
                return;

            if (string.IsNullOrWhiteSpace(decl.Name))
                return;

            var fullName = decl.QualifiedName;
            if (decl is Function function)
                fullName = DeclarationName.FixSignatureForConversions(function, fullName);

            // If the name is not yet on the map, then add it.
            if (!names.ContainsKey(fullName))
                names.Add(fullName, new DeclarationName());

            if (names[fullName].UpdateName(decl))
            {
                Diagnostics.Debug("Duplicate name {0}, renamed to {1}",
                    fullName, decl.Name);
            }
        }
    }
}
