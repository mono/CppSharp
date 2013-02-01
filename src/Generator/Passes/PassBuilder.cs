using System.Collections.Generic;
using Cxxi.Passes;

namespace Cxxi
{
    /// <summary>
    /// This class is used to build passes that will be run against the AST
    /// that comes from C++.
    /// </summary>
    public class PassBuilder
    {
        public List<TranslationUnitPass> Passes { get; private set; }
        public Library Library { get; private set; }

        public PassBuilder(Library library)
        {
            Passes = new List<TranslationUnitPass>();
            Library = library;
        }

        public void AddPass(TranslationUnitPass pass)
        {
            pass.Library = Library;
            Passes.Add(pass);
        }
    }
}
