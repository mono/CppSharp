using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CppSharp
{
    // DeclInfo holds names exported by a library for later importing by another library,
    // allowing dependencies between wrappers generated with C++#.
    [Serializable]
    public struct DeclInfo
    {
        public string originalQualName;
        public string convertedQualName;
        public string kind;
        public string transUnit;

        public override string ToString()
        {
            return "Name: " + convertedQualName + ", Type: " + kind + ", Unit: " + transUnit;
        }
    }
}
