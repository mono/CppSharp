#pragma once

#include "CppSharp.h"
#include <AST.h>

namespace CppSharp
{
    namespace Parser
    {
        namespace AST
        {
            enum struct AccessSpecifier;
            enum struct CXXMethodKind;
            enum struct CXXOperatorKind;
            enum struct CallingConvention;
            enum struct CommentKind;
            enum struct CppAbi;
            enum struct DeclarationKind;
            enum struct MacroLocation;
            enum struct PrimitiveType;
            enum struct RawCommentKind;
            enum struct TemplateSpecializationKind;
            enum struct TypeKind;
            enum struct VTableComponentKind;
            ref class ASTContext;
            ref class AccessSpecifierDecl;
            ref class ArrayType;
            ref class AttributedType;
            ref class BaseClassSpecifier;
            ref class BuiltinType;
            ref class Class;
            ref class ClassLayout;
            ref class ClassTemplate;
            ref class ClassTemplatePartialSpecialization;
            ref class ClassTemplateSpecialization;
            ref class Comment;
            ref class DecayedType;
            ref class Declaration;
            ref class DeclarationContext;
            ref class DependentNameType;
            ref class Enumeration;
            ref class Field;
            ref class FullComment;
            ref class Function;
            ref class FunctionTemplate;
            ref class FunctionTemplateSpecialization;
            ref class FunctionType;
            ref class InjectedClassNameType;
            ref class MacroDefinition;
            ref class MacroExpansion;
            ref class MemberPointerType;
            ref class Method;
            ref class Namespace;
            ref class NativeLibrary;
            ref class PackExpansionType;
            ref class Parameter;
            ref class PointerType;
            ref class PreprocessedEntity;
            ref class QualifiedType;
            ref class RawComment;
            ref class TagType;
            ref class Template;
            ref class TemplateArgument;
            ref class TemplateParameter;
            ref class TemplateParameterSubstitutionType;
            ref class TemplateParameterType;
            ref class TemplateSpecializationType;
            ref class TranslationUnit;
            ref class Type;
            ref class TypeQualifiers;
            ref class TypedefDecl;
            ref class TypedefType;
            ref class VFTableInfo;
            ref class VTableComponent;
            ref class VTableLayout;
            ref class Variable;
        }
    }
}

namespace CppSharp
{
    namespace Parser
    {
        namespace AST
        {
            public enum struct TypeKind
            {
                Tag = 0,
                Array = 1,
                Function = 2,
                Pointer = 3,
                MemberPointer = 4,
                Typedef = 5,
                Attributed = 6,
                Decayed = 7,
                TemplateSpecialization = 8,
                TemplateParameter = 9,
                TemplateParameterSubstitution = 10,
                InjectedClassName = 11,
                DependentName = 12,
                PackExpansion = 13,
                Builtin = 14
            };

            public enum struct DeclarationKind
            {
                DeclarationContext = 0,
                Typedef = 1,
                Parameter = 2,
                Function = 3,
                Method = 4,
                Enumeration = 5,
                EnumerationItem = 6,
                Variable = 7,
                Field = 8,
                AccessSpecifier = 9,
                Class = 10,
                Template = 11,
                ClassTemplate = 12,
                ClassTemplateSpecialization = 13,
                ClassTemplatePartialSpecialization = 14,
                FunctionTemplate = 15,
                Namespace = 16,
                PreprocessedEntity = 17,
                MacroDefinition = 18,
                MacroExpansion = 19,
                TranslationUnit = 20
            };

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

            public enum struct TemplateSpecializationKind
            {
                Undeclared = 0,
                ImplicitInstantiation = 1,
                ExplicitSpecialization = 2,
                ExplicitInstantiationDeclaration = 3,
                ExplicitInstantiationDefinition = 4
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
                Char = 4,
                UChar = 5,
                Short = 6,
                UShort = 7,
                Int32 = 8,
                UInt32 = 9,
                Long = 10,
                ULong = 11,
                Int64 = 12,
                UInt64 = 13,
                Float = 14,
                Double = 15,
                IntPtr = 16
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

            public enum struct CommentKind
            {
                FullComment = 0
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
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                Type(::CppSharp::CppParser::AST::Type* native);
                Type(System::IntPtr native);
                Type(CppSharp::Parser::AST::TypeKind kind);

                property CppSharp::Parser::AST::TypeKind Kind
                {
                    CppSharp::Parser::AST::TypeKind get();
                    void set(CppSharp::Parser::AST::TypeKind);
                }

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
                property System::IntPtr __Instance
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
                property System::IntPtr __Instance
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

                property unsigned int ParametersCount
                {
                    unsigned int get();
                }

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

                CppSharp::Parser::AST::Parameter^ getParameters(unsigned int i);

