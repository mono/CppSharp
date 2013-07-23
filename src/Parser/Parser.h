/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

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
#include <clang/Lex/Preprocessor.h>
#include <clang/Lex/PreprocessingRecord.h>
#include <clang/Parse/ParseAST.h>
#include <clang/Sema/Sema.h>
#include "CXXABI.h"

#include <string>
#include <cstdarg>

#using <CppSharp.AST.dll>
#include <vcclr.h>

#define ARRAY_SIZE(arr) (sizeof(arr) / sizeof(arr[0]))
#define Debug printf

using namespace System::Collections::Generic;

public ref struct ParserOptions
{
    ParserOptions()
    {
        IncludeDirs = gcnew List<System::String^>();
        SystemIncludeDirs = gcnew List<System::String^>();
        Defines = gcnew List<System::String^>();
        LibraryDirs = gcnew List<System::String^>();
        MicrosoftMode = false;
        NoStandardIncludes = false;
        NoBuiltinIncludes = false;
    }

    // Include directories
    List<System::String^>^ IncludeDirs;
    List<System::String^>^ SystemIncludeDirs;
    List<System::String^>^ Defines;
    List<System::String^>^ LibraryDirs;

    // C/C++ header file name.
    System::String^ FileName;

    CppSharp::AST::Library^ Library;

    // Toolset version - 2005 - 8, 2008 - 9, 2010 - 10, 0 - autoprobe for any.
    int ToolSetToUse;
    System::String^ TargetTriple;

    bool NoStandardIncludes;
    bool NoBuiltinIncludes;
    bool MicrosoftMode;

    bool Verbose;
};

public enum struct ParserDiagnosticLevel
{
    Ignored,
    Note,
    Warning,
    Error,
    Fatal
};

public value struct ParserDiagnostic
{
    System::String^ FileName;
    System::String^ Message;
    ParserDiagnosticLevel Level;
    int LineNumber;
    int ColumnNumber;
};

public enum struct ParserResultKind
{
    Success,
    Error,
    FileNotFound
};

public ref struct ParserResult
{
    ParserResult()
    {
        Diagnostics = gcnew List<ParserDiagnostic>(); 
    }

    ParserResultKind Kind;
    CppSharp::AST::Library^ Library;
    List<ParserDiagnostic>^ Diagnostics;
};

enum class SourceLocationKind
{
    Invalid,
    Builtin,
    CommandLine,
    System,
    User
};

struct Parser
{
    Parser(ParserOptions^ Opts);

    void SetupHeader();
    ParserResult^ ParseHeader(const std::string& File);
    ParserResult^ ParseLibrary(const std::string& File);
    ParserResultKind ParseArchive(llvm::StringRef File,
                                  llvm::MemoryBuffer *Buffer);
    ParserResultKind ParseSharedLib(llvm::StringRef File,
                                    llvm::MemoryBuffer *Buffer);

protected:

    // AST traversers
    void WalkAST();
    void WalkMacros(clang::PreprocessingRecord* PR);
    CppSharp::AST::Declaration^ WalkDeclaration(clang::Decl* D,
        bool IgnoreSystemDecls = true, bool CanBeDefinition = false);
    CppSharp::AST::Declaration^ WalkDeclarationDef(clang::Decl* D);
    CppSharp::AST::Enumeration^ WalkEnum(clang::EnumDecl*);
    CppSharp::AST::Function^ WalkFunction(clang::FunctionDecl*, bool IsDependent = false,
        bool AddToNamespace = true);
    CppSharp::AST::Class^ WalkRecordCXX(clang::CXXRecordDecl*);
    CppSharp::AST::Method^ WalkMethodCXX(clang::CXXMethodDecl*);
    CppSharp::AST::Field^ WalkFieldCXX(clang::FieldDecl*, CppSharp::AST::Class^);
    CppSharp::AST::ClassTemplate^ Parser::WalkClassTemplate(clang::ClassTemplateDecl*);
    CppSharp::AST::FunctionTemplate^ Parser::WalkFunctionTemplate(
        clang::FunctionTemplateDecl*);
    CppSharp::AST::Variable^ WalkVariable(clang::VarDecl*);
	CppSharp::AST::RawComment^ WalkRawComment(const clang::RawComment*);
    CppSharp::AST::Type^ WalkType(clang::QualType, clang::TypeLoc* = 0,
      bool DesugarType = false);

    // Clang helpers
    SourceLocationKind GetLocationKind(const clang::SourceLocation& Loc);
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(clang::Decl*, clang::TargetCXXABI,
        bool IsDependent = false);
    std::string GetTypeName(const clang::Type*);
    void HandleComments(clang::Decl* D, CppSharp::AST::Declaration^);
    void WalkFunction(clang::FunctionDecl* FD, CppSharp::AST::Function^ F,
        bool IsDependent = false);
    void HandlePreprocessedEntities(CppSharp::AST::Declaration^ Decl, clang::SourceRange sourceRange,
                                    CppSharp::AST::MacroLocation macroLocation = CppSharp::AST::MacroLocation::Unknown);
    bool GetDeclText(clang::SourceRange, std::string& Text);

    CppSharp::AST::TranslationUnit^ GetTranslationUnit(clang::SourceLocation Loc,
        SourceLocationKind *Kind = 0);
    CppSharp::AST::TranslationUnit^ GetTranslationUnit(const clang::Decl*);

    CppSharp::AST::DeclarationContext^ GetNamespace(clang::Decl*, clang::DeclContext*);
    CppSharp::AST::DeclarationContext^ GetNamespace(clang::Decl*);

    clang::CallingConv GetAbiCallConv(clang::CallingConv CC,
        bool IsInstMethod, bool IsVariadic);

    int Index;
    gcroot<CppSharp::AST::Library^> Lib;
    gcroot<ParserOptions^> Opts;
    llvm::OwningPtr<clang::CompilerInstance> C;
    clang::ASTContext* AST;
    clang::TargetCXXABI::Kind TargetABI;
};

//-----------------------------------//

typedef std::string String;

String StringFormatArgs(const char* str, va_list args);
String StringFormat(const char* str, ...);
