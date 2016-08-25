using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.Generators;
using CppSharp.Passes;

namespace CppSharp
{
    /// <summary>
    /// This class is used to build passes that will be run against the AST
    /// that comes from C++.
    /// </summary>
    public class PassBuilder<T>
    {
        public BindingContext Context { get; private set; }
        public List<T> Passes { get; private set; }

        public PassBuilder(BindingContext context)
        {
            Context = context;
            Passes = new List<T>();
        }

        /// <summary>
        /// Adds a new pass to the builder.
        /// </summary>
        public void AddPass(T pass)
        {
            if (pass is TranslationUnitPass)
                (pass as TranslationUnitPass).Context = Context;

            Passes.Add(pass);
        }

        /// <summary>
        /// Finds a previously-added pass of the given type.
        /// </summary>
        public U FindPass<U>() where U : TranslationUnitPass
        {
            return Passes.OfType<U>().Select(pass => pass as U).FirstOrDefault();
        }

        /// <summary>
        /// Runs the passes in the builder.
        /// </summary>
        public void RunPasses(Action<T> action)
        {
            foreach (var pass in Passes)
                action(pass);
        }
    }
}
