/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include <clang/AST/ASTFwd.h>
#include <llvm/IR/DataLayout.h>

#include <string>

namespace clang
{
    class ASTContext;
    class MangleContext;
    struct ThunkInfo;
}

namespace llvm
{
    class raw_ostream;
}

namespace CppSharp::CppParser {

/// <summary>
/// Helper class for getting the mangled name of a declaration
/// </summary>
/// <remarks>Source adapted from https://clang.llvm.org/doxygen/Mangle_8cpp_source.html#l00394</remarks>
class ASTNameMangler
{
public:
    explicit ASTNameMangler(clang::ASTContext& Ctx);

    std::string GetName(const clang::Decl* D) const;
    bool WriteName(const clang::Decl* D, llvm::raw_ostream& OS) const;

private:
    std::string GetMangledStructor(const clang::NamedDecl* ND, unsigned StructorType) const;
    std::string GetMangledThunk(const clang::CXXMethodDecl* MD, const clang::ThunkInfo& T, bool ElideOverrideInfo) const;
    bool WriteFuncOrVarName(const clang::NamedDecl* D, llvm::raw_ostream& OS) const;
    
    llvm::DataLayout DL;
    std::unique_ptr<clang::MangleContext> MC;
};

}
