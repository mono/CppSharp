/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include <llvm/Support/Host.h>
#include <clang/Frontend/CompilerInstance.h>
#include <clang/Frontend/CompilerInvocation.h>
#include <clang/Frontend/ASTConsumers.h>
#include <clang/Basic/FileManager.h>
#include <clang/Basic/TargetOptions.h>
#include <clang/Basic/TargetInfo.h>
#include <clang/Basic/IdentifierTable.h>
#include <clang/AST/ASTConsumer.h>
#include <clang/AST/Mangle.h>
#include <clang/AST/RawCommentList.h>
#include <clang/AST/Comment.h>
#include <clang/AST/RecordLayout.h>
#include <clang/AST/VTableBuilder.h>
#include <clang/Lex/Preprocessor.h>
#include <clang/Lex/PreprocessingRecord.h>
#include <clang/Parse/ParseAST.h>
#include <clang/Sema/Sema.h>
#include "CXXABI.h"

#include "CppParser.h"

#include <string>
typedef std::string String;

namespace clang {
  class TargetCodeGenInfo;
  namespace CodeGen {
    class CodeGenTypes;
  }
}

#define Debug printf

namespace CppSharp { namespace CppParser {

struct Parser
{
    Parser(ParserOptions* Opts);

    void SetupHeader();
    ParserResult* ParseHeader(const std::string& File);
    ParserResult* ParseLibrary(const std::string& File);
    ParserResultKind ParseArchive(llvm::StringRef File,
                                  llvm::MemoryBuffer *Buffer);
    ParserResultKind ParseSharedLib(llvm::StringRef File,
                                    llvm::MemoryBuffer *Buffer);

protected:

    // AST traversers
    void WalkAST();
    void WalkMacros(clang::PreprocessingRecord* PR);
    Declaration* WalkDeclaration(clang::Decl* D,
        bool IgnoreSystemDecls = true, bool CanBeDefinition = false);
    Declaration* WalkDeclarationDef(clang::Decl* D);
    Enumeration* WalkEnum(clang::EnumDecl* ED);
    Function* WalkFunction(clang::FunctionDecl* FD, bool IsDependent = false,
        bool AddToNamespace = true);
    Class* WalkRecordCXX(clang::CXXRecordDecl* Record);
    Method* WalkMethodCXX(clang::CXXMethodDecl* MD);
    Field* WalkFieldCXX(clang::FieldDecl* FD, Class* Class);
    ClassTemplate* WalkClassTemplate(clang::ClassTemplateDecl* TD);
    FunctionTemplate* WalkFunctionTemplate(
        clang::FunctionTemplateDecl* TD);
    Variable* WalkVariable(clang::VarDecl* VD);
    RawComment* WalkRawComment(const clang::RawComment* RC);
    Type* WalkType(clang::QualType QualType, clang::TypeLoc* TL = 0,
      bool DesugarType = false);
    void WalkVTable(clang::CXXRecordDecl* RD, Class* C);
    VTableLayout WalkVTableLayout(const clang::VTableLayout& VTLayout);
    VTableComponent WalkVTableComponent(const clang::VTableComponent& Component);
    PreprocessedEntity* WalkPreprocessedEntity(Declaration* Decl,
        clang::PreprocessedEntity* PPEntity);

    // Clang helpers
    SourceLocationKind GetLocationKind(const clang::SourceLocation& Loc);
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(clang::Decl* D, clang::TargetCXXABI ABI,
        bool IsDependent = false);
    std::string GetTypeName(const clang::Type* Type);
    void WalkFunction(clang::FunctionDecl* FD, Function* F,
        bool IsDependent = false);
    void HandlePreprocessedEntities(Declaration* Decl, clang::SourceRange sourceRange,
                                    MacroLocation macroLocation = MacroLocation::Unknown);
    bool GetDeclText(clang::SourceRange SR, std::string& Text);

    TranslationUnit* GetTranslationUnit(clang::SourceLocation Loc,
        SourceLocationKind *Kind = 0);
    TranslationUnit* GetTranslationUnit(const clang::Decl* D);

    DeclarationContext* GetNamespace(clang::Decl* D, clang::DeclContext* Ctx);
    DeclarationContext* GetNamespace(clang::Decl* D);

    clang::CallingConv GetAbiCallConv(clang::CallingConv CC,
        bool IsInstMethod, bool IsVariadic);

    void HandleDeclaration(clang::Decl* D, Declaration* Decl);
    void HandleOriginalText(clang::Decl* D, Declaration* Decl);
    void HandleComments(clang::Decl* D, Declaration* Decl);
    void HandleDiagnostics(ParserResult* res);

    int Index;
    ASTContext* Lib;
    ParserOptions* Opts;
    llvm::OwningPtr<clang::CompilerInstance> C;
    clang::ASTContext* AST;
    clang::TargetCXXABI::Kind TargetABI;
    clang::TargetCodeGenInfo* CodeGenInfo;
    clang::CodeGen::CodeGenTypes* CodeGenTypes;
};

} }