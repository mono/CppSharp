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

            var method = decl as Method;
            if (method != null)
            {
                return UpdateName(method);
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

        private bool UpdateName(Method method)
        {
            var @params = method.Parameters.Where(p => p.Kind != ParameterKind.IndirectReturnType)
                                .Select(p => p.QualifiedType.ToString());
            var signature = string.Format("{0}({1})", Name,string.Join( ", ", @params));

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

            if (method.IsOperator)
            {
                // TODO: turn into a method; append the original type (say, "signed long") of the last parameter to the type so that the user knows which overload is called
                Driver.Diagnostics.EmitWarning("Duplicate operator {0} ignored", method.Name);
                method.ExplicityIgnored = true;
            }
            else if (method.IsConstructor)
            {
                Driver.Diagnostics.EmitWarning("Duplicate constructor {0} ignored", method.Name);
                method.ExplicityIgnored = true;
            }
            else
                method.Name += methodCount.ToString(CultureInfo.InvariantCulture);
            return true;
        }
    }

    public class CheckDuplicatedNamesPass : TranslationUnitPass
    {
        private readonly IDictionary<string, DeclarationName> names;

        public CheckDuplicatedNamesPass()
        {
            names = new Dictionary<string, DeclarationName>();
        }

        public override bool VisitFieldDecl(Field decl)
        {
            if (ASTUtils.CheckIgnoreField(decl))
                return false;

            if(!AlreadyVisited(decl))
                CheckDuplicate(decl);

            return false;
        }

        public override bool VisitProperty(Property decl)
        {
            if(!AlreadyVisited(decl) && decl.ExplicitInterfaceImpl == null)
                CheckDuplicate(decl);

            return false;
        }

        public override bool VisitMethodDecl(Method decl)
        {
            if (ASTUtils.CheckIgnoreMethod(decl))
                return false;

            if (!AlreadyVisited(decl) && decl.ExplicitInterfaceImpl == null)
                CheckDuplicate(decl);

            return false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (AlreadyVisited(@class) || @class.IsIncomplete)
                return false;

            // DeclarationName should always process methods first, 
            // so we visit methods first.
            foreach (var method in @class.Methods)
                VisitMethodDecl(method);

            foreach (var field in @class.Fields)
                VisitFieldDecl(field);

            foreach (var property in @class.Properties)
                VisitProperty(property);

            return false;
        }

        void CheckDuplicate(Declaration decl)
        {
            if (decl.IsDependent || decl.Ignore)
                return;

            if (string.IsNullOrWhiteSpace(decl.Name))
                return;

            var fullName = decl.QualifiedName;

            // If the name is not yet on the map, then add it.
            if (!names.ContainsKey(fullName))
                names.Add(fullName, new DeclarationName(decl.Name, Driver));

            if (names[fullName].UpdateName(decl))
                Driver.Diagnostics.EmitWarning("Duplicate name {0}, renamed to {1}", fullName, decl.Name);
        }
    }
}
