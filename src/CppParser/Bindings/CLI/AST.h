#pragma once

#include "CppSharp.h"
#include <AST.h>

namespace CppSharp
{
    namespace Parser
    {
        namespace AST
        {
            ref class Type;
            ref class TypeQualifiers;
            ref class QualifiedType;
            ref class TagType;
            ref class Declaration;
            enum struct AccessSpecifier;
            ref class DeclarationContext;
            ref class Namespace;
            ref class Enumeration;
            ref class BuiltinType;
            enum struct PrimitiveType;
            ref class Function;
            enum struct CXXOperatorKind;
            enum struct CallingConvention;
            ref class Parameter;
            ref class Class;
            ref class BaseClassSpecifier;
            ref class Field;
            ref class Method;
            ref class AccessSpecifierDecl;
            enum struct CXXMethodKind;
            ref class ClassLayout;
            enum struct CppAbi;
            ref class VFTableInfo;
            ref class VTableLayout;
            ref class VTableComponent;
            enum struct VTableComponentKind;
            ref class Template;
            ref class TemplateParameter;
            ref class TypedefDecl;
            ref class Variable;
            ref class RawComment;
            enum struct RawCommentKind;
            ref class PreprocessedEntity;
            enum struct MacroLocation;
            ref class ArrayType;
            ref class FunctionType;
            ref class PointerType;
            ref class MemberPointerType;
            ref class TypedefType;
            ref class AttributedType;
            ref class DecayedType;
            ref class TemplateArgument;
            ref class TemplateSpecializationType;
            ref class TemplateParameterType;
            ref class TemplateParameterSubstitutionType;
            ref class InjectedClassNameType;
            ref class DependentNameType;
            ref class ClassTemplate;
            ref class ClassTemplateSpecialization;
            ref class ClassTemplatePartialSpecialization;
            ref class FunctionTemplate;
            ref class MacroDefinition;
            ref class MacroExpansion;
            ref class TranslationUnit;
            ref class NativeLibrary;
            ref class ASTContext;
        }
    }
}

namespace CppSharp
{
    namespace Parser
    {
        namespace AST
        {
            public enum struct AccessSpecifier
            {
                Private = 0,
                Protected = 1,
                Public = 2
            };

            public enum struct CXXMethodKind
            {
                Normal = 0,
                Constructor = 1,
                Destructor = 2,
                Conversion = 3,
                Operator = 4,
                UsingDirective = 5
            };

            public enum struct CXXOperatorKind
            {
                None = 0,
                New = 1,
                Delete = 2,
                Array_New = 3,
                Array_Delete = 4,
                Plus = 5,
                Minus = 6,
                Star = 7,
                Slash = 8,
                Percent = 9,
                Caret = 10,
                Amp = 11,
                Pipe = 12,
                Tilde = 13,
                Exclaim = 14,
                Equal = 15,
                Less = 16,
                Greater = 17,
                PlusEqual = 18,
                MinusEqual = 19,
                StarEqual = 20,
                SlashEqual = 21,
                PercentEqual = 22,
                CaretEqual = 23,
                AmpEqual = 24,
                PipeEqual = 25,
                LessLess = 26,
                GreaterGreater = 27,
                LessLessEqual = 28,
                GreaterGreaterEqual = 29,
                EqualEqual = 30,
                ExclaimEqual = 31,
                LessEqual = 32,
                GreaterEqual = 33,
                AmpAmp = 34,
                PipePipe = 35,
                PlusPlus = 36,
                MinusMinus = 37,
                Comma = 38,
                ArrowStar = 39,
                Arrow = 40,
                Call = 41,
                Subscript = 42,
                Conditional = 43
            };

            public enum struct CallingConvention
            {
                Default = 0,
                C = 1,
                StdCall = 2,
                ThisCall = 3,
                FastCall = 4,
                Unknown = 5
            };

            public enum struct CppAbi
            {
                Itanium = 0,
                Microsoft = 1,
                ARM = 2
            };

            public enum struct VTableComponentKind
            {
                VCallOffset = 0,
                VBaseOffset = 1,
                OffsetToTop = 2,
                RTTI = 3,
                FunctionPointer = 4,
                CompleteDtorPointer = 5,
                DeletingDtorPointer = 6,
                UnusedFunctionPointer = 7
            };

            public enum struct PrimitiveType
            {
                Null = 0,
                Void = 1,
                Bool = 2,
                WideChar = 3,
                Int8 = 4,
                Char = 4,
                UInt8 = 5,
                UChar = 5,
                Int16 = 6,
                UInt16 = 7,
                Int32 = 8,
                UInt32 = 9,
                Int64 = 10,
                UInt64 = 11,
                Float = 12,
                Double = 13,
                IntPtr = 14
            };

