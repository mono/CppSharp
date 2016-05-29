using CppSharp.AST;
using System;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass will look through semantic comments (Doxygen) in the source code
    /// and fix the parameter usage (out or ref) if that information is available.
    /// </summary>
    public class FixParameterUsageFromComments : TranslationUnitPass, ICommentVisitor<bool>
    {
        Method currentMethod;

        public override bool VisitMethodDecl(Method method)
        {
            if (method.Comment == null || method.Comment.FullComment == null)
                return base.VisitMethodDecl(method);

            Method previousMethod = currentMethod;
            currentMethod = method;

            var fullComment = method.Comment.FullComment;
            VisitFull(fullComment);

            currentMethod = previousMethod;

            return base.VisitMethodDecl(method);
        }

        static ParameterUsage GetParameterUsageFromDirection(ParamCommandComment.PassDirection dir)
        {
            switch (dir)
            {
                case ParamCommandComment.PassDirection.In:
                    return ParameterUsage.In;
                case ParamCommandComment.PassDirection.InOut:
                    return ParameterUsage.InOut;
                case ParamCommandComment.PassDirection.Out:
                    return ParameterUsage.Out;
            }

            throw new NotSupportedException("Unknown parameter pass direction");
        }

        public bool VisitParamCommand(ParamCommandComment comment)
        {
            if (comment.Direction == ParamCommandComment.PassDirection.In)
                return false;

            if (!comment.IsParamIndexValid)
                return false;

            var parameter = currentMethod.Parameters[(int)comment.ParamIndex];
            parameter.Usage = GetParameterUsageFromDirection(comment.Direction);

            return true;
        }

        public bool VisitFull(FullComment comment)
        {
            foreach (var block in comment.Blocks)
                block.Visit(this);

            return true;
        }

        #region Comments visitor stubs

        public bool VisitBlockCommand(BlockCommandComment comment)
        {
            return true;
        }

        public bool VisitParagraphCommand(ParagraphComment comment)
        {
            return true;
        }

        public bool VisitTParamCommand(TParamCommandComment comment)
        {
            return true;
        }

        public bool VisitVerbatimBlock(VerbatimBlockComment comment)
        {
            return true;
        }

        public bool VisitVerbatimLine(VerbatimLineComment comment)
        {
            return true;
        }

        public bool VisitHTMLStartTag(HTMLStartTagComment comment)
        {
            return true;
        }

        public bool VisitHTMLEndTag(HTMLEndTagComment comment)
        {
            return true;
        }

        public bool VisitText(TextComment comment)
        {
            return true;
        }

        public bool VisitInlineCommand(InlineCommandComment comment)
        {
            return true;
        }

        public bool VisitVerbatimBlockLine(VerbatimBlockLineComment comment)
        {
            return true;
        }

        #endregion
    }
}