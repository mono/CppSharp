using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CppSharp
{
    /// <summary>
    /// Represents the kind of the diagnostic.
    /// </summary>
    public enum DiagnosticKind
    {
        Debug,
        Message,
        Warning,
        Error
    }

    /// <summary>
    /// Keeps information related to a single diagnostic.
    /// </summary>
    public struct DiagnosticInfo
    {
        public DiagnosticKind Kind;
        public string Message;
        public string File;
        public int Line;
        public int Column;
    }

    public interface IDiagnostics
    {
        DiagnosticKind Level { get; set; }
        void Emit(DiagnosticInfo info);
        void PushIndent(int level = 4);
        void PopIndent();
    }

    public static class Diagnostics
    {
        public static IDiagnostics Implementation { get; set; } = new ConsoleDiagnostics();

        public static DiagnosticKind Level
        {
            get { return Implementation.Level; }
            set { Implementation.Level = value; }
        }

        public static void PushIndent(int level = 4)
        {
            Implementation.PushIndent(level);
        }

        public static void PopIndent()
        {
            Implementation.PopIndent();
        }

        public static void Debug(string msg, params object[] args)
        {
            var diagInfo = new DiagnosticInfo
            {
                Kind = DiagnosticKind.Debug,
                Message = args.Any() ? string.Format(msg, args) : msg
            };

            Implementation.Emit(diagInfo);
        }

        public static void Message(string msg, params object[] args)
        {
            var diagInfo = new DiagnosticInfo
            {
                Kind = DiagnosticKind.Message,
                Message = args.Any() ? string.Format(msg, args) : msg
            };

            Implementation.Emit(diagInfo);
        }

        public static void Warning(string msg, params object[] args)
        {
            var diagInfo = new DiagnosticInfo
            {
                Kind = DiagnosticKind.Warning,
                Message = args.Any() ? string.Format(msg, args) : msg
            };

            Implementation.Emit(diagInfo);
        }

        public static void Error(string msg, params object[] args)
        {
            var diagInfo = new DiagnosticInfo
            {
                Kind = DiagnosticKind.Error,
                Message = args.Any() ? string.Format(msg, args) : msg
            };

            Implementation.Emit(diagInfo);
        }
    }

    public class ConsoleDiagnostics : IDiagnostics
    {
        public Stack<int> Indents;
        public DiagnosticKind Level { get; set; }

        public ConsoleDiagnostics()
        {
            Indents = new Stack<int>();
            Level = DiagnosticKind.Message;
        }

        public void Emit(DiagnosticInfo info)
        {
            if (info.Kind < Level)
                return;

            var currentIndent = Indents.Sum();
            var message = new string(' ', currentIndent) + info.Message;

            Console.WriteLine(message);
            Debug.WriteLine(message);
        }

        public void PushIndent(int level)
        {
            Indents.Push(level);
        }

        public void PopIndent()
        {
            Indents.Pop();
        }
    }
}
