/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#pragma once

#include <llvm/Object/Archive.h>
#include <llvm/Object/ObjectFile.h>
#include <llvm/Object/SymbolicFile.h>

#include <clang/AST/ASTFwd.h>
#include <clang/AST/Type.h>
#include <clang/Basic/TargetInfo.h>
#include <clang/Frontend/CompilerInstance.h>

#include "CXXABI.h"
#include "CppParser.h"

#include <string>
typedef std::string String;

namespace clang {
  class TargetCodeGenInfo;
  namespace CodeGen {
    class CodeGenTypes;
  }
  struct ASTTemplateArgumentListInfo;
  class FunctionTemplateSpecialization;
  class FunctionTemplateSpecializationInfo;
  class PreprocessingRecord;
  class PreprocessedEntity;
  class RawComment;
  class TemplateSpecializationTypeLoc;
  class TemplateArgumentList;
  class VTableLayout;
  class VTableComponent;
}

#define Debug printf

namespace CppSharp { namespace CppParser {

class Parser
{
public:
    Parser(ParserOptions* Opts);

    void SetupHeader();
    ParserResult* ParseHeader(const std::string& File, ParserResult* res);
    ParserResult* ParseLibrary(const std::string& File, ParserResult* res);
    ParserResultKind ParseArchive(llvm::StringRef File,
                                  llvm::object::Archive* Archive,
                                  CppSharp::CppParser::NativeLibrary*& NativeLib);
    ParserResultKind ParseSharedLib(llvm::StringRef File,
                                    llvm::object::ObjectFile* ObjectFile,
                                    CppSharp::CppParser::NativeLibrary*& NativeLib);
    ParserTargetInfo*  GetTargetInfo();

protected:

    // AST traversers
    void WalkAST();
    void WalkMacros(clang::PreprocessingRecord* PR);
    Declaration* WalkDeclaration(clang::Decl* D,
        bool IgnoreSystemDecls = true, bool CanBeDefinition = false);
    Declaration* WalkDeclarationDef(clang::Decl* D);
    Enumeration* WalkEnum(clang::EnumDecl* ED);
	Enumeration::Item* WalkEnumItem(clang::EnumConstantDecl* ECD);
    Function* WalkFunction(clang::FunctionDecl* FD, bool IsDependent = false,
        bool AddToNamespace = true);
    Class* GetRecord(clang::RecordDecl* Record, bool& IsComplete);
    Class* WalkRecord(clang::RecordDecl* Record);
    void WalkRecord(clang::RecordDecl* Record, Class* RC);
    Class* WalkRecordCXX(clang::CXXRecordDecl* Record);
    void WalkRecordCXX(clang::CXXRecordDecl* Record, Class* RC);
    ClassTemplateSpecialization*
    WalkClassTemplateSpecialization(clang::ClassTemplateSpecializationDecl* CTS);
    ClassTemplatePartialSpecialization*
    WalkClassTemplatePartialSpecialization(clang::ClassTemplatePartialSpecializationDecl* CTS);
    Method* WalkMethodCXX(clang::CXXMethodDecl* MD, bool AddToClass = true);
    Field* WalkFieldCXX(clang::FieldDecl* FD, Class* Class);
    ClassTemplate* WalkClassTemplate(clang::ClassTemplateDecl* TD);
    FunctionTemplate* WalkFunctionTemplate(clang::FunctionTemplateDecl* TD);
    FunctionTemplateSpecialization* WalkFunctionTemplateSpec(clang::FunctionTemplateSpecializationInfo* FTS, Function* Function);
    Variable* WalkVariable(clang::VarDecl* VD);
    Friend* WalkFriend(clang::FriendDecl* FD);
    RawComment* WalkRawComment(const clang::RawComment* RC);
    Type* WalkType(clang::QualType QualType, clang::TypeLoc* TL = 0,
      bool DesugarType = false);
    TemplateArgument WalkTemplateArgument(const clang::TemplateArgument& TA, clang::TemplateArgumentLoc* ArgLoc);
    std::vector<TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, clang::TemplateSpecializationTypeLoc* TSTL);
    std::vector<TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, const clang::ASTTemplateArgumentListInfo* TSTL);
    void WalkVTable(clang::CXXRecordDecl* RD, Class* C);
    VTableLayout WalkVTableLayout(const clang::VTableLayout& VTLayout);
    VTableComponent WalkVTableComponent(const clang::VTableComponent& Component);
    PreprocessedEntity* WalkPreprocessedEntity(Declaration* Decl,
        clang::PreprocessedEntity* PPEntity);
    AST::Expression* WalkExpression(clang::Expr* Expression);
    std::string GetStringFromStatement(const clang::Stmt* Statement);

    // Clang helpers
    SourceLocationKind GetLocationKind(const clang::SourceLocation& Loc);
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(clang::Decl* D);
    std::string GetTypeName(const clang::Type* Type);
    void WalkFunction(clang::FunctionDecl* FD, Function* F,
        bool IsDependent = false);
    void HandlePreprocessedEntities(Declaration* Decl);
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
    std::unique_ptr<clang::CompilerInstance> C;
    clang::ASTContext* AST;
    clang::TargetCXXABI::Kind TargetABI;
    clang::TargetCodeGenInfo* CodeGenInfo;
    clang::CodeGen::CodeGenTypes* CodeGenTypes;

private:
    ParserResultKind ReadSymbols(llvm::StringRef File,
                                 llvm::object::basic_symbol_iterator Begin,
                                 llvm::object::basic_symbol_iterator End,
                                 CppSharp::CppParser::NativeLibrary*& NativeLib);
    Declaration* GetDeclarationFromFriend(clang::NamedDecl* FriendDecl);
};

} }