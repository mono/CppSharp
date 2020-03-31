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
        BlockComment,
        InlineComment,
        Header,
        Footer,
        Usings,
        Namespace,
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

            var blockIndex = 0;
            foreach (var childBlock in Blocks)
            {
                var childText = childBlock.Generate();

                var nextBlock = (++blockIndex < Blocks.Count)
                    ? Blocks[blockIndex]
                    : null;

                var skipBlock = false;
                if (nextBlock != null)
                {
                    var nextText = nextBlock.Generate();
                    if (nextText.Length == 0 &&
                        childBlock.NewLineKind == NewLineKind.IfNotEmpty)
                        skipBlock = true;
                }

                if (skipBlock)
                    continue;

                if (childText.Length == 0)
                    continue;

                if (previousBlock != null &&
                    previousBlock.NewLineKind == NewLineKind.BeforeNextBlock)
                    builder.AppendLine();

                builder.Append(childText);

                if (childBlock.NewLineKind == NewLineKind.Always)
                    builder.AppendLine();

                previousBlock = childBlock;
            }

            if (Text.StringBuilder.Length != 0)
                builder.Append(Text.StringBuilder);

            return builder;
        }

        public bool IsEmpty
        {
            get
            {
                if (Blocks.Any(block => !block.IsEmpty))
                    return false;

                return string.IsNullOrEmpty(Text.ToString());
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
            var block = new Block { Kind = kind, Object = obj };
            block.Text.CurrentIndentation = CurrentIndentation;
            block.Text.IsStartOfLine = ActiveBlock.Text.IsStartOfLine;
            block.Text.NeedsNewLine = ActiveBlock.Text.NeedsNewLine;
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