                void addParameters(CppSharp::Parser::AST::Parameter^ s);
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
                property System::IntPtr __Instance
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

                property unsigned int ArgumentsCount
                {
                    unsigned int get();
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

                void addArguments(CppSharp::Parser::AST::TemplateArgument^ s);
            };

            public ref class TemplateParameter : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::TemplateParameter* NativePtr;
                property System::IntPtr __Instance
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

                property bool IsTypeParameter
                {
                    bool get();
                    void set(bool);
                }

                static bool operator==(CppSharp::Parser::AST::TemplateParameter^ __op, CppSharp::Parser::AST::TemplateParameter^ param);
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

                property unsigned int Depth
                {
                    unsigned int get();
                    void set(unsigned int);
                }

                property unsigned int Index
                {
                    unsigned int get();
                    void set(unsigned int);
                }

                property bool IsParameterPack
                {
                    bool get();
                    void set(bool);
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

            public ref class PackExpansionType : CppSharp::Parser::AST::Type
            {
            public:

                PackExpansionType(::CppSharp::CppParser::AST::PackExpansionType* native);
                PackExpansionType(System::IntPtr native);
                PackExpansionType();
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

            public ref class VTableComponent : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::VTableComponent* NativePtr;
                property System::IntPtr __Instance
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
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                VTableLayout(::CppSharp::CppParser::AST::VTableLayout* native);
                VTableLayout(System::IntPtr native);
                VTableLayout();

                property unsigned int ComponentsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::VTableComponent^ getComponents(unsigned int i);

                void addComponents(CppSharp::Parser::AST::VTableComponent^ s);
            };

            public ref class VFTableInfo : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::VFTableInfo* NativePtr;
                property System::IntPtr __Instance
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
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                ClassLayout(::CppSharp::CppParser::AST::ClassLayout* native);
                ClassLayout(System::IntPtr native);
                ClassLayout();

                property unsigned int VFTablesCount
                {
                    unsigned int get();
                }

                property CppSharp::Parser::AST::CppAbi ABI
                {
                    CppSharp::Parser::AST::CppAbi get();
                    void set(CppSharp::Parser::AST::CppAbi);
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

                void addVFTables(CppSharp::Parser::AST::VFTableInfo^ s);
            };

            public ref class Declaration : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::Declaration* NativePtr;
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                Declaration(::CppSharp::CppParser::AST::Declaration* native);
                Declaration(System::IntPtr native);
                Declaration(CppSharp::Parser::AST::DeclarationKind kind);

                property System::String^ Name
                {
                    System::String^ get();
                    void set(System::String^);
                }

                property System::String^ DebugText
                {
                    System::String^ get();
                    void set(System::String^);
                }

                property unsigned int PreprocessedEntitiesCount
                {
                    unsigned int get();
                }

                property CppSharp::Parser::AST::DeclarationKind Kind
                {
                    CppSharp::Parser::AST::DeclarationKind get();
                    void set(CppSharp::Parser::AST::DeclarationKind);
                }

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

                property CppSharp::Parser::AST::RawComment^ Comment
                {
                    CppSharp::Parser::AST::RawComment^ get();
                    void set(CppSharp::Parser::AST::RawComment^);
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

                property void* OriginalPtr
                {
                    void* get();
                    void set(void*);
                }

                CppSharp::Parser::AST::PreprocessedEntity^ getPreprocessedEntities(unsigned int i);

                void addPreprocessedEntities(CppSharp::Parser::AST::PreprocessedEntity^ s);
            };

            public ref class DeclarationContext : CppSharp::Parser::AST::Declaration
            {
            public:

                DeclarationContext(::CppSharp::CppParser::AST::DeclarationContext* native);
                DeclarationContext(System::IntPtr native);
                DeclarationContext(CppSharp::Parser::AST::DeclarationKind kind);

                property unsigned int NamespacesCount
                {
                    unsigned int get();
                }

                property unsigned int EnumsCount
                {
                    unsigned int get();
                }

                property unsigned int FunctionsCount
                {
                    unsigned int get();
                }

                property unsigned int ClassesCount
                {
                    unsigned int get();
                }

                property unsigned int TemplatesCount
                {
                    unsigned int get();
                }

                property unsigned int TypedefsCount
                {
                    unsigned int get();
                }

                property unsigned int VariablesCount
                {
                    unsigned int get();
                }

                property bool IsAnonymous
                {
                    bool get();
                    void set(bool);
                }

                CppSharp::Parser::AST::Namespace^ getNamespaces(unsigned int i);

                void addNamespaces(CppSharp::Parser::AST::Namespace^ s);

                CppSharp::Parser::AST::Enumeration^ getEnums(unsigned int i);

                void addEnums(CppSharp::Parser::AST::Enumeration^ s);

                CppSharp::Parser::AST::Function^ getFunctions(unsigned int i);

                void addFunctions(CppSharp::Parser::AST::Function^ s);

                CppSharp::Parser::AST::Class^ getClasses(unsigned int i);

                void addClasses(CppSharp::Parser::AST::Class^ s);

                CppSharp::Parser::AST::Template^ getTemplates(unsigned int i);

                void addTemplates(CppSharp::Parser::AST::Template^ s);

                CppSharp::Parser::AST::TypedefDecl^ getTypedefs(unsigned int i);

                void addTypedefs(CppSharp::Parser::AST::TypedefDecl^ s);

                CppSharp::Parser::AST::Variable^ getVariables(unsigned int i);

                void addVariables(CppSharp::Parser::AST::Variable^ s);
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

                property unsigned int Index
                {
                    unsigned int get();
                    void set(unsigned int);
                }
            };

            public ref class Function : CppSharp::Parser::AST::Declaration
            {
            public:

                Function(::CppSharp::CppParser::AST::Function* native);
                Function(System::IntPtr native);
                Function();

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

                property unsigned int ParametersCount
                {
                    unsigned int get();
                }

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

                property CppSharp::Parser::AST::CallingConvention CallingConvention
                {
                    CppSharp::Parser::AST::CallingConvention get();
                    void set(CppSharp::Parser::AST::CallingConvention);
                }

                property CppSharp::Parser::AST::FunctionTemplateSpecialization^ SpecializationInfo
                {
                    CppSharp::Parser::AST::FunctionTemplateSpecialization^ get();
                    void set(CppSharp::Parser::AST::FunctionTemplateSpecialization^);
                }

                CppSharp::Parser::AST::Parameter^ getParameters(unsigned int i);

                void addParameters(CppSharp::Parser::AST::Parameter^ s);
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

                property bool IsExplicit
                {
                    bool get();
                    void set(bool);
                }

                property bool IsOverride
                {
                    bool get();
                    void set(bool);
                }

                property CppSharp::Parser::AST::CXXMethodKind MethodKind
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

                property unsigned int ItemsCount
                {
                    unsigned int get();
                }

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

                CppSharp::Parser::AST::Enumeration::Item^ getItems(unsigned int i);

                void addItems(CppSharp::Parser::AST::Enumeration::Item^ s);
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
                property System::IntPtr __Instance
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

                property unsigned int BasesCount
                {
                    unsigned int get();
                }

                property unsigned int FieldsCount
                {
                    unsigned int get();
                }

                property unsigned int MethodsCount
                {
                    unsigned int get();
                }

                property unsigned int SpecifiersCount
                {
                    unsigned int get();
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

                property bool HasNonTrivialDestructor
                {
                    bool get();
                    void set(bool);
                }

                property bool IsExternCContext
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

                void addBases(CppSharp::Parser::AST::BaseClassSpecifier^ s);

                CppSharp::Parser::AST::Field^ getFields(unsigned int i);

                void addFields(CppSharp::Parser::AST::Field^ s);

                CppSharp::Parser::AST::Method^ getMethods(unsigned int i);

                void addMethods(CppSharp::Parser::AST::Method^ s);

                CppSharp::Parser::AST::AccessSpecifierDecl^ getSpecifiers(unsigned int i);

                void addSpecifiers(CppSharp::Parser::AST::AccessSpecifierDecl^ s);
            };

            public ref class Template : CppSharp::Parser::AST::Declaration
            {
            public:

                Template(::CppSharp::CppParser::AST::Template* native);
                Template(System::IntPtr native);
                Template(CppSharp::Parser::AST::DeclarationKind kind);

                Template();

                property unsigned int ParametersCount
                {
                    unsigned int get();
                }

                property CppSharp::Parser::AST::Declaration^ TemplatedDecl
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }

                CppSharp::Parser::AST::TemplateParameter^ getParameters(unsigned int i);

                void addParameters(CppSharp::Parser::AST::TemplateParameter^ s);
            };

            public ref class ClassTemplate : CppSharp::Parser::AST::Template
            {
            public:

                ClassTemplate(::CppSharp::CppParser::AST::ClassTemplate* native);
                ClassTemplate(System::IntPtr native);
                ClassTemplate();

                property unsigned int SpecializationsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::ClassTemplateSpecialization^ getSpecializations(unsigned int i);

                void addSpecializations(CppSharp::Parser::AST::ClassTemplateSpecialization^ s);
            };

            public ref class ClassTemplateSpecialization : CppSharp::Parser::AST::Class
            {
            public:

                ClassTemplateSpecialization(::CppSharp::CppParser::AST::ClassTemplateSpecialization* native);
                ClassTemplateSpecialization(System::IntPtr native);
                ClassTemplateSpecialization();

                property unsigned int ArgumentsCount
                {
                    unsigned int get();
                }

                property CppSharp::Parser::AST::ClassTemplate^ TemplatedDecl
                {
                    CppSharp::Parser::AST::ClassTemplate^ get();
                    void set(CppSharp::Parser::AST::ClassTemplate^);
                }

                property CppSharp::Parser::AST::TemplateSpecializationKind SpecializationKind
                {
                    CppSharp::Parser::AST::TemplateSpecializationKind get();
                    void set(CppSharp::Parser::AST::TemplateSpecializationKind);
                }

                CppSharp::Parser::AST::TemplateArgument^ getArguments(unsigned int i);

                void addArguments(CppSharp::Parser::AST::TemplateArgument^ s);
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

                property unsigned int SpecializationsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::FunctionTemplateSpecialization^ getSpecializations(unsigned int i);

                void addSpecializations(CppSharp::Parser::AST::FunctionTemplateSpecialization^ s);
            };

            public ref class FunctionTemplateSpecialization : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::FunctionTemplateSpecialization* NativePtr;
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                FunctionTemplateSpecialization(::CppSharp::CppParser::AST::FunctionTemplateSpecialization* native);
                FunctionTemplateSpecialization(System::IntPtr native);
                FunctionTemplateSpecialization();

                property unsigned int ArgumentsCount
                {
                    unsigned int get();
                }

                property CppSharp::Parser::AST::FunctionTemplate^ Template
                {
                    CppSharp::Parser::AST::FunctionTemplate^ get();
                    void set(CppSharp::Parser::AST::FunctionTemplate^);
                }

                property CppSharp::Parser::AST::Function^ SpecializedFunction
                {
                    CppSharp::Parser::AST::Function^ get();
                    void set(CppSharp::Parser::AST::Function^);
                }

                property CppSharp::Parser::AST::TemplateSpecializationKind SpecializationKind
                {
                    CppSharp::Parser::AST::TemplateSpecializationKind get();
                    void set(CppSharp::Parser::AST::TemplateSpecializationKind);
                }

                CppSharp::Parser::AST::TemplateArgument^ getArguments(unsigned int i);

                void addArguments(CppSharp::Parser::AST::TemplateArgument^ s);
            };

