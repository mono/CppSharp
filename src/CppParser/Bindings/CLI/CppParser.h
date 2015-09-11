#pragma once

#include "CppSharp.h"
#include <CppParser.h>

namespace CppSharp
{
    namespace Parser
    {
        enum struct LanguageVersion;
        enum struct ParserDiagnosticLevel;
        enum struct ParserResultKind;
        enum struct SourceLocationKind;
        ref class ClangParser;
        ref class Parser;
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
        public enum struct LanguageVersion
        {
            /// <summary> The C programming language. </summary>
            C = 0,
            /// <summary> The C++ programming language year 1998; supports deprecated constructs. </summary>
            CPlusPlus98 = 1,
            /// <summary> The C++ programming language year 2011. </summary>
            CPlusPlus11 = 2
        };

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
            static ParserOptions^ __CreateInstance(::System::IntPtr native);
            static ParserOptions^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
            ParserOptions();

            ParserOptions(CppSharp::Parser::ParserOptions^ _0);

            ~ParserOptions();

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

            property unsigned int UndefinesCount
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

            property CppSharp::Parser::LanguageVersion LanguageVersion
            {
                CppSharp::Parser::LanguageVersion get();
                void set(CppSharp::Parser::LanguageVersion);
            }

            property CppSharp::Parser::ParserTargetInfo^ TargetInfo
            {
                CppSharp::Parser::ParserTargetInfo^ get();
                void set(CppSharp::Parser::ParserTargetInfo^);
            }

            System::String^ getArguments(unsigned int i);

            void addArguments(System::String^ s);

            void clearArguments();

            System::String^ getIncludeDirs(unsigned int i);

            void addIncludeDirs(System::String^ s);

            void clearIncludeDirs();

            System::String^ getSystemIncludeDirs(unsigned int i);

            void addSystemIncludeDirs(System::String^ s);

            void clearSystemIncludeDirs();

            System::String^ getDefines(unsigned int i);

            void addDefines(System::String^ s);

            void clearDefines();

            System::String^ getUndefines(unsigned int i);

            void addUndefines(System::String^ s);

            void clearUndefines();

            System::String^ getLibraryDirs(unsigned int i);

            void addLibraryDirs(System::String^ s);

            void clearLibraryDirs();

            protected:
            bool __ownsNativeInstance;
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
            static ParserDiagnostic^ __CreateInstance(::System::IntPtr native);
            static ParserDiagnostic^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
            ParserDiagnostic();

            ParserDiagnostic(CppSharp::Parser::ParserDiagnostic^ _0);

            ~ParserDiagnostic();

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

            protected:
            bool __ownsNativeInstance;
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
            static ParserResult^ __CreateInstance(::System::IntPtr native);
            static ParserResult^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
            ParserResult();

            ParserResult(CppSharp::Parser::ParserResult^ _0);

            ~ParserResult();

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

            void clearDiagnostics();

            protected:
            bool __ownsNativeInstance;
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
            static ClangParser^ __CreateInstance(::System::IntPtr native);
            static ClangParser^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
            ClangParser();

            ClangParser(CppSharp::Parser::ClangParser^ _0);

            ~ClangParser();

            static CppSharp::Parser::ParserResult^ ParseHeader(CppSharp::Parser::ParserOptions^ Opts);

            static CppSharp::Parser::ParserResult^ ParseLibrary(CppSharp::Parser::ParserOptions^ Opts);

            static CppSharp::Parser::ParserTargetInfo^ GetTargetInfo(CppSharp::Parser::ParserOptions^ Opts);

            protected:
            bool __ownsNativeInstance;
        };
    }
}
