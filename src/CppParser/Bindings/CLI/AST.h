#pragma once

#include "CppSharp.h"
#include <AST.h>
#include "Sources.h"

namespace CppSharp
{
    namespace Parser
    {
        namespace AST
        {
            enum struct AccessSpecifier;
            enum struct ArchType;
            enum struct CXXMethodKind;
            enum struct CXXOperatorKind;
            enum struct CallingConvention;
            enum struct CommentKind;
            enum struct CppAbi;
            enum struct DeclarationKind;
            enum struct MacroLocation;
            enum struct PrimitiveType;
            enum struct RawCommentKind;
            enum struct StatementClass;
            enum struct TemplateSpecializationKind;
            enum struct TypeKind;
            enum struct VTableComponentKind;
            ref class ASTContext;
            ref class AccessSpecifierDecl;
            ref class ArrayType;
            ref class AttributedType;
            ref class BaseClassSpecifier;
            ref class BinaryOperator;
            ref class BlockCommandComment;
            ref class BlockContentComment;
            ref class BuiltinType;
            ref class CXXConstructExpr;
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
            ref class Expression;
            ref class Field;
            ref class Friend;
            ref class FullComment;
            ref class Function;
            ref class FunctionTemplate;
            ref class FunctionTemplateSpecialization;
            ref class FunctionType;
            ref class HTMLEndTagComment;
            ref class HTMLStartTagComment;
            ref class HTMLTagComment;
            ref class InjectedClassNameType;
            ref class InlineCommandComment;
            ref class InlineContentComment;
            ref class MacroDefinition;
            ref class MacroExpansion;
            ref class MemberPointerType;
            ref class Method;
            ref class Namespace;
            ref class NativeLibrary;
            ref class PackExpansionType;
            ref class ParagraphComment;
            ref class ParamCommandComment;
            ref class Parameter;
            ref class PointerType;
            ref class PreprocessedEntity;
            ref class QualifiedType;
            ref class RawComment;
            ref class Statement;
            ref class TParamCommandComment;
            ref class TagType;
            ref class Template;
            ref class TemplateArgument;
            ref class TemplateParameter;
            ref class TemplateParameterSubstitutionType;
            ref class TemplateParameterType;
            ref class TemplateSpecializationType;
            ref class TextComment;
            ref class TranslationUnit;
            ref class Type;
            ref class TypeQualifiers;
            ref class TypedefDecl;
            ref class TypedefType;
            ref class VFTableInfo;
            ref class VTableComponent;
            ref class VTableLayout;
            ref class Variable;
            ref class VerbatimBlockComment;
            ref class VerbatimBlockLineComment;
            ref class VerbatimLineComment;
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
                TranslationUnit = 20,
                Friend = 21
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

            public enum struct StatementClass
            {
                Any = 0,
                BinaryOperator = 1,
                DeclRefExprClass = 2,
                CXXConstructExprClass = 3,
                CXXOperatorCallExpr = 4,
                ImplicitCastExpr = 5,
                ExplicitCastExpr = 6
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
                ARM = 2,
                iOS = 3,
                iOS64 = 4
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
                Int = 8,
                UInt = 9,
                Long = 10,
                ULong = 11,
                LongLong = 12,
                ULongLong = 13,
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
                FullComment = 0,
                BlockContentComment = 1,
                BlockCommandComment = 2,
                ParamCommandComment = 3,
                TParamCommandComment = 4,
                VerbatimBlockComment = 5,
                VerbatimLineComment = 6,
                ParagraphComment = 7,
                HTMLTagComment = 8,
                HTMLStartTagComment = 9,
                HTMLEndTagComment = 10,
                TextComment = 11,
                InlineContentComment = 12,
                InlineCommandComment = 13,
                VerbatimBlockLineComment = 14
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

            public enum struct ArchType
            {
                UnknownArch = 0,
                x86 = 1,
                x86_64 = 2
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
                static Type^ __CreateInstance(::System::IntPtr native);
                static Type^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Type(CppSharp::Parser::AST::TypeKind kind);

                Type(CppSharp::Parser::AST::Type^ _0);

                ~Type();

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

                protected:
                bool __ownsNativeInstance;
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
                static TypeQualifiers^ __CreateInstance(::System::IntPtr native);
                static TypeQualifiers^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TypeQualifiers(CppSharp::Parser::AST::TypeQualifiers^ _0);

                TypeQualifiers();

                ~TypeQualifiers();

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

                protected:
                bool __ownsNativeInstance;
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
                static QualifiedType^ __CreateInstance(::System::IntPtr native);
                static QualifiedType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                QualifiedType();

                QualifiedType(CppSharp::Parser::AST::QualifiedType^ _0);

                ~QualifiedType();

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

                protected:
                bool __ownsNativeInstance;
            };

            public ref class TagType : CppSharp::Parser::AST::Type
            {
            public:

                TagType(::CppSharp::CppParser::AST::TagType* native);
                static TagType^ __CreateInstance(::System::IntPtr native);
                static TagType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TagType();

                TagType(CppSharp::Parser::AST::TagType^ _0);

                ~TagType();

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
                static ArrayType^ __CreateInstance(::System::IntPtr native);
                static ArrayType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ArrayType();

                ArrayType(CppSharp::Parser::AST::ArrayType^ _0);

                ~ArrayType();

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

                property long Size
                {
                    long get();
                    void set(long);
                }

                property long ElementSize
                {
                    long get();
                    void set(long);
                }
            };

