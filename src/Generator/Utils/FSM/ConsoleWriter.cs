using System;

namespace CppSharp.Utils.FSM
{
    public class ConsoleWriter
    {
        public static void Failure(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Write(message);
        }

        public static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Write(message);
        }

        private static void Write(string message)
        {
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
