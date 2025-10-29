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
#include <unordered_map>
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
} // namespace clang

#define Debug printf

namespace CppSharp { namespace CppParser {
    class ASTNameMangler;

    class Parser
    {
        friend class ASTNodeVisitor;

    public:
        Parser(CppParserOptions* Opts);

        void Setup(bool Compile = false);
        ParserResult* Parse(const std::vector<std::string>& SourceFiles);
        static ParserResult* ParseLibrary(const CppLinkerOptions* Opts);
        ParserResult* Build(const CppLinkerOptions* LinkerOptions, const std::string& File, bool Last);
        ParserResult* Compile(const std::string& File);
        bool Link(const std::string& File, const CppLinkerOptions* LinkerOptions);
        void WalkAST(clang::TranslationUnitDecl* TU);
        void HandleDeclaration(const clang::Decl* D, AST::Declaration* Decl);
        CppParserOptions* opts = nullptr;

    private:

        void SetupLLVMCodegen();
        bool SetupSourceFiles(const std::vector<std::string>& SourceFiles,
                              std::vector<clang::OptionalFileEntryRef>& FileEntries);

        bool IsSupported(const clang::NamedDecl* ND);
        bool IsSupported(const clang::CXXMethodDecl* MD);
        // AST traversers
        AST::Declaration* WalkDeclaration(const clang::Decl* D);
        AST::Declaration* WalkDeclarationDef(clang::Decl* D);
        AST::Enumeration* WalkEnum(const clang::EnumDecl* ED);
        AST::Enumeration::Item* WalkEnumItem(clang::EnumConstantDecl* ECD);
        AST::Function* WalkFunction(const clang::FunctionDecl* FD);
        void EnsureCompleteRecord(const clang::RecordDecl* Record, AST::DeclarationContext* NS, AST::Class* RC);
        AST::Class* GetRecord(const clang::RecordDecl* Record, bool& IsComplete);
        AST::Class* WalkRecord(const clang::RecordDecl* Record);
        void WalkRecord(const clang::RecordDecl* Record, AST::Class* RC);
        AST::Class* WalkRecordCXX(const clang::CXXRecordDecl* Record);
        void WalkRecordCXX(const clang::CXXRecordDecl* Record, AST::Class* RC);
        AST::ClassTemplateSpecialization*
        WalkClassTemplateSpecialization(const clang::ClassTemplateSpecializationDecl* CTS);
        AST::ClassTemplatePartialSpecialization*
        WalkClassTemplatePartialSpecialization(const clang::ClassTemplatePartialSpecializationDecl* CTS);
        AST::Method* WalkMethodCXX(const clang::CXXMethodDecl* MD);
        AST::Field* WalkFieldCXX(const clang::FieldDecl* FD, AST::Class* Class);
        AST::FunctionTemplateSpecialization* WalkFunctionTemplateSpec(clang::FunctionTemplateSpecializationInfo* FTS, AST::Function* Function);
        AST::Variable* WalkVariable(const clang::VarDecl* VD);
        void WalkVariable(const clang::VarDecl* VD, AST::Variable* Var);
        AST::Friend* WalkFriend(const clang::FriendDecl* FD);
        AST::RawComment* WalkRawComment(const clang::RawComment* RC);
        AST::Type* WalkType(clang::QualType QualType, const clang::TypeLoc* TL = 0, bool DesugarType = false);
        AST::TemplateArgument WalkTemplateArgument(const clang::TemplateArgument& TA, clang::TemplateArgumentLoc* ArgLoc = 0);
        AST::TemplateTemplateParameter* WalkTemplateTemplateParameter(const clang::TemplateTemplateParmDecl* TTP);
        AST::TypeTemplateParameter* WalkTypeTemplateParameter(const clang::TemplateTypeParmDecl* TTPD);
        AST::NonTypeTemplateParameter* WalkNonTypeTemplateParameter(const clang::NonTypeTemplateParmDecl* TTPD);
        AST::UnresolvedUsingTypename* WalkUnresolvedUsingTypename(const clang::UnresolvedUsingTypenameDecl* UUTD);
        std::vector<AST::Declaration*> WalkTemplateParameterList(const clang::TemplateParameterList* TPL);
        AST::TypeAliasTemplate* WalkTypeAliasTemplate(const clang::TypeAliasTemplateDecl* TD);
        AST::ClassTemplate* WalkClassTemplate(const clang::ClassTemplateDecl* TD);
        AST::FunctionTemplate* WalkFunctionTemplate(const clang::FunctionTemplateDecl* TD);
        AST::VarTemplate* WalkVarTemplate(const clang::VarTemplateDecl* VT);
        AST::VarTemplateSpecialization*
        WalkVarTemplateSpecialization(const clang::VarTemplateSpecializationDecl* VTS);
        AST::VarTemplatePartialSpecialization*
        WalkVarTemplatePartialSpecialization(const clang::VarTemplatePartialSpecializationDecl* VTS);
        template <typename TypeLoc>
        std::vector<AST::TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, TypeLoc* TSTL);
        template <typename TypeLoc>
        std::vector<AST::TemplateArgument> WalkTemplateArgumentList(llvm::ArrayRef<clang::TemplateArgument> TAL, TypeLoc* TSTL);
        std::vector<AST::TemplateArgument> WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, const clang::ASTTemplateArgumentListInfo* TSTL);
        void WalkVTable(const clang::CXXRecordDecl* RD, AST::Class* C);
        AST::QualifiedType GetQualifiedType(clang::QualType qual, const clang::TypeLoc* TL = 0);
        void ReadClassLayout(AST::Class* Class, const clang::RecordDecl* RD, clang::CharUnits Offset, bool IncludeVirtualBases);
        AST::LayoutField WalkVTablePointer(AST::Class* Class, const clang::CharUnits& Offset, const std::string& prefix);
        AST::VTableLayout WalkVTableLayout(const clang::VTableLayout& VTLayout);
        AST::VTableComponent WalkVTableComponent(const clang::VTableComponent& Component);
        AST::PreprocessedEntity* WalkPreprocessedEntity(AST::Declaration* Decl,
                                                        clang::PreprocessedEntity* PPEntity);
        AST::ExpressionObsolete* WalkVariableInitializerExpression(const clang::Expr* Expression);
        AST::ExpressionObsolete* WalkExpressionObsolete(const clang::Expr* Expression);
        AST::Stmt* WalkStatement(const clang::Stmt* Stmt);
        AST::Expr* WalkExpression(const clang::Expr* Stmt);
        std::string GetStringFromStatement(const clang::Stmt* Statement);
        std::string GetFunctionBody(const clang::FunctionDecl* FD);
        static bool IsCastStmt(clang::Stmt::StmtClass stmt);
        static bool IsLiteralStmt(clang::Stmt::StmtClass stmt);

