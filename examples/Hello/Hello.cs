using System;
using Mono.VisualC.Interop;

public class HelloExample
{
	public static void Main (String[] args) {
		var h = new Hello.Hello ();
		h.PrintHello ();
	}
}

	