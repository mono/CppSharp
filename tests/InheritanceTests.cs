using System;
using NUnit.Framework;

namespace Tests {

	[TestFixture]
	public class InheritanceTests {

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
		public void TestNativeOverride1 ()
		{
			var cls = new ClassThatOverridesStuff (5, 3);
			Assert.AreEqual (3, cls.Number, "#1");
			Assert.AreEqual (3, ((NumberClass)cls).Number, "#2");
			Assert.AreEqual (-3, cls.NegativeNumber, "#3");
			Assert.AreEqual (5, cls.BaseNumber, "#4");
		}

		[Test]
		public void TestNativeOverride2 ()
		{
			var cls = ClassThatOverridesStuff.GetInstance (5, 3);
			Assert.AreEqual (3, cls.Number, "#1");
			Assert.AreEqual (3, ((NumberClass)cls).Number, "#2");
			Assert.AreEqual (-3, cls.NegativeNumber, "#3");
			Assert.AreEqual (5, ((ClassThatOverridesStuff)cls).BaseNumber, "#4");
		}

		class ManagedOverride1 : NumberClass {

			public ManagedOverride1 () : base (3)
			{
			}

			public override int Number {
				get {
					return 25;
				}
			}
		}

		[Test]
		public void TestManagedOverride1 ()
		{
			var cls = new ManagedOverride1 ();
			Assert.AreEqual (-25, cls.NegativeNumber, "#1");
		}

		class ManagedOverride2 : ClassWithNonVirtualBases {

			public ManagedOverride2 () : base (5, 3)
			{
			}

			// override virtual member inherited from non-primary base
			public override void Multiply (int n)
			{
				base.Multiply (10);
			}
		}

		[Test]
		public void TestManagedOverride2 ()
		{
			var cls = new ManagedOverride2 ();
			cls.Multiply (7);
			Assert.AreEqual (5, cls.Number, "#1");
			Assert.AreEqual (30, ((MultiplierClass)cls).Number, "#2");
			cls.CallMultiply (2);
			Assert.AreEqual (5, cls.Number, "#3");
			Assert.AreEqual (300, ((MultiplierClass)cls).Number, "#4");
		}

		[Test]
		public void TestRoundtripManagedOverride ()
		{
			var managed = new ManagedOverride1 ();
			var roundtripper = new ClassThatRoundtrips (managed);
			var cls = roundtripper.GetIt ();
			Assert.AreEqual (25, cls.Number, "#1");
			Assert.AreEqual (-25, cls.NegativeNumber, "#2");
			Assert.IsNotNull (cls as ManagedOverride1);
		}

	}
}

