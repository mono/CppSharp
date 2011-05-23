//
// ItaniumAbiTests.cs: Test cases to exercise the ItaniumAbi
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using NUnit.Framework;

using Mono.VisualC.Interop.ABI;

namespace Tests {
        [TestFixture]
        public class ItaniumAbiTests : SharedAbiTests {

                public ItaniumAbiTests () : base (new ItaniumAbi ())
                {
                }
                
        }
}

