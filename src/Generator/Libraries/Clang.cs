
namespace Cxxi
{
	/// <summary>
	/// Transform the Clang library declarations to something more .NET friendly.
	/// </summary>
	class ClangTransforms : LibraryTransform
	{
		public void Preprocess(Generator g)
		{
			//g.SetNameOfEnumWithMatchingItem("SDL_LOG_CATEGORY_CUSTOM", "LogCategory");
			g.RemovePrefix("CX");
			g.RemovePrefix("clang_");
			g.RenameWithPattern("^_", string.Empty, RenameFlags.Declaration);

			// Clean up types
			g.FindClass("CXString").IsOpaque = true;
			g.FindClass("CXSourceLocation").IsOpaque = true;
			g.FindClass("CXSourceRange").IsOpaque = true;
			g.FindClass("CXCursor").IsOpaque = true;
			g.FindClass("CXType").IsOpaque = true;
			g.FindClass("CXToken").IsOpaque = true;
			g.FindClass("CXIdxLoc").IsOpaque = true;
			g.FindClass("CXTranslationUnitImpl").IsOpaque = true;
			
			// Clean up enums
			g.RemovePrefixEnumItem("ChildVisit_");
			g.RemovePrefixEnumItem("Comment_");
			g.RemovePrefixEnumItem("Availability_");
			g.RemovePrefixEnumItem("GlobalOpt_");
			g.RemovePrefixEnumItem("Diagnostic_");
			g.RemovePrefixEnumItem("LoadDiag_");
			g.RemovePrefixEnumItem("TranslationUnit_");
			g.RemovePrefixEnumItem("SaveTranslationUnit_");
			g.RemovePrefixEnumItem("SaveError_");
			g.RemovePrefixEnumItem("TranslationUnit_");
			g.RemovePrefixEnumItem("Reparse_");
			g.RemovePrefixEnumItem("TUResourceUsage_");
			g.RemovePrefixEnumItem("Cursor_");
			g.RemovePrefixEnumItem("Linkage_");
			g.RemovePrefixEnumItem("Language_");
			g.RemovePrefixEnumItem("Type_");
			g.RemovePrefixEnumItem("CallingConv_");
			g.RemovePrefixEnumItem("CommentInlineCommandRenderKind_");
			g.RemovePrefixEnumItem("CommentParamPassDirection_");
			g.RemovePrefixEnumItem("NameRange_");
			g.RemovePrefixEnumItem("Token_");
			g.RemovePrefixEnumItem("CompletionChunk_");
			g.RemovePrefixEnumItem("CodeComplete_");
			g.RemovePrefixEnumItem("CompletionContext_");
			g.RemovePrefixEnumItem("Visit_");
			g.RemovePrefixEnumItem("IdxEntity_");
			g.RemovePrefixEnumItem("IdxEntityLang_");
			g.RemovePrefixEnumItem("IdxAttr_");
			g.RemovePrefixEnumItem("IdxObjCContainer_");
			g.RemovePrefixEnumItem("IdxEntityRef_");
			g.RemovePrefixEnumItem("IndexOpt_");
			g.RemovePrefixEnumItem("IdxObjCContainer_");
		}

		public void Postprocess(Generator g)
		{
			g.FindEnum("CompletionContext").SetFlags();
			g.FindClass("String").Name = "CXString";
			//gen.SetNameOfEnumWithName("LOG_CATEGORY", "LogCategory");
		}
	}
}
