using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CppSharp
{
    public enum NewLineKind
    {
        Never,
        Always,
        BeforeNextBlock,
        IfNotEmpty
    }

    public enum BlockKind
    {
        Unknown,
        Block,
        BlockComment,
        InlineComment,
        Header,
        Footer,
        Usings,
        Namespace,
        TranslationUnit,
        Enum,
        EnumItem,
        Typedef,
        Class,
        InternalsClass,
        InternalsClassMethod,
        InternalsClassField,
        Functions,
        Function,
        Method,
        Event,
        Variable,
        Property,
        Field,
        VTableDelegate,
        Region,
        Interface,
        Finalizer,
        Includes,
        IncludesForwardReferences,
        ForwardReferences,
        MethodBody,
        FunctionsClass,
        Template,
        Destructor,
        AccessSpecifier,
        Fields,
        Constructor,
        ConstructorBody,
        DestructorBody,
        FinalizerBody
    }

    [DebuggerDisplay("{Kind} | {Object}")]
    public class Block : ITextGenerator
    {
        public TextGenerator Text { get; set; }
        public BlockKind Kind { get; set; }
        public NewLineKind NewLineKind { get; set; }

        public object Object { get; set; }

        public Block Parent { get; set; }
        public List<Block> Blocks { get; set; }

        private bool hasIndentChanged;

        public Func<bool> CheckGenerate;

        public Block() : this(BlockKind.Unknown)
        {

        }

        public Block(BlockKind kind)
        {
            Kind = kind;
            Blocks = new List<Block>();
            Text = new TextGenerator();
            hasIndentChanged = false;
        }

        public void AddBlock(Block block)
        {
            if (Text.StringBuilder.Length != 0 || hasIndentChanged)
            {
                hasIndentChanged = false;
                var newBlock = new Block { Text = Text.Clone() };
                Text.StringBuilder.Clear();

                AddBlock(newBlock);
            }

            block.Parent = this;
            Blocks.Add(block);
        }

        public IEnumerable<Block> FindBlocks(BlockKind kind)
        {
            foreach (var block in Blocks)
            {
                if (block.Kind == kind)
                    yield return block;

                foreach (var childBlock in block.FindBlocks(kind))
                    yield return childBlock;
            }
        }

        public virtual StringBuilder Generate()
        {
            if (CheckGenerate != null && !CheckGenerate())
                return new StringBuilder();

            if (Blocks.Count == 0)
                return Text.StringBuilder;

            var builder = new StringBuilder();
            Block previousBlock = null;
            var previousBlockEmpty = false;

            foreach (var childBlock in Blocks)
            {
                var childText = childBlock.Generate();
                if (childText.Length == 0)
                    continue;

                if (previousBlock != null &&
                    (previousBlock.NewLineKind == NewLineKind.BeforeNextBlock ||
                     (previousBlock.NewLineKind == NewLineKind.IfNotEmpty && !previousBlockEmpty)))
                    builder.AppendLine();

                builder.Append(childText);

                if (childBlock.NewLineKind == NewLineKind.Always)
                    builder.AppendLine();

                previousBlock = childBlock;
                previousBlockEmpty = childText.Length == 0;
            }

            if (Text.StringBuilder.Length != 0)
                builder.Append(Text.StringBuilder);

            return builder;
        }

        public bool IsEmpty
        {
            get
            {
                return Blocks.All(block => block.IsEmpty) && string.IsNullOrEmpty(Text.ToString());
            }
        }

        #region ITextGenerator implementation

        public void Write(string msg, params object[] args)
        {
            Text.Write(msg, args);
        }

        public void WriteLine(string msg, params object[] args)
        {
            Text.WriteLine(msg, args);
        }

        public void WriteLineIndent(string msg, params object[] args)
        {
            Text.WriteLineIndent(msg, args);
        }

        public void NewLine()
        {
            Text.NewLine();
        }

        public void NewLineIfNeeded()
        {
            Text.NewLineIfNeeded();
        }

        public void NeedNewLine()
        {
            Text.NeedNewLine();
        }

        public bool NeedsNewLine
        {
            get => Text.NeedsNewLine;
            set => Text.NeedsNewLine = value;
        }

        public void ResetNewLine()
        {
            Text.ResetNewLine();
        }

        public void Indent(uint indentation = 4u)
        {
            hasIndentChanged = true;
            Text.Indent(indentation);
        }

        public void Unindent()
        {
            hasIndentChanged = true;
            Text.Unindent();
        }

        public void WriteOpenBraceAndIndent()
        {
            Text.WriteOpenBraceAndIndent();
        }

        public void UnindentAndWriteCloseBrace()
        {
            Text.UnindentAndWriteCloseBrace();
        }

        #endregion
    }

    public abstract class BlockGenerator : ITextGenerator
    {
        private static string[] LineBreakSequences = new[] { "\r\n", "\r", "\n" };

        public Block RootBlock { get; }
        public Block ActiveBlock { get; private set; }
        public uint CurrentIndentation => ActiveBlock.Text.CurrentIndentation;

        protected BlockGenerator()
        {
            RootBlock = new Block();
            ActiveBlock = RootBlock;
        }

        public virtual string Generate()
        {
            return RootBlock.Generate().ToString();
        }

        #region Block helpers

        public void AddBlock(Block block)
        {
            ActiveBlock.AddBlock(block);
        }

        public void PushBlock(BlockKind kind = BlockKind.Unknown, object obj = null)
        {
            var block = new Block
            {
                Kind = kind,
                Object = obj,
                Text =
                {
                    CurrentIndentation = CurrentIndentation,
                    IsStartOfLine = ActiveBlock.Text.IsStartOfLine,
                    NeedsNewLine = ActiveBlock.Text.NeedsNewLine
                }
            };

            PushBlock(block);
        }

        public void PushBlock(Block block)
        {
            block.Parent = ActiveBlock;
            ActiveBlock.AddBlock(block);
            ActiveBlock = block;
        }

        public Block PopBlock(NewLineKind newLineKind = NewLineKind.Never)
        {
            var block = ActiveBlock;

            ActiveBlock.NewLineKind = newLineKind;
            ActiveBlock = ActiveBlock.Parent;

            return block;
        }

        public IEnumerable<Block> FindBlocks(BlockKind kind)
        {
            return RootBlock.FindBlocks(kind);
        }

        public Block FindBlock(BlockKind kind)
        {
            return FindBlocks(kind).SingleOrDefault();
        }

        internal PushedBlock PushWriteBlock(BlockKind kind, string msg, NewLineKind next)
        {
            PushBlock(kind);
            WriteLine(msg);
            WriteOpenBraceAndIndent();
            return new PushedBlock(this, next);
        }

        internal TextBlock WriteBlock(string msg)
        {
            WriteLine(msg);
            WriteOpenBraceAndIndent();
            return new TextBlock(this);
        }

        internal ref struct PushedBlock
        {
            private readonly BlockGenerator generator;
            private readonly NewLineKind next;

            public PushedBlock(BlockGenerator generator, NewLineKind next) 
            {
                this.generator = generator;
                this.next = next;
            }

            public void Dispose()
            {
                if (generator == null)
                    return;

                generator.UnindentAndWriteCloseBrace();
                generator.PopBlock(next);
            }
        }

        #endregion

        #region ITextGenerator implementation

        public void Write(string msg, params object[] args)
        {
            ActiveBlock.Write(msg, args);
        }

        public void WriteLine(string msg, params object[] args)
        {
            ActiveBlock.WriteLine(msg, args);
        }

        public void WriteLines(string msg, bool trimIndentation = false)
        {
            var lines = msg.Split(LineBreakSequences, StringSplitOptions.None);
            int indentation = int.MaxValue;

            if (trimIndentation)
            {
                foreach (var line in lines)
                {
                    for (int i = 0; i < line.Length; ++i)
                    {
                        if (char.IsWhiteSpace(line[i]))
                            continue;

                        if (i < indentation)
                        {
                            indentation = i;
                            break;
                        }
                    }
                }
            }

            bool foundNonEmptyLine = false;
            foreach (var line in lines)
            {
                if (!foundNonEmptyLine && string.IsNullOrEmpty(line))
                    continue;

                WriteLine(line.Length >= indentation ? line.Substring(indentation) : line);
                foundNonEmptyLine = true;
            }
        }

        public void WriteLineIndent(string msg, params object[] args)
        {
            ActiveBlock.WriteLineIndent(msg, args);
        }

        public void NewLine()
        {
            ActiveBlock.NewLine();
        }

        public void NewLineIfNeeded()
        {
            ActiveBlock.NewLineIfNeeded();
        }

        public void NeedNewLine()
        {
            ActiveBlock.NeedNewLine();
        }

        public bool NeedsNewLine
        {
            get => ActiveBlock.NeedsNewLine;
            set => ActiveBlock.NeedsNewLine = value;
        }

        public void ResetNewLine()
        {
            ActiveBlock.ResetNewLine();
        }

        public void Indent(uint indentation = 4u)
        {
            ActiveBlock.Indent(indentation);
        }

        public void Unindent()
        {
            ActiveBlock.Unindent();
        }

        public void WriteOpenBraceAndIndent()
        {
            ActiveBlock.WriteOpenBraceAndIndent();
        }

        public void UnindentAndWriteCloseBrace()
        {
            ActiveBlock.UnindentAndWriteCloseBrace();
        }

        #endregion
    }
}
