using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Util;
using CppSharp.AST;

namespace CppSharp.Generators.CSharp
{
    public static class CSharpCommentPrinter
    {
        public static string CommentToString(this Comment comment, CommentKind kind)
        {
            var sections = new List<Section> { new Section(CommentElement.Summary) };
            GetCommentSections(comment, sections);
            foreach (var section in sections)
                TrimSection(section);
            return FormatComment(sections, kind);
        }

        private static void GetCommentSections(this Comment comment, List<Section> sections)
        {
            switch (comment.Kind)
            {
                case DocumentationCommentKind.FullComment:
                    var fullComment = (FullComment) comment;
                    foreach (var block in fullComment.Blocks)
                        block.GetCommentSections(sections);
                    break;
                case DocumentationCommentKind.BlockCommandComment:
                    var blockCommandComment = (BlockCommandComment) comment;
                    if (blockCommandComment.CommandKind == CommentCommandKind.Return &&
                        blockCommandComment.ParagraphComment != null)
                    {
                        sections.Add(new Section(CommentElement.Returns));
                        blockCommandComment.ParagraphComment.GetCommentSections(sections);
                    }
                    break;
                case DocumentationCommentKind.ParamCommandComment:
                    var paramCommandComment = (ParamCommandComment) comment;
                    var param = new Section(CommentElement.Param);
                    sections.Add(param);
                    if (paramCommandComment.Arguments.Count > 0)
                        param.Attributes.Add(
                            string.Format("name=\"{0}\"", paramCommandComment.Arguments[0].Text));
                    if (paramCommandComment.ParagraphComment != null)
                        foreach (var inlineContentComment in paramCommandComment.ParagraphComment.Content)
                            inlineContentComment.GetCommentSections(sections);
                    break;
                case DocumentationCommentKind.TParamCommandComment:
                    break;
                case DocumentationCommentKind.VerbatimBlockComment:
                    break;
                case DocumentationCommentKind.VerbatimLineComment:
                    break;
                case DocumentationCommentKind.ParagraphComment:
                    var summaryParagraph = sections.Count == 1;
                    var paragraphComment = (ParagraphComment) comment;
                    foreach (var inlineContentComment in paragraphComment.Content)
                        inlineContentComment.GetCommentSections(sections);
                    if (summaryParagraph)
                    {
                        sections[0].Lines.AddRange(sections.Skip(1).SelectMany(s => s.Lines));
                        sections.RemoveRange(1, sections.Count - 1);
                        sections.Add(new Section(CommentElement.Summary));
                    }
                    break;
                case DocumentationCommentKind.HTMLTagComment:
                    break;
                case DocumentationCommentKind.HTMLStartTagComment:
                    break;
                case DocumentationCommentKind.HTMLEndTagComment:
                    break;
                case DocumentationCommentKind.TextComment:
                    var section = sections.Last();
                    section.Lines.Add(GetText(comment,
                        section.Type == CommentElement.Returns || section.Type == CommentElement.Param));
                    if (sections.Count == 1)
                        sections.Add(new Section(CommentElement.Remarks));
                    break;
                case DocumentationCommentKind.InlineContentComment:
                    break;
                case DocumentationCommentKind.InlineCommandComment:
                    break;
                case DocumentationCommentKind.VerbatimBlockLineComment:
                    break;
            }
        }

        private static string GetText(Comment comment, bool trim = false)
        {
            var textComment = ((TextComment) comment);
            var text = textComment.Text;
            if (trim)
                text = text.Trim();

            if (Helpers.RegexTag.IsMatch(text))
                return String.Empty;

            return HtmlEncoder.HtmlEncode(
                text.Length > 1 && text[0] == ' ' && text[1] != ' ' ? text.Substring(1) : text);
        }

        private static void TrimSection(Section section)
        {
            for (int i = 0; i < section.Lines.Count - 1; i++)
            {
                if (string.IsNullOrWhiteSpace(section.Lines[i]))
                    section.Lines.RemoveAt(i--);
                else
                    break;
            }
            for (int i = section.Lines.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(section.Lines[i]))
                    section.Lines.RemoveAt(i);
                else
                    break;
            }
        }

        private static string FormatComment(List<Section> sections, CommentKind kind)
        {
            var commentPrefix = Comment.GetMultiLineCommentPrologue(kind);
        
            var commentBuilder = new StringBuilder();
            foreach (var section in sections.Where(s => s.Lines.Count > 0))
            {
                var tag = section.Type.ToString().ToLowerInvariant();
                var attributes = string.Empty;
                if (section.Attributes.Any())
                    attributes = ' ' + string.Join(" ", section.Attributes);
                commentBuilder.Append($"{commentPrefix} <{tag}{attributes}>");
                if (section.Lines.Count == 1)
                {
                    commentBuilder.Append(section.Lines[0]);
                }
                else
                {
                    commentBuilder.AppendLine();
                    foreach (var line in section.Lines)
                        commentBuilder.AppendLine($"{commentPrefix} <para>{line}</para>");
                    commentBuilder.Append($"{commentPrefix} ");
                }
                commentBuilder.AppendLine($"</{tag}>");
            }
            if (commentBuilder.Length > 0)
            {
                var newLineLength = Environment.NewLine.Length;
                commentBuilder.Remove(commentBuilder.Length - newLineLength, newLineLength);
            }
            return commentBuilder.ToString();
        }

        private class Section
        {
            public Section(CommentElement type)
            {
                Type = type;
            }

            public CommentElement Type { get; set; }

            public List<string> Attributes { get; } = new List<string>();

            public List<string> Lines { get; } = new List<string>();
        }

        private enum CommentElement
        {
            Summary,
            Remarks,
            Param,
            Returns
        }
    }
}
