/************************************************************************
*
* Flush3D <http://www.flush3d.com> © (2008-201x) 
* Licensed under the LGPL 2.1 (GNU Lesser General Public License)
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
#include <clang/AST/RecordLayout.h>
#include <clang/Lex/Preprocessor.h>
#include <clang/Parse/ParseAST.h>
#include <clang/Sema/Sema.h>
#include "CXXABI.h"

#include <string>
#include <cstdarg>

#using <Bridge.dll>
#include <vcclr.h>

#define ARRAY_SIZE(arr) (sizeof(arr) / sizeof(arr[0]))
#define Debug printf

public ref struct ParserOptions
{
    // C/C++ header file name.
    System::String^ FileName;

    Cxxi::Library^ Library;

    bool Verbose;
};

struct Parser
{
    Parser(ParserOptions^ Opts);

    void Setup(ParserOptions^ Opts);
    bool Parse(const std::string& File);

protected:

    // AST traversers
    void WalkAST();
    void WalkDeclaration(clang::Decl* D);
    void WalkMacros(clang::PreprocessingRecord* PR);
    Cxxi::Enumeration^ WalkEnum(clang::EnumDecl*);
    Cxxi::Function^ WalkFunction(clang::FunctionDecl*);
    Cxxi::Class^ WalkRecordCXX(clang::CXXRecordDecl*);
    Cxxi::Method^ WalkMethodCXX(clang::CXXMethodDecl*);
    Cxxi::Field^ WalkFieldCXX(clang::FieldDecl*);

    // Clang helpers
    bool IsValidDeclaration(const clang::SourceLocation& Loc);
    std::string GetDeclMangledName(clang::Decl*, clang::TargetCXXABI);
    std::string GetTypeBindName(const clang::Type*);
    void HandleComments(clang::Decl* D, Cxxi::Declaration^);
    
    Cxxi::Module^ GetModule(clang::SourceLocation Loc);
    Cxxi::Namespace^ GetNamespace(const clang::NamedDecl*);
    Cxxi::Type^ ConvertTypeToCLR(clang::QualType QualType);

    gcroot<Cxxi::Library^> Lib;
    llvm::OwningPtr<clang::CompilerInstance> C;
    clang::ASTContext* AST;
};

//-----------------------------------//

typedef std::string String;

String StringFormatArgs(const char* str, va_list args);
String StringFormat(const char* str, ...);
