using System.Collections.Generic;

namespace CppSharp.AST
{
    /// <summary>
    /// Raw comment kind.
    /// </summary>
    public enum RawCommentKind
    {
        // Invalid comment.
        Invalid,
        // Any normal BCPL comments.
        OrdinaryBCPL,
        // Any normal C comment.
        OrdinaryC,
        // "/// stuff"
        BCPLSlash,
        // "//! stuff"
        BCPLExcl,
        // "/** stuff */"
        JavaDoc,
        // "/*! stuff */", also used by HeaderDoc
        Qt,
        // Two or more documentation comments merged together.
        Merged
    }

    public enum CommentKind
    {
        FullComment,
        BlockContentComment,
        BlockCommandComment,
        ParamCommandComment,
        TParamCommandComment,
        VerbatimBlockComment,
        VerbatimLineComment,
        ParagraphComment,
        HTMLTagComment,
        HTMLStartTagComment,
        HTMLEndTagComment,
        TextComment,
        InlineContentComment,
        InlineCommandComment,
        VerbatimBlockLineComment,
    }

    /// <summary>
    /// Represents a raw C++ comment.
    /// </summary>
    public class RawComment
    {
        /// <summary>
        /// Kind of the comment.
        /// </summary>
        public RawCommentKind Kind;

        /// <summary>
        /// Raw text of the comment.
        /// </summary>
        public string Text;

        /// <summary>
        /// Brief text if it is a documentation comment.
        /// </summary>
        public string BriefText;

        /// <summary>
        /// Returns if the comment is invalid.
        /// </summary>
        public bool IsInvalid
        {
            get { return Kind == RawCommentKind.Invalid; }
        }

        /// <summary>
        /// Returns if the comment is ordinary (non-documentation).
        /// </summary>
        public bool IsOrdinary
        {
            get
            {
                return Kind == RawCommentKind.OrdinaryBCPL ||
                       Kind == RawCommentKind.OrdinaryC;
            }
        }

        /// <summary>
        /// Returns if this is a documentation comment.
        /// </summary>
        public bool IsDocumentation
        {
            get { return !IsInvalid && !IsOrdinary; }
        }

        /// <summary>
        /// Provides the full comment information.
        /// </summary>
        public FullComment FullComment;
    }

    /// <summary>
    /// Visitor for comments.
    /// </summary>
    public interface ICommentVisitor<out T>
    {
        T VisitBlockCommand(BlockCommandComment comment);
        T VisitParamCommand(ParamCommandComment comment);
        T VisitTParamCommand(TParamCommandComment comment);
        T VisitVerbatimBlock(VerbatimBlockComment comment);
        T VisitVerbatimLine(VerbatimLineComment comment);
        T VisitParagraphCommand(ParagraphComment comment);
        T VisitFull(FullComment comment);
        T VisitHTMLStartTag(HTMLStartTagComment comment);
        T VisitHTMLEndTag(HTMLEndTagComment comment);
        T VisitText(TextComment comment);
        T VisitInlineCommand(InlineCommandComment comment);
        T VisitVerbatimBlockLine(VerbatimBlockLineComment comment);
    }

    /// <summary>
    /// Any part of the comment.
    /// </summary>
    public abstract class Comment
    {
        public CommentKind Kind { get; set; }

        public abstract void Visit<T>(ICommentVisitor<T> visitor);
    }

    #region Comments

    /// <summary>
    /// A full comment attached to a declaration, contains block content.
    /// </summary>
    public class FullComment : Comment
    {
        public List<BlockContentComment> Blocks;

        public FullComment()
        {
            Blocks = new List<BlockContentComment>();
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitFull(this);
        }
    }

    /// <summary>
    /// Block content (contains inline content).
    /// </summary>
    public abstract class BlockContentComment : Comment
    {

    }

    /// <summary>
    /// A command that has zero or more word-like arguments (number of
    /// word-like arguments depends on command name) and a paragraph as
    /// an argument (e. g., \brief).
    /// </summary>
    public class BlockCommandComment : BlockContentComment
    {
        public struct Argument
        {
            public string Text;
        }

