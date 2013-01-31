using System;
using System.Collections.Generic;
using System.IO;

namespace Cxxi.Passes
{
    public class Transform
    {
        public Options Options;
        public PassBuilder Passes;

        public void TransformLibrary(Library library)
        {
            if (string.IsNullOrEmpty(library.Name))
                library.Name = string.Empty;

            // Process everything in the global namespace for now.
            foreach (var module in library.TranslationUnits)
                TransformModule(module);
        }

        string GetIncludePath(string filePath)
        {
            string includePath = filePath;
            string ShortestIncludePath = filePath;

            foreach (var path in Options.IncludeDirs)
            {
                int idx = filePath.IndexOf(path, System.StringComparison.Ordinal);
                if (idx == -1) continue;

                string inc = filePath.Substring(path.Length);

                if (inc.Length < includePath.Length && inc.Length < ShortestIncludePath.Length)
                    ShortestIncludePath = inc;
            }

            return "..\\" + ShortestIncludePath.TrimStart(new char[] { '\\', '/' });
        }

        void TransformModule(TranslationUnit unit)
        {
            // Try to get an include path that works from the original include
            // directories paths.

            unit.IncludePath = GetIncludePath(unit.FilePath);

            foreach (var pass in Passes.Passes)
                pass.ProcessUnit(unit);

            foreach (var @enum in unit.Enums)
                TransformEnum(@enum);

            foreach (var function in unit.Functions)
                TransformFunction(function);

            foreach (var @class in unit.Classes)
                TransformClass(@class);

            foreach (var typedef in unit.Typedefs)
                TransformTypedef(typedef);
        }

        void TransformDeclaration(Declaration decl)
        {
            foreach (var pass in Passes.Passes)
                pass.ProcessDeclaration(decl);
        }

        void TransformTypedef(TypedefDecl typedef)
        {
            foreach (var pass in Passes.Passes)
                pass.ProcessDeclaration(typedef);
        }

        void TransformClass(Class @class)
        {
            foreach (var pass in Passes.Passes)
                pass.ProcessClass(@class);

            TransformDeclaration(@class);

            foreach (var method in @class.Methods)
            {
                foreach (var pass in Passes.Passes)
                    pass.ProcessMethod(method);
            }

            foreach (var field in @class.Fields)
            {
                foreach (var pass in Passes.Passes)
                    pass.ProcessField(field);

                TransformDeclaration(field);
            }
        }

        void TransformFunction(Function function)
        {
            foreach (var pass in Passes.Passes)
                pass.ProcessFunction(function);

            TransformDeclaration(function);

            foreach (var param in function.Parameters)
                TransformDeclaration(param);
        }

        void TransformEnum(Enumeration @enum)
        {
            TransformDeclaration(@enum);

            foreach (var item in @enum.Items)
            {
                foreach (var pass in Passes.Passes)
                    pass.ProcessEnumItem(item);
            }
        }
    }
}