#pragma once

#include "CppSharp.h"
#include <CppParser.h>

namespace CppSharp
{
    namespace Parser
    {
        enum struct ParserDiagnosticLevel;
        enum struct ParserResultKind;
        enum struct SourceLocationKind;
        ref class ClangParser;
        ref class ParserDiagnostic;
        ref class ParserOptions;
        ref class ParserResult;
        ref class ParserTargetInfo;
        namespace AST
        {
            enum struct CppAbi;
            ref class ASTContext;
            ref class NativeLibrary;
        }
    }
}

namespace CppSharp
{
    namespace Parser
    {
        public enum struct ParserDiagnosticLevel
        {
            Ignored = 0,
            Note = 1,
            Warning = 2,
            Error = 3,
            Fatal = 4
        };

        public enum struct ParserResultKind
        {
            Success = 0,
            Error = 1,
            FileNotFound = 2
        };

        public enum struct SourceLocationKind
        {
            Invalid = 0,
            Builtin = 1,
            CommandLine = 2,
            System = 3,
            User = 4
        };

        public ref class ParserOptions : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::ParserOptions* NativePtr;
            property System::IntPtr __Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            ParserOptions(::CppSharp::CppParser::ParserOptions* native);
            ParserOptions(System::IntPtr native);
            ParserOptions();

            ParserOptions(CppSharp::Parser::ParserOptions^ _0);

            property unsigned int ArgumentsCount
            {
                unsigned int get();
            }

            property System::String^ FileName
            {
                System::String^ get();
                void set(System::String^);
            }

            property unsigned int IncludeDirsCount
            {
                unsigned int get();
            }

            property unsigned int SystemIncludeDirsCount
            {
                unsigned int get();
            }

            property unsigned int DefinesCount
            {
                unsigned int get();
            }

            property unsigned int LibraryDirsCount
            {
                unsigned int get();
            }

            property System::String^ TargetTriple
            {
                System::String^ get();
                void set(System::String^);
            }

            property CppSharp::Parser::AST::ASTContext^ ASTContext
            {
                CppSharp::Parser::AST::ASTContext^ get();
                void set(CppSharp::Parser::AST::ASTContext^);
            }

            property int ToolSetToUse
            {
                int get();
                void set(int);
            }

            property CppSharp::Parser::AST::CppAbi Abi
            {
                CppSharp::Parser::AST::CppAbi get();
                void set(CppSharp::Parser::AST::CppAbi);
            }

            property bool NoStandardIncludes
            {
                bool get();
                void set(bool);
            }

            property bool NoBuiltinIncludes
            {
                bool get();
                void set(bool);
            }

            property bool MicrosoftMode
            {
                bool get();
                void set(bool);
            }

            property bool Verbose
            {
                bool get();
                void set(bool);
            }

            System::String^ getArguments(unsigned int i);

            void addArguments(System::String^ s);

            System::String^ getIncludeDirs(unsigned int i);

            void addIncludeDirs(System::String^ s);

            System::String^ getSystemIncludeDirs(unsigned int i);

            void addSystemIncludeDirs(System::String^ s);

            System::String^ getDefines(unsigned int i);

            void addDefines(System::String^ s);

            System::String^ getLibraryDirs(unsigned int i);

            void addLibraryDirs(System::String^ s);
        };

        public ref class ParserDiagnostic : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::ParserDiagnostic* NativePtr;
            property System::IntPtr __Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            ParserDiagnostic(::CppSharp::CppParser::ParserDiagnostic* native);
            ParserDiagnostic(System::IntPtr native);
            ParserDiagnostic();

            ParserDiagnostic(CppSharp::Parser::ParserDiagnostic^ _0);

            property System::String^ FileName
            {
                System::String^ get();
                void set(System::String^);
            }

            property System::String^ Message
            {
                System::String^ get();
                void set(System::String^);
            }

            property CppSharp::Parser::ParserDiagnosticLevel Level
            {
                CppSharp::Parser::ParserDiagnosticLevel get();
                void set(CppSharp::Parser::ParserDiagnosticLevel);
            }

            property int LineNumber
            {
                int get();
                void set(int);
            }

            property int ColumnNumber
            {
                int get();
                void set(int);
            }
        };

        public ref class ParserResult : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::ParserResult* NativePtr;
            property System::IntPtr __Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            ParserResult(::CppSharp::CppParser::ParserResult* native);
            ParserResult(System::IntPtr native);
            ParserResult();

            ParserResult(CppSharp::Parser::ParserResult^ _0);

            property unsigned int DiagnosticsCount
            {
                unsigned int get();
            }

            property CppSharp::Parser::ParserResultKind Kind
            {
                CppSharp::Parser::ParserResultKind get();
                void set(CppSharp::Parser::ParserResultKind);
            }

            property CppSharp::Parser::AST::ASTContext^ ASTContext
            {
                CppSharp::Parser::AST::ASTContext^ get();
                void set(CppSharp::Parser::AST::ASTContext^);
            }

            property CppSharp::Parser::AST::NativeLibrary^ Library
            {
                CppSharp::Parser::AST::NativeLibrary^ get();
                void set(CppSharp::Parser::AST::NativeLibrary^);
            }

            CppSharp::Parser::ParserDiagnostic^ getDiagnostics(unsigned int i);

            void addDiagnostics(CppSharp::Parser::ParserDiagnostic^ s);
        };

        public ref class ClangParser : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::ClangParser* NativePtr;
            property System::IntPtr __Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            ClangParser(::CppSharp::CppParser::ClangParser* native);
            ClangParser(System::IntPtr native);
            ClangParser();

            ClangParser(CppSharp::Parser::ClangParser^ _0);

            static CppSharp::Parser::ParserResult^ ParseHeader(CppSharp::Parser::ParserOptions^ Opts);

            static CppSharp::Parser::ParserResult^ ParseLibrary(CppSharp::Parser::ParserOptions^ Opts);

            static CppSharp::Parser::ParserTargetInfo^ GetTargetInfo(CppSharp::Parser::ParserOptions^ Opts);
        };
    }
}
