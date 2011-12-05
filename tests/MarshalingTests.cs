using System;
using NUnit.Framework;

namespace Tests {

	[TestFixture]
	public class MarshalingTests {

		[Test]
		public void TestClassReturn ()
		{
			// Section 3.1.4:
			// Classes with non-default copy ctors/destructors are returned using a hidden
			// argument
			var c = ClassWithCopyCtor.Return (42);
			Assert.AreEqual (42, c.GetX (), "#1");

			var c2 = ClassWithDtor.Return (43);
			Assert.AreEqual (43, c2.GetX (), "#2");

			// This class is returned normally
			var c3 = ClassWithoutCopyCtor.Return (44);
			Assert.AreEqual (44, c3.GetX (), "#3");
		}

		// An object as ref argument
		[Test]
		public void TestClassArg ()
		{
			var c1 = new Class (4);
			var c2 = new Class (5);
	
			c1.CopyTo (c2);
			Assert.AreEqual (4, c2.GetX (), "#1");
		}

		// A null object as ref argument
		[Test]
		public void TestClassArgNull ()
		{
			var c1 = new Class (4);
			Assert.That (c1.IsNull (null), "#1");
		}

		// An object as byval argument
		[Test]
		public void TestClassArgByval ()
		{
			var c1 = new Class (4);
			var c2 = new Class (5);
	
			c1.CopyFromValue (c2);
			Assert.AreEqual (5, c1.GetX (), "#1");
		}
	
		// A null object as byval argument
		[Test]
		[ExpectedException (typeof (ArgumentException))]
		public void TestClassArgByvalNull ()
		{
			var c1 = new Class (4);
			c1.CopyFromValue (null);
		}

		[Test]
		public void TestByRefReturn ()
		{
			var c1 = new Class (7);
			Assert.AreEqual (7, c1.GetXRef ());
		}
	
	}

}