using System;
using CppTests;

public class Tests
{
	public static void Main (String[] args) {
		TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_class_return () {
		// Section 3.1.4:
		// Classes with non-default copy ctors/destructors are returned using a hidden
		// argument
		var c = ClassWithCopyCtor.Return (42);
		if (c.GetX () != 42)
			return 1;

		var c2 = ClassWithDtor.Return (43);
		if (c2.GetX () != 43)
			return 2;

		// This class is returned normally
		var c3 = ClassWithoutCopyCtor.Return (44);
		if (c3.GetX () != 44)
			return 3;
		return 0;
	}

	// An object as ref argument
	public static int test_0_class_arg () {
		var c1 = new Class (4);
		var c2 = new Class (5);

		c1.CopyTo (c2);
		return c2.GetX () == 4 ? 0 : 1;
	}

	// A null object as ref argument
	public static int test_0_class_arg_null () {
		var c1 = new Class (4);

		return c1.IsNull (null) ? 0 : 1;
	}

	// An object as byval argument
	public static int test_0_class_arg_byval () {
		var c1 = new Class (4);
		var c2 = new Class (5);

		c1.CopyFromValue (c2);
		return c1.GetX () == 5 ? 0 : 1;
	}

	// A null object as byval argument
	public static int test_0_class_arg_byval_null () {
		var c1 = new Class (4);

		try {
			c1.CopyFromValue (null);
		} catch (ArgumentException) {
			return 0;
		}
		return 1;
	}

}
