using System;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;

namespace Qt {
        internal static class Libs {
                public static CppLibrary QtCore = null;
                public static CppLibrary QtGui = null;

                static Libs ()
                {
                        CppAbi abi;
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                                abi = new MsvcAbi ();
                        else
                                abi = new ItaniumAbi ();


                        QtCore = new CppLibrary ("/Library/Frameworks/QtCore.framework/Versions/Current/QtCore", abi);
                        QtGui  = new CppLibrary ("/Library/Frameworks/QtGui.framework/Versions/Current/QtGui",  abi);
                }
        }
}

