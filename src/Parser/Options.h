/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#using <CppSharp.AST.dll>
using namespace System::Collections::Generic;

public enum struct CppAbi
{
    Itanium,
    Microsoft,
    ARM
};

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

    CppSharp::AST::ASTContext^ ASTContext;

    int ToolSetToUse;
    System::String^ TargetTriple;

    bool NoStandardIncludes;
    bool NoBuiltinIncludes;
    bool MicrosoftMode;
    CppAbi Abi;

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
    List<ParserDiagnostic>^ Diagnostics;

    CppSharp::AST::ASTContext^ ASTContext;
    CppSharp::AST::NativeLibrary^ Library;
};

enum class SourceLocationKind
{
    Invalid,
    Builtin,
    CommandLine,
    System,
    User
};