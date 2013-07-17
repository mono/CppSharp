using System.Collections.Generic;
using System.Text;
using CppSharp.AST;

namespace CppSharp.Generators
{
    public abstract class TextTemplate : TextGenerator
    {
        public Driver Driver { get; private set; }
        public DriverOptions Options { get; private set; }
        public Library Library { get; private set; }
        public TranslationUnit TranslationUnit { get; private set; }

        protected TextTemplate(Driver driver, TranslationUnit unit)
        {
            Driver = driver;
            Options = driver.Options;
            Library = driver.Library;
            TranslationUnit = unit;
        }

        public abstract string FileExtension { get; }

        public abstract void GenerateBlocks();

        public virtual string GenerateText()
        {
            return base.ToString();
        }
    }

    /// <summary>
    /// Represents a (nestable) block of text with a specific kind.
    /// </summary>
    public interface IBlock<TBlock, TKind>
    {
        TKind Kind { get; set; }
        List<TBlock> Blocks { get; set; }
        TBlock Parent { get; set; }

        TextGenerator Text { get; set; }
        Declaration Declaration { get; set; }
    }

    /// <summary>
    /// Block generator used by language-specific generators to generate
    /// their output.
    /// </summary>
    public abstract class BlockGenerator<TKind, TBlock> : TextTemplate
        where TBlock : IBlock<TBlock, TKind>, new()
    {
        public struct BlockData
        {
            public TBlock Block;
            public int StartPosition;
        }

        public List<TBlock> Blocks { get; private set; }
        protected readonly Stack<BlockData> CurrentBlocks;

        protected BlockGenerator(Driver driver, TranslationUnit unit)
            : base(driver, unit)
        {
            Blocks = new List<TBlock>();
            CurrentBlocks = new Stack<BlockData>();
        }

        public void PushBlock(TKind kind)
        {
            var data = new BlockData
            {
                Block = new TBlock { Kind = kind },
                StartPosition = StringBuilder.Length
            };

            CurrentBlocks.Push(data);
        }

        public void PopBlock()
        {
            var data = CurrentBlocks.Pop();
            var block = data.Block;

            var text = StringBuilder.ToString().Substring(data.StartPosition);
            block.Text = Clone();
            block.Text.StringBuilder = new StringBuilder(text);

            var hasParentBlock = CurrentBlocks.Count > 0;
            if (hasParentBlock)
                CurrentBlocks.Peek().Block.Blocks.Add(block);
            else
                Blocks.Add(block);
        }

        public TBlock FindBlock(TKind kind)
        {
            //foreach (var block in Blocks)
            //    if (block.Kind == kind)
            //        return block;
            return default(TBlock);
        }

        public override string GenerateText()
        {
            var generator = new TextGenerator();

            foreach (var block in Blocks)
                GenerateBlock(block, generator);

            return generator.ToString();
        }

        void GenerateBlock(TBlock block, TextGenerator gen)
        {
            if (block.Blocks.Count == 0)
                gen.Write(block.Text);

            foreach (var childBlock in block.Blocks)
                GenerateBlock(childBlock, gen);
        }
    }
}