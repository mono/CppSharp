#pragma once

#include "CppSharp.h"
#include <AST.h>

namespace CppSharp
{
    enum struct AccessSpecifier;
    enum struct CallingConvention;
    enum struct CppAbi;
    enum struct CXXMethodKind;
    enum struct CXXOperatorKind;
    enum struct MacroLocation;
    enum struct PrimitiveType;
    enum struct RawCommentKind;
    enum struct VTableComponentKind;
    ref class AccessSpecifierDecl;
    ref class ArrayType;
    ref class BaseClassSpecifier;
    ref class BuiltinType;
    ref class Class;
    ref class ClassLayout;
    ref class ClassTemplate;
    ref class ClassTemplatePartialSpecialization;
    ref class ClassTemplateSpecialization;
    ref class DecayedType;
    ref class Declaration;
    ref class DeclarationContext;
    ref class DependentNameType;
    ref class Enumeration;
    ref class Field;
    ref class Function;
    ref class FunctionTemplate;
    ref class FunctionType;
    ref class InjectedClassNameType;
    ref class Library;
    ref class MacroDefinition;
    ref class MacroExpansion;
    ref class MemberPointerType;
    ref class Method;
    ref class Namespace;
    ref class NativeLibrary;
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
    ref class TypedefDecl;
    ref class TypedefType;
    ref class TypeQualifiers;
    ref class Variable;
    ref class VFTableInfo;
    ref class VTableComponent;
    ref class VTableLayout;

    namespace Parser
    {
        enum struct AccessSpecifier;
        enum struct ArgumentKind;
        enum struct ArraySize;
        enum struct CallingConvention;
        enum struct CppAbi;
        enum struct CXXMethodKind;
        enum struct CXXOperatorKind;
        enum struct EnumModifiers;
        enum struct MacroLocation;
        enum struct PrimitiveType;
        enum struct RawCommentKind;
        enum struct TypeModifier;
        enum struct VTableComponentKind;
        ref class AccessSpecifierDecl;
        ref class ArrayType;
        ref class BaseClassSpecifier;
        ref class BuiltinType;
        ref class Class;
        ref class ClassLayout;
        ref class ClassTemplate;
        ref class ClassTemplatePartialSpecialization;
        ref class ClassTemplateSpecialization;
        ref class DecayedType;
        ref class Declaration;
        ref class DeclarationContext;
        ref class DependentNameType;
        ref class Enumeration;
        ref class Field;
        ref class Function;
        ref class FunctionTemplate;
        ref class FunctionType;
        ref class InjectedClassNameType;
        ref class Item;
        ref class Library;
        ref class MacroDefinition;
        ref class MacroExpansion;
        ref class MemberPointerType;
        ref class Method;
        ref class Namespace;
        ref class NativeLibrary;
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
        ref class TypedefDecl;
        ref class TypedefType;
        ref class TypeQualifiers;
        ref class Variable;
        ref class VFTableInfo;
        ref class VTableComponent;
        ref class VTableLayout;

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

            property ::CppSharp::CppParser::Type* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            Type(::CppSharp::CppParser::Type* native);
            Type(System::IntPtr native);
            Type();

        };

        public ref class TypeQualifiers : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::TypeQualifiers* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            TypeQualifiers(::CppSharp::CppParser::TypeQualifiers* native);
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

            property ::CppSharp::CppParser::QualifiedType* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            QualifiedType(::CppSharp::CppParser::QualifiedType* native);
            QualifiedType(System::IntPtr native);
            QualifiedType();

