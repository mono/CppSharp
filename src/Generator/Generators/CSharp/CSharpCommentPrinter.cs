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
            var sections = new List<Section>();
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
                {
                    foreach (var block in ((FullComment)comment).Blocks)
                        block.GetCommentSections(sections);
                    break;
                }
                case DocumentationCommentKind.BlockCommandComment:
                {
                    var blockCommandComment = (BlockCommandComment)comment;
                    if (blockCommandComment.ParagraphComment == null)
                        break;

                    switch (blockCommandComment.CommandKind)
                    {
                        case CommentCommandKind.Brief:
                            sections.Add(new Section(CommentElement.Summary));
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
                }
                case DocumentationCommentKind.ParamCommandComment:
                {
                    var paramCommandComment = (ParamCommandComment)comment;
                    var param = new Section(CommentElement.Param);
                    sections.Add(param);
                    if (paramCommandComment.Arguments.Count > 0)
                        param.Attributes.Add($"name=\"{paramCommandComment.Arguments[0].Text}\"");

                    if (paramCommandComment.ParagraphComment != null)
                    {
                        foreach (var inlineContentComment in paramCommandComment.ParagraphComment.Content)
                        {
                            inlineContentComment.GetCommentSections(sections);
                            if (inlineContentComment.HasTrailingNewline)
                                sections.Last().NewLine();
                        }
                    }

                    if (!string.IsNullOrEmpty(sections.Last().CurrentLine.ToString()))
                        sections.Add(new Section(CommentElement.Remarks));
                    break;
                }
                case DocumentationCommentKind.ParagraphComment:
                {
                    bool summaryParagraph = false;
                    if (sections.Count == 0)
                    {
                        sections.Add(new Section(CommentElement.Summary));
                        summaryParagraph = true;
                    }

                    var lastParagraphSection = sections.Last();
                    var paragraphComment = (ParagraphComment)comment;
                    foreach (var inlineContentComment in paragraphComment.Content)
                    {
                        inlineContentComment.GetCommentSections(sections);
                        if (inlineContentComment.HasTrailingNewline)
                            lastParagraphSection.NewLine();

                        lastParagraphSection = sections.Last();
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
                }
                case DocumentationCommentKind.TextComment:
                {
                    var lastTextSection = sections.Last();
                    lastTextSection.CurrentLine
                        .Append(
                            GetText(comment, lastTextSection.Type is CommentElement.Returns or CommentElement.Param)
                                .TrimStart()
                        );
                    break;
                }
                case DocumentationCommentKind.InlineCommandComment:
                {
                    var lastInlineSection = sections.Last();
                    var inlineCommand = (InlineCommandComment)comment;

                    if (inlineCommand.CommandKind == CommentCommandKind.B)
                    {
                        var argText = $" <c>{inlineCommand.Arguments[0].Text}</c> ";
                        lastInlineSection.CurrentLine.Append(argText);
                    }

                    break;
                }
                case DocumentationCommentKind.HTMLStartTagComment:
                {
                    var startTag = (HTMLStartTagComment)comment;
                    var sectionType = CommentElementFromTag(startTag.TagName);

                    if (IsInlineCommentElement(sectionType))
                    {
                        var lastSection = sections.Last();
                        lastSection.CurrentLine.Append(startTag);
                        break;
                    }

                    sections.Add(new Section(sectionType)
                    {
                        Attributes = startTag.Attributes.Select(a => a.ToString()).ToList()
                    });
                    break;
                }
                case DocumentationCommentKind.HTMLEndTagComment:
                {
                    var endTag = (HTMLEndTagComment)comment;
                    var sectionType = CommentElementFromTag(endTag.TagName);

                    if (IsInlineCommentElement(sectionType))
                    {
                        var lastSection = sections.Last();
                        lastSection.CurrentLine.Append(endTag);
                    }

                    break;
                }
                case DocumentationCommentKind.HTMLTagComment:
                case DocumentationCommentKind.TParamCommandComment:
                case DocumentationCommentKind.VerbatimBlockComment:
                case DocumentationCommentKind.VerbatimLineComment:
                case DocumentationCommentKind.InlineContentComment:
                case DocumentationCommentKind.VerbatimBlockLineComment:
                case DocumentationCommentKind.BlockContentComment:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetText(Comment comment, bool trim = false)
        {
            var textComment = ((TextComment)comment);
            var text = textComment.Text;
            if (trim)
                text = text.Trim();

            if (Helpers.RegexTag.IsMatch(text))
                return string.Empty;

            return HtmlEncoder.HtmlEncode(
                text.Length > 1 && text[0] == ' ' && text[1] != ' ' ? text[1..] : text);
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
            sections.Sort((x, y) => x.Type.CompareTo(y.Type));

            var remarks = sections.Where(s => s.Type == CommentElement.Remarks).ToList();
            if (remarks.Count != 0)
                remarks.First().GetLines().AddRange(remarks.Skip(1).SelectMany(s => s.GetLines()));

            if (remarks.Count > 1)
                sections.RemoveRange(sections.IndexOf(remarks.First()) + 1, remarks.Count - 1);

            var commentPrefix = Comment.GetMultiLineCommentPrologue(kind);
            foreach (var section in sections.Where(s => s.HasLines))
            {
                var lines = section.GetLines();
                var tag = section.Type.ToString().ToLowerInvariant();

                var attributes = string.Empty;
                if (section.Attributes.Count != 0)
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

            public StringBuilder CurrentLine { get; set; } = new();

            public CommentElement Type { get; set; }

            public List<string> Attributes { get; init; } = new();

            private List<string> lines { get; } = new();

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

        private static CommentElement CommentElementFromTag(string tag)
        {
            return tag.ToLowerInvariant() switch
            {
                "c" => CommentElement.C,
                "code" => CommentElement.Code,
                "example" => CommentElement.Example,
                "exception" => CommentElement.Exception,
                "include" => CommentElement.Include,
                "list" => CommentElement.List,
                "para" => CommentElement.Para,
                "param" => CommentElement.Param,
                "paramref" => CommentElement.ParamRef,
                "permission" => CommentElement.Permission,
                "remarks" => CommentElement.Remarks,
                "return" or "returns" => CommentElement.Returns,
                "summary" => CommentElement.Summary,
                "typeparam" => CommentElement.TypeParam,
                "typeparamref" => CommentElement.TypeParamRef,
                "value" => CommentElement.Value,
                "seealso" => CommentElement.SeeAlso,
                "see" => CommentElement.See,
                "inheritdoc" => CommentElement.InheritDoc,
                _ => CommentElement.Unknown
            };
        }

        /// <summary>From https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d3-recommended-tags</summary>
        /// <remarks>Enum value is equal to sorting priority</remarks>
        private enum CommentElement
        {
            C = 1000,               // Set text in a code-like font
            Code = 1001,            // Set one or more lines of source code or program output
            Example = 11,           // Indicate an example
            Exception = 8,          // Identifies the exceptions a method can throw
            Include = 1,            // Includes XML from an external file
            List = 1002,            // Create a list or table
            Para = 1003,            // Permit structure to be added to text
            Param = 5,              // Describe a parameter for a method or constructor
            ParamRef = 1004,        // Identify that a word is a parameter name
            Permission = 7,         // Document the security accessibility of a member
            Remarks = 9,            // Describe additional information about a type
            Returns = 6,            // Describe the return value of a method
            See = 1005,             // Specify a link
            SeeAlso = 10,           // Generate a See Also entry
            Summary = 2,            // Describe a type or a member of a type
            TypeParam = 4,          // Describe a type parameter for a generic type or method
            TypeParamRef = 1006,    // Identify that a word is a type parameter name
            Value = 3,              // Describe a property
            InheritDoc = 0,         // Inherit documentation from a base class
            Unknown = 9999,         // Unknown tag
        }

        private static bool IsInlineCommentElement(CommentElement element) =>
            element switch
            {
                CommentElement.C => true,
                CommentElement.Code => true,
                CommentElement.List => true,
                CommentElement.Para => true,
                CommentElement.ParamRef => true,
                CommentElement.See => true,
                CommentElement.TypeParamRef => true,
                CommentElement.Unknown => true, // Print unknown tags as inline
                _ => ((int)element) >= 1000
            };
    }
}
