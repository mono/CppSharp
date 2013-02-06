using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cxxi
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
            if (isStartOfLine)
                sb.Append(new string(' ', (int)CurrentIndent.Sum(u => u)));

            if (args.Length > 0)
                msg = string.Format(msg, args);

            if (msg.Length > 0)
                isStartOfLine = false;

            sb.Append(msg);
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
