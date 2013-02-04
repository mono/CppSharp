using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cxxi.Generators
{
    public abstract class TextTemplate
    {
        private const uint DefaultIndent = 4;
        private const uint MaxIndent = 80;

        public Generator Generator { get; set; }
        public DriverOptions DriverOptions { get; set; }
        public Library Library { get; set; }
        public ILibrary Transform;
        public TranslationUnit Module { get; set; }
        public abstract string FileExtension { get; }

        protected abstract void Generate();

        protected StringBuilder Builder;
        protected Stack<uint> CurrentIndent;

        private bool isStartOfLine;

        protected TextTemplate()
        {
            Builder = new StringBuilder();
            CurrentIndent = new Stack<uint>();
            isStartOfLine = true;
        }

        public void Write(string msg, params object[] args)
        {
            if (isStartOfLine)
                Builder.Append(new string(' ', (int)CurrentIndent.Sum(u => u)));

            if (args.Length > 0)
                msg = string.Format(msg, args);

            if (msg.Length > 0)
                isStartOfLine = false;

            Builder.Append(msg);
        }

        public void WriteLine(string msg, params object[] args)
        {
            Write(msg, args);
            NewLine();
        }

        public void NewLine()
        {
            Builder.AppendLine(string.Empty);
            isStartOfLine = true;
        }

        public void PushIndent(uint indent = DefaultIndent)
        {
            CurrentIndent.Push(indent);
        }

        public void PopIndent()
        {
            CurrentIndent.Pop();
        }

        public override string ToString()
        {
            Builder.Clear();
            Generate();
            return Builder.ToString();
        }
    }
}