using System;
using NUnit.Framework;

namespace Tests {

	[TestFixture]
	public class VirtualTests {

		[Test]
		public void TestVirtualCall ()
		{
			var cls = new NumberClass (5);
			Assert.AreEqual (5, cls.Number, "#1");
			Assert.AreEqual (-5, cls.NegativeNumber, "#2");
		}

		[Test]
		public void TestVirtualCallOnBaseClass ()
		{
			var cls = new AdderClass (8);
			Assert.AreEqual (8, cls.Number, "#1");

			cls.Add (2);
			Assert.AreEqual (10, ((NumberClass)cls).Number, "#2");
		}

		[Test]
		[Ignore ("virtual inheritance not implemented yet")]
		public void TestVirtualCallOnVirtualBaseClass ()
		{
			var cls = new AdderClassWithVirtualBase (8);
			Assert.AreEqual (8, cls.Number, "#1");

			cls.Add (2);
			Assert.AreEqual (10, ((NumberClass)cls).Number, "#2");
		}

		[Test]
		public void TestMultipleBases ()
		{
			var cls = new ClassWithNonVirtualBases (5, 3);
			Assert.AreEqual (5, cls.Number, "#1");
			Assert.AreEqual (3, ((MultiplierClass)cls).Number, "#2");

			cls.Add (4);
			Assert.AreEqual (9, cls.Number, "#3");
			Assert.AreEqual (3, ((MultiplierClass)cls).Number, "#4");

			cls.Multiply (10);
			Assert.AreEqual (9, cls.Number, "#5");
			Assert.AreEqual (30, ((MultiplierClass)cls).Number, "#6");
		}

		[Test]
		[Ignore ("virtual inheritance not implemented yet")]
		public void TestMultipleVirtualBases ()
		{
			var cls = new ClassWithVirtualBases (4);
			Assert.AreEqual (4, cls.Number, "#1");
			Assert.AreEqual (4, ((MultiplierClassWithVirtualBase)cls).Number, "#2");

			cls.Add (5);
			Assert.AreEqual (9, cls.Number, "#3");
			Assert.AreEqual (9, ((MultiplierClassWithVirtualBase)cls).Number, "#4");

			cls.Multiply (6);
			Assert.AreEqual (30, cls.Number, "#5");
			Assert.AreEqual (30, ((MultiplierClassWithVirtualBase)cls).Number, "#6");
		}

		[Test]
		public void TestClassThatOverridesStuff ()
		{
			var cls = new ClassThatOverridesStuff (5, 3);
			Assert.AreEqual (3, cls.Number, "#1");
			Assert.AreEqual (3, ((NumberClass)cls).Number, "#2");
			Assert.AreEqual (-3, cls.NegativeNumber, "#3");
			Assert.AreEqual (5, cls.BaseNumber, "#4");
		}


	}
}

