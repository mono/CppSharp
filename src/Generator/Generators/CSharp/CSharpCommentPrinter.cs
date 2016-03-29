using System.Collections.Generic;
using System.Text;
using System.Web.Util;
using CppSharp.AST;

namespace CppSharp.Generators.CSharp
{
    public static class CSharpCommentPrinter
    {
        public static string CommentToString(this Comment comment, string commentPrefix)
        {
            int boundary = 0;
            var commentLines = GetCommentLines(comment, ref boundary);
            TrimSection(commentLines, 0, boundary);
            TrimSection(commentLines, boundary, commentLines.Count);
            return FormatComment(commentLines, boundary, commentPrefix);
        }

        private static List<string> GetCommentLines(Comment comment, ref int boundary)
        {
            var commentLines = new List<string>();
            switch (comment.Kind)
            {
                case CommentKind.FullComment:
                    var fullComment = (FullComment) comment;
                    foreach (var block in fullComment.Blocks)
                        commentLines.AddRange(GetCommentLines(block, ref boundary));
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
                    var summaryParagraph = boundary == 0;
                    var paragraphComment = (ParagraphComment) comment;
                    foreach (var inlineContentComment in paragraphComment.Content)
                        commentLines.AddRange(GetCommentLines(inlineContentComment, ref boundary));
                    if (summaryParagraph)
                        boundary = commentLines.Count;
                    break;
                case CommentKind.HTMLTagComment:
                    break;
                case CommentKind.HTMLStartTagComment:
                    break;
                case CommentKind.HTMLEndTagComment:
                    break;
                case CommentKind.TextComment:
                    commentLines.Add(GetText(comment));
                    if (boundary == 0)
                        boundary = commentLines.Count;
                    break;
                case CommentKind.InlineContentComment:
                    break;
                case CommentKind.InlineCommandComment:
                    break;
                case CommentKind.VerbatimBlockLineComment:
                    break;
            }
            return commentLines;
        }

        private static string GetText(Comment comment)
        {
            var textComment = ((TextComment) comment);
            var text = textComment.Text;
            return HtmlEncoder.HtmlEncode(
                text.Length > 1 && text[0] == ' ' && text[1] != ' ' ? text.Substring(1) : text);
        }

        private static void TrimSection(List<string> commentLines, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (string.IsNullOrWhiteSpace(commentLines[i]))
                    commentLines.RemoveAt(i--);
                else
                    break;
            }
            for (int i = end - 1; i >= start; i--)
            {
                if (string.IsNullOrWhiteSpace(commentLines[i]))
                    commentLines.RemoveAt(i);
                else
                    break;
            }
        }

        private static string FormatComment(List<string> commentLines, int boundary, string commentPrefix)
        {
            var commentBuilder = new StringBuilder();
            commentBuilder.AppendLine("<summary>");
            for (int i = 0; i < boundary; i++)
            {
                commentBuilder.AppendFormat("{0} <para>{1}</para>", commentPrefix, commentLines[i]);
                commentBuilder.AppendLine();
            }
            commentBuilder.Append("</summary>");
            if (boundary < commentLines.Count)
            {
                commentBuilder.AppendLine();
                commentBuilder.AppendLine("<remarks>");
                for (int i = boundary; i < commentLines.Count; i++)
                {
                    commentBuilder.AppendFormat("{0} <para>{1}</para>", commentPrefix, commentLines[i]);
                    commentBuilder.AppendLine();
                }
                commentBuilder.Append("</remarks>");
            }
            return commentBuilder.ToString();
        }
    }
}
