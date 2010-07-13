using System;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

namespace Qt {
	// Will be internal; public for testing
        public static class Libs {
                public static CppLibrary QtCore = null;
                public static CppLibrary QtGui = null;

                static Libs ()
                {
                        string lib;
                        CppAbi abi;
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        { // for Windows...
                                lib = "{0}4.dll";
                                abi = new MsvcAbi ();
                        } else { // for Mac...
                                lib = "/Library/Frameworks/{0}.framework/Versions/Current/{0}";
                                abi = new ItaniumAbi ();
                        }


                        QtCore = new CppLibrary (string.Format(lib, "QtCore"), abi);
                        QtGui  = new CppLibrary (string.Format(lib, "QtGui"),  abi);
                }
        }
}

