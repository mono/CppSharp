using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class CleanCommentsPass : TranslationUnitPass, ICommentVisitor<bool>
    {
        public CleanCommentsPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassBases | VisitFlags.FunctionReturnType |
            VisitFlags.TemplateArguments);

        public bool VisitBlockCommand(BlockCommandComment comment) => true;

        public override bool VisitParameterDecl(Parameter parameter) =>
            base.VisitDeclaration(parameter);

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            if (decl.Comment != null)
            {
                var fullComment = decl.Comment.FullComment;
                VisitFull(fullComment);

            }
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

        public bool VisitParagraphCommand(ParagraphComment comment)
        {
            for (int i = 0; i < comment.Content.Count; i++)
            {
                if (comment.Content[i].Kind == DocumentationCommentKind.InlineCommandComment &&
                    i + 1 < comment.Content.Count &&
                    comment.Content[i + 1].Kind == DocumentationCommentKind.TextComment)
                {
                    var textComment = (TextComment)comment.Content[i + 1];
                    textComment.Text = Helpers.RegexCommentCommandLeftover.Replace(
                        textComment.Text, string.Empty);
                }
            }
            foreach (var item in comment.Content.Where(c => c.Kind == DocumentationCommentKind.TextComment))
            {
                var textComment = (TextComment)item;

                if (textComment.Text.StartsWith("<", StringComparison.Ordinal))
                    textComment.Text = $"{textComment.Text}>";
                else if (textComment.Text.StartsWith(">", StringComparison.Ordinal))
                    textComment.Text = textComment.Text.Substring(1);
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
