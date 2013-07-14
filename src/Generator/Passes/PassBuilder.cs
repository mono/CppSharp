using System.Collections.Generic;
using System.Linq;
using CppSharp.Passes;

namespace CppSharp
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

        /// <summary>
        /// Finds a previously-added pass of the given type.
        /// </summary>
        public T FindPass<T>() where T : TranslationUnitPass
        {
            return Passes.OfType<T>().Select(pass => pass as T).FirstOrDefault();
        }
    }
}
