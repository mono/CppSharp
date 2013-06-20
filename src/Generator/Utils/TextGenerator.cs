using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CppSharp
{
    public class TextGenerator
    {
        private const uint DefaultIndent = 4;
        private const uint MaxIndent = 80;

        private readonly StringBuilder sb;
        private bool isStartOfLine;
        private bool needsNewLine;

        protected readonly Stack<uint> CurrentIndent;

        public TextGenerator()
        {
            sb = new StringBuilder();
            isStartOfLine = false;
            CurrentIndent = new Stack<uint>();
        }

        public void Write(string msg, params object[] args)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            if (args.Length > 0)
                msg = string.Format(msg, args);

            foreach(var line in msg.SplitAndKeep(Environment.NewLine))
            {
                if (isStartOfLine && !string.IsNullOrWhiteSpace(line))
                    sb.Append(new string(' ', (int) CurrentIndent.Sum(u => u)));

                if (line.Length > 0)
                    isStartOfLine = line.EndsWith(Environment.NewLine);

                sb.Append(line);
            }
        }

        public void WriteLine(string msg, params object[] args)
        {
            Write(msg, args);
            NewLine();
        }

        public void WriteLineIndent(string msg, params object[] args)
        {
            PushIndent();
            WriteLine(msg, args);
            PopIndent();
        }

        public void NewLine()
        {
            sb.AppendLine(string.Empty);
            isStartOfLine = true;
        }

        public void NewLineIfNeeded()
        {
            if (!needsNewLine) return;

            NewLine();
            needsNewLine = false;
        }

        public void NeedNewLine()
        {
            needsNewLine = true;
        }

        public void ResetNewLine()
        {
            needsNewLine = false;
        }

        public void PushIndent(uint indent = DefaultIndent)
        {
            CurrentIndent.Push(indent);
        }

        public void PopIndent()
        {
            CurrentIndent.Pop();
        }

        public void WriteStartBraceIndent()
        {
            WriteLine("{");
            PushIndent();
        }

        public void WriteCloseBraceIndent()
        {
            PopIndent();
            WriteLine("}");
        }

        public override string ToString()
        {
            return sb.ToString();
        }

        public static implicit operator string(TextGenerator tg)
        {
            return tg.ToString();
        }
    }
}
