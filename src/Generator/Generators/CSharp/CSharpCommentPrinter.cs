using System.Text;
using System.Web.Util;
using CppSharp.AST;

namespace CppSharp.Generators.CSharp
{
    public static class CSharpCommentPrinter
    {
        public static string CommentToString(this Comment comment, string commentPrefix)
        {
            var summaryAdded = false;
            var remarksAdded = false;
            return CommentToString(
                comment, ref summaryAdded, ref remarksAdded, commentPrefix).ToString();
        }

        private static StringBuilder CommentToString(Comment comment,
            ref bool summaryAdded, ref bool remarksAdded, string commentPrefix)
        {
            var commentBuilder = new StringBuilder();
            switch (comment.Kind)
            {
                case CommentKind.FullComment:
                    var fullComment = (FullComment) comment;
                    foreach (var block in fullComment.Blocks)
                        commentBuilder.Append(CommentToString(block,
                            ref summaryAdded, ref remarksAdded, commentPrefix));
                    if (remarksAdded)
                        commentBuilder.AppendFormat("{0} </remarks>", commentPrefix);
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
                            ref summaryAdded, ref remarksAdded, commentPrefix));
                    break;
                case CommentKind.HTMLTagComment:
                    break;
                case CommentKind.HTMLStartTagComment:
                    break;
                case CommentKind.HTMLEndTagComment:
                    break;
                case CommentKind.TextComment:
                    if (!summaryAdded)
                        commentBuilder.AppendFormat("{0} <summary>", commentPrefix).AppendLine();
                    if (summaryAdded && !remarksAdded)
                    {
                        commentBuilder.AppendFormat("{0} <remarks>", commentPrefix).AppendLine();
                        remarksAdded = true;
                    }
                    commentBuilder.AppendFormat(
                        "{0} <para>{1}</para>", commentPrefix, GetText(comment));
                    commentBuilder.AppendLine();
                    if (!summaryAdded)
                    {
                        commentBuilder.AppendFormat("{0} </summary>", commentPrefix).AppendLine();
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
