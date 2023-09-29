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
        public static void Print(this ITextGenerator textGenerator, Comment comment, CommentKind kind)
        {
            var sections = new List<Section> { new Section(CommentElement.Summary) };
            GetCommentSections(comment, sections);
            foreach (var section in sections)
                TrimSection(section);
            FormatComment(textGenerator, sections, kind);
        }

        private static void GetCommentSections(this Comment comment, List<Section> sections)
        {
            switch (comment.Kind)
            {
                case DocumentationCommentKind.FullComment:
                    var fullComment = (FullComment)comment;
                    foreach (var block in fullComment.Blocks)
                        block.GetCommentSections(sections);
                    break;
                case DocumentationCommentKind.BlockCommandComment:
                    var blockCommandComment = (BlockCommandComment)comment;
                    if (blockCommandComment.ParagraphComment == null)
                        break;
                    switch (blockCommandComment.CommandKind)
                    {
                        case CommentCommandKind.Brief:
                            blockCommandComment.ParagraphComment.GetCommentSections(sections);
                            break;
                        case CommentCommandKind.Return:
                        case CommentCommandKind.Returns:
                            sections.Add(new Section(CommentElement.Returns));
                            blockCommandComment.ParagraphComment.GetCommentSections(sections);
                            break;
                        case CommentCommandKind.Since:
                            var lastBlockSection = sections.Last();
                            foreach (var inlineContentComment in blockCommandComment.ParagraphComment.Content)
                            {
                                inlineContentComment.GetCommentSections(sections);
                                if (inlineContentComment.HasTrailingNewline)
                                    lastBlockSection.NewLine();
                            }
                            break;
                        default:
                            sections.Add(new Section(CommentElement.Remarks));
                            blockCommandComment.ParagraphComment.GetCommentSections(sections);
                            break;
                    }


                    break;
                case DocumentationCommentKind.ParamCommandComment:
                    var paramCommandComment = (ParamCommandComment)comment;
                    var param = new Section(CommentElement.Param);
                    sections.Add(param);
                    if (paramCommandComment.Arguments.Count > 0)
                        param.Attributes.Add(
                            string.Format("name=\"{0}\"", paramCommandComment.Arguments[0].Text));
                    if (paramCommandComment.ParagraphComment != null)
                        foreach (var inlineContentComment in paramCommandComment.ParagraphComment.Content)
                        {
                            inlineContentComment.GetCommentSections(sections);
                            if (inlineContentComment.HasTrailingNewline)
                                sections.Last().NewLine();
                        }
                    if (!string.IsNullOrEmpty(sections.Last().CurrentLine.ToString()))
                        sections.Add(new Section(CommentElement.Remarks));
                    break;
                case DocumentationCommentKind.TParamCommandComment:
                    break;
                case DocumentationCommentKind.VerbatimBlockComment:
                    break;
                case DocumentationCommentKind.VerbatimLineComment:
                    break;
                case DocumentationCommentKind.ParagraphComment:
                    var summaryParagraph = sections.Count == 1;
                    var paragraphComment = (ParagraphComment)comment;
                    var lastParagraphSection = sections.Last();
                    foreach (var inlineContentComment in paragraphComment.Content)
                    {
                        inlineContentComment.GetCommentSections(sections);
                        if (inlineContentComment.HasTrailingNewline)
                            lastParagraphSection.NewLine();
                    }

                    if (!string.IsNullOrEmpty(lastParagraphSection.CurrentLine.ToString()))
                        lastParagraphSection.NewLine();

                    if (sections[0].GetLines().Count > 0 && summaryParagraph)
                    {
                        sections[0].GetLines().AddRange(sections.Skip(1).SelectMany(s => s.GetLines()));
                        sections.RemoveRange(1, sections.Count - 1);
                        sections.Add(new Section(CommentElement.Remarks));
                    }
                    break;
                case DocumentationCommentKind.HTMLTagComment:
                    break;
                case DocumentationCommentKind.HTMLStartTagComment:
                    break;
                case DocumentationCommentKind.HTMLEndTagComment:
                    break;
                case DocumentationCommentKind.TextComment:
                    var lastTextsection = sections.Last();
                    lastTextsection.CurrentLine.Append(GetText(comment,
                        lastTextsection.Type == CommentElement.Returns ||
                        lastTextsection.Type == CommentElement.Param).Trim());
                    break;
                case DocumentationCommentKind.InlineContentComment:
                    break;
                case DocumentationCommentKind.InlineCommandComment:
                    var lastInlineSection = sections.Last();
                    var inlineCommand = (InlineCommandComment)comment;

                    if (inlineCommand.CommandKind == CommentCommandKind.B)
                    {
                        var argText = $" <c>{inlineCommand.Arguments[0].Text}</c> ";
                        lastInlineSection.CurrentLine.Append(argText);
                    }
                    break;
                case DocumentationCommentKind.VerbatimBlockLineComment:
                    break;
            }
        }

        private static string GetText(Comment comment, bool trim = false)
        {
            var textComment = ((TextComment)comment);
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
            var lines = section.GetLines();
            for (int i = 0; i < lines.Count - 1; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    lines.RemoveAt(i--);
                else
                    break;
            }
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    lines.RemoveAt(i);
                else
                    break;
            }
        }

        private static void FormatComment(ITextGenerator textGenerator, List<Section> sections, CommentKind kind)
        {
            var commentPrefix = Comment.GetMultiLineCommentPrologue(kind);

            sections.Sort((x, y) => x.Type.CompareTo(y.Type));
            var remarks = sections.Where(s => s.Type == CommentElement.Remarks).ToList();
            if (remarks.Any())
                remarks.First().GetLines().AddRange(remarks.Skip(1).SelectMany(s => s.GetLines()));
            if (remarks.Count > 1)
                sections.RemoveRange(sections.IndexOf(remarks.First()) + 1, remarks.Count - 1);

            foreach (var section in sections.Where(s => s.HasLines))
            {
                var lines = section.GetLines();
                var tag = section.Type.ToString().ToLowerInvariant();
                var attributes = string.Empty;
                if (section.Attributes.Any())
                    attributes = ' ' + string.Join(" ", section.Attributes);
                textGenerator.Write($"{commentPrefix} <{tag}{attributes}>");
                if (lines.Count == 1)
                {
                    textGenerator.Write(lines[0]);
                }
                else
                {
                    textGenerator.NewLine();
                    foreach (var line in lines)
                        textGenerator.WriteLine($"{commentPrefix} <para>{line}</para>");
                    textGenerator.Write($"{commentPrefix} ");
                }
                textGenerator.WriteLine($"</{tag}>");
            }
        }

        private class Section
        {
            public Section(CommentElement type)
            {
                Type = type;
            }

            public StringBuilder CurrentLine { get; set; } = new StringBuilder();

            public CommentElement Type { get; set; }

            public List<string> Attributes { get; } = new List<string>();

            private List<string> lines { get; } = new List<string>();

            public bool HasLines => lines.Any();

            public void NewLine()
            {
                lines.Add(CurrentLine.ToString());
                CurrentLine.Clear();
            }

            public List<string> GetLines()
            {
                if (CurrentLine.Length > 0)
                    NewLine();
                return lines;
            }
        }

        private enum CommentElement
        {
            Summary,
            Typeparam,
            Param,
            Returns,
            Exception,
            Remarks,
            Example
        }
    }
}
