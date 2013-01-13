using System;
using CppCsBind;

public class HelloExample
{
	public static void Main (String[] args) {
		var h = new Hello();
		h.PrintHello ("Hello world");
	}
}

	