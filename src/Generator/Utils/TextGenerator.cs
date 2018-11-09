using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CppSharp
{
    public interface ITextGenerator
    {
        void Write(string msg, params object[] args);
        void WriteLine(string msg, params object[] args);
        void WriteLineIndent(string msg, params object[] args);
        void NewLine();
        void NewLineIfNeeded();
        void NeedNewLine();
        void ResetNewLine();
        void PushIndent(uint indent = TextGenerator.DefaultIndent);
        void PopIndent();
        void WriteStartBraceIndent();
        void WriteCloseBraceIndent();
    }

    public class TextGenerator : ITextGenerator
    {
        public const uint DefaultIndent = 4;

        public StringBuilder StringBuilder = new StringBuilder();
        public bool IsStartOfLine { get; set; }
        public bool NeedsNewLine { get; set; }
        public Stack<uint> CurrentIndent { get; } = new Stack<uint>();

        public TextGenerator()
        {
        }

        public TextGenerator(TextGenerator generator)
        {
            StringBuilder = new StringBuilder(generator);
            IsStartOfLine = generator.IsStartOfLine;
            NeedsNewLine = generator.NeedsNewLine;
            CurrentIndent = new Stack<uint>(generator.CurrentIndent);
        }

        public TextGenerator Clone()
        {
            return new TextGenerator(this);
        }

        public void Write(string msg, params object[] args)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            if (args.Length > 0)
                msg = string.Format(msg, args);

            if (IsStartOfLine && !string.IsNullOrWhiteSpace(msg))
                StringBuilder.Append(new string(' ', (int) CurrentIndent.Sum(u => u)));

            if (msg.Length > 0)
                IsStartOfLine = msg.EndsWith(Environment.NewLine);

            StringBuilder.Append(msg);
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
            StringBuilder.AppendLine(string.Empty);
            IsStartOfLine = true;
        }

        public void NewLineIfNeeded()
        {
            if (!NeedsNewLine) return;

            NewLine();
            NeedsNewLine = false;
        }

        public void NeedNewLine()
        {
            NeedsNewLine = true;
        }

        public void ResetNewLine()
        {
            NeedsNewLine = false;
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
            return StringBuilder.ToString();
        }

        public static implicit operator string(TextGenerator tg)
        {
            return tg.ToString();
        }
    }
}
