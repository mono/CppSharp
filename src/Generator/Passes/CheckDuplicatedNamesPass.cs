using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    class DeclarationName
    {
        private readonly Dictionary<string, int> methodSignatures;
        private int Count;
        private readonly IDiagnosticConsumer diagnostics;

        public DeclarationName(IDiagnosticConsumer diagnostics)
        {
            this.diagnostics = diagnostics;
            methodSignatures = new Dictionary<string, int>();
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
            var signature = string.Format("{0}({1})", function.Name, string.Join( ", ", @params));
            signature = FixSignatureForConversions(function, signature);

            if (Count == 0)
                Count++;

            if (!methodSignatures.ContainsKey(signature))
            {
                methodSignatures.Add(signature, 0);
                return false;
            }

            var methodCount = ++methodSignatures[signature];

            if (Count < methodCount + 1)
                Count = methodCount + 1;

            if (function.IsOperator)
            {
                // TODO: turn into a method; append the original type (say, "signed long") of the last parameter to the type so that the user knows which overload is called
                diagnostics.Warning("Duplicate operator {0} ignored", function.Name);
                function.ExplicitlyIgnore();
            }
            else if (method != null && method.IsConstructor)
            {
                diagnostics.Warning("Duplicate constructor {0} ignored", function.Name);
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

            if (ASTUtils.CheckIgnoreFunction(decl, Driver.Options))
                return false;

            CheckDuplicate(decl);
            return false;
        }

        public override bool VisitMethodDecl(Method decl)
        {
            if (!VisitDeclaration(decl))
                return false;

            if (ASTUtils.CheckIgnoreMethod(decl, Driver.Options))
                return false;

            if (decl.ExplicitInterfaceImpl == null)
                CheckDuplicate(decl);

            return false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            if (@class.IsIncomplete)
                return false;

            // DeclarationName should always process methods first, 
            // so we visit methods first.
            foreach (var method in @class.Methods)
                VisitMethodDecl(method);

            foreach (var function in @class.Functions)
                VisitFunctionDecl(function);

            foreach (var fields in GetAllFields(@class).GroupBy(f => f.OriginalName).Where(
                g => !string.IsNullOrEmpty(g.Key)).Select(g => g.ToList()))
            {
                for (var i = 1; i < fields.Count; i++)
                    fields[i].InternalName = fields[i].OriginalName + i;
            }

            foreach (var property in @class.Properties)
                VisitProperty(property);

            var total = (uint)0;
            foreach (var method in @class.Methods.Where(m => m.IsConstructor &&
                !m.IsCopyConstructor && !m.IsMoveConstructor))
                method.Index =  total++;

            total = 0;
            foreach (var method in @class.Methods.Where(m => m.IsCopyConstructor))
                method.Index = total++;

            return false;
        }

        private static IEnumerable<Field> GetAllFields(Class @class, List<Field> fields = null)
        {
            fields = fields ?? new List<Field>();
            foreach (var @base in @class.Bases.Where(b => b.IsClass && b.Class != @class))
                GetAllFields(@base.Class, fields);
            fields.AddRange(@class.Fields);
            return fields;
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
                names.Add(fullName, new DeclarationName(Driver.Diagnostics));

            if (names[fullName].UpdateName(decl))
            {
                Driver.Diagnostics.Debug("Duplicate name {0}, renamed to {1}",
                    fullName, decl.Name);
            }
        }
    }
}
