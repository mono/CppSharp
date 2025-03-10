using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class CleanCommentsPass : TranslationUnitPass, ICommentVisitor<bool>
    {
        public bool VisitBlockCommand(BlockCommandComment comment) => true;

        public override bool VisitParameterDecl(Parameter parameter) =>
            base.VisitDeclaration(parameter);

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            if (decl.Comment == null)
                return true;

            var fullComment = decl.Comment.FullComment;
            VisitFull(fullComment);
            return true;
        }

        public bool VisitFull(FullComment comment)
        {
            foreach (var block in comment.Blocks)
                block.Visit(this);

            return true;
        }

        #region Comments Visit
        public bool VisitHTMLEndTag(HTMLEndTagComment comment) => true;

        public bool VisitHTMLStartTag(HTMLStartTagComment comment) => true;

        public bool VisitInlineCommand(InlineCommandComment comment) => true;

        public bool VisitParagraph(ParagraphComment comment)
        {
            // Fix clang parsing html tags as TextComment's
            var textComments = comment.Content
                .Where(c => c.Kind == DocumentationCommentKind.TextComment)
                .Cast<TextComment>()
                .ToArray();

            for (var i = 0; i < textComments.Length; ++i)
            {
                TextComment textComment = textComments[i];
                if (textComment.IsEmpty)
                {
                    comment.Content.Remove(textComment);
                    continue;
                }

                if (!textComment.Text.StartsWith('<'))
                    continue;

                if (textComment.Text.Length < 2 || i + 1 >= textComments.Length)
                    continue;

                bool isEndTag = textComment.Text[1] == '/';

                // Replace the TextComment node with a HTMLTagComment node
                HTMLTagComment htmlTag = isEndTag ? new HTMLEndTagComment() : new HTMLStartTagComment();
                htmlTag.TagName = textComment.Text[(1 + (isEndTag ? 1 : 0))..];

                // Cleanup next element
                TextComment next = textComments[i + 1];
                int tagEnd = next.Text.IndexOf('>');
                if (tagEnd == -1)
                    continue;

                if (!isEndTag)
                {
                    var startTag = (htmlTag as HTMLStartTagComment)!;
                    var tagRemains = next.Text[..tagEnd];

                    if (tagRemains.EndsWith('/'))
                    {
                        startTag.SelfClosing = true;
                        tagRemains = tagRemains[..^1];
                    }

                    var attributes = tagRemains.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var attribute in attributes)
                    {
                        var args = attribute.Split('=');
                        startTag.Attributes.Add(new HTMLStartTagComment.Attribute
                        {
                            Name = args[0].Trim(),
                            Value = args.ElementAtOrDefault(1)?.Trim(' ', '"'),
                        });
                    }
                }

                // Strip tagRemains from next element
                next.Text = next.Text[(tagEnd + 1)..];

                if (string.IsNullOrEmpty(next.Text))
                {
                    htmlTag.HasTrailingNewline = next.HasTrailingNewline;
                    comment.Content.Remove(next);
                }

                //  Replace element
                var insertPos = comment.Content.IndexOf(textComment);
                comment.Content.RemoveAt(insertPos);
                comment.Content.Insert(insertPos, htmlTag);
            }

            for (int i = 0; i < comment.Content.Count; i++)
            {
                if (comment.Content[i].Kind != DocumentationCommentKind.InlineCommandComment)
                    continue;

                if (i + 1 >= comment.Content.Count)
                    continue;

                if (comment.Content[i + 1].Kind != DocumentationCommentKind.TextComment)
                    continue;


                var textComment = (TextComment)comment.Content[i + 1];
                textComment.Text = Helpers.RegexCommentCommandLeftover.Replace(
                    textComment.Text, string.Empty);
            }
            return true;
        }

        public bool VisitParamCommand(ParamCommandComment comment) => true;

        public bool VisitText(TextComment comment) => true;

        public bool VisitTParamCommand(TParamCommandComment comment) => true;

        public bool VisitVerbatimBlock(VerbatimBlockComment comment) => true;

        public bool VisitVerbatimBlockLine(VerbatimBlockLineComment comment) => true;

        public bool VisitVerbatimLine(VerbatimLineComment comment) => true;
        #endregion
    }
}
