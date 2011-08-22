using System;
using NUnit.Framework;

namespace Tests {

	[TestFixture]
	public class ManglingTests {

		[Test]
		public void TestCompression ()
		{
			Compression.Test1 (null, "foo", null, "bar");
		}

		[Test]
		public void TestNamespaced ()
		{
			Ns1.Namespaced.Test1 ();
			Ns1.Namespaced.Test2 (null);
		}

		[Test]
		public void TestNamespaced2 ()
		{
			var cls = new Ns1.Ns2.Namespaced2 ();
			cls.Test1 ();
			cls.Test2 (null);
		}
	}
}

