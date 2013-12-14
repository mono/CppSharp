/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#using <CppSharp.AST.dll>

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
#include "Options.h"

#include <vcclr.h>
#include <string>
typedef std::string String;

namespace clang {
  class TargetCodeGenInfo;
  namespace CodeGen {
    class CodeGenTypes;
  }
}

#define Debug printf

struct Parser
{
    Parser(ParserOptions^ Opts);

    void SetupHeader();
    ParserResult^ ParseHeader(const std::string& File);
    ParserResult^ ParseLibrary(const std::string& File);
    ParserResultKind ParseArchive(llvm::StringRef File,
                                  llvm::MemoryBuffer *Buffer,
                                  CppSharp::AST::NativeLibrary^ NativeLib);
    ParserResultKind ParseSharedLib(llvm::StringRef File,
                                    llvm::MemoryBuffer *Buffer,
                                    CppSharp::AST::NativeLibrary^ NativeLib);

protected:

    // AST traversers
    void WalkAST();
    void WalkMacros(clang::PreprocessingRecord* PR);
    CppSharp::AST::Declaration^ WalkDeclaration(clang::Decl* D,
        bool IgnoreSystemDecls = true, bool CanBeDefinition = false);
    CppSharp::AST::Declaration^ WalkDeclarationDef(clang::Decl* D);
    CppSharp::AST::Enumeration^ WalkEnum(clang::EnumDecl* ED);
    CppSharp::AST::Function^ WalkFunction(clang::FunctionDecl* FD, bool IsDependent = false,
        bool AddToNamespace = true);
    CppSharp::AST::Class^ WalkRecordCXX(clang::CXXRecordDecl* Record);
    void WalkRecordCXX(clang::CXXRecordDecl* Record, CppSharp::AST::Class^ RC);
    CppSharp::AST::ClassTemplateSpecialization^
    WalkClassTemplateSpecialization(clang::ClassTemplateSpecializationDecl* CTS);
    CppSharp::AST::ClassTemplatePartialSpecialization^
    WalkClassTemplatePartialSpecialization(clang::ClassTemplatePartialSpecializationDecl* CTS);
    CppSharp::AST::Method^ WalkMethodCXX(clang::CXXMethodDecl* MD);
    CppSharp::AST::Field^ WalkFieldCXX(clang::FieldDecl* FD, CppSharp::AST::Class^ Class);
    CppSharp::AST::ClassTemplate^ Parser::WalkClassTemplate(clang::ClassTemplateDecl* TD);
    CppSharp::AST::FunctionTemplate^ Parser::WalkFunctionTemplate(
        clang::FunctionTemplateDecl* TD);
    CppSharp::AST::Variable^ WalkVariable(clang::VarDecl* VD);
    CppSharp::AST::RawComment^ WalkRawComment(const clang::RawComment* RC);
    CppSharp::AST::Type^ WalkType(clang::QualType QualType, clang::TypeLoc* TL = 0,
      bool DesugarType = false);
    CppSharp::AST::QualifiedType^ WalkQualifiedType(clang::TypeSourceInfo* TSI);
    void WalkVTable(clang::CXXRecordDecl* RD, CppSharp::AST::Class^ C);
    CppSharp::AST::VTableLayout^ WalkVTableLayout(const clang::VTableLayout& VTLayout);
    CppSharp::AST::VTableComponent WalkVTableComponent(const clang::VTableComponent& Component);
    CppSharp::AST::PreprocessedEntity^ WalkPreprocessedEntity(CppSharp::AST::Declaration^ Decl,
        clang::PreprocessedEntity* PPEntity);

    // Clang helpers
    SourceLocationKind GetLocationKind(const clang::SourceLocation& Loc);
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(clang::Decl* D, clang::TargetCXXABI ABI,
        bool IsDependent = false);
    std::string GetTypeName(const clang::Type* Type);
    void WalkFunction(clang::FunctionDecl* FD, CppSharp::AST::Function^ F,
        bool IsDependent = false);
    void HandlePreprocessedEntities(CppSharp::AST::Declaration^ Decl, clang::SourceRange sourceRange,
                                    CppSharp::AST::MacroLocation macroLocation = CppSharp::AST::MacroLocation::Unknown);
    bool GetDeclText(clang::SourceRange SR, std::string& Text);

    CppSharp::AST::TranslationUnit^ GetTranslationUnit(clang::SourceLocation Loc,
        SourceLocationKind *Kind = 0);
    CppSharp::AST::TranslationUnit^ GetTranslationUnit(const clang::Decl* D);

    CppSharp::AST::DeclarationContext^ GetNamespace(clang::Decl* D, clang::DeclContext* Ctx);
    CppSharp::AST::DeclarationContext^ GetNamespace(clang::Decl* D);

    void HandleDeclaration(clang::Decl* D, CppSharp::AST::Declaration^ Decl);
    void HandleOriginalText(clang::Decl* D, CppSharp::AST::Declaration^ Decl);
    void HandleComments(clang::Decl* D, CppSharp::AST::Declaration^ Decl);
    void HandleDiagnostics(ParserResult^ res);

    int Index;
    gcroot<CppSharp::AST::ASTContext^> Lib;
    gcroot<CppSharp::AST::SymbolContext^> Symbols;
    gcroot<ParserOptions^> Opts;
    llvm::OwningPtr<clang::CompilerInstance> C;
    clang::ASTContext* AST;
    clang::TargetCXXABI::Kind TargetABI;
    clang::TargetCodeGenInfo* CodeGenInfo;
    clang::CodeGen::CodeGenTypes* CodeGenTypes;
};
