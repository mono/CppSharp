using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    class DeclarationName
    {
        private readonly string Name;
        private readonly Dictionary<string, int> methodSignatures;
        private int Count;

        public DeclarationName(string name)
        {
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

            var count = Count++;
            if (count == 0)
                return false;

            decl.Name += count.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private bool UpdateName(Method method)
        {
            var @params = method.Parameters.Select(p => p.QualifiedType.ToString());
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
            if (ASTUtils.CheckIgnoreField(null, decl))
                return false;

            if(!AlreadyVisited(decl))
                CheckDuplicate(decl);
            return false;
        }

        public override bool VisitProperty(Property decl)
        {
            if(!AlreadyVisited(decl))
                CheckDuplicate(decl);
            return false;
        }

        public override bool VisitMethodDecl(Method decl)
        {
            if (ASTUtils.CheckIgnoreMethod(null, decl))
                return false;

            if(!AlreadyVisited(decl))
                CheckDuplicate(decl);
            return false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (AlreadyVisited(@class) || @class.IsIncomplete)
                return false;

            // In order to DeclarationName works, we visit methods first.
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
                names.Add(fullName, new DeclarationName(decl.Name));

            if (names[fullName].UpdateName(decl))
                Driver.Diagnostics.EmitWarning("Duplicate name {0}, renamed to {1}", fullName, decl.Name);
        }
    }

    public static class CheckDuplicateNamesExtensions
    {
        public static void CheckDuplicateNames(this PassBuilder builder)
        {
            var pass = new CheckDuplicatedNamesPass();
            builder.AddPass(pass);
        }
    }
}