            public ref class FunctionType : CppSharp::Parser::AST::Type
            {
            public:

                FunctionType(::CppSharp::CppParser::AST::FunctionType* native);
                static FunctionType^ __CreateInstance(::System::IntPtr native);
                static FunctionType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                FunctionType();

                FunctionType(CppSharp::Parser::AST::FunctionType^ _0);

                ~FunctionType();

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

                void clearParameters();
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
                static PointerType^ __CreateInstance(::System::IntPtr native);
                static PointerType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                PointerType();

                PointerType(CppSharp::Parser::AST::PointerType^ _0);

                ~PointerType();

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
                static MemberPointerType^ __CreateInstance(::System::IntPtr native);
                static MemberPointerType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                MemberPointerType();

                MemberPointerType(CppSharp::Parser::AST::MemberPointerType^ _0);

                ~MemberPointerType();

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
                static TypedefType^ __CreateInstance(::System::IntPtr native);
                static TypedefType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TypedefType();

                TypedefType(CppSharp::Parser::AST::TypedefType^ _0);

                ~TypedefType();

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
                static AttributedType^ __CreateInstance(::System::IntPtr native);
                static AttributedType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                AttributedType();

                AttributedType(CppSharp::Parser::AST::AttributedType^ _0);

                ~AttributedType();

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
                static DecayedType^ __CreateInstance(::System::IntPtr native);
                static DecayedType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                DecayedType();

                DecayedType(CppSharp::Parser::AST::DecayedType^ _0);

                ~DecayedType();

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
                static TemplateArgument^ __CreateInstance(::System::IntPtr native);
                static TemplateArgument^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TemplateArgument();

                TemplateArgument(CppSharp::Parser::AST::TemplateArgument^ _0);

                ~TemplateArgument();

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

                property long Integral
                {
                    long get();
                    void set(long);
                }

                protected:
                bool __ownsNativeInstance;
            };

            public ref class TemplateSpecializationType : CppSharp::Parser::AST::Type
            {
            public:

                TemplateSpecializationType(::CppSharp::CppParser::AST::TemplateSpecializationType* native);
                static TemplateSpecializationType^ __CreateInstance(::System::IntPtr native);
                static TemplateSpecializationType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TemplateSpecializationType();

                TemplateSpecializationType(CppSharp::Parser::AST::TemplateSpecializationType^ _0);

                ~TemplateSpecializationType();

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

                void clearArguments();
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
                static TemplateParameter^ __CreateInstance(::System::IntPtr native);
                static TemplateParameter^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TemplateParameter();

                TemplateParameter(CppSharp::Parser::AST::TemplateParameter^ _0);

                ~TemplateParameter();

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

                virtual bool Equals(::System::Object^ obj) override;

                protected:
                bool __ownsNativeInstance;
            };

            public ref class TemplateParameterType : CppSharp::Parser::AST::Type
            {
            public:

                TemplateParameterType(::CppSharp::CppParser::AST::TemplateParameterType* native);
                static TemplateParameterType^ __CreateInstance(::System::IntPtr native);
                static TemplateParameterType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TemplateParameterType();

                TemplateParameterType(CppSharp::Parser::AST::TemplateParameterType^ _0);

                ~TemplateParameterType();

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
                static TemplateParameterSubstitutionType^ __CreateInstance(::System::IntPtr native);
                static TemplateParameterSubstitutionType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TemplateParameterSubstitutionType();

                TemplateParameterSubstitutionType(CppSharp::Parser::AST::TemplateParameterSubstitutionType^ _0);

                ~TemplateParameterSubstitutionType();

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
                static InjectedClassNameType^ __CreateInstance(::System::IntPtr native);
                static InjectedClassNameType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                InjectedClassNameType();

                InjectedClassNameType(CppSharp::Parser::AST::InjectedClassNameType^ _0);

                ~InjectedClassNameType();

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
                static DependentNameType^ __CreateInstance(::System::IntPtr native);
                static DependentNameType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                DependentNameType();

                DependentNameType(CppSharp::Parser::AST::DependentNameType^ _0);

                ~DependentNameType();
            };

            public ref class PackExpansionType : CppSharp::Parser::AST::Type
            {
            public:

                PackExpansionType(::CppSharp::CppParser::AST::PackExpansionType* native);
                static PackExpansionType^ __CreateInstance(::System::IntPtr native);
                static PackExpansionType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                PackExpansionType();

                PackExpansionType(CppSharp::Parser::AST::PackExpansionType^ _0);

                ~PackExpansionType();
            };