            public enum struct RawCommentKind
            {
                Invalid = 0,
                OrdinaryBCPL = 1,
                OrdinaryC = 2,
                BCPLSlash = 3,
                BCPLExcl = 4,
                JavaDoc = 5,
                Qt = 6,
                Merged = 7
            };

            public enum struct MacroLocation
            {
                Unknown = 0,
                ClassHead = 1,
                ClassBody = 2,
                FunctionHead = 3,
                FunctionParameters = 4,
                FunctionBody = 5
            };

            public ref class Type : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::Type* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                Type(::CppSharp::CppParser::AST::Type* native);
                Type(System::IntPtr native);
                Type();

                property bool IsDependent
                {
                    bool get();
                    void set(bool);
                }
            };

            public ref class TypeQualifiers : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::TypeQualifiers* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                TypeQualifiers(::CppSharp::CppParser::AST::TypeQualifiers* native);
                TypeQualifiers(System::IntPtr native);
                TypeQualifiers();

                property bool IsConst
                {
                    bool get();
                    void set(bool);
                }
                property bool IsVolatile
                {
                    bool get();
                    void set(bool);
                }
                property bool IsRestrict
                {
                    bool get();
                    void set(bool);
                }
            };

            public ref class QualifiedType : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::QualifiedType* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                QualifiedType(::CppSharp::CppParser::AST::QualifiedType* native);
                QualifiedType(System::IntPtr native);
                QualifiedType();

