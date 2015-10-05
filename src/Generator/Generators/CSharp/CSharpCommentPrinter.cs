using System.Text;
using System.Web.Util;
using CppSharp.AST;

namespace CppSharp.Generators.CSharp
{
    public static class CSharpCommentPrinter
    {
        public static string CommentToString(this Comment comment)
        {
            var summaryAdded = false;
            var remarksAdded = false;
            return CommentToString(comment, ref summaryAdded, ref remarksAdded).ToString();
        }

        private static StringBuilder CommentToString(Comment comment,
            ref bool summaryAdded, ref bool remarksAdded)
        {
            var commentBuilder = new StringBuilder();
            switch (comment.Kind)
            {
                case CommentKind.FullComment:
                    var fullComment = (FullComment) comment;
                    foreach (var block in fullComment.Blocks)
                        commentBuilder.Append(CommentToString(block,
                            ref summaryAdded, ref remarksAdded));
                    if (remarksAdded)
                        commentBuilder.Append("</remarks>");
                    break;
                case CommentKind.BlockCommandComment:
                    break;
                case CommentKind.ParamCommandComment:
                    break;
                case CommentKind.TParamCommandComment:
                    break;
                case CommentKind.VerbatimBlockComment:
                    break;
                case CommentKind.VerbatimLineComment:
                    break;
                case CommentKind.ParagraphComment:
                    var paragraphComment = (ParagraphComment) comment;
                    foreach (var inlineContentComment in paragraphComment.Content)
                        commentBuilder.Append(CommentToString(inlineContentComment,
                            ref summaryAdded, ref remarksAdded));
                    break;
                case CommentKind.HTMLTagComment:
                    break;
                case CommentKind.HTMLStartTagComment:
                    break;
                case CommentKind.HTMLEndTagComment:
                    break;
                case CommentKind.TextComment:
                    if (!summaryAdded)
                        commentBuilder.AppendLine("<summary>");
                    if (summaryAdded && !remarksAdded)
                    {
                        commentBuilder.AppendLine("<remarks>");
                        remarksAdded = true;
                    }
                    commentBuilder.Append("<para>" + GetText(comment) + "</para>").AppendLine();
                    if (!summaryAdded)
                    {
                        commentBuilder.AppendLine("</summary>");
                        summaryAdded = true;
                    }
                    break;
                case CommentKind.InlineContentComment:
                    break;
                case CommentKind.InlineCommandComment:
                    break;
                case CommentKind.VerbatimBlockLineComment:
                    break;
            }
            return commentBuilder;
        }

        private static string GetText(Comment comment)
        {
            var textComment = ((TextComment) comment);
            var text = textComment.Text;
            return HtmlEncoder.HtmlEncode(
                text.Length > 1 && text[0] == ' ' && text[1] != ' ' ? text.Substring(1) : text);
        }
    }
}
