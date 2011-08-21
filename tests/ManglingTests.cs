using System;
using NUnit.Framework;

namespace Tests {

	[TestFixture]
	public class ManglingTests {

		[Test]
		public void TestCompression1 ()
		{
			Mangling.CompressionTest1 (null, "foo", null, "bar");
		}
	}
}

