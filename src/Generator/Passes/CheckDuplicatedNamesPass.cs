using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

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
            var function = decl as Function;
            if (function != null)
            {
                return UpdateName(function);
            }

            var property = decl as Property;
            var isIndexer = property != null && property.Parameters.Count > 0;
            if (isIndexer)
            {
                return false;
            }

            var count = Count++;
            if (count == 0)
                return false;

            decl.Name += count.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private bool UpdateName(Function function)
        {
            var @params = function.Parameters.Where(p => p.Kind != ParameterKind.IndirectReturnType)
                                .Select(p => p.QualifiedType.ToString());
            // Include the conversion type in case of conversion operators
            var method = function as Method;
            if (method != null &&
                method.IsOperator &&
                (method.OperatorKind == CXXOperatorKind.Conversion ||
                 method.OperatorKind == CXXOperatorKind.ExplicitConversion))
                @params = @params.Concat(new[] { method.ConversionType.ToString() });
            var signature = $"{function.Name}({string.Join(", ", @params)})";
            signature = FixSignatureForConversions(function, signature);

            if (Count == 0)
                Count++;

            var duplicate = functions.Keys.FirstOrDefault(f =>
                f.Parameters.SequenceEqual(function.Parameters, ParameterTypeComparer.Instance));

            if (duplicate == null)
            {
                functions.Add(function, 0);
                return false;
            }

            var methodCount = ++functions[duplicate];

            if (Count < methodCount + 1)
                Count = methodCount + 1;

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

        private class ParameterTypeComparer : IEqualityComparer<Parameter>
        {
            public static readonly ParameterTypeComparer Instance = new ParameterTypeComparer();

            private ParameterTypeComparer()
            {
            }

            public bool Equals(Parameter x, Parameter y)
            {
                Type left = x.Type.Desugar(resolveTemplateSubstitution: false);
                Type right = y.Type.Desugar(resolveTemplateSubstitution: false);
                if (left.Equals(right))
                    return true;

                // TODO: some target languages might maek a difference between values and pointers
                Type leftPointee = left.GetPointee();
                Type rightPointee = right.GetPointee();
                return (leftPointee != null && leftPointee.Desugar(false).Equals(right)) ||
                    (rightPointee != null && rightPointee.Desugar(false).Equals(left));
            }

            public int GetHashCode(Parameter obj)
            {
                return obj.Type.GetHashCode();
            }
        }
    }

    public class CheckDuplicatedNamesPass : TranslationUnitPass
    {
        private readonly IDictionary<string, DeclarationName> names;

        public CheckDuplicatedNamesPass()
        {
            ClearVisitedDeclarations = false;
            names = new Dictionary<string, DeclarationName>();
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
            var function = decl as Function;
            if (function != null)
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
