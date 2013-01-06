using Cxxi.Generators;
using Cxxi.Passes;
using Cxxi.Templates;
using Generator = Cxxi.Generators.Generator;

namespace Cxxi
{
    /// <summary>
    /// Transform the Clang library declarations to something more .NET friendly.
    /// </summary>
    class Clang : ILibrary
    {
        public void Preprocess(LibraryHelpers g)
        {
            //g.SetNameOfEnumWithMatchingItem("SDL_LOG_CATEGORY_CUSTOM", "LogCategory");

            // Clean up types
            g.FindClass("CXString").IsOpaque = true;
            g.FindClass("CXSourceLocation").IsOpaque = true;
            g.FindClass("CXSourceRange").IsOpaque = true;
            g.FindClass("CXCursor").IsOpaque = true;
            g.FindClass("CXType").IsOpaque = true;
            g.FindClass("CXToken").IsOpaque = true;
            g.FindClass("CXIdxLoc").IsOpaque = true;
            g.FindClass("CXTranslationUnitImpl").IsOpaque = true;
        }

        public void Postprocess(LibraryHelpers g)
        {
            g.FindEnum("CompletionContext").SetFlags();
            g.FindClass("String").Name = "CXString";
            //gen.SetNameOfEnumWithName("LOG_CATEGORY", "LogCategory");
        }

        public void Postprocess(Generators.Generator generator)
        {

        }

        public void SetupPasses(PassBuilder p)
        {
            p.RemovePrefix("CX");
            p.RemovePrefix("clang_");
            p.RenameWithPattern("^_", string.Empty, RenameTargets.Any);

            // Clean up enums
            p.RemovePrefixEnumItem("ChildVisit_");
            p.RemovePrefixEnumItem("Comment_");
            p.RemovePrefixEnumItem("Availability_");
            p.RemovePrefixEnumItem("GlobalOpt_");
            p.RemovePrefixEnumItem("Diagnostic_");
            p.RemovePrefixEnumItem("LoadDiag_");
            p.RemovePrefixEnumItem("TranslationUnit_");
            p.RemovePrefixEnumItem("SaveTranslationUnit_");
            p.RemovePrefixEnumItem("SaveError_");
            p.RemovePrefixEnumItem("TranslationUnit_");
            p.RemovePrefixEnumItem("Reparse_");
            p.RemovePrefixEnumItem("TUResourceUsage_");
            p.RemovePrefixEnumItem("Cursor_");
            p.RemovePrefixEnumItem("Linkage_");
            p.RemovePrefixEnumItem("Language_");
            p.RemovePrefixEnumItem("Type_");
            p.RemovePrefixEnumItem("CallingConv_");
            p.RemovePrefixEnumItem("CommentInlineCommandRenderKind_");
            p.RemovePrefixEnumItem("CommentParamPassDirection_");
            p.RemovePrefixEnumItem("NameRange_");
            p.RemovePrefixEnumItem("Token_");
            p.RemovePrefixEnumItem("CompletionChunk_");
            p.RemovePrefixEnumItem("CodeComplete_");
            p.RemovePrefixEnumItem("CompletionContext_");
            p.RemovePrefixEnumItem("Visit_");
            p.RemovePrefixEnumItem("IdxEntity_");
            p.RemovePrefixEnumItem("IdxEntityLang_");
            p.RemovePrefixEnumItem("IdxAttr_");
            p.RemovePrefixEnumItem("IdxObjCContainer_");
            p.RemovePrefixEnumItem("IdxEntityRef_");
            p.RemovePrefixEnumItem("IndexOpt_");
            p.RemovePrefixEnumItem("IdxObjCContainer_");
        }

        public void GenerateStart(TextTemplate template)
        {
            throw new System.NotImplementedException();
        }

        public void GenerateAfterNamespaces(TextTemplate template)
        {
            throw new System.NotImplementedException();
        }
    }
}
