using System;
using System.Collections.Generic;
using System.Linq;
using Cxxi.Passes;

namespace Cxxi
{
    /// <summary>
    /// This class is used to build passes that will be run against the AST
    /// that comes from C++.
    /// </summary>
    public class PassBuilder
    {
        public List<TranslationUnitPass> Passes { get; set; }

        public PassBuilder()
        {
            Passes = new List<TranslationUnitPass>();
        }

        public void AddPass(TranslationUnitPass pass)
        {
            Passes.Add(pass);
        }

        #region Operation Helpers

        public void RenameWithPattern(string pattern, string replacement, RenameTargets targets)
        {
            AddPass(new RegexRenamePass(pattern, replacement, targets));
        }

        public void RemovePrefix(string prefix)
        {
            AddPass(new RegexRenamePass("^" + prefix, String.Empty));
        }

        public void RemovePrefixEnumItem(string prefix)
        {
            AddPass(new RegexRenamePass("^" + prefix, String.Empty, RenameTargets.EnumItem));
        }

        public void RenameDeclsCase(RenameTargets targets, RenameCasePattern pattern)
        {
            AddPass(new CaseRenamePass(targets, pattern));
        }

        #endregion
    }
}
