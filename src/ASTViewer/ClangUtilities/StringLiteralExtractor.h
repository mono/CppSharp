#pragma once

#include <vector>
#include <string>
#pragma warning (push)
#pragma warning (disable:4100 4127 4800 4512 4245 4291 4510 4610 4324 4267 4244 4996 4146)
#include <clang/AST/Decl.h>
#include <clang/Lex/Lexer.h>
#include <clang/Frontend/FrontendActions.h>
#include <llvm/ADT/ArrayRef.h>
#include <clang/Basic/LangOptions.h>
#pragma warning (pop)


namespace clang_utilities {

// This function is an direct adaptation from ProcessCharEscape in llvm-3.7.1\src\tools\clang\lib\Lex\LiteralSupport.cpp (just removed the diag part)
unsigned ProcessCharEscape(const char *ThisTokBegin,
    const char *&ThisTokBuf,
    const char *ThisTokEnd, bool &HadError,
    clang::FullSourceLoc Loc, unsigned CharWidth,
    const clang::LangOptions &Features);

// This function is an direct adaptation from ProcessUCNEscape in llvm-3.7.1\src\tools\clang\lib\Lex\LiteralSupport.cpp (just removed the diag part)
bool ProcessUCNEscape(const char *ThisTokBegin, const char *&ThisTokBuf,
    const char *ThisTokEnd,
    uint32_t &UcnVal, unsigned short &UcnLen,
    clang::FullSourceLoc Loc,
    const clang::LangOptions &Features,
    bool in_char_string_literal = false);


// This function is an direct adaptation from ProcessUCNEscape in llvm-3.7.1\src\tools\clang\lib\Lex\LiteralSupport.cpp (just removed the diag part)
int MeasureUCNEscape(const char *ThisTokBegin, const char *&ThisTokBuf,
    const char *ThisTokEnd, unsigned CharByteWidth,
    const clang::LangOptions &Features, bool &HadError);


bool convertUTF32ToUTF8String(llvm::ArrayRef<char> SrcBytes, std::string &Out);


std::vector<std::string>
splitStringLiteral(clang::StringLiteral *S, const clang::SourceManager &SM, const clang::LangOptions &Features, const clang::TargetInfo &Target);

} // namespace clang_utilities