            public ref class BuiltinType : CppSharp::Parser::AST::Type
            {
            public:

                BuiltinType(::CppSharp::CppParser::AST::BuiltinType* native);
                static BuiltinType^ __CreateInstance(::System::IntPtr native);
                static BuiltinType^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                BuiltinType();

                BuiltinType(CppSharp::Parser::AST::BuiltinType^ _0);

                ~BuiltinType();

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
                static VTableComponent^ __CreateInstance(::System::IntPtr native);
                static VTableComponent^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                VTableComponent();

                VTableComponent(CppSharp::Parser::AST::VTableComponent^ _0);

                ~VTableComponent();

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

                protected:
                bool __ownsNativeInstance;
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
                static VTableLayout^ __CreateInstance(::System::IntPtr native);
                static VTableLayout^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                VTableLayout();

                VTableLayout(CppSharp::Parser::AST::VTableLayout^ _0);

                ~VTableLayout();

                property unsigned int ComponentsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::VTableComponent^ getComponents(unsigned int i);

                void addComponents(CppSharp::Parser::AST::VTableComponent^ s);

                void clearComponents();

                protected:
                bool __ownsNativeInstance;
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
                static VFTableInfo^ __CreateInstance(::System::IntPtr native);
                static VFTableInfo^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                VFTableInfo();

                VFTableInfo(CppSharp::Parser::AST::VFTableInfo^ _0);

                ~VFTableInfo();

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

                protected:
                bool __ownsNativeInstance;
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
                static ClassLayout^ __CreateInstance(::System::IntPtr native);
                static ClassLayout^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ClassLayout();

                ClassLayout(CppSharp::Parser::AST::ClassLayout^ _0);

                ~ClassLayout();

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

                property long VBPtrOffset
                {
                    long get();
                    void set(long);
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

                void clearVFTables();

                protected:
                bool __ownsNativeInstance;
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
                static Declaration^ __CreateInstance(::System::IntPtr native);
                static Declaration^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Declaration(CppSharp::Parser::AST::DeclarationKind kind);

                Declaration(CppSharp::Parser::AST::Declaration^ _0);

                ~Declaration();

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

                property CppSharp::Parser::SourceLocation Location
                {
                    CppSharp::Parser::SourceLocation get();
                    void set(CppSharp::Parser::SourceLocation);
                }

                property int LineNumberStart
                {
                    int get();
                    void set(int);
                }

                property int LineNumberEnd
                {
                    int get();
                    void set(int);
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

                property ::System::IntPtr OriginalPtr
                {
                    ::System::IntPtr get();
                    void set(::System::IntPtr);
                }

                CppSharp::Parser::AST::PreprocessedEntity^ getPreprocessedEntities(unsigned int i);

                void addPreprocessedEntities(CppSharp::Parser::AST::PreprocessedEntity^ s);

                void clearPreprocessedEntities();

                protected:
                bool __ownsNativeInstance;
            };

            public ref class DeclarationContext : CppSharp::Parser::AST::Declaration
            {
            public:

                DeclarationContext(::CppSharp::CppParser::AST::DeclarationContext* native);
                static DeclarationContext^ __CreateInstance(::System::IntPtr native);
                static DeclarationContext^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                DeclarationContext(CppSharp::Parser::AST::DeclarationKind kind);

                DeclarationContext(CppSharp::Parser::AST::DeclarationContext^ _0);

                ~DeclarationContext();

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

                property unsigned int FriendsCount
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

                void clearNamespaces();

                CppSharp::Parser::AST::Enumeration^ getEnums(unsigned int i);

                void addEnums(CppSharp::Parser::AST::Enumeration^ s);

                void clearEnums();

                CppSharp::Parser::AST::Function^ getFunctions(unsigned int i);

                void addFunctions(CppSharp::Parser::AST::Function^ s);

                void clearFunctions();

                CppSharp::Parser::AST::Class^ getClasses(unsigned int i);

                void addClasses(CppSharp::Parser::AST::Class^ s);

                void clearClasses();

                CppSharp::Parser::AST::Template^ getTemplates(unsigned int i);

                void addTemplates(CppSharp::Parser::AST::Template^ s);

                void clearTemplates();

                CppSharp::Parser::AST::TypedefDecl^ getTypedefs(unsigned int i);

                void addTypedefs(CppSharp::Parser::AST::TypedefDecl^ s);

                void clearTypedefs();

                CppSharp::Parser::AST::Variable^ getVariables(unsigned int i);

                void addVariables(CppSharp::Parser::AST::Variable^ s);

                void clearVariables();

                CppSharp::Parser::AST::Friend^ getFriends(unsigned int i);

                void addFriends(CppSharp::Parser::AST::Friend^ s);

                void clearFriends();
            };

            public ref class TypedefDecl : CppSharp::Parser::AST::Declaration
            {
            public:

                TypedefDecl(::CppSharp::CppParser::AST::TypedefDecl* native);
                static TypedefDecl^ __CreateInstance(::System::IntPtr native);
                static TypedefDecl^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TypedefDecl();

                TypedefDecl(CppSharp::Parser::AST::TypedefDecl^ _0);

                ~TypedefDecl();

                property CppSharp::Parser::AST::QualifiedType^ QualifiedType
                {
                    CppSharp::Parser::AST::QualifiedType^ get();
                    void set(CppSharp::Parser::AST::QualifiedType^);
                }
            };

            public ref class Friend : CppSharp::Parser::AST::Declaration
            {
            public:

                Friend(::CppSharp::CppParser::AST::Friend* native);
                static Friend^ __CreateInstance(::System::IntPtr native);
                static Friend^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Friend();

                Friend(CppSharp::Parser::AST::Friend^ _0);

                ~Friend();

                property CppSharp::Parser::AST::Declaration^ Declaration
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }
            };

            public ref class Statement : ICppInstance
            {
            public:

                property ::CppSharp::CppParser::AST::Statement* NativePtr;
                property System::IntPtr __Instance
                {
                    virtual System::IntPtr get();
                    virtual void set(System::IntPtr instance);
                }

                Statement(::CppSharp::CppParser::AST::Statement* native);
                static Statement^ __CreateInstance(::System::IntPtr native);
                static Statement^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Statement(CppSharp::Parser::AST::Statement^ _0);

                ~Statement();

                property System::String^ String
                {
                    System::String^ get();
                    void set(System::String^);
                }

                property CppSharp::Parser::AST::StatementClass Class
                {
                    CppSharp::Parser::AST::StatementClass get();
                    void set(CppSharp::Parser::AST::StatementClass);
                }

                property CppSharp::Parser::AST::Declaration^ Decl
                {
                    CppSharp::Parser::AST::Declaration^ get();
                    void set(CppSharp::Parser::AST::Declaration^);
                }

                protected:
                bool __ownsNativeInstance;
            };

            public ref class Expression : CppSharp::Parser::AST::Statement
            {
            public:

                Expression(::CppSharp::CppParser::AST::Expression* native);
                static Expression^ __CreateInstance(::System::IntPtr native);
                static Expression^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Expression(CppSharp::Parser::AST::Expression^ _0);

                ~Expression();
            };

            public ref class BinaryOperator : CppSharp::Parser::AST::Expression
            {
            public:

                BinaryOperator(::CppSharp::CppParser::AST::BinaryOperator* native);
                static BinaryOperator^ __CreateInstance(::System::IntPtr native);
                static BinaryOperator^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                BinaryOperator(CppSharp::Parser::AST::BinaryOperator^ _0);

                ~BinaryOperator();

                property System::String^ OpcodeStr
                {
                    System::String^ get();
                    void set(System::String^);
                }

                property CppSharp::Parser::AST::Expression^ LHS
                {
                    CppSharp::Parser::AST::Expression^ get();
                    void set(CppSharp::Parser::AST::Expression^);
                }

                property CppSharp::Parser::AST::Expression^ RHS
                {
                    CppSharp::Parser::AST::Expression^ get();
                    void set(CppSharp::Parser::AST::Expression^);
                }
            };

            public ref class CXXConstructExpr : CppSharp::Parser::AST::Expression
            {
            public:

                CXXConstructExpr(::CppSharp::CppParser::AST::CXXConstructExpr* native);
                static CXXConstructExpr^ __CreateInstance(::System::IntPtr native);
                static CXXConstructExpr^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                CXXConstructExpr(CppSharp::Parser::AST::CXXConstructExpr^ _0);

                ~CXXConstructExpr();

                property unsigned int ArgumentsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::Expression^ getArguments(unsigned int i);

                void addArguments(CppSharp::Parser::AST::Expression^ s);

                void clearArguments();
            };

            public ref class Parameter : CppSharp::Parser::AST::Declaration
            {
            public:

                Parameter(::CppSharp::CppParser::AST::Parameter* native);
                static Parameter^ __CreateInstance(::System::IntPtr native);
                static Parameter^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Parameter();

                Parameter(CppSharp::Parser::AST::Parameter^ _0);

                ~Parameter();

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

                property CppSharp::Parser::AST::Expression^ DefaultArgument
                {
                    CppSharp::Parser::AST::Expression^ get();
                    void set(CppSharp::Parser::AST::Expression^);
                }
            };

            public ref class Function : CppSharp::Parser::AST::Declaration
            {
            public:

                Function(::CppSharp::CppParser::AST::Function* native);
                static Function^ __CreateInstance(::System::IntPtr native);
                static Function^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Function();

                Function(CppSharp::Parser::AST::Function^ _0);

                ~Function();

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

                property bool HasThisReturn
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

                void clearParameters();
            };

            public ref class Method : CppSharp::Parser::AST::Function
            {
            public:

                Method(::CppSharp::CppParser::AST::Method* native);
                static Method^ __CreateInstance(::System::IntPtr native);
                static Method^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Method();

                Method(CppSharp::Parser::AST::Method^ _0);

                ~Method();

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

            public ref class Enumeration : CppSharp::Parser::AST::DeclarationContext
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
                    static Item^ __CreateInstance(::System::IntPtr native);
                    static Item^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                    Item();

                    Item(CppSharp::Parser::AST::Enumeration::Item^ _0);

                    ~Item();

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
                static Enumeration^ __CreateInstance(::System::IntPtr native);
                static Enumeration^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Enumeration();

                Enumeration(CppSharp::Parser::AST::Enumeration^ _0);

                ~Enumeration();

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

                void clearItems();
            };

            public ref class Variable : CppSharp::Parser::AST::Declaration
            {
            public:

                Variable(::CppSharp::CppParser::AST::Variable* native);
                static Variable^ __CreateInstance(::System::IntPtr native);
                static Variable^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Variable();

                Variable(CppSharp::Parser::AST::Variable^ _0);

                ~Variable();

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
                static BaseClassSpecifier^ __CreateInstance(::System::IntPtr native);
                static BaseClassSpecifier^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                BaseClassSpecifier();

                BaseClassSpecifier(CppSharp::Parser::AST::BaseClassSpecifier^ _0);

                ~BaseClassSpecifier();

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

                property int Offset
                {
                    int get();
                    void set(int);
                }

                protected:
                bool __ownsNativeInstance;
            };

            public ref class Field : CppSharp::Parser::AST::Declaration
            {
            public:

                Field(::CppSharp::CppParser::AST::Field* native);
                static Field^ __CreateInstance(::System::IntPtr native);
                static Field^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Field();

                Field(CppSharp::Parser::AST::Field^ _0);

                ~Field();

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

                property bool IsBitField
                {
                    bool get();
                    void set(bool);
                }

                property unsigned int BitWidth
                {
                    unsigned int get();
                    void set(unsigned int);
                }
            };

            public ref class AccessSpecifierDecl : CppSharp::Parser::AST::Declaration
            {
            public:

                AccessSpecifierDecl(::CppSharp::CppParser::AST::AccessSpecifierDecl* native);
                static AccessSpecifierDecl^ __CreateInstance(::System::IntPtr native);
                static AccessSpecifierDecl^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                AccessSpecifierDecl();

                AccessSpecifierDecl(CppSharp::Parser::AST::AccessSpecifierDecl^ _0);

                ~AccessSpecifierDecl();
            };

            public ref class Class : CppSharp::Parser::AST::DeclarationContext
            {
            public:

                Class(::CppSharp::CppParser::AST::Class* native);
                static Class^ __CreateInstance(::System::IntPtr native);
                static Class^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Class();

                Class(CppSharp::Parser::AST::Class^ _0);

                ~Class();

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

                void clearBases();

                CppSharp::Parser::AST::Field^ getFields(unsigned int i);

                void addFields(CppSharp::Parser::AST::Field^ s);

                void clearFields();

                CppSharp::Parser::AST::Method^ getMethods(unsigned int i);

                void addMethods(CppSharp::Parser::AST::Method^ s);

                void clearMethods();

                CppSharp::Parser::AST::AccessSpecifierDecl^ getSpecifiers(unsigned int i);

                void addSpecifiers(CppSharp::Parser::AST::AccessSpecifierDecl^ s);

                void clearSpecifiers();
            };

            public ref class Template : CppSharp::Parser::AST::Declaration
            {
            public:

                Template(::CppSharp::CppParser::AST::Template* native);
                static Template^ __CreateInstance(::System::IntPtr native);
                static Template^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Template(CppSharp::Parser::AST::DeclarationKind kind);

                Template();

                Template(CppSharp::Parser::AST::Template^ _0);

                ~Template();

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

                void clearParameters();
            };

            public ref class ClassTemplate : CppSharp::Parser::AST::Template
            {
            public:

                ClassTemplate(::CppSharp::CppParser::AST::ClassTemplate* native);
                static ClassTemplate^ __CreateInstance(::System::IntPtr native);
                static ClassTemplate^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ClassTemplate();

                ClassTemplate(CppSharp::Parser::AST::ClassTemplate^ _0);

                ~ClassTemplate();

                property unsigned int SpecializationsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::ClassTemplateSpecialization^ getSpecializations(unsigned int i);

                void addSpecializations(CppSharp::Parser::AST::ClassTemplateSpecialization^ s);

                void clearSpecializations();
            };

            public ref class ClassTemplateSpecialization : CppSharp::Parser::AST::Class
            {
            public:

                ClassTemplateSpecialization(::CppSharp::CppParser::AST::ClassTemplateSpecialization* native);
                static ClassTemplateSpecialization^ __CreateInstance(::System::IntPtr native);
                static ClassTemplateSpecialization^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ClassTemplateSpecialization();

                ClassTemplateSpecialization(CppSharp::Parser::AST::ClassTemplateSpecialization^ _0);

                ~ClassTemplateSpecialization();

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

                void clearArguments();
            };

            public ref class ClassTemplatePartialSpecialization : CppSharp::Parser::AST::ClassTemplateSpecialization
            {
            public:

                ClassTemplatePartialSpecialization(::CppSharp::CppParser::AST::ClassTemplatePartialSpecialization* native);
                static ClassTemplatePartialSpecialization^ __CreateInstance(::System::IntPtr native);
                static ClassTemplatePartialSpecialization^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ClassTemplatePartialSpecialization();

                ClassTemplatePartialSpecialization(CppSharp::Parser::AST::ClassTemplatePartialSpecialization^ _0);

                ~ClassTemplatePartialSpecialization();
            };

            public ref class FunctionTemplate : CppSharp::Parser::AST::Template
            {
            public:

                FunctionTemplate(::CppSharp::CppParser::AST::FunctionTemplate* native);
                static FunctionTemplate^ __CreateInstance(::System::IntPtr native);
                static FunctionTemplate^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                FunctionTemplate();

                FunctionTemplate(CppSharp::Parser::AST::FunctionTemplate^ _0);

                ~FunctionTemplate();

                property unsigned int SpecializationsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::FunctionTemplateSpecialization^ getSpecializations(unsigned int i);

                void addSpecializations(CppSharp::Parser::AST::FunctionTemplateSpecialization^ s);

                void clearSpecializations();
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
                static FunctionTemplateSpecialization^ __CreateInstance(::System::IntPtr native);
                static FunctionTemplateSpecialization^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                FunctionTemplateSpecialization();

                FunctionTemplateSpecialization(CppSharp::Parser::AST::FunctionTemplateSpecialization^ _0);

                ~FunctionTemplateSpecialization();

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

                void clearArguments();

                protected:
                bool __ownsNativeInstance;
            };

            public ref class Namespace : CppSharp::Parser::AST::DeclarationContext
            {
            public:

                Namespace(::CppSharp::CppParser::AST::Namespace* native);
                static Namespace^ __CreateInstance(::System::IntPtr native);
                static Namespace^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Namespace();

                Namespace(CppSharp::Parser::AST::Namespace^ _0);

                ~Namespace();

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
                static PreprocessedEntity^ __CreateInstance(::System::IntPtr native);
                static PreprocessedEntity^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                PreprocessedEntity();

                PreprocessedEntity(CppSharp::Parser::AST::PreprocessedEntity^ _0);

                ~PreprocessedEntity();

                property CppSharp::Parser::AST::MacroLocation MacroLocation
                {
                    CppSharp::Parser::AST::MacroLocation get();
                    void set(CppSharp::Parser::AST::MacroLocation);
                }
            };

            public ref class MacroDefinition : CppSharp::Parser::AST::PreprocessedEntity
            {
            public:

                MacroDefinition(::CppSharp::CppParser::AST::MacroDefinition* native);
                static MacroDefinition^ __CreateInstance(::System::IntPtr native);
                static MacroDefinition^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                MacroDefinition();

                MacroDefinition(CppSharp::Parser::AST::MacroDefinition^ _0);

                ~MacroDefinition();

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
                static MacroExpansion^ __CreateInstance(::System::IntPtr native);
                static MacroExpansion^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                MacroExpansion();

                MacroExpansion(CppSharp::Parser::AST::MacroExpansion^ _0);

                ~MacroExpansion();

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
                static TranslationUnit^ __CreateInstance(::System::IntPtr native);
                static TranslationUnit^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TranslationUnit();

                TranslationUnit(CppSharp::Parser::AST::TranslationUnit^ _0);

                ~TranslationUnit();

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

                void clearMacros();
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
                static NativeLibrary^ __CreateInstance(::System::IntPtr native);
                static NativeLibrary^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                NativeLibrary();

                NativeLibrary(CppSharp::Parser::AST::NativeLibrary^ _0);

                ~NativeLibrary();

                property System::String^ FileName
                {
                    System::String^ get();
                    void set(System::String^);
                }

                property unsigned int SymbolsCount
                {
                    unsigned int get();
                }

                property unsigned int DependenciesCount
                {
                    unsigned int get();
                }

                property CppSharp::Parser::AST::ArchType ArchType
                {
                    CppSharp::Parser::AST::ArchType get();
                    void set(CppSharp::Parser::AST::ArchType);
                }

                System::String^ getSymbols(unsigned int i);

                void addSymbols(System::String^ s);

                void clearSymbols();

                System::String^ getDependencies(unsigned int i);

                void addDependencies(System::String^ s);

                void clearDependencies();

                protected:
                bool __ownsNativeInstance;
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
                static ASTContext^ __CreateInstance(::System::IntPtr native);
                static ASTContext^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ASTContext();

                ASTContext(CppSharp::Parser::AST::ASTContext^ _0);

                ~ASTContext();

                property unsigned int TranslationUnitsCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::TranslationUnit^ getTranslationUnits(unsigned int i);

                void addTranslationUnits(CppSharp::Parser::AST::TranslationUnit^ s);

                void clearTranslationUnits();

                protected:
                bool __ownsNativeInstance;
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
                static Comment^ __CreateInstance(::System::IntPtr native);
                static Comment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                Comment(CppSharp::Parser::AST::CommentKind kind);

                Comment(CppSharp::Parser::AST::Comment^ _0);

                ~Comment();

                property CppSharp::Parser::AST::CommentKind Kind
                {
                    CppSharp::Parser::AST::CommentKind get();
                    void set(CppSharp::Parser::AST::CommentKind);
                }

                protected:
                bool __ownsNativeInstance;
            };

            public ref class BlockContentComment : CppSharp::Parser::AST::Comment
            {
            public:

                BlockContentComment(::CppSharp::CppParser::AST::BlockContentComment* native);
                static BlockContentComment^ __CreateInstance(::System::IntPtr native);
                static BlockContentComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                BlockContentComment();

                BlockContentComment(CppSharp::Parser::AST::CommentKind Kind);

                BlockContentComment(CppSharp::Parser::AST::BlockContentComment^ _0);

                ~BlockContentComment();
            };

            public ref class FullComment : CppSharp::Parser::AST::Comment
            {
            public:

                FullComment(::CppSharp::CppParser::AST::FullComment* native);
                static FullComment^ __CreateInstance(::System::IntPtr native);
                static FullComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                FullComment();

                FullComment(CppSharp::Parser::AST::FullComment^ _0);

                ~FullComment();

                property unsigned int BlocksCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::BlockContentComment^ getBlocks(unsigned int i);

                void addBlocks(CppSharp::Parser::AST::BlockContentComment^ s);

                void clearBlocks();
            };

            public ref class BlockCommandComment : CppSharp::Parser::AST::BlockContentComment
            {
            public:

                ref class Argument : ICppInstance
                {
                public:

                    property ::CppSharp::CppParser::AST::BlockCommandComment::Argument* NativePtr;
                    property System::IntPtr __Instance
                    {
                        virtual System::IntPtr get();
                        virtual void set(System::IntPtr instance);
                    }

                    Argument(::CppSharp::CppParser::AST::BlockCommandComment::Argument* native);
                    static Argument^ __CreateInstance(::System::IntPtr native);
                    static Argument^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                    Argument();

                    Argument(CppSharp::Parser::AST::BlockCommandComment::Argument^ _0);

                    ~Argument();

                    property System::String^ Text
                    {
                        System::String^ get();
                        void set(System::String^);
                    }

                    protected:
                    bool __ownsNativeInstance;
                };

                BlockCommandComment(::CppSharp::CppParser::AST::BlockCommandComment* native);
                static BlockCommandComment^ __CreateInstance(::System::IntPtr native);
                static BlockCommandComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                BlockCommandComment();

                BlockCommandComment(CppSharp::Parser::AST::CommentKind Kind);

                BlockCommandComment(CppSharp::Parser::AST::BlockCommandComment^ _0);

                ~BlockCommandComment();

                property unsigned int ArgumentsCount
                {
                    unsigned int get();
                }

                property unsigned int CommandId
                {
                    unsigned int get();
                    void set(unsigned int);
                }

                CppSharp::Parser::AST::BlockCommandComment::Argument^ getArguments(unsigned int i);

                void addArguments(CppSharp::Parser::AST::BlockCommandComment::Argument^ s);

                void clearArguments();
            };

            public ref class ParamCommandComment : CppSharp::Parser::AST::BlockCommandComment
            {
            public:

                enum struct PassDirection
                {
                    In = 0,
                    Out = 1,
                    InOut = 2
                };

                ParamCommandComment(::CppSharp::CppParser::AST::ParamCommandComment* native);
                static ParamCommandComment^ __CreateInstance(::System::IntPtr native);
                static ParamCommandComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ParamCommandComment();

                ParamCommandComment(CppSharp::Parser::AST::ParamCommandComment^ _0);

                ~ParamCommandComment();

                property CppSharp::Parser::AST::ParamCommandComment::PassDirection Direction
                {
                    CppSharp::Parser::AST::ParamCommandComment::PassDirection get();
                    void set(CppSharp::Parser::AST::ParamCommandComment::PassDirection);
                }

                property unsigned int ParamIndex
                {
                    unsigned int get();
                    void set(unsigned int);
                }
            };

            public ref class TParamCommandComment : CppSharp::Parser::AST::BlockCommandComment
            {
            public:

                TParamCommandComment(::CppSharp::CppParser::AST::TParamCommandComment* native);
                static TParamCommandComment^ __CreateInstance(::System::IntPtr native);
                static TParamCommandComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TParamCommandComment();

                TParamCommandComment(CppSharp::Parser::AST::TParamCommandComment^ _0);

                ~TParamCommandComment();

                property unsigned int PositionCount
                {
                    unsigned int get();
                }

                unsigned int getPosition(unsigned int i);

                void addPosition([System::Runtime::InteropServices::In, System::Runtime::InteropServices::Out] unsigned int% s);

                void clearPosition();
            };

            public ref class VerbatimBlockLineComment : CppSharp::Parser::AST::Comment
            {
            public:

                VerbatimBlockLineComment(::CppSharp::CppParser::AST::VerbatimBlockLineComment* native);
                static VerbatimBlockLineComment^ __CreateInstance(::System::IntPtr native);
                static VerbatimBlockLineComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                VerbatimBlockLineComment();

                VerbatimBlockLineComment(CppSharp::Parser::AST::VerbatimBlockLineComment^ _0);

                ~VerbatimBlockLineComment();

                property System::String^ Text
                {
                    System::String^ get();
                    void set(System::String^);
                }
            };

            public ref class VerbatimBlockComment : CppSharp::Parser::AST::BlockCommandComment
            {
            public:

                VerbatimBlockComment(::CppSharp::CppParser::AST::VerbatimBlockComment* native);
                static VerbatimBlockComment^ __CreateInstance(::System::IntPtr native);
                static VerbatimBlockComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                VerbatimBlockComment();

                VerbatimBlockComment(CppSharp::Parser::AST::VerbatimBlockComment^ _0);

                ~VerbatimBlockComment();

                property unsigned int LinesCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::VerbatimBlockLineComment^ getLines(unsigned int i);

                void addLines(CppSharp::Parser::AST::VerbatimBlockLineComment^ s);

                void clearLines();
            };

            public ref class VerbatimLineComment : CppSharp::Parser::AST::BlockCommandComment
            {
            public:

                VerbatimLineComment(::CppSharp::CppParser::AST::VerbatimLineComment* native);
                static VerbatimLineComment^ __CreateInstance(::System::IntPtr native);
                static VerbatimLineComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                VerbatimLineComment();

                VerbatimLineComment(CppSharp::Parser::AST::VerbatimLineComment^ _0);

                ~VerbatimLineComment();

                property System::String^ Text
                {
                    System::String^ get();
                    void set(System::String^);
                }
            };

            public ref class InlineContentComment : CppSharp::Parser::AST::Comment
            {
            public:

                InlineContentComment(::CppSharp::CppParser::AST::InlineContentComment* native);
                static InlineContentComment^ __CreateInstance(::System::IntPtr native);
                static InlineContentComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                InlineContentComment();

                InlineContentComment(CppSharp::Parser::AST::CommentKind Kind);

                InlineContentComment(CppSharp::Parser::AST::InlineContentComment^ _0);

                ~InlineContentComment();
            };

            public ref class ParagraphComment : CppSharp::Parser::AST::BlockContentComment
            {
            public:

                ParagraphComment(::CppSharp::CppParser::AST::ParagraphComment* native);
                static ParagraphComment^ __CreateInstance(::System::IntPtr native);
                static ParagraphComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                ParagraphComment();

                ParagraphComment(CppSharp::Parser::AST::ParagraphComment^ _0);

                ~ParagraphComment();

                property unsigned int ContentCount
                {
                    unsigned int get();
                }

                property bool IsWhitespace
                {
                    bool get();
                    void set(bool);
                }

                CppSharp::Parser::AST::InlineContentComment^ getContent(unsigned int i);

                void addContent(CppSharp::Parser::AST::InlineContentComment^ s);

                void clearContent();
            };

            public ref class InlineCommandComment : CppSharp::Parser::AST::InlineContentComment
            {
            public:

                enum struct RenderKind
                {
                    RenderNormal = 0,
                    RenderBold = 1,
                    RenderMonospaced = 2,
                    RenderEmphasized = 3
                };

                ref class Argument : ICppInstance
                {
                public:

                    property ::CppSharp::CppParser::AST::InlineCommandComment::Argument* NativePtr;
                    property System::IntPtr __Instance
                    {
                        virtual System::IntPtr get();
                        virtual void set(System::IntPtr instance);
                    }

                    Argument(::CppSharp::CppParser::AST::InlineCommandComment::Argument* native);
                    static Argument^ __CreateInstance(::System::IntPtr native);
                    static Argument^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                    Argument();

                    Argument(CppSharp::Parser::AST::InlineCommandComment::Argument^ _0);

                    ~Argument();

                    property System::String^ Text
                    {
                        System::String^ get();
                        void set(System::String^);
                    }

                    protected:
                    bool __ownsNativeInstance;
                };

                InlineCommandComment(::CppSharp::CppParser::AST::InlineCommandComment* native);
                static InlineCommandComment^ __CreateInstance(::System::IntPtr native);
                static InlineCommandComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                InlineCommandComment();

                InlineCommandComment(CppSharp::Parser::AST::InlineCommandComment^ _0);

                ~InlineCommandComment();

                property unsigned int ArgumentsCount
                {
                    unsigned int get();
                }

                property CppSharp::Parser::AST::InlineCommandComment::RenderKind CommentRenderKind
                {
                    CppSharp::Parser::AST::InlineCommandComment::RenderKind get();
                    void set(CppSharp::Parser::AST::InlineCommandComment::RenderKind);
                }

                CppSharp::Parser::AST::InlineCommandComment::Argument^ getArguments(unsigned int i);

                void addArguments(CppSharp::Parser::AST::InlineCommandComment::Argument^ s);

                void clearArguments();
            };

            public ref class HTMLTagComment : CppSharp::Parser::AST::InlineContentComment
            {
            public:

                HTMLTagComment(::CppSharp::CppParser::AST::HTMLTagComment* native);
                static HTMLTagComment^ __CreateInstance(::System::IntPtr native);
                static HTMLTagComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                HTMLTagComment();

                HTMLTagComment(CppSharp::Parser::AST::CommentKind Kind);

                HTMLTagComment(CppSharp::Parser::AST::HTMLTagComment^ _0);

                ~HTMLTagComment();
            };

            public ref class HTMLStartTagComment : CppSharp::Parser::AST::HTMLTagComment
            {
            public:

                ref class Attribute : ICppInstance
                {
                public:

                    property ::CppSharp::CppParser::AST::HTMLStartTagComment::Attribute* NativePtr;
                    property System::IntPtr __Instance
                    {
                        virtual System::IntPtr get();
                        virtual void set(System::IntPtr instance);
                    }

                    Attribute(::CppSharp::CppParser::AST::HTMLStartTagComment::Attribute* native);
                    static Attribute^ __CreateInstance(::System::IntPtr native);
                    static Attribute^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                    Attribute();

                    Attribute(CppSharp::Parser::AST::HTMLStartTagComment::Attribute^ _0);

                    ~Attribute();

                    property System::String^ Name
                    {
                        System::String^ get();
                        void set(System::String^);
                    }

                    property System::String^ Value
                    {
                        System::String^ get();
                        void set(System::String^);
                    }

                    protected:
                    bool __ownsNativeInstance;
                };

                HTMLStartTagComment(::CppSharp::CppParser::AST::HTMLStartTagComment* native);
                static HTMLStartTagComment^ __CreateInstance(::System::IntPtr native);
                static HTMLStartTagComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                HTMLStartTagComment();

                HTMLStartTagComment(CppSharp::Parser::AST::HTMLStartTagComment^ _0);

                ~HTMLStartTagComment();

                property System::String^ TagName
                {
                    System::String^ get();
                    void set(System::String^);
                }

                property unsigned int AttributesCount
                {
                    unsigned int get();
                }

                CppSharp::Parser::AST::HTMLStartTagComment::Attribute^ getAttributes(unsigned int i);

                void addAttributes(CppSharp::Parser::AST::HTMLStartTagComment::Attribute^ s);

                void clearAttributes();
            };

            public ref class HTMLEndTagComment : CppSharp::Parser::AST::HTMLTagComment
            {
            public:

                HTMLEndTagComment(::CppSharp::CppParser::AST::HTMLEndTagComment* native);
                static HTMLEndTagComment^ __CreateInstance(::System::IntPtr native);
                static HTMLEndTagComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                HTMLEndTagComment();

                HTMLEndTagComment(CppSharp::Parser::AST::HTMLEndTagComment^ _0);

                ~HTMLEndTagComment();

                property System::String^ TagName
                {
                    System::String^ get();
                    void set(System::String^);
                }
            };

            public ref class TextComment : CppSharp::Parser::AST::InlineContentComment
            {
            public:

                TextComment(::CppSharp::CppParser::AST::TextComment* native);
                static TextComment^ __CreateInstance(::System::IntPtr native);
                static TextComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                TextComment();

                TextComment(CppSharp::Parser::AST::TextComment^ _0);

                ~TextComment();

                property System::String^ Text
                {
                    System::String^ get();
                    void set(System::String^);
                }
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
                static RawComment^ __CreateInstance(::System::IntPtr native);
                static RawComment^ __CreateInstance(::System::IntPtr native, bool __ownsNativeInstance);
                RawComment();

                RawComment(CppSharp::Parser::AST::RawComment^ _0);

                ~RawComment();

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

                property CppSharp::Parser::AST::RawCommentKind Kind
                {
                    CppSharp::Parser::AST::RawCommentKind get();
                    void set(CppSharp::Parser::AST::RawCommentKind);
                }

                property CppSharp::Parser::AST::FullComment^ FullCommentBlock
                {
                    CppSharp::Parser::AST::FullComment^ get();
                    void set(CppSharp::Parser::AST::FullComment^);
                }

                protected:
                bool __ownsNativeInstance;
            };
        }
    }
}