        // Clang helpers
        SourceLocationKind GetLocationKind(const clang::SourceLocation& Loc);
        bool IsValidDeclaration(const clang::SourceLocation& Loc);
        std::string GetDeclMangledName(const clang::Decl* D) const;
        std::string GetTypeName(const clang::Type* Type) const;
        bool CanCheckCodeGenInfo(const clang::Type* Ty);
        void CompleteIfSpecializationType(const clang::QualType& QualType);
        AST::Parameter* WalkParameter(const clang::ParmVarDecl* PVD,
                                      const clang::SourceLocation& ParamStartLoc);
        void SetBody(const clang::FunctionDecl* FD, AST::Function* F);
        void MarkValidity(AST::Function* F);
        void WalkFunction(const clang::FunctionDecl* FD, AST::Function* F);
        int GetAlignAs(const clang::AlignedAttr* alignedAttr);
        void HandlePreprocessedEntities(AST::Declaration* Decl);
        void HandlePreprocessedEntities(AST::Declaration* Decl, clang::SourceRange sourceRange, AST::MacroLocation macroLocation = AST::MacroLocation::Unknown);
        bool GetDeclText(clang::SourceRange SR, std::string& Text);
        bool HasLayout(const clang::RecordDecl* Record);

        AST::TranslationUnit* GetTranslationUnit(clang::SourceLocation Loc,
                                                 SourceLocationKind* Kind = 0);
        AST::TranslationUnit* GetTranslationUnit(const clang::Decl* D);

        AST::DeclarationContext* GetNamespace(const clang::Decl* D, const clang::DeclContext* Ctx);
        AST::DeclarationContext* GetNamespace(const clang::Decl* D);

        void HandleOriginalText(const clang::Decl* D, AST::Declaration* Decl);
        void HandleComments(const clang::Decl* D, AST::Declaration* Decl);
        void HandleDiagnostics(ParserResult* res);

        ParserResultKind ReadSymbols(llvm::StringRef File,
                                     llvm::object::basic_symbol_iterator Begin,
                                     llvm::object::basic_symbol_iterator End,
                                     AST::NativeLibrary*& NativeLib);
        AST::Declaration* GetDeclarationFromFriend(clang::NamedDecl* FriendDecl);
        static ParserResultKind ParseArchive(const std::string& File,
                                             llvm::object::Archive* Archive,
                                             std::vector<AST::NativeLibrary*>& NativeLibs);
        static ParserResultKind ParseSharedLib(const std::string& File,
                                               llvm::object::ObjectFile* ObjectFile,
                                               std::vector<AST::NativeLibrary*>& NativeLibs);
        ParserTargetInfo* GetTargetInfo();

        bool LinkWindows(const CppLinkerOptions* LinkerOptions, std::vector<const char*>& args, const llvm::StringRef& Dir, llvm::StringRef& Stem, bool MinGW = false);
        bool LinkELF(const CppLinkerOptions* LinkerOptions, std::vector<const char*>& args, llvm::StringRef& Dir, llvm::StringRef& Stem);
        bool LinkMachO(const CppLinkerOptions* LinkerOptions, std::vector<const char*>& args, llvm::StringRef& Dir, llvm::StringRef& Stem);

        int index = 0;
        std::unique_ptr<clang::CompilerInstance> c;
        llvm::LLVMContext LLVMCtx;
        std::unique_ptr<ASTNameMangler> NameMangler;
        std::unique_ptr<llvm::Module> LLVMModule;
        std::unique_ptr<clang::CodeGen::CodeGenModule> CGM;
        std::unique_ptr<clang::CodeGen::CodeGenTypes> codeGenTypes;
        std::unordered_map<const clang::DeclContext*, AST::DeclarationContext*> walkedNamespaces;
        std::unordered_map<const clang::TemplateTypeParmDecl*, AST::TypeTemplateParameter*> walkedTypeTemplateParameters;
        std::unordered_map<const clang::TemplateTemplateParmDecl*, AST::TemplateTemplateParameter*> walkedTemplateTemplateParameters;
        std::unordered_map<const clang::NonTypeTemplateParmDecl*, AST::NonTypeTemplateParameter*> walkedNonTypeTemplateParameters;
        std::unordered_map<const clang::ParmVarDecl*, AST::Parameter*> walkedParameters;
        std::unordered_set<std::string> supportedStdTypes;
        std::unordered_set<std::string> supportedFunctionTemplates;
    };

}} // namespace CppSharp::CppParser