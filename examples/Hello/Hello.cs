using System;
using CppCsBind;

public class HelloExample
{
	public static void Main (String[] args) {
		var h = new Hello();
		h.PrintHello ("Hello world");
        Console.WriteLine("True =" + h.test1(3, 3.0f).ToString());
        Console.WriteLine("False =" + h.test1(2, 3.0f).ToString());
	}
}

	