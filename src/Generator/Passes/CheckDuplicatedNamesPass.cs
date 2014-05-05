using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    class DeclarationName
    {
        public Driver Driver { get; set; }
        private readonly string Name;
        private readonly Dictionary<string, int> methodSignatures;
        private int Count;

        public DeclarationName(string name, Driver driver)
        {
            Driver = driver;
            Name = name;
            methodSignatures = new Dictionary<string, int>();
        }

        public bool UpdateName(Declaration decl)
        {
            if (decl.Name != Name)
                throw new Exception("Invalid name");

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
            var signature = string.Format("{0}({1})", Name, string.Join( ", ", @params));

            if (Count == 0)
                Count++;

            if (!methodSignatures.ContainsKey(signature))
            {
                methodSignatures.Add(signature, 0);
                return false;
            }

            var methodCount = ++methodSignatures[signature];

            if (Count < methodCount+1)
                Count = methodCount+1;

            if (function.IsOperator)
            {
                // TODO: turn into a method; append the original type (say, "signed long") of the last parameter to the type so that the user knows which overload is called
                Driver.Diagnostics.EmitWarning("Duplicate operator {0} ignored", function.Name);
                function.ExplicitlyIgnore();
            }
            else if (method != null && method.IsConstructor)
            {
                Driver.Diagnostics.EmitWarning("Duplicate constructor {0} ignored", function.Name);
                function.ExplicitlyIgnore();
            }
            else
                function.Name += methodCount.ToString(CultureInfo.InvariantCulture);
            return true;
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

        public override bool VisitFieldDecl(Field decl)
        {
            if (!VisitDeclaration(decl))
                return false;

            if (ASTUtils.CheckIgnoreField(decl))
                return false;

            CheckDuplicate(decl);
            return false;
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

            foreach (var field in @class.Fields)
                VisitFieldDecl(field);

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

        void CheckDuplicate(Declaration decl)
        {
            if (decl.IsDependent || !decl.IsGenerated)
                return;

            if (string.IsNullOrWhiteSpace(decl.Name))
                return;

            var fullName = decl.QualifiedName;

            // If the name is not yet on the map, then add it.
            if (!names.ContainsKey(fullName))
                names.Add(fullName, new DeclarationName(decl.Name, Driver));

            if (names[fullName].UpdateName(decl))
            {
                Driver.Diagnostics.Debug("Duplicate name {0}, renamed to {1}",
                    fullName, decl.Name);
            }
        }
    }
}