        public uint CommandId;

        public CommentCommandKind CommandKind
        {
            get { return (CommentCommandKind) CommandId; }
        }

        public List<Argument> Arguments;

        public BlockCommandComment()
        {
            Kind = CommentKind.BlockCommandComment;
            Arguments = new List<Argument>();
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitBlockCommand(this);
        }
    }

    /// <summary>
    /// Doxygen \param command.
    /// </summary>
    public class ParamCommandComment : BlockCommandComment
    {
        public const uint InvalidParamIndex = ~0U;
        public const uint VarArgParamIndex = ~0U/*InvalidParamIndex*/ - 1U;

        public ParamCommandComment()
        {
            Kind = CommentKind.ParamCommandComment;
        }

        public enum PassDirection
        {
            In,
            Out,
            InOut,
        }

        public bool IsParamIndexValid
        {
            get { return ParamIndex != InvalidParamIndex; }
        }

        public bool IsVarArgParam
        {
            get { return ParamIndex == VarArgParamIndex; }
        }

        public uint ParamIndex;

        public PassDirection Direction;

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitParamCommand(this);
        }
    }

    /// <summary>
    /// Doxygen \tparam command, describes a template parameter.
    /// </summary>
    public class TParamCommandComment : BlockCommandComment
    {
        /// If this template parameter name was resolved (found in template parameter
        /// list), then this stores a list of position indexes in all template
        /// parameter lists.
        ///
        /// For example:
        /// \verbatim
        ///     template<typename C, template<typename T> class TT>
        ///     void test(TT<int> aaa);
        /// \endverbatim
        /// For C:  Position = { 0 }
        /// For TT: Position = { 1 }
        /// For T:  Position = { 1, 0 }
        public List<uint> Position;

        public TParamCommandComment()
        {
            Kind = CommentKind.TParamCommandComment;
            Position = new List<uint>();
        }
  
        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitTParamCommand(this);
        }
    }

    /// <summary>
    /// A verbatim block command (e. g., preformatted code). Verbatim block
    /// has an opening and a closing command and contains multiple lines of
    /// text (VerbatimBlockLineComment nodes).
    /// </summary>
    public class VerbatimBlockComment : BlockCommandComment
    {
        public List<VerbatimBlockLineComment> Lines;

        public VerbatimBlockComment()
        {
            Kind = CommentKind.VerbatimBlockComment;
            Lines = new List<VerbatimBlockLineComment>();
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitVerbatimBlock(this);
        }
    }

    /// <summary>
    /// A verbatim line command. Verbatim line has an opening command, a
    /// single line of text (up to the newline after the opening command)
    /// and has no closing command.
    /// </summary>
    public class VerbatimLineComment : BlockCommandComment
    {
        public string Text;

        public VerbatimLineComment()
        {
            Kind = CommentKind.VerbatimLineComment;
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitVerbatimLine(this);
        }
    }

    /// <summary>
    /// A single paragraph that contains inline content.
    /// </summary>
    public class ParagraphComment : BlockContentComment
    {
        public List<InlineContentComment> Content;
 
        public bool IsWhitespace;

        public ParagraphComment()
        {
            Kind = CommentKind.ParagraphComment;
            Content = new List<InlineContentComment>();
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitParagraphCommand(this);
        }
    }

    /// <summary>
    /// Inline content (contained within a block).
    /// </summary>
    public abstract class InlineContentComment : Comment
    {
        protected InlineContentComment()
        {
            Kind = CommentKind.InlineContentComment;
        }
    }

    /// <summary>
    /// Abstract class for opening and closing HTML tags. HTML tags are
    /// always treated as inline content (regardless HTML semantics);
    /// opening and closing tags are not matched.
    /// </summary>
    public abstract class HTMLTagComment : InlineContentComment
    {
        public string TagName;

        protected HTMLTagComment()
        {
            Kind = CommentKind.HTMLTagComment;
        }
    }

    /// <summary>
    /// An opening HTML tag with attributes.
    /// </summary>
    public class HTMLStartTagComment : HTMLTagComment
    {
        public struct Attribute
        {
            public string Name;
            public string Value;
        }

        public List<Attribute> Attributes;

        public HTMLStartTagComment()
        {
            Kind = CommentKind.HTMLStartTagComment;
            Attributes = new List<Attribute>();
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitHTMLStartTag(this);
        }
    }

    /// <summary>
    /// A closing HTML tag.
    /// </summary>
    public class HTMLEndTagComment : HTMLTagComment
    {
        public HTMLEndTagComment()
        {
            Kind = CommentKind.HTMLEndTagComment;
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitHTMLEndTag(this);
        }
    }

    /// <summary>
    /// Plain text.
    /// </summary>
    public class TextComment : InlineContentComment
    {
        public string Text;

        public TextComment()
        {
            Kind = CommentKind.TextComment;
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitText(this);
        }
    }

    /// <summary>
    /// A command with word-like arguments that is considered inline content.
    /// </summary>
    public class InlineCommandComment : InlineContentComment
    {
        public struct Argument
        {
            public string Text;
        }

        public enum RenderKind
        {
            RenderNormal,
            RenderBold,
            RenderMonospaced, 
            RenderEmphasized
        }

        public RenderKind CommentRenderKind;

        public List<Argument> Arguments;

        public InlineCommandComment()
        {
            Kind = CommentKind.InlineCommandComment;
            Arguments = new List<Argument>();
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitInlineCommand(this);
        }
    }

    /// <summary>
    /// A line of text contained in a verbatim block.
    /// </summary>
    public class VerbatimBlockLineComment : Comment
    {
        public string Text;

        public VerbatimBlockLineComment()
        {
            Kind = CommentKind.VerbatimBlockLineComment;
        }

        public override void Visit<T>(ICommentVisitor<T> visitor)
        {
            visitor.VisitVerbatimBlockLine(this);
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Kinds of comment commands.
    /// Synchronized from "clang/AST/CommentCommandList.inc".
    /// </summary>
    public enum CommentCommandKind
    {
        A,
        Abstract,
        Addtogroup,
        Arg,
        Attention,
        Author,
        Authors,
        B,
        Brief,
        Bug,
        C,
        Callback,
        Category,
        Class,
        Classdesign,
        Coclass,
        Code,
        Endcode,
        Const,
        Constant,
        Copyright,
        Date,
        Defgroup,
        Dependency,
        Deprecated,
        Details,
        Discussion,
        Dot,
        Enddot,
        E,
        Em,
        Enum,
        Flbrace,
        Frbrace,
        Flsquare,
        Frsquare,
        Fdollar,
        Fn,
        Function,
        Functiongroup,
        Headerfile,
        Helper,
        Helperclass,
        Helps,
        Htmlonly,
        Endhtmlonly,
        Ingroup,
        Instancesize,
        Interface,
        Invariant,
        Latexonly,
        Endlatexonly,
        Li,
        Link,
        Slashlink,
        Mainpage,
        Manonly,
        Endmanonly,
        Method,
        Methodgroup,
        Msc,
        Endmsc,
        Name,
        Namespace,
        Note,
        Overload,
        Ownership,
        P,
        Par,
        Paragraph,
        Param,
        Performance,
        Post,
        Pre,
        Property,
        Protocol,
        Ref,
        Related,
        Relatedalso,
        Relates,
        Relatesalso,
        Remark,
        Remarks,
        Result,
        Return,
        Returns,
        Rtfonly,
        Endrtfonly,
        Sa,
        Section,
        Security,
        See,
        Seealso,
        Short,
        Since,
        Struct,
        Subpage,
        Subsection,
        Subsubsection,
        Superclass,
        Template,
        Templatefield,
        Textblock,
        Slashtextblock,
        Todo,
        Tparam,
        Typedef,
        Union,
        Var,
        Verbatim,
        Endverbatim,
        Version,
        Warning,
        Weakgroup,
        Xmlonly,
        Endxmlonly
    }

    #endregion

}
