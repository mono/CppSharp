using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;

namespace CppSharp.Generators
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

        public Block Parent { get; set; }
        public List<Block> Blocks { get; set; }

        public Declaration Declaration { get; set; }

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
                if (block.Kind ==  kind)
                    yield return block;

                foreach (var childBlock in block.FindBlocks(kind))
                    yield return childBlock;
            }
        }

        public virtual string Generate(DriverOptions options)
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
                var childText = childBlock.Generate(options);

                var nextBlock = (++blockIndex < Blocks.Count)
                    ? Blocks[blockIndex]
                    : null;

                var skipBlock = false;
                if (nextBlock != null)
                {
                    var nextText = nextBlock.Generate(options);
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


                    if (childBlock.Kind == BlockKind.BlockComment &&
                        !line.StartsWith(options.CommentPrefix))
                    {
                        builder.Append(options.CommentPrefix);
                        builder.Append(' ');
                    }
                    builder.Append(line);

                    if (!line.EndsWith(Environment.NewLine))
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

    public abstract class Template : ITextGenerator
    {
        public Driver Driver { get; private set; }
        public DriverOptions Options { get; private set; }
        public List<TranslationUnit> TranslationUnits { get; private set; }

        public TranslationUnit TranslationUnit { get { return TranslationUnits[0]; } }

        public IDiagnosticConsumer Log
        {
            get { return Driver.Diagnostics; }
        }

        public Block RootBlock { get; private set; }
        public Block ActiveBlock { get; private set; }

        public abstract string FileExtension { get; }

        protected Template(Driver driver, IEnumerable<TranslationUnit> units)
        {
            Driver = driver;
            Options = driver.Options;
            TranslationUnits = new List<TranslationUnit>(units);
            RootBlock = new Block();
            ActiveBlock = RootBlock;
        }

        public abstract void Process();

        public string Generate()
        {
            return RootBlock.Generate(Options);
        }

        #region Block helpers

        public void AddBlock(Block block)
        {
            ActiveBlock.AddBlock(block);
        }

        public void PushBlock(int kind, Declaration decl = null)
        {
            var block = new Block { Kind = kind, Declaration = decl };
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
