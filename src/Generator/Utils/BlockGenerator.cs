using System;
using System.Collections.Generic;
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

    public class BlockKind
    {
        public const int Unknown = 0;
        public const int BlockComment = 1;
        public const int InlineComment = 2;
        public const int Header = 3;
        public const int Footer = 4;
        public const int LAST = 5;
    }

    public class Block : ITextGenerator
    {
        public TextGenerator Text { get; set; }
        public int Kind { get; set; }
        public NewLineKind NewLineKind { get; set; }

        public object Object { get; set; }

        public Block Parent { get; set; }
        public List<Block> Blocks { get; set; }

        private bool hasIndentChanged;
        private bool isSubBlock;

        public Func<bool> CheckGenerate;

        public Block() : this(BlockKind.Unknown)
        {

        }

        public Block(int kind)
        {
            Kind = kind;
            Blocks = new List<Block>();
            Text = new TextGenerator();
            hasIndentChanged = false;
            isSubBlock = false;
        }

        public void AddBlock(Block block)
        {
            if (Text.StringBuilder.Length != 0 || hasIndentChanged)
            {
                hasIndentChanged = false;
                var newBlock = new Block { Text = Text.Clone(), isSubBlock = true };
                Text.StringBuilder.Clear();

                AddBlock(newBlock);
            }

            block.Parent = this;
            Blocks.Add(block);
        }

        public IEnumerable<Block> FindBlocks(int kind)
        {
            foreach (var block in Blocks)
            {
                if (block.Kind == kind)
                    yield return block;

                foreach (var childBlock in block.FindBlocks(kind))
                    yield return childBlock;
            }
        }

        public virtual string Generate()
        {
            if (CheckGenerate != null && !CheckGenerate())
                return "";

            if (Blocks.Count == 0)
                return Text.ToString();

            var builder = new StringBuilder();
            uint totalIndent = 0;
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
                    if (string.IsNullOrEmpty(nextText) &&
                        childBlock.NewLineKind == NewLineKind.IfNotEmpty)
                        skipBlock = true;
                }

                if (skipBlock)
                    continue;

                if (string.IsNullOrEmpty(childText))
                    continue;

                var lines = childText.SplitAndKeep(Environment.NewLine).ToList();

                if (previousBlock != null &&
                    previousBlock.NewLineKind == NewLineKind.BeforeNextBlock)
                    builder.AppendLine();

                if (childBlock.isSubBlock)
                    totalIndent = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    if (!string.IsNullOrWhiteSpace(line))
                        builder.Append(new string(' ', (int)totalIndent));

                    builder.Append(line);

                    if (!line.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                        builder.AppendLine();
                }

                if (childBlock.NewLineKind == NewLineKind.Always)
                    builder.AppendLine();

                totalIndent += childBlock.Text.Indent;

                previousBlock = childBlock;
            }

            if (Text.StringBuilder.Length != 0)
                builder.Append(Text.StringBuilder);

            return builder.ToString();
        }

        public StringBuilder GenerateUnformatted()
        {
            if (CheckGenerate != null && !CheckGenerate())
                return new StringBuilder(0);

            if (Blocks.Count == 0)
                return Text.StringBuilder;

            var builder = new StringBuilder();
            Block previousBlock = null;

            var blockIndex = 0;
            foreach (var childBlock in Blocks)
            {
                var childText = childBlock.GenerateUnformatted();

                var nextBlock = (++blockIndex < Blocks.Count)
                    ? Blocks[blockIndex]
                    : null;

                if (nextBlock != null)
                {
                    var nextText = nextBlock.GenerateUnformatted();
                    if (nextText.Length == 0 &&
                        childBlock.NewLineKind == NewLineKind.IfNotEmpty)
                        continue;
                }

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

        public uint Indent { get { return Text.Indent; } }

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

        public void PushIndent(uint indent = 4u)
        {
            hasIndentChanged = true;
            Text.PushIndent(indent);
        }

        public void PopIndent()
        {
            hasIndentChanged = true;
            Text.PopIndent();
        }

        public void WriteStartBraceIndent()
        {
            Text.WriteStartBraceIndent();
        }

        public void WriteCloseBraceIndent()
        {
            Text.WriteCloseBraceIndent();
        }

        #endregion
    }

    public abstract class BlockGenerator : ITextGenerator
    {
        public Block RootBlock { get; private set; }
        public Block ActiveBlock { get; private set; }

        protected BlockGenerator()
        {
            RootBlock = new Block();
            ActiveBlock = RootBlock;
        }

        public virtual string Generate()
        {
            return RootBlock.Generate();
        }

        public string GenerateUnformatted()
        {
            return RootBlock.GenerateUnformatted().ToString();
        }

        #region Block helpers

        public void AddBlock(Block block)
        {
            ActiveBlock.AddBlock(block);
        }

        public void PushBlock(int kind = 0, object obj = null)
        {
            var block = new Block { Kind = kind, Object = obj };
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

        public IEnumerable<Block> FindBlocks(int kind)
        {
            return RootBlock.FindBlocks(kind);
        }

        public Block FindBlock(int kind)
        {
            return FindBlocks(kind).SingleOrDefault();
        }

        #endregion

        #region ITextGenerator implementation

        public uint Indent { get { return ActiveBlock.Indent; } }

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

        public void PushIndent(uint indent = 4u)
        {
            ActiveBlock.PushIndent(indent);
        }

        public void PopIndent()
        {
            ActiveBlock.PopIndent();
        }

        public void WriteStartBraceIndent()
        {
            ActiveBlock.WriteStartBraceIndent();
        }

        public void WriteCloseBraceIndent()
        {
            ActiveBlock.WriteCloseBraceIndent();
        }

        #endregion
    }
}