            public ref class Namespace : CppSharp::Parser::AST::DeclarationContext
            {
            public:

                Namespace(::CppSharp::CppParser::AST::Namespace* native);
                Namespace(System::IntPtr native);
                Namespace();

                property bool IsInline
                {
                    bool get();
                    void set(bool);
                }
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

                property unsigned int MacrosCount
                {
                    unsigned int get();
                }

                property bool IsSystemHeader
                {
                    bool get();
                    void set(bool);
                }

                CppSharp::Parser::AST::MacroDefinition^ getMacros(unsigned int i);

                void addMacros(CppSharp::Parser::AST::MacroDefinition^ s);
            };

            public ref class NativeLibrary : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::NativeLibrary* NativePtr;
                property System::IntPtr __Instance
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

                property unsigned int SymbolsCount
                {
                    unsigned int get();
                }

                System::String^ getSymbols(unsigned int i);

                void addSymbols(System::String^ s);
            };

            public ref class ASTContext : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::ASTContext* NativePtr;
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                ASTContext(::CppSharp::CppParser::AST::ASTContext* native);
                ASTContext(System::IntPtr native);
                ASTContext();

                property unsigned int TranslationUnitsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::TranslationUnit^ getTranslationUnits(unsigned int i);

                void addTranslationUnits(CppSharp::Parser::AST::TranslationUnit^ s);
            };