                property CppSharp::Parser::AST::Type^ Type
                {
                    CppSharp::Parser::AST::Type^ get();
                    void set(CppSharp::Parser::AST::Type^);
                }
                property CppSharp::Parser::AST::TypeQualifiers^ Qualifiers
                {
                    CppSharp::Parser::AST::TypeQualifiers^ get();
                    void set(CppSharp::Parser::AST::TypeQualifiers^);
                }
            };

            public ref class TagType : CppSharp::Parser::AST::Type
            {
            public:

                TagType(::CppSharp::CppParser::AST::TagType* native);
                TagType(System::IntPtr native);
                TagType();

                property CppSharp::Parser::AST::Declaration^ Declaration
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }
            };

            public ref class ArrayType : CppSharp::Parser::AST::Type
            {
            public:

                enum struct ArraySize
                {
                    Constant = 0,
                    Variable = 1,
                    Dependent = 2,
                    Incomplete = 3
                };

                ArrayType(::CppSharp::CppParser::AST::ArrayType* native);
                ArrayType(System::IntPtr native);
                ArrayType();

                property CppSharp::Parser::AST::QualifiedType^ QualifiedType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::ArrayType::ArraySize SizeType
                {
                    CppSharp::Parser::AST::ArrayType::ArraySize get();
                    void set(CppSharp::Parser::AST::ArrayType::ArraySize);
                }
                property int Size
                {
                    int get();
                    void set(int);
                }
            };

            public ref class FunctionType : CppSharp::Parser::AST::Type
            {
            public:

                FunctionType(::CppSharp::CppParser::AST::FunctionType* native);
                FunctionType(System::IntPtr native);
                FunctionType();

                property CppSharp::Parser::AST::QualifiedType^ ReturnType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::CallingConvention CallingConvention
                {
                    CppSharp::Parser::AST::CallingConvention get();
                    void set(CppSharp::Parser::AST::CallingConvention);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ Parameters
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^);
                }
                CppSharp::Parser::AST::Parameter^ getParameters(unsigned int i);

                unsigned int getParametersCount();

            };

            public ref class PointerType : CppSharp::Parser::AST::Type
            {
            public:

                enum struct TypeModifier
                {
                    Value = 0,
                    Pointer = 1,
                    LVReference = 2,
                    RVReference = 3
                };

                PointerType(::CppSharp::CppParser::AST::PointerType* native);
                PointerType(System::IntPtr native);
                PointerType();

                property CppSharp::Parser::AST::QualifiedType^ QualifiedPointee
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::PointerType::TypeModifier Modifier
                {
                    CppSharp::Parser::AST::PointerType::TypeModifier get();
                    void set(CppSharp::Parser::AST::PointerType::TypeModifier);
                }
            };

            public ref class MemberPointerType : CppSharp::Parser::AST::Type
            {
            public:

                MemberPointerType(::CppSharp::CppParser::AST::MemberPointerType* native);
                MemberPointerType(System::IntPtr native);
                MemberPointerType();

                property CppSharp::Parser::AST::QualifiedType^ Pointee
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class TypedefType : CppSharp::Parser::AST::Type
            {
            public:

                TypedefType(::CppSharp::CppParser::AST::TypedefType* native);
                TypedefType(System::IntPtr native);
                TypedefType();

                property CppSharp::Parser::AST::TypedefDecl^ Declaration
                {
                    CppSharp::Parser::AST::TypedefDecl^ get();
                    void set(CppSharp::Parser::AST::TypedefDecl^);
                }
            };

            public ref class AttributedType : CppSharp::Parser::AST::Type
            {
            public:

                AttributedType(::CppSharp::CppParser::AST::AttributedType* native);
                AttributedType(System::IntPtr native);
                AttributedType();

                property CppSharp::Parser::AST::QualifiedType^ Modified
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::QualifiedType^ Equivalent
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class DecayedType : CppSharp::Parser::AST::Type
            {
            public:

                DecayedType(::CppSharp::CppParser::AST::DecayedType* native);
                DecayedType(System::IntPtr native);
                DecayedType();

                property CppSharp::Parser::AST::QualifiedType^ Decayed
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::QualifiedType^ Original
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::QualifiedType^ Pointee
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class TemplateArgument : ICppInstance
            {
            public:

                enum struct ArgumentKind
                {
                    Type = 0,
                    Declaration = 1,
                    NullPtr = 2,
                    Integral = 3,
                    Template = 4,
                    TemplateExpansion = 5,
                    Expression = 6,
                    Pack = 7
                };

                property ::CppSharp::CppParser::AST::TemplateArgument* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                TemplateArgument(::CppSharp::CppParser::AST::TemplateArgument* native);
                TemplateArgument(System::IntPtr native);
                TemplateArgument();

                property CppSharp::Parser::AST::TemplateArgument::ArgumentKind Kind
                {
                    CppSharp::Parser::AST::TemplateArgument::ArgumentKind get();
                    void set(CppSharp::Parser::AST::TemplateArgument::ArgumentKind);
                }
                property CppSharp::Parser::AST::QualifiedType^ Type
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::Declaration^ Declaration
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }
                property int Integral
                {
                    int get();
                    void set(int);
                }
            };

            public ref class TemplateSpecializationType : CppSharp::Parser::AST::Type
            {
            public:

                TemplateSpecializationType(::CppSharp::CppParser::AST::TemplateSpecializationType* native);
                TemplateSpecializationType(System::IntPtr native);
                TemplateSpecializationType();

                property System::Collections::Generic::List<CppSharp::Parser::AST::TemplateArgument^>^ Arguments
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::TemplateArgument^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::TemplateArgument^>^);
                }
                property CppSharp::Parser::AST::Template^ Template
                {
                    CppSharp::Parser::AST::Template^ get();
                    void set(CppSharp::Parser::AST::Template^);
                }
                property CppSharp::Parser::AST::Type^ Desugared
                {
                    CppSharp::Parser::AST::Type^ get();
                    void set(CppSharp::Parser::AST::Type^);
                }
                CppSharp::Parser::AST::TemplateArgument^ getArguments(unsigned int i);

                unsigned int getArgumentsCount();

            };

            public ref class TemplateParameter : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::TemplateParameter* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                TemplateParameter(::CppSharp::CppParser::AST::TemplateParameter* native);
                TemplateParameter(System::IntPtr native);
                TemplateParameter();

                property System::String^ Name
                {
                    System::String^ get();
                    void set(System::String^);
                }
            };

            public ref class TemplateParameterType : CppSharp::Parser::AST::Type
            {
            public:

                TemplateParameterType(::CppSharp::CppParser::AST::TemplateParameterType* native);
                TemplateParameterType(System::IntPtr native);
                TemplateParameterType();

                property CppSharp::Parser::AST::TemplateParameter^ Parameter
                {
                    CppSharp::Parser::AST::TemplateParameter^ get();
                    void set(CppSharp::Parser::AST::TemplateParameter^);
                }
            };

            public ref class TemplateParameterSubstitutionType : CppSharp::Parser::AST::Type
            {
            public:

                TemplateParameterSubstitutionType(::CppSharp::CppParser::AST::TemplateParameterSubstitutionType* native);
                TemplateParameterSubstitutionType(System::IntPtr native);
                TemplateParameterSubstitutionType();

                property CppSharp::Parser::AST::QualifiedType^ Replacement
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class InjectedClassNameType : CppSharp::Parser::AST::Type
            {
            public:

                InjectedClassNameType(::CppSharp::CppParser::AST::InjectedClassNameType* native);
                InjectedClassNameType(System::IntPtr native);
                InjectedClassNameType();

                property CppSharp::Parser::AST::TemplateSpecializationType^ TemplateSpecialization
                {
                    CppSharp::Parser::AST::TemplateSpecializationType^ get();
                    void set(CppSharp::Parser::AST::TemplateSpecializationType^);
                }
                property CppSharp::Parser::AST::Class^ Class
                {
                    CppSharp::Parser::AST::Class^ get();
                    void set(CppSharp::Parser::AST::Class^);
                }
            };

            public ref class DependentNameType : CppSharp::Parser::AST::Type
            {
            public:

                DependentNameType(::CppSharp::CppParser::AST::DependentNameType* native);
                DependentNameType(System::IntPtr native);
                DependentNameType();

            };

            public ref class BuiltinType : CppSharp::Parser::AST::Type
            {
            public:

                BuiltinType(::CppSharp::CppParser::AST::BuiltinType* native);
                BuiltinType(System::IntPtr native);
                BuiltinType();

                property CppSharp::Parser::AST::PrimitiveType Type
                {
                    CppSharp::Parser::AST::PrimitiveType get();
                    void set(CppSharp::Parser::AST::PrimitiveType);
                }
            };

            public ref class RawComment : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::RawComment* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                RawComment(::CppSharp::CppParser::AST::RawComment* native);
                RawComment(System::IntPtr native);
                RawComment();

                property CppSharp::Parser::AST::RawCommentKind Kind
                {
                    CppSharp::Parser::AST::RawCommentKind get();
                    void set(CppSharp::Parser::AST::RawCommentKind);
                }
                property System::String^ Text
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property System::String^ BriefText
                {
                    System::String^ get();
                    void set(System::String^);
                }
            };

            public ref class VTableComponent : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::VTableComponent* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                VTableComponent(::CppSharp::CppParser::AST::VTableComponent* native);
                VTableComponent(System::IntPtr native);
                VTableComponent();

                property CppSharp::Parser::AST::VTableComponentKind Kind
                {
                    CppSharp::Parser::AST::VTableComponentKind get();
                    void set(CppSharp::Parser::AST::VTableComponentKind);
                }
                property unsigned int Offset
                {
                    unsigned int get();
                    void set(unsigned int);
                }
                property CppSharp::Parser::AST::Declaration^ Declaration
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }
            };

            public ref class VTableLayout : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::VTableLayout* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                VTableLayout(::CppSharp::CppParser::AST::VTableLayout* native);
                VTableLayout(System::IntPtr native);
                VTableLayout();

                property System::Collections::Generic::List<CppSharp::Parser::AST::VTableComponent^>^ Components
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::VTableComponent^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::VTableComponent^>^);
                }
                CppSharp::Parser::AST::VTableComponent^ getComponents(unsigned int i);

                unsigned int getComponentsCount();

            };

            public ref class VFTableInfo : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::VFTableInfo* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                VFTableInfo(::CppSharp::CppParser::AST::VFTableInfo* native);
                VFTableInfo(System::IntPtr native);
                VFTableInfo();

                property unsigned long long VBTableIndex
                {
                    unsigned long long get();
                    void set(unsigned long long);
                }
                property unsigned int VFPtrOffset
                {
                    unsigned int get();
                    void set(unsigned int);
                }
                property unsigned int VFPtrFullOffset
                {
                    unsigned int get();
                    void set(unsigned int);
                }
                property CppSharp::Parser::AST::VTableLayout^ Layout
                {
                    CppSharp::Parser::AST::VTableLayout^ get();
                    void set(CppSharp::Parser::AST::VTableLayout^);
                }
            };

            public ref class ClassLayout : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::ClassLayout* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                ClassLayout(::CppSharp::CppParser::AST::ClassLayout* native);
                ClassLayout(System::IntPtr native);
                ClassLayout();

                property CppSharp::Parser::AST::CppAbi ABI
                {
                    CppSharp::Parser::AST::CppAbi get();
                    void set(CppSharp::Parser::AST::CppAbi);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::VFTableInfo^>^ VFTables
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::VFTableInfo^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::VFTableInfo^>^);
                }
                property CppSharp::Parser::AST::VTableLayout^ Layout
                {
                    CppSharp::Parser::AST::VTableLayout^ get();
                    void set(CppSharp::Parser::AST::VTableLayout^);
                }
                property bool HasOwnVFPtr
                {
                    bool get();
                    void set(bool);
                }
                property int VBPtrOffset
                {
                    int get();
                    void set(int);
                }
                property int Alignment
                {
                    int get();
                    void set(int);
                }
                property int Size
                {
                    int get();
                    void set(int);
                }
                property int DataSize
                {
                    int get();
                    void set(int);
                }
                CppSharp::Parser::AST::VFTableInfo^ getVFTables(unsigned int i);

                unsigned int getVFTablesCount();

            };

            public ref class Declaration : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::Declaration* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                Declaration(::CppSharp::CppParser::AST::Declaration* native);
                Declaration(System::IntPtr native);
                Declaration();

                property CppSharp::Parser::AST::AccessSpecifier Access
                {
                    CppSharp::Parser::AST::AccessSpecifier get();
                    void set(CppSharp::Parser::AST::AccessSpecifier);
                }
                property CppSharp::Parser::AST::DeclarationContext^ _Namespace
                {
                    CppSharp::Parser::AST::DeclarationContext^ get();
                    void set(CppSharp::Parser::AST::DeclarationContext^);
                }
                property System::String^ Name
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property CppSharp::Parser::AST::RawComment^ Comment
                {
                    CppSharp::Parser::AST::RawComment^ get();
                    void set(CppSharp::Parser::AST::RawComment^);
                }
                property System::String^ DebugText
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property bool IsIncomplete
                {
                    bool get();
                    void set(bool);
                }
                property bool IsDependent
                {
                    bool get();
                    void set(bool);
                }
                property CppSharp::Parser::AST::Declaration^ CompleteDeclaration
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }
                property unsigned int DefinitionOrder
                {
                    unsigned int get();
                    void set(unsigned int);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::PreprocessedEntity^>^ PreprocessedEntities
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::PreprocessedEntity^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::PreprocessedEntity^>^);
                }
                property System::IntPtr OriginalPtr
                {
                    System::IntPtr get();
                    void set(System::IntPtr);
                }
                CppSharp::Parser::AST::PreprocessedEntity^ getPreprocessedEntities(unsigned int i);

                unsigned int getPreprocessedEntitiesCount();

            };

            public ref class DeclarationContext : CppSharp::Parser::AST::Declaration
            {
            public:

                DeclarationContext(::CppSharp::CppParser::AST::DeclarationContext* native);
                DeclarationContext(System::IntPtr native);
                DeclarationContext();

                property System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ Namespaces
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration^>^ Enums
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Function^>^ Functions
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Function^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Function^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Class^>^ Classes
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Class^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Class^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Template^>^ Templates
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Template^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Template^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::TypedefDecl^>^ Typedefs
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::TypedefDecl^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::TypedefDecl^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Variable^>^ Variables
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Variable^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Variable^>^);
                }
                CppSharp::Parser::AST::Declaration^ FindAnonymous(unsigned long long key);

                CppSharp::Parser::AST::Namespace^ FindNamespace(System::String^ Name);

                CppSharp::Parser::AST::Namespace^ FindNamespace(System::Collections::Generic::List<System::String^>^ _0);

                CppSharp::Parser::AST::Namespace^ FindCreateNamespace(System::String^ Name);

                CppSharp::Parser::AST::Class^ CreateClass(System::String^ Name, bool IsComplete);

                CppSharp::Parser::AST::Class^ FindClass(System::String^ Name);

                CppSharp::Parser::AST::Class^ FindClass(System::String^ Name, bool IsComplete, bool Create);

                CppSharp::Parser::AST::Enumeration^ FindEnum(System::String^ Name, bool Create);

                CppSharp::Parser::AST::Function^ FindFunction(System::String^ Name, bool Create);

                CppSharp::Parser::AST::TypedefDecl^ FindTypedef(System::String^ Name, bool Create);

                CppSharp::Parser::AST::Namespace^ getNamespaces(unsigned int i);

                unsigned int getNamespacesCount();

                CppSharp::Parser::AST::Enumeration^ getEnums(unsigned int i);

                unsigned int getEnumsCount();

                CppSharp::Parser::AST::Function^ getFunctions(unsigned int i);

                unsigned int getFunctionsCount();

                CppSharp::Parser::AST::Class^ getClasses(unsigned int i);

                unsigned int getClassesCount();

                CppSharp::Parser::AST::Template^ getTemplates(unsigned int i);

                unsigned int getTemplatesCount();

                CppSharp::Parser::AST::TypedefDecl^ getTypedefs(unsigned int i);

                unsigned int getTypedefsCount();

                CppSharp::Parser::AST::Variable^ getVariables(unsigned int i);

                unsigned int getVariablesCount();

            };

            public ref class TypedefDecl : CppSharp::Parser::AST::Declaration
            {
            public:

                TypedefDecl(::CppSharp::CppParser::AST::TypedefDecl* native);
                TypedefDecl(System::IntPtr native);
                TypedefDecl();

                property CppSharp::Parser::AST::QualifiedType^ QualifiedType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class Parameter : CppSharp::Parser::AST::Declaration
            {
            public:

                Parameter(::CppSharp::CppParser::AST::Parameter* native);
                Parameter(System::IntPtr native);
                Parameter();

                property CppSharp::Parser::AST::QualifiedType^ QualifiedType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property bool IsIndirect
                {
                    bool get();
                    void set(bool);
                }
                property bool HasDefaultValue
                {
                    bool get();
                    void set(bool);
                }
            };

            public ref class Function : CppSharp::Parser::AST::Declaration
            {
            public:

                Function(::CppSharp::CppParser::AST::Function* native);
                Function(System::IntPtr native);
                Function();

                property CppSharp::Parser::AST::QualifiedType^ ReturnType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property bool IsReturnIndirect
                {
                    bool get();
                    void set(bool);
                }
                property bool IsVariadic
                {
                    bool get();
                    void set(bool);
                }
                property bool IsInline
                {
                    bool get();
                    void set(bool);
                }
                property bool IsPure
                {
                    bool get();
                    void set(bool);
                }
                property bool IsDeleted
                {
                    bool get();
                    void set(bool);
                }
                property CppSharp::Parser::AST::CXXOperatorKind OperatorKind
                {
                    CppSharp::Parser::AST::CXXOperatorKind get();
                    void set(CppSharp::Parser::AST::CXXOperatorKind);
                }
                property System::String^ Mangled
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property System::String^ Signature
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property CppSharp::Parser::AST::CallingConvention CallingConvention
                {
                    CppSharp::Parser::AST::CallingConvention get();
                    void set(CppSharp::Parser::AST::CallingConvention);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ Parameters
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^);
                }
                CppSharp::Parser::AST::Parameter^ getParameters(unsigned int i);

                unsigned int getParametersCount();

            };

            public ref class Method : CppSharp::Parser::AST::Function
            {
            public:

                Method(::CppSharp::CppParser::AST::Method* native);
                Method(System::IntPtr native);
                Method();

                property CppSharp::Parser::AST::AccessSpecifierDecl^ AccessDecl
                {
                    CppSharp::Parser::AST::AccessSpecifierDecl^ get();
                    void set(CppSharp::Parser::AST::AccessSpecifierDecl^);
                }
                property bool IsVirtual
                {
                    bool get();
                    void set(bool);
                }
                property bool IsStatic
                {
                    bool get();
                    void set(bool);
                }
                property bool IsConst
                {
                    bool get();
                    void set(bool);
                }
                property bool IsImplicit
                {
                    bool get();
                    void set(bool);
                }
                property bool IsOverride
                {
                    bool get();
                    void set(bool);
                }
                property CppSharp::Parser::AST::CXXMethodKind Kind
                {
                    CppSharp::Parser::AST::CXXMethodKind get();
                    void set(CppSharp::Parser::AST::CXXMethodKind);
                }
                property bool IsDefaultConstructor
                {
                    bool get();
                    void set(bool);
                }
                property bool IsCopyConstructor
                {
                    bool get();
                    void set(bool);
                }
                property bool IsMoveConstructor
                {
                    bool get();
                    void set(bool);
                }
                property CppSharp::Parser::AST::QualifiedType^ ConversionType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class Enumeration : CppSharp::Parser::AST::Declaration
            {
            public:

                [System::Flags]
                enum struct EnumModifiers
                {
                    Anonymous = 1,
                    Scoped = 2,
                    Flags = 4
                };

                ref class Item : CppSharp::Parser::AST::Declaration
                {
                public:

                    Item(::CppSharp::CppParser::AST::Enumeration::Item* native);
                    Item(System::IntPtr native);
                    Item();

                    property System::String^ Expression
                    {
                        System::String^ get();
                        void set(System::String^);
                    }
                    property unsigned long long Value
                    {
                        unsigned long long get();
                        void set(unsigned long long);
                    }
                };

                Enumeration(::CppSharp::CppParser::AST::Enumeration* native);
                Enumeration(System::IntPtr native);
                Enumeration();

                property CppSharp::Parser::AST::Enumeration::EnumModifiers Modifiers
                {
                    CppSharp::Parser::AST::Enumeration::EnumModifiers get();
                    void set(CppSharp::Parser::AST::Enumeration::EnumModifiers);
                }
                property CppSharp::Parser::AST::Type^ Type
                {
                    CppSharp::Parser::AST::Type^ get();
                    void set(CppSharp::Parser::AST::Type^);
                }
                property CppSharp::Parser::AST::BuiltinType^ BuiltinType
                {
                    CppSharp::Parser::AST::BuiltinType^ get();
                    void set(CppSharp::Parser::AST::BuiltinType^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration::Item^>^ Items
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration::Item^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration::Item^>^);
                }
                CppSharp::Parser::AST::Enumeration::Item^ getItems(unsigned int i);

                unsigned int getItemsCount();

            };

            public ref class Variable : CppSharp::Parser::AST::Declaration
            {
            public:

                Variable(::CppSharp::CppParser::AST::Variable* native);
                Variable(System::IntPtr native);
                Variable();

                property System::String^ Mangled
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property CppSharp::Parser::AST::QualifiedType^ QualifiedType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class BaseClassSpecifier : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::BaseClassSpecifier* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                BaseClassSpecifier(::CppSharp::CppParser::AST::BaseClassSpecifier* native);
                BaseClassSpecifier(System::IntPtr native);
                BaseClassSpecifier();

                property CppSharp::Parser::AST::AccessSpecifier Access
                {
                    CppSharp::Parser::AST::AccessSpecifier get();
                    void set(CppSharp::Parser::AST::AccessSpecifier);
                }
                property bool IsVirtual
                {
                    bool get();
                    void set(bool);
                }
                property CppSharp::Parser::AST::Type^ Type
                {
                    CppSharp::Parser::AST::Type^ get();
                    void set(CppSharp::Parser::AST::Type^);
                }
            };

            public ref class Field : CppSharp::Parser::AST::Declaration
            {
            public:

                Field(::CppSharp::CppParser::AST::Field* native);
                Field(System::IntPtr native);
                Field();

                property CppSharp::Parser::AST::QualifiedType^ QualifiedType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
                property CppSharp::Parser::AST::AccessSpecifier Access
                {
                    CppSharp::Parser::AST::AccessSpecifier get();
                    void set(CppSharp::Parser::AST::AccessSpecifier);
                }
                property unsigned int Offset
                {
                    unsigned int get();
                    void set(unsigned int);
                }
                property CppSharp::Parser::AST::Class^ Class
                {
                    CppSharp::Parser::AST::Class^ get();
                    void set(CppSharp::Parser::AST::Class^);
                }
            };

            public ref class AccessSpecifierDecl : CppSharp::Parser::AST::Declaration
            {
            public:

                AccessSpecifierDecl(::CppSharp::CppParser::AST::AccessSpecifierDecl* native);
                AccessSpecifierDecl(System::IntPtr native);
                AccessSpecifierDecl();

            };

            public ref class Class : CppSharp::Parser::AST::DeclarationContext
            {
            public:

                Class(::CppSharp::CppParser::AST::Class* native);
                Class(System::IntPtr native);
                Class();

                property System::Collections::Generic::List<CppSharp::Parser::AST::BaseClassSpecifier^>^ Bases
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::BaseClassSpecifier^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::BaseClassSpecifier^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Field^>^ Fields
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Field^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Field^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Method^>^ Methods
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Method^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Method^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::AccessSpecifierDecl^>^ Specifiers
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::AccessSpecifierDecl^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::AccessSpecifierDecl^>^);
                }
                property bool IsPOD
                {
                    bool get();
                    void set(bool);
                }
                property bool IsAbstract
                {
                    bool get();
                    void set(bool);
                }
                property bool IsUnion
                {
                    bool get();
                    void set(bool);
                }
                property bool IsDynamic
                {
                    bool get();
                    void set(bool);
                }
                property bool IsPolymorphic
                {
                    bool get();
                    void set(bool);
                }
                property bool HasNonTrivialDefaultConstructor
                {
                    bool get();
                    void set(bool);
                }
                property bool HasNonTrivialCopyConstructor
                {
                    bool get();
                    void set(bool);
                }
                property CppSharp::Parser::AST::ClassLayout^ Layout
                {
                    CppSharp::Parser::AST::ClassLayout^ get();
                    void set(CppSharp::Parser::AST::ClassLayout^);
                }
                CppSharp::Parser::AST::BaseClassSpecifier^ getBases(unsigned int i);

                unsigned int getBasesCount();

                CppSharp::Parser::AST::Field^ getFields(unsigned int i);

                unsigned int getFieldsCount();

                CppSharp::Parser::AST::Method^ getMethods(unsigned int i);

                unsigned int getMethodsCount();

                CppSharp::Parser::AST::AccessSpecifierDecl^ getSpecifiers(unsigned int i);

                unsigned int getSpecifiersCount();

            };

            public ref class Template : CppSharp::Parser::AST::Declaration
            {
            public:

                Template(::CppSharp::CppParser::AST::Template* native);
                Template(System::IntPtr native);
                Template();

                property CppSharp::Parser::AST::Declaration^ TemplatedDecl
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::TemplateParameter^>^ Parameters
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::TemplateParameter^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::TemplateParameter^>^);
                }
                CppSharp::Parser::AST::TemplateParameter^ getParameters(unsigned int i);

                unsigned int getParametersCount();

            };

            public ref class ClassTemplate : CppSharp::Parser::AST::Template
            {
            public:

                ClassTemplate(::CppSharp::CppParser::AST::ClassTemplate* native);
                ClassTemplate(System::IntPtr native);
                ClassTemplate();

            };

            public ref class ClassTemplateSpecialization : CppSharp::Parser::AST::Class
            {
            public:

                ClassTemplateSpecialization(::CppSharp::CppParser::AST::ClassTemplateSpecialization* native);
                ClassTemplateSpecialization(System::IntPtr native);
                ClassTemplateSpecialization();

            };

            public ref class ClassTemplatePartialSpecialization : CppSharp::Parser::AST::ClassTemplateSpecialization
            {
            public:

                ClassTemplatePartialSpecialization(::CppSharp::CppParser::AST::ClassTemplatePartialSpecialization* native);
                ClassTemplatePartialSpecialization(System::IntPtr native);
                ClassTemplatePartialSpecialization();

            };

            public ref class FunctionTemplate : CppSharp::Parser::AST::Template
            {
            public:

                FunctionTemplate(::CppSharp::CppParser::AST::FunctionTemplate* native);
                FunctionTemplate(System::IntPtr native);
                FunctionTemplate();

            };

            public ref class Namespace : CppSharp::Parser::AST::DeclarationContext
            {
            public:

                Namespace(::CppSharp::CppParser::AST::Namespace* native);
                Namespace(System::IntPtr native);
                Namespace();

            };

            public ref class PreprocessedEntity : CppSharp::Parser::AST::Declaration
            {
            public:

                PreprocessedEntity(::CppSharp::CppParser::AST::PreprocessedEntity* native);
                PreprocessedEntity(System::IntPtr native);
                PreprocessedEntity();

                property CppSharp::Parser::AST::MacroLocation Location
                {
                    CppSharp::Parser::AST::MacroLocation get();
                    void set(CppSharp::Parser::AST::MacroLocation);
                }
            };

            public ref class MacroDefinition : CppSharp::Parser::AST::PreprocessedEntity
            {
            public:

                MacroDefinition(::CppSharp::CppParser::AST::MacroDefinition* native);
                MacroDefinition(System::IntPtr native);
                MacroDefinition();

                property System::String^ Expression
                {
                    System::String^ get();
                    void set(System::String^);
                }
            };

            public ref class MacroExpansion : CppSharp::Parser::AST::PreprocessedEntity
            {
            public:

                MacroExpansion(::CppSharp::CppParser::AST::MacroExpansion* native);
                MacroExpansion(System::IntPtr native);
                MacroExpansion();

                property System::String^ Text
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property CppSharp::Parser::AST::MacroDefinition^ Definition
                {
                    CppSharp::Parser::AST::MacroDefinition^ get();
                    void set(CppSharp::Parser::AST::MacroDefinition^);
                }
            };

            public ref class TranslationUnit : CppSharp::Parser::AST::Namespace
            {
            public:

                TranslationUnit(::CppSharp::CppParser::AST::TranslationUnit* native);
                TranslationUnit(System::IntPtr native);
                TranslationUnit();

                property System::String^ FileName
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property bool IsSystemHeader
                {
                    bool get();
                    void set(bool);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ Namespaces
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^);
                }
                property System::Collections::Generic::List<CppSharp::Parser::AST::MacroDefinition^>^ Macros
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::MacroDefinition^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::MacroDefinition^>^);
                }
                CppSharp::Parser::AST::Namespace^ getNamespaces(unsigned int i);

                unsigned int getNamespacesCount();

                CppSharp::Parser::AST::MacroDefinition^ getMacros(unsigned int i);

                unsigned int getMacrosCount();

            };

            public ref class NativeLibrary : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::NativeLibrary* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                NativeLibrary(::CppSharp::CppParser::AST::NativeLibrary* native);
                NativeLibrary(System::IntPtr native);
                NativeLibrary();

                property System::String^ FileName
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property System::Collections::Generic::List<System::String^>^ Symbols
                {
                    System::Collections::Generic::List<System::String^>^ get();
                    void set(System::Collections::Generic::List<System::String^>^);
                }
                System::String^ getSymbols(unsigned int i);

                unsigned int getSymbolsCount();

            };

            public ref class ASTContext : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::ASTContext* NativePtr;
                property System::IntPtr Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                ASTContext(::CppSharp::CppParser::AST::ASTContext* native);
                ASTContext(System::IntPtr native);
                ASTContext();

                property System::Collections::Generic::List<CppSharp::Parser::AST::TranslationUnit^>^ TranslationUnits
                {
                    System::Collections::Generic::List<CppSharp::Parser::AST::TranslationUnit^>^ get();
                    void set(System::Collections::Generic::List<CppSharp::Parser::AST::TranslationUnit^>^);
                }
                CppSharp::Parser::AST::TranslationUnit^ FindOrCreateModule(System::String^ File);

                CppSharp::Parser::AST::TranslationUnit^ getTranslationUnits(unsigned int i);

                unsigned int getTranslationUnitsCount();

            };
        }
    }
}
