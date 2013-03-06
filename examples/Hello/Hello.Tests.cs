using System;

public class HelloTests
{
    public static void Main (String[] args)
    {
        var hello = new Hello();
        hello.PrintHello("Hello world");

        Console.WriteLine("True =" + hello.test1(3, 3.0f).ToString());
        Console.WriteLine("False =" + hello.test1(2, 3.0f).ToString());
    }
}