            public ref class Comment : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::Comment* NativePtr;
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                Comment(::CppSharp::CppParser::AST::Comment* native);
                Comment(System::IntPtr native);
                Comment(CppSharp::Parser::AST::CommentKind kind);

                property CppSharp::Parser::AST::CommentKind Kind
                {
                    CppSharp::Parser::AST::CommentKind get();
                    void set(CppSharp::Parser::AST::CommentKind);
                }
            };

            public ref class FullComment : CppSharp::Parser::AST::Comment
            {
            public:

                FullComment(::CppSharp::CppParser::AST::FullComment* native);
                FullComment(System::IntPtr native);
                FullComment();
            };

            public ref class RawComment : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::RawComment* NativePtr;
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                RawComment(::CppSharp::CppParser::AST::RawComment* native);
                RawComment(System::IntPtr native);
                RawComment();

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

                property CppSharp::Parser::AST::RawCommentKind RawCommentKind
                {
                    CppSharp::Parser::AST::RawCommentKind get();
                    void set(CppSharp::Parser::AST::RawCommentKind);
                }

                property CppSharp::Parser::AST::FullComment^ FullComment
                {
                    CppSharp::Parser::AST::FullComment^ get();
                    void set(CppSharp::Parser::AST::FullComment^);
                }
            };
        }
    }
}