            property CppSharp::Parser::Type^ Type
            {
                CppSharp::Parser::Type^ get();
                void set(CppSharp::Parser::Type^);
            }
            property CppSharp::Parser::TypeQualifiers^ Qualifiers
            {
                CppSharp::Parser::TypeQualifiers^ get();
                void set(CppSharp::Parser::TypeQualifiers^);
            }
        };

        public ref class TagType : CppSharp::Parser::Type
        {
        public:

            TagType(::CppSharp::CppParser::TagType* native);
            TagType(System::IntPtr native);
            TagType();

            property CppSharp::Parser::Declaration^ Declaration
            {
                CppSharp::Parser::Declaration^ get();
                void set(CppSharp::Parser::Declaration^);
            }
        };

        public ref class ArrayType : CppSharp::Parser::Type
        {
        public:

            enum struct ArraySize
            {
                Constant = 0,
                Variable = 1,
                Dependent = 2,
                Incomplete = 3
            };

            ArrayType(::CppSharp::CppParser::ArrayType* native);
            ArrayType(System::IntPtr native);
            ArrayType();

            property CppSharp::Parser::QualifiedType^ QualifiedType
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
            property CppSharp::Parser::ArrayType::ArraySize SizeType
            {
                CppSharp::Parser::ArrayType::ArraySize get();
                void set(CppSharp::Parser::ArrayType::ArraySize);
            }
            property int Size
            {
                int get();
                void set(int);
            }
        };

        public ref class FunctionType : CppSharp::Parser::Type
        {
        public:

            FunctionType(::CppSharp::CppParser::FunctionType* native);
            FunctionType(System::IntPtr native);
            FunctionType();

            property CppSharp::Parser::QualifiedType^ ReturnType
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ Parameters
            {
                System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Parameter^>^);
            }
            property CppSharp::Parser::CallingConvention CallingConvention
            {
                CppSharp::Parser::CallingConvention get();
                void set(CppSharp::Parser::CallingConvention);
            }
        };

        public ref class PointerType : CppSharp::Parser::Type
        {
        public:

            enum struct TypeModifier
            {
                Value = 0,
                Pointer = 1,
                LVReference = 2,
                RVReference = 3
            };

            PointerType(::CppSharp::CppParser::PointerType* native);
            PointerType(System::IntPtr native);
            PointerType();

            property CppSharp::Parser::QualifiedType^ QualifiedPointee
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
            property CppSharp::Parser::PointerType::TypeModifier Modifier
            {
                CppSharp::Parser::PointerType::TypeModifier get();
                void set(CppSharp::Parser::PointerType::TypeModifier);
            }
        };

        public ref class MemberPointerType : CppSharp::Parser::Type
        {
        public:

            MemberPointerType(::CppSharp::CppParser::MemberPointerType* native);
            MemberPointerType(System::IntPtr native);
            MemberPointerType();

            property CppSharp::Parser::QualifiedType^ Pointee
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
        };

        public ref class TypedefType : CppSharp::Parser::Type
        {
        public:

            TypedefType(::CppSharp::CppParser::TypedefType* native);
            TypedefType(System::IntPtr native);
            TypedefType();

            property CppSharp::Parser::TypedefDecl^ Declaration
            {
                CppSharp::Parser::TypedefDecl^ get();
                void set(CppSharp::Parser::TypedefDecl^);
            }
        };

        public ref class DecayedType : CppSharp::Parser::Type
        {
        public:

            DecayedType(::CppSharp::CppParser::DecayedType* native);
            DecayedType(System::IntPtr native);
            DecayedType();

            property CppSharp::Parser::QualifiedType^ Decayed
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
            property CppSharp::Parser::QualifiedType^ Original
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
            property CppSharp::Parser::QualifiedType^ Pointee
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
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

            property ::CppSharp::CppParser::TemplateArgument* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            TemplateArgument(::CppSharp::CppParser::TemplateArgument* native);
            TemplateArgument(System::IntPtr native);
            TemplateArgument();

            property CppSharp::Parser::TemplateArgument::ArgumentKind Kind
            {
                CppSharp::Parser::TemplateArgument::ArgumentKind get();
                void set(CppSharp::Parser::TemplateArgument::ArgumentKind);
            }
            property CppSharp::Parser::QualifiedType^ Type
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
            property CppSharp::Parser::Declaration^ Declaration
            {
                CppSharp::Parser::Declaration^ get();
                void set(CppSharp::Parser::Declaration^);
            }
            property int Integral
            {
                int get();
                void set(int);
            }
        };

        public ref class TemplateSpecializationType : CppSharp::Parser::Type
        {
        public:

            TemplateSpecializationType(::CppSharp::CppParser::TemplateSpecializationType* native);
            TemplateSpecializationType(System::IntPtr native);
            TemplateSpecializationType();

            property System::Collections::Generic::List<CppSharp::Parser::TemplateArgument^>^ Arguments
            {
                System::Collections::Generic::List<CppSharp::Parser::TemplateArgument^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::TemplateArgument^>^);
            }
            property CppSharp::Parser::Template^ Template
            {
                CppSharp::Parser::Template^ get();
                void set(CppSharp::Parser::Template^);
            }
            property CppSharp::Parser::Type^ Desugared
            {
                CppSharp::Parser::Type^ get();
                void set(CppSharp::Parser::Type^);
            }
        };

        public ref class TemplateParameter : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::TemplateParameter* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            TemplateParameter(::CppSharp::CppParser::TemplateParameter* native);
            TemplateParameter(System::IntPtr native);
            TemplateParameter();

            property System::String^ Name
            {
                System::String^ get();
                void set(System::String^);
            }
        };

        public ref class TemplateParameterType : CppSharp::Parser::Type
        {
        public:

            TemplateParameterType(::CppSharp::CppParser::TemplateParameterType* native);
            TemplateParameterType(System::IntPtr native);
            TemplateParameterType();

            property CppSharp::Parser::TemplateParameter^ Parameter
            {
                CppSharp::Parser::TemplateParameter^ get();
                void set(CppSharp::Parser::TemplateParameter^);
            }
        };

        public ref class TemplateParameterSubstitutionType : CppSharp::Parser::Type
        {
        public:

            TemplateParameterSubstitutionType(::CppSharp::CppParser::TemplateParameterSubstitutionType* native);
            TemplateParameterSubstitutionType(System::IntPtr native);
            TemplateParameterSubstitutionType();

            property CppSharp::Parser::QualifiedType^ Replacement
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
        };

        public ref class InjectedClassNameType : CppSharp::Parser::Type
        {
        public:

            InjectedClassNameType(::CppSharp::CppParser::InjectedClassNameType* native);
            InjectedClassNameType(System::IntPtr native);
            InjectedClassNameType();

            property CppSharp::Parser::TemplateSpecializationType^ TemplateSpecialization
            {
                CppSharp::Parser::TemplateSpecializationType^ get();
                void set(CppSharp::Parser::TemplateSpecializationType^);
            }
            property CppSharp::Parser::Class^ Class
            {
                CppSharp::Parser::Class^ get();
                void set(CppSharp::Parser::Class^);
            }
        };

        public ref class DependentNameType : CppSharp::Parser::Type
        {
        public:

            DependentNameType(::CppSharp::CppParser::DependentNameType* native);
            DependentNameType(System::IntPtr native);
            DependentNameType();

        };

        public ref class BuiltinType : CppSharp::Parser::Type
        {
        public:

            BuiltinType(::CppSharp::CppParser::BuiltinType* native);
            BuiltinType(System::IntPtr native);
            BuiltinType();

            property CppSharp::Parser::PrimitiveType Type
            {
                CppSharp::Parser::PrimitiveType get();
                void set(CppSharp::Parser::PrimitiveType);
            }
        };

        public ref class RawComment : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::RawComment* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            RawComment(::CppSharp::CppParser::RawComment* native);
            RawComment(System::IntPtr native);
            RawComment();

            property CppSharp::Parser::RawCommentKind Kind
            {
                CppSharp::Parser::RawCommentKind get();
                void set(CppSharp::Parser::RawCommentKind);
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

            property ::CppSharp::CppParser::VTableComponent* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            VTableComponent(::CppSharp::CppParser::VTableComponent* native);
            VTableComponent(System::IntPtr native);
            VTableComponent();

            property CppSharp::Parser::VTableComponentKind Kind
            {
                CppSharp::Parser::VTableComponentKind get();
                void set(CppSharp::Parser::VTableComponentKind);
            }
            property unsigned int Offset
            {
                unsigned int get();
                void set(unsigned int);
            }
            property CppSharp::Parser::Declaration^ Declaration
            {
                CppSharp::Parser::Declaration^ get();
                void set(CppSharp::Parser::Declaration^);
            }
        };

        public ref class VTableLayout : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::VTableLayout* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            VTableLayout(::CppSharp::CppParser::VTableLayout* native);
            VTableLayout(System::IntPtr native);
            VTableLayout();

            property System::Collections::Generic::List<CppSharp::Parser::VTableComponent^>^ Components
            {
                System::Collections::Generic::List<CppSharp::Parser::VTableComponent^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::VTableComponent^>^);
            }
        };

        public ref class VFTableInfo : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::VFTableInfo* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            VFTableInfo(::CppSharp::CppParser::VFTableInfo* native);
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
            property CppSharp::Parser::VTableLayout^ Layout
            {
                CppSharp::Parser::VTableLayout^ get();
                void set(CppSharp::Parser::VTableLayout^);
            }
        };

        public ref class ClassLayout : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::ClassLayout* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            ClassLayout(::CppSharp::CppParser::ClassLayout* native);
            ClassLayout(System::IntPtr native);
            ClassLayout();

            property CppSharp::Parser::CppAbi ABI
            {
                CppSharp::Parser::CppAbi get();
                void set(CppSharp::Parser::CppAbi);
            }
            property System::Collections::Generic::List<CppSharp::Parser::VFTableInfo^>^ VFTables
            {
                System::Collections::Generic::List<CppSharp::Parser::VFTableInfo^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::VFTableInfo^>^);
            }
            property CppSharp::Parser::VTableLayout^ Layout
            {
                CppSharp::Parser::VTableLayout^ get();
                void set(CppSharp::Parser::VTableLayout^);
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
        };

        public ref class Declaration : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::Declaration* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            Declaration(::CppSharp::CppParser::Declaration* native);
            Declaration(System::IntPtr native);
            Declaration();

            property CppSharp::Parser::AccessSpecifier Access
            {
                CppSharp::Parser::AccessSpecifier get();
                void set(CppSharp::Parser::AccessSpecifier);
            }
            property CppSharp::Parser::DeclarationContext^ _Namespace
            {
                CppSharp::Parser::DeclarationContext^ get();
                void set(CppSharp::Parser::DeclarationContext^);
            }
            property System::String^ Name
            {
                System::String^ get();
                void set(System::String^);
            }
            property CppSharp::Parser::RawComment^ Comment
            {
                CppSharp::Parser::RawComment^ get();
                void set(CppSharp::Parser::RawComment^);
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
            property CppSharp::Parser::Declaration^ CompleteDeclaration
            {
                CppSharp::Parser::Declaration^ get();
                void set(CppSharp::Parser::Declaration^);
            }
            property unsigned int DefinitionOrder
            {
                unsigned int get();
                void set(unsigned int);
            }
            property System::Collections::Generic::List<CppSharp::Parser::PreprocessedEntity^>^ PreprocessedEntities
            {
                System::Collections::Generic::List<CppSharp::Parser::PreprocessedEntity^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::PreprocessedEntity^>^);
            }
            property System::IntPtr OriginalPtr
            {
                System::IntPtr get();
                void set(System::IntPtr);
            }
        };

        public ref class DeclarationContext : CppSharp::Parser::Declaration
        {
        public:

            DeclarationContext(::CppSharp::CppParser::DeclarationContext* native);
            DeclarationContext(System::IntPtr native);
            DeclarationContext();

            property System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ Namespaces
            {
                System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Namespace^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Enumeration^>^ Enums
            {
                System::Collections::Generic::List<CppSharp::Parser::Enumeration^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Enumeration^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Function^>^ Functions
            {
                System::Collections::Generic::List<CppSharp::Parser::Function^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Function^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Class^>^ Classes
            {
                System::Collections::Generic::List<CppSharp::Parser::Class^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Class^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Template^>^ Templates
            {
                System::Collections::Generic::List<CppSharp::Parser::Template^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Template^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::TypedefDecl^>^ Typedefs
            {
                System::Collections::Generic::List<CppSharp::Parser::TypedefDecl^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::TypedefDecl^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Variable^>^ Variables
            {
                System::Collections::Generic::List<CppSharp::Parser::Variable^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Variable^>^);
            }
            CppSharp::Parser::Declaration^ FindAnonymous(unsigned long long key);

            CppSharp::Parser::Namespace^ FindNamespace(System::String^ Name);

            CppSharp::Parser::Namespace^ FindNamespace(System::Collections::Generic::List<System::String^>^ _0);

            CppSharp::Parser::Namespace^ FindCreateNamespace(System::String^ Name);

            CppSharp::Parser::Class^ CreateClass(System::String^ Name, bool IsComplete);

            CppSharp::Parser::Class^ FindClass(System::String^ Name);

            CppSharp::Parser::Class^ FindClass(System::String^ Name, bool IsComplete, bool Create);

            CppSharp::Parser::Enumeration^ FindEnum(System::String^ Name, bool Create);

            CppSharp::Parser::Function^ FindFunction(System::String^ Name, bool Create);

            CppSharp::Parser::TypedefDecl^ FindTypedef(System::String^ Name, bool Create);

        };

        public ref class TypedefDecl : CppSharp::Parser::Declaration
        {
        public:

            TypedefDecl(::CppSharp::CppParser::TypedefDecl* native);
            TypedefDecl(System::IntPtr native);
            TypedefDecl();

            property CppSharp::Parser::QualifiedType^ QualifiedType
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
        };

        public ref class Parameter : CppSharp::Parser::Declaration
        {
        public:

            Parameter(::CppSharp::CppParser::Parameter* native);
            Parameter(System::IntPtr native);
            Parameter();

            property CppSharp::Parser::QualifiedType^ QualifiedType
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
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

        public ref class Function : CppSharp::Parser::Declaration
        {
        public:

            Function(::CppSharp::CppParser::Function* native);
            Function(System::IntPtr native);
            Function();

            property CppSharp::Parser::QualifiedType^ ReturnType
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
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
            property CppSharp::Parser::CXXOperatorKind OperatorKind
            {
                CppSharp::Parser::CXXOperatorKind get();
                void set(CppSharp::Parser::CXXOperatorKind);
            }
            property System::String^ Mangled
            {
                System::String^ get();
                void set(System::String^);
            }
            property CppSharp::Parser::CallingConvention CallingConvention
            {
                CppSharp::Parser::CallingConvention get();
                void set(CppSharp::Parser::CallingConvention);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ Parameters
            {
                System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Parameter^>^);
            }
        };

        public ref class Method : CppSharp::Parser::Function
        {
        public:

            Method(::CppSharp::CppParser::Method* native);
            Method(System::IntPtr native);
            Method();

            property CppSharp::Parser::AccessSpecifierDecl^ AccessDecl
            {
                CppSharp::Parser::AccessSpecifierDecl^ get();
                void set(CppSharp::Parser::AccessSpecifierDecl^);
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
            property CppSharp::Parser::CXXMethodKind Kind
            {
                CppSharp::Parser::CXXMethodKind get();
                void set(CppSharp::Parser::CXXMethodKind);
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
        };

        public ref class Enumeration : CppSharp::Parser::Declaration
        {
        public:

            [System::Flags]
            enum struct EnumModifiers
            {
                Anonymous = 1,
                Scoped = 2,
                Flags = 4
            };

            ref class Item : CppSharp::Parser::Declaration
            {
            public:

                Item(::CppSharp::CppParser::Enumeration::Item* native);
                Item(System::IntPtr native);
                Item();

                property System::String^ Name
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property System::String^ Expression
                {
                    System::String^ get();
                    void set(System::String^);
                }
                property System::String^ Comment
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

            Enumeration(::CppSharp::CppParser::Enumeration* native);
            Enumeration(System::IntPtr native);
            Enumeration();

            property CppSharp::Parser::Enumeration::EnumModifiers Modifiers
            {
                CppSharp::Parser::Enumeration::EnumModifiers get();
                void set(CppSharp::Parser::Enumeration::EnumModifiers);
            }
            property CppSharp::Parser::Type^ Type
            {
                CppSharp::Parser::Type^ get();
                void set(CppSharp::Parser::Type^);
            }
            property CppSharp::Parser::BuiltinType^ BuiltinType
            {
                CppSharp::Parser::BuiltinType^ get();
                void set(CppSharp::Parser::BuiltinType^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Enumeration::Item^>^ Items
            {
                System::Collections::Generic::List<CppSharp::Parser::Enumeration::Item^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Enumeration::Item^>^);
            }
        };

        public ref class Variable : CppSharp::Parser::Declaration
        {
        public:

            Variable(::CppSharp::CppParser::Variable* native);
            Variable(System::IntPtr native);
            Variable();

            property System::String^ Mangled
            {
                System::String^ get();
                void set(System::String^);
            }
            property CppSharp::Parser::QualifiedType^ QualifiedType
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
        };

        public ref class BaseClassSpecifier : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::BaseClassSpecifier* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            BaseClassSpecifier(::CppSharp::CppParser::BaseClassSpecifier* native);
            BaseClassSpecifier(System::IntPtr native);
            BaseClassSpecifier();

            property CppSharp::Parser::AccessSpecifier Access
            {
                CppSharp::Parser::AccessSpecifier get();
                void set(CppSharp::Parser::AccessSpecifier);
            }
            property bool IsVirtual
            {
                bool get();
                void set(bool);
            }
            property CppSharp::Parser::Type^ Type
            {
                CppSharp::Parser::Type^ get();
                void set(CppSharp::Parser::Type^);
            }
        };

        public ref class Field : CppSharp::Parser::Declaration
        {
        public:

            Field(::CppSharp::CppParser::Field* native);
            Field(System::IntPtr native);
            Field();

            property CppSharp::Parser::QualifiedType^ QualifiedType
            {
                CppSharp::Parser::QualifiedType^ get();
                void set(CppSharp::Parser::QualifiedType^);
            }
            property CppSharp::Parser::AccessSpecifier Access
            {
                CppSharp::Parser::AccessSpecifier get();
                void set(CppSharp::Parser::AccessSpecifier);
            }
            property unsigned int Offset
            {
                unsigned int get();
                void set(unsigned int);
            }
            property CppSharp::Parser::Class^ Class
            {
                CppSharp::Parser::Class^ get();
                void set(CppSharp::Parser::Class^);
            }
        };

        public ref class AccessSpecifierDecl : CppSharp::Parser::Declaration
        {
        public:

            AccessSpecifierDecl(::CppSharp::CppParser::AccessSpecifierDecl* native);
            AccessSpecifierDecl(System::IntPtr native);
            AccessSpecifierDecl();

        };

        public ref class Class : CppSharp::Parser::DeclarationContext
        {
        public:

            Class(::CppSharp::CppParser::Class* native);
            Class(System::IntPtr native);
            Class();

            property System::Collections::Generic::List<CppSharp::Parser::BaseClassSpecifier^>^ Bases
            {
                System::Collections::Generic::List<CppSharp::Parser::BaseClassSpecifier^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::BaseClassSpecifier^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Field^>^ Fields
            {
                System::Collections::Generic::List<CppSharp::Parser::Field^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Field^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::Method^>^ Methods
            {
                System::Collections::Generic::List<CppSharp::Parser::Method^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Method^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::AccessSpecifierDecl^>^ Specifiers
            {
                System::Collections::Generic::List<CppSharp::Parser::AccessSpecifierDecl^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::AccessSpecifierDecl^>^);
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
            property CppSharp::Parser::ClassLayout^ Layout
            {
                CppSharp::Parser::ClassLayout^ get();
                void set(CppSharp::Parser::ClassLayout^);
            }
        };

        public ref class Template : CppSharp::Parser::Declaration
        {
        public:

            Template(::CppSharp::CppParser::Template* native);
            Template(System::IntPtr native);
            Template();

            property CppSharp::Parser::Declaration^ TemplatedDecl
            {
                CppSharp::Parser::Declaration^ get();
                void set(CppSharp::Parser::Declaration^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::TemplateParameter^>^ Parameters
            {
                System::Collections::Generic::List<CppSharp::Parser::TemplateParameter^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::TemplateParameter^>^);
            }
        };

        public ref class ClassTemplate : CppSharp::Parser::Template
        {
        public:

            ClassTemplate(::CppSharp::CppParser::ClassTemplate* native);
            ClassTemplate(System::IntPtr native);
            ClassTemplate();

        };

        public ref class ClassTemplateSpecialization : CppSharp::Parser::Declaration
        {
        public:

            ClassTemplateSpecialization(::CppSharp::CppParser::ClassTemplateSpecialization* native);
            ClassTemplateSpecialization(System::IntPtr native);
            ClassTemplateSpecialization();

        };

        public ref class ClassTemplatePartialSpecialization : CppSharp::Parser::Declaration
        {
        public:

            ClassTemplatePartialSpecialization(::CppSharp::CppParser::ClassTemplatePartialSpecialization* native);
            ClassTemplatePartialSpecialization(System::IntPtr native);
            ClassTemplatePartialSpecialization();

        };

        public ref class FunctionTemplate : CppSharp::Parser::Template
        {
        public:

            FunctionTemplate(::CppSharp::CppParser::FunctionTemplate* native);
            FunctionTemplate(System::IntPtr native);
            FunctionTemplate();

        };

        public ref class Namespace : CppSharp::Parser::DeclarationContext
        {
        public:

            Namespace(::CppSharp::CppParser::Namespace* native);
            Namespace(System::IntPtr native);
            Namespace();

        };

        public ref class PreprocessedEntity : CppSharp::Parser::Declaration
        {
        public:

            PreprocessedEntity(::CppSharp::CppParser::PreprocessedEntity* native);
            PreprocessedEntity(System::IntPtr native);
            PreprocessedEntity();

            property CppSharp::Parser::MacroLocation Location
            {
                CppSharp::Parser::MacroLocation get();
                void set(CppSharp::Parser::MacroLocation);
            }
        };

        public ref class MacroDefinition : CppSharp::Parser::PreprocessedEntity
        {
        public:

            MacroDefinition(::CppSharp::CppParser::MacroDefinition* native);
            MacroDefinition(System::IntPtr native);
            MacroDefinition();

            property System::String^ Expression
            {
                System::String^ get();
                void set(System::String^);
            }
        };

        public ref class MacroExpansion : CppSharp::Parser::PreprocessedEntity
        {
        public:

            MacroExpansion(::CppSharp::CppParser::MacroExpansion* native);
            MacroExpansion(System::IntPtr native);
            MacroExpansion();

            property System::String^ Text
            {
                System::String^ get();
                void set(System::String^);
            }
            property CppSharp::Parser::MacroDefinition^ Definition
            {
                CppSharp::Parser::MacroDefinition^ get();
                void set(CppSharp::Parser::MacroDefinition^);
            }
        };

        public ref class TranslationUnit : CppSharp::Parser::Namespace
        {
        public:

            TranslationUnit(::CppSharp::CppParser::TranslationUnit* native);
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
            property System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ Namespaces
            {
                System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::Namespace^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::MacroDefinition^>^ Macros
            {
                System::Collections::Generic::List<CppSharp::Parser::MacroDefinition^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::MacroDefinition^>^);
            }
        };

        public ref class NativeLibrary : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::NativeLibrary* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            NativeLibrary(::CppSharp::CppParser::NativeLibrary* native);
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
        };

        public ref class Library : ICppInstance
        {
        public:

            property ::CppSharp::CppParser::Library* NativePtr;
            property System::IntPtr Instance
            {
                virtual System::IntPtr get();
                virtual void set(System::IntPtr instance);
            }

            Library(::CppSharp::CppParser::Library* native);
            Library(System::IntPtr native);
            Library();

            property System::Collections::Generic::List<CppSharp::Parser::TranslationUnit^>^ TranslationUnits
            {
                System::Collections::Generic::List<CppSharp::Parser::TranslationUnit^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::TranslationUnit^>^);
            }
            property System::Collections::Generic::List<CppSharp::Parser::NativeLibrary^>^ Libraries
            {
                System::Collections::Generic::List<CppSharp::Parser::NativeLibrary^>^ get();
                void set(System::Collections::Generic::List<CppSharp::Parser::NativeLibrary^>^);
            }
            CppSharp::Parser::TranslationUnit^ FindOrCreateModule(System::String^ File);

            CppSharp::Parser::NativeLibrary^ FindOrCreateLibrary(System::String^ File);

        };
    }
}
