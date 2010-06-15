//
// MsvcAbiTests.cs: Test cases to exercise the MsvcAbi
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using NUnit.Framework;

using Mono.VisualC.Interop;
using Mono.VisualC.Interop.ABI;
using Tests.Support;

namespace Tests {
        [TestFixture]
        public class MsvcAbiTests : CPPTestLibBase {

                public MsvcAbiTests () : base (new MsvcAbi ())
                {
                }
        }
}

