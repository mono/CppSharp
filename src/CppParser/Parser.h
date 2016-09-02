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
#include <clang/AST/DeclTemplate.h>
#include <clang/AST/Type.h>
#include <clang/Basic/TargetInfo.h>
#include <clang/Frontend/CompilerInstance.h>

#include "CXXABI.h"
#include "CppParser.h"

#include <string>

namespace clang {
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
    Parser(CppParserOptions* Opts);

    void SetupHeader();
    ParserResult* ParseHeader(const std::vector<std::string>& SourceFiles, ParserResult* res);
    ParserResult* ParseLibrary(const std::string& File, ParserResult* res);
    ParserResultKind ParseArchive(llvm::StringRef File,
                                  llvm::object::Archive* Archive,
                                  CppSharp::CppParser::NativeLibrary*& NativeLib);
    ParserResultKind ParseSharedLib(llvm::StringRef File,
                                    llvm::object::ObjectFile* ObjectFile,
                                    CppSharp::CppParser::NativeLibrary*& NativeLib);
    ParserTargetInfo*  GetTargetInfo();

private:
    // AST traversers
    void WalkAST();
    Declaration* WalkDeclaration(const clang::Decl* D, bool CanBeDefinition = false);
    Declaration* WalkDeclarationDef(clang::Decl* D);
    Enumeration* WalkEnum(const clang::EnumDecl* ED);
	Enumeration::Item* WalkEnumItem(clang::EnumConstantDecl* ECD);
    Function* WalkFunction(const clang::FunctionDecl* FD, bool IsDependent = false,
        bool AddToNamespace = true);
    void EnsureCompleteRecord(const clang::RecordDecl* Record, DeclarationContext* NS, Class* RC);
    Class* GetRecord(const clang::RecordDecl* Record, bool& IsComplete);
    Class* WalkRecord(const clang::RecordDecl* Record);
    void WalkRecord(const clang::RecordDecl* Record, Class* RC);
    Class* WalkRecordCXX(const clang::CXXRecordDecl* Record);
    void WalkRecordCXX(const clang::CXXRecordDecl* Record, Class* RC);
    ClassTemplateSpecialization*
    WalkClassTemplateSpecialization(const clang::ClassTemplateSpecializationDecl* CTS);
    ClassTemplatePartialSpecialization*
    WalkClassTemplatePartialSpecialization(const clang::ClassTemplatePartialSpecializationDecl* CTS);
    Method* WalkMethodCXX(const clang::CXXMethodDecl* MD);
    Field* WalkFieldCXX(const clang::FieldDecl* FD, Class* Class);
    FunctionTemplateSpecialization* WalkFunctionTemplateSpec(clang::FunctionTemplateSpecializationInfo* FTS, Function* Function);
    Variable* WalkVariable(const clang::VarDecl* VD);
    void WalkVariable(const clang::VarDecl* VD, Variable* Var);
    Friend* WalkFriend(const clang::FriendDecl* FD);
    RawComment* WalkRawComment(const clang::RawComment* RC);
    bool ShouldCompleteType(const clang::QualType& QualType, bool LocValid);
    Type* WalkType(clang::QualType QualType, clang::TypeLoc* TL = 0,
      bool DesugarType = false);
    TemplateArgument WalkTemplateArgument(const clang::TemplateArgument& TA, clang::TemplateArgumentLoc* ArgLoc);
    TemplateTemplateParameter* WalkTemplateTemplateParameter(const clang::TemplateTemplateParmDecl* TTP);
    TypeTemplateParameter* WalkTypeTemplateParameter(const clang::TemplateTypeParmDecl* TTPD);
    NonTypeTemplateParameter* WalkNonTypeTemplateParameter(const clang::NonTypeTemplateParmDecl* TTPD);
    std::vector<Declaration*> WalkTemplateParameterList(const clang::TemplateParameterList* TPL);
    TypeAliasTemplate* WalkTypeAliasTemplate(const clang::TypeAliasTemplateDecl* TD);
    ClassTemplate* WalkClassTemplate(const clang::ClassTemplateDecl* TD);
    FunctionTemplate* WalkFunctionTemplate(const clang::FunctionTemplateDecl* TD);
    VarTemplate* WalkVarTemplate(const clang::VarTemplateDecl* VT);
    VarTemplateSpecialization*
    WalkVarTemplateSpecialization(const clang::VarTemplateSpecializationDecl* VTS);
    VarTemplatePartialSpecialization*
    WalkVarTemplatePartialSpecialization(const clang::VarTemplatePartialSpecializationDecl* VTS);
    std::vector<TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, clang::TemplateSpecializationTypeLoc* TSTL);
    std::vector<TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, const clang::ASTTemplateArgumentListInfo* TSTL);
    void WalkVTable(const clang::CXXRecordDecl* RD, Class* C);
    QualifiedType GetQualifiedType(const clang::QualType& qual, clang::TypeLoc* TL = 0);
    void ReadClassLayout(Class* Class, const clang::RecordDecl* RD, clang::CharUnits Offset, bool IncludeVirtualBases);
    LayoutField WalkVTablePointer(Class* Class, const clang::CharUnits& Offset, const std::string& prefix);
    VTableLayout WalkVTableLayout(const clang::VTableLayout& VTLayout);
    VTableComponent WalkVTableComponent(const clang::VTableComponent& Component);
    PreprocessedEntity* WalkPreprocessedEntity(Declaration* Decl,
        clang::PreprocessedEntity* PPEntity);
    AST::Expression* WalkExpression(clang::Expr* Expression);
    std::string GetStringFromStatement(const clang::Stmt* Statement);

    // Clang helpers
    SourceLocationKind GetLocationKind(const clang::SourceLocation& Loc);
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(const clang::Decl* D);
    std::string GetTypeName(const clang::Type* Type);
    bool CanCheckCodeGenInfo(clang::Sema & S, const clang::Type * Ty);
    void WalkFunction(const clang::FunctionDecl* FD, Function* F,
        bool IsDependent = false);
    void HandlePreprocessedEntities(Declaration* Decl);
    void HandlePreprocessedEntities(Declaration* Decl, clang::SourceRange sourceRange,
                                    MacroLocation macroLocation = MacroLocation::Unknown);
    bool GetDeclText(clang::SourceRange SR, std::string& Text);

    TranslationUnit* GetTranslationUnit(clang::SourceLocation Loc,
        SourceLocationKind *Kind = 0);
    TranslationUnit* GetTranslationUnit(const clang::Decl* D);

    DeclarationContext* GetNamespace(const clang::Decl* D, const clang::DeclContext* Ctx);
    DeclarationContext* GetNamespace(const clang::Decl* D);

    void HandleDeclaration(const clang::Decl* D, Declaration* Decl);
    void HandleOriginalText(const clang::Decl* D, Declaration* Decl);
    void HandleComments(const clang::Decl* D, Declaration* Decl);
    void HandleDiagnostics(ParserResult* res);

    int Index;
    ASTContext* Lib;
    CppParserOptions* Opts;
    std::unique_ptr<clang::CompilerInstance> C;
    clang::ASTContext* AST;
    clang::TargetCXXABI::Kind TargetABI;
    clang::CodeGen::CodeGenTypes* CodeGenTypes;

    ParserResultKind ReadSymbols(llvm::StringRef File,
                                 llvm::object::basic_symbol_iterator Begin,
                                 llvm::object::basic_symbol_iterator End,
                                 CppSharp::CppParser::NativeLibrary*& NativeLib);
    Declaration* GetDeclarationFromFriend(clang::NamedDecl* FriendDecl);
};

} }