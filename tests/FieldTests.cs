using System;
using NUnit.Framework;

namespace Tests {

	[TestFixture]
	public class FieldTests {

		[Test]
		public void TestReadCppObject ()
		{
			var hf1 = new HasField (1, null);
			var hf2 = new HasField (2, hf1);
			var hf3 = new HasField (3, hf2);

			Assert.IsNull (hf1.other, "#1");
			Assert.AreEqual (1, hf1.number);

			Assert.AreSame (hf2.other, hf1, "#2");
			Assert.AreEqual (1, hf2.other.number);

			Assert.AreSame (hf3.other.other, hf1, "#3");
			Assert.AreEqual (1, hf3.other.other.number, "#4");
		}
	}
}

