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
#include <clang/Sema/Scope.h>

#include <CodeGen/CodeGenModule.h>
#include <CodeGen/CodeGenTypes.h>

#include "CXXABI.h"
#include "CppParser.h"

#include <string>
#include <unordered_set>

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

    void Setup();
    ParserResult* Parse(const std::vector<std::string>& SourceFiles);
    static ParserResult* ParseLibrary(const LinkerOptions* Opts);

    void WalkAST(clang::TranslationUnitDecl* TU);
    void HandleDeclaration(const clang::Decl* D, Declaration* Decl);
    CppParserOptions* opts;

private:

    void SetupLLVMCodegen();
    bool SetupSourceFiles(const std::vector<std::string>& SourceFiles,
        std::vector<const clang::FileEntry*>& FileEntries);

    bool IsSupported(const clang::NamedDecl* ND);
    bool IsSupported(const clang::CXXMethodDecl* MD);
    // AST traversers
    Declaration* WalkDeclaration(const clang::Decl* D);
    Declaration* WalkDeclarationDef(clang::Decl* D);
    Enumeration* WalkEnum(const clang::EnumDecl* ED);
	Enumeration::Item* WalkEnumItem(clang::EnumConstantDecl* ECD);
    Function* WalkFunction(const clang::FunctionDecl* FD);
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
    Type* WalkType(clang::QualType QualType, const clang::TypeLoc* TL = 0,
      bool DesugarType = false);
    TemplateArgument WalkTemplateArgument(clang::TemplateArgument TA, clang::TemplateArgumentLoc* ArgLoc = 0);
    TemplateTemplateParameter* WalkTemplateTemplateParameter(const clang::TemplateTemplateParmDecl* TTP);
    TypeTemplateParameter* WalkTypeTemplateParameter(const clang::TemplateTypeParmDecl* TTPD);
    NonTypeTemplateParameter* WalkNonTypeTemplateParameter(const clang::NonTypeTemplateParmDecl* TTPD);
    UnresolvedUsingTypename* WalkUnresolvedUsingTypename(const clang::UnresolvedUsingTypenameDecl* UUTD);
    std::vector<Declaration*> WalkTemplateParameterList(const clang::TemplateParameterList* TPL);
    TypeAliasTemplate* WalkTypeAliasTemplate(const clang::TypeAliasTemplateDecl* TD);
    ClassTemplate* WalkClassTemplate(const clang::ClassTemplateDecl* TD);
    FunctionTemplate* WalkFunctionTemplate(const clang::FunctionTemplateDecl* TD);
    VarTemplate* WalkVarTemplate(const clang::VarTemplateDecl* VT);
    VarTemplateSpecialization*
    WalkVarTemplateSpecialization(const clang::VarTemplateSpecializationDecl* VTS);
    VarTemplatePartialSpecialization*
    WalkVarTemplatePartialSpecialization(const clang::VarTemplatePartialSpecializationDecl* VTS);
    template<typename TypeLoc>
    std::vector<TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, TypeLoc* TSTL);
    std::vector<TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, const clang::ASTTemplateArgumentListInfo* TSTL);
    void WalkVTable(const clang::CXXRecordDecl* RD, Class* C);
    QualifiedType GetQualifiedType(clang::QualType qual, const clang::TypeLoc* TL = 0);
    void ReadClassLayout(Class* Class, const clang::RecordDecl* RD, clang::CharUnits Offset, bool IncludeVirtualBases);
    LayoutField WalkVTablePointer(Class* Class, const clang::CharUnits& Offset, const std::string& prefix);
    VTableLayout WalkVTableLayout(const clang::VTableLayout& VTLayout);
    VTableComponent WalkVTableComponent(const clang::VTableComponent& Component);
    PreprocessedEntity* WalkPreprocessedEntity(Declaration* Decl,
        clang::PreprocessedEntity* PPEntity);
    AST::ExpressionObsolete* WalkExpressionObsolete(const clang::Expr* Expression);
    AST::Stmt* WalkStatement(const clang::Stmt* Stmt);
    AST::Expr* WalkExpression(const clang::Expr* Stmt);
    std::string GetStringFromStatement(const clang::Stmt* Statement);
    std::string GetFunctionBody(const clang::FunctionDecl* FD);

    // Clang helpers
    SourceLocationKind GetLocationKind(const clang::SourceLocation& Loc);
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(const clang::Decl* D);
    std::string GetTypeName(const clang::Type* Type);
    bool CanCheckCodeGenInfo(clang::Sema & S, const clang::Type * Ty);
    void CompleteIfSpecializationType(const clang::QualType& QualType);
    Parameter* WalkParameter(const clang::ParmVarDecl* PVD,
        const clang::SourceLocation& ParamStartLoc);
    void SetBody(const clang::FunctionDecl* FD, Function* F);
    std::stack<clang::Scope> GetScopesFor(clang::FunctionDecl* FD);
    void MarkValidity(Function* F);
    void WalkFunction(const clang::FunctionDecl* FD, Function* F);
    void HandlePreprocessedEntities(Declaration* Decl);
    void HandlePreprocessedEntities(Declaration* Decl, clang::SourceRange sourceRange,
                                    MacroLocation macroLocation = MacroLocation::Unknown);
    bool GetDeclText(clang::SourceRange SR, std::string& Text);
    bool HasLayout(const clang::RecordDecl* Record);

    TranslationUnit* GetTranslationUnit(clang::SourceLocation Loc,
        SourceLocationKind *Kind = 0);
    TranslationUnit* GetTranslationUnit(const clang::Decl* D);

    DeclarationContext* GetNamespace(const clang::Decl* D, const clang::DeclContext* Ctx);
    DeclarationContext* GetNamespace(const clang::Decl* D);

    void HandleOriginalText(const clang::Decl* D, Declaration* Decl);
    void HandleComments(const clang::Decl* D, Declaration* Decl);
    void HandleDiagnostics(ParserResult* res);

    ParserResultKind ReadSymbols(llvm::StringRef File,
                                 llvm::object::basic_symbol_iterator Begin,
                                 llvm::object::basic_symbol_iterator End,
                                 CppSharp::CppParser::NativeLibrary*& NativeLib);
    Declaration* GetDeclarationFromFriend(clang::NamedDecl* FriendDecl);
    static ParserResultKind ParseArchive(const std::string& File,
        llvm::object::Archive* Archive, std::vector<CppSharp::CppParser::NativeLibrary*>& NativeLibs);
    static ParserResultKind ParseSharedLib(const std::string& File,
        llvm::object::ObjectFile* ObjectFile, std::vector<CppSharp::CppParser::NativeLibrary*>& NativeLibs);
    ParserTargetInfo* GetTargetInfo();

    int index;
    std::unique_ptr<clang::CompilerInstance> c;
    llvm::LLVMContext LLVMCtx;
    std::unique_ptr<llvm::Module> LLVMModule;
    std::unique_ptr<clang::CodeGen::CodeGenModule> CGM;
    std::unique_ptr<clang::CodeGen::CodeGenTypes> codeGenTypes;
    std::unordered_map<const clang::TemplateTypeParmDecl*, TypeTemplateParameter*> walkedTypeTemplateParameters;
    std::unordered_map<const clang::TemplateTemplateParmDecl*, TemplateTemplateParameter*> walkedTemplateTemplateParameters;
    std::unordered_map<const clang::NonTypeTemplateParmDecl*, NonTypeTemplateParameter*> walkedNonTypeTemplateParameters;
    std::unordered_map<const clang::ParmVarDecl*, Parameter*> walkedParameters;
    std::unordered_set<std::string> supportedStdTypes;
};

} }