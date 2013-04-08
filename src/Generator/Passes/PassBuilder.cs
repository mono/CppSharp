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
        public Driver Driver { get; private set; }

        public PassBuilder(Driver driver)
        {
            Passes = new List<TranslationUnitPass>();
            Driver = driver;
        }

        public void AddPass(TranslationUnitPass pass)
        {
            pass.Driver = Driver;
            pass.Library = Driver.Library;
            Passes.Add(pass);
        }

        public void RunPasses()
        {
            foreach (var pass in Passes)
            {
                pass.VisitLibrary(Driver.Library);
            }
        }
    }
}
