/************************************************************************
 *
 * CppSharp
 * Licensed under the MIT license.
 *
 ************************************************************************/

#pragma once

#include "Helpers.h"
#include "Sources.h"
#include "Types.h"
#include <algorithm>

namespace CppSharp
{
    namespace CppParser
    {
        namespace AST
        {

            enum class DeclarationKind
            {
                DeclarationContext,
                Typedef,
                TypeAlias,
                Parameter,
                Function,
                Method,
                Enumeration,
                EnumerationItem,
                Variable,
                Field,
                AccessSpecifier,
                Class,
                Template,
                TypeAliasTemplate,
                ClassTemplate,
                ClassTemplateSpecialization,
                ClassTemplatePartialSpecialization,
                FunctionTemplate,
                Namespace,
                PreprocessedEntity,
                MacroDefinition,
                MacroExpansion,
                TranslationUnit,
                Friend,
                TemplateTemplateParm,
                TemplateTypeParm,
                NonTypeTemplateParm,
                VarTemplate,
                VarTemplateSpecialization,
                VarTemplatePartialSpecialization,
                UnresolvedUsingTypename,
            };

#define DECLARE_DECL_KIND(klass, kind) \
    klass();

            enum class AccessSpecifier
            {
                Private,
                Protected,
                Public
            };

            class DeclarationContext;
            class RawComment;
            class PreprocessedEntity;

            class ExpressionObsolete;
            class CS_API Declaration
            {
            public:
                Declaration(DeclarationKind kind);
                Declaration(const Declaration &);
                ~Declaration();

                DeclarationKind kind;
                int alignAs;
                int maxFieldAlignment;
                AccessSpecifier access;
                DeclarationContext *_namespace;
                SourceLocation location;
                int lineNumberStart;
                int lineNumberEnd;
                std::string name;
                std::string USR;
                std::string debugText;
                bool isIncomplete;
                bool isDependent;
                bool isImplicit;
                bool isInvalid;
                bool isDeprecated;
                Declaration *completeDeclaration;
                unsigned definitionOrder;
                VECTOR(PreprocessedEntity *, PreprocessedEntities)
                VECTOR(Declaration *, Redeclarations)
                void *originalPtr;
                RawComment *comment;
            };

            class Class;
            class Enumeration;
            class Function;
            class TypedefDecl;
            class TypeAlias;
            class Namespace;
            class Template;
            class TypeAliasTemplate;
            class ClassTemplate;
            class FunctionTemplate;
            class Variable;
            class Friend;

            class CS_API DeclarationContext : public Declaration
            {
            public:
                DeclarationContext(DeclarationKind kind);

                CS_IGNORE Declaration *FindAnonymous(const std::string &USR);

                CS_IGNORE Namespace *FindNamespace(const std::string &Name);
                CS_IGNORE Namespace *FindNamespace(const std::vector<std::string> &);
                CS_IGNORE Namespace *FindCreateNamespace(const std::string &Name);

                CS_IGNORE Class *CreateClass(const std::string &Name, bool IsComplete);
                CS_IGNORE Class *FindClass(const void *OriginalPtr, const std::string &Name, bool IsComplete);
                CS_IGNORE Class *FindClass(const void *OriginalPtr, const std::string &Name, bool IsComplete,
                                           bool Create);

                CS_IGNORE template <typename T>
                T *FindTemplate(const std::string &USR);

                CS_IGNORE Enumeration *FindEnum(const void *OriginalPtr);
                CS_IGNORE Enumeration *FindEnum(const std::string &Name, bool Create = false);
                CS_IGNORE Enumeration *FindEnumWithItem(const std::string &Name);

                CS_IGNORE Function *FindFunction(const std::string &USR);

                CS_IGNORE TypedefDecl *FindTypedef(const std::string &Name, bool Create = false);

                CS_IGNORE TypeAlias *FindTypeAlias(const std::string &Name, bool Create = false);

                CS_IGNORE Variable *FindVariable(const std::string &USR);

                CS_IGNORE Friend *FindFriend(const std::string &USR);

                VECTOR(Namespace *, Namespaces)
                VECTOR(Enumeration *, Enums)
                VECTOR(Function *, Functions)
                VECTOR(Class *, Classes)
                VECTOR(Template *, Templates)
                VECTOR(TypedefDecl *, Typedefs)
                VECTOR(TypeAlias *, TypeAliases)
                VECTOR(Variable *, Variables)
                VECTOR(Friend *, Friends)

                std::map<std::string, Declaration *> anonymous;

                bool isAnonymous;
            };

            class CS_API TypedefNameDecl : public Declaration
            {
            public:
                TypedefNameDecl(DeclarationKind kind);
                ~TypedefNameDecl();
                QualifiedType qualifiedType;
            };

            class CS_API TypedefDecl : public TypedefNameDecl
            {
            public:
                DECLARE_DECL_KIND(TypedefDecl, Typedef)
                ~TypedefDecl();
            };

            class CS_API TypeAlias : public TypedefNameDecl
            {
            public:
                TypeAlias();
                ~TypeAlias();
                TypeAliasTemplate *describedAliasTemplate;
            };

            class CS_API Friend : public Declaration
            {
            public:
                DECLARE_DECL_KIND(Friend, Friend)
                ~Friend();
                Declaration *declaration;
            };

            enum class StatementClassObsolete
            {
                Any,
                BinaryOperator,
                CallExprClass,
                DeclRefExprClass,
                CXXConstructExprClass,
                CXXOperatorCallExpr,
                ImplicitCastExpr,
                ExplicitCastExpr,
            };

            class CS_API StatementObsolete
            {
            public:
                StatementObsolete(const std::string &str, StatementClassObsolete Class = StatementClassObsolete::Any, Declaration *decl = 0);
                StatementClassObsolete _class;
                Declaration *decl;
                std::string string;
            };

            class CS_API ExpressionObsolete : public StatementObsolete
            {
            public:
                ExpressionObsolete(const std::string &str, StatementClassObsolete Class = StatementClassObsolete::Any, Declaration *decl = 0);
            };

            class Expr;

            class CS_API BinaryOperatorObsolete : public ExpressionObsolete
            {
            public:
                BinaryOperatorObsolete(const std::string &str, ExpressionObsolete *lhs, ExpressionObsolete *rhs, const std::string &opcodeStr);
                ~BinaryOperatorObsolete();
                ExpressionObsolete *LHS;
                ExpressionObsolete *RHS;
                std::string opcodeStr;
            };

            class CS_API CallExprObsolete : public ExpressionObsolete
            {
            public:
                CallExprObsolete(const std::string &str, Declaration *decl);
                ~CallExprObsolete();
                VECTOR(ExpressionObsolete *, Arguments)
            };

            class CS_API CXXConstructExprObsolete : public ExpressionObsolete
            {
            public:
                CXXConstructExprObsolete(const std::string &str, Declaration *decl = 0);
                ~CXXConstructExprObsolete();
                VECTOR(ExpressionObsolete *, Arguments)
            };

            class CS_API Parameter : public Declaration
            {
            public:
                Parameter();
                ~Parameter();

                QualifiedType qualifiedType;
                bool isIndirect;
                bool hasDefaultValue;
                unsigned int index;
                ExpressionObsolete *defaultArgument;
                Expr *defaultValue;
            };

            enum class CXXMethodKind
            {
                Normal,
                Constructor,
                Destructor,
                Conversion,
                Operator,
                UsingDirective
            };

            enum class CXXOperatorKind
            {
                None,
                New,
                Delete,
                Array_New,
                Array_Delete,
                Plus,
                Minus,
                Star,
                Slash,
                Percent,
                Caret,
                Amp,
                Pipe,
                Tilde,
                Exclaim,
                Equal,
                Less,
                Greater,
                PlusEqual,
                MinusEqual,
                StarEqual,
                SlashEqual,
                PercentEqual,
                CaretEqual,
                AmpEqual,
                PipeEqual,
                LessLess,
                GreaterGreater,
                LessLessEqual,
                GreaterGreaterEqual,
                EqualEqual,
                ExclaimEqual,
                LessEqual,
                GreaterEqual,
                Spaceship,
                AmpAmp,
                PipePipe,
                PlusPlus,
                MinusMinus,
                Comma,
                ArrowStar,
                Arrow,
                Call,
                Subscript,
                Conditional,
                Coawait
            };

            class FunctionTemplateSpecialization;

            enum class FriendKind
            {
                None,
                Declared,
                Undeclared
            };

            class Stmt;

            class CS_API Function : public DeclarationContext
            {
            public:
                Function();
                ~Function();

                QualifiedType returnType;
                bool isReturnIndirect;
                bool hasThisReturn;

                bool isConstExpr;
                bool isVariadic;
                bool isInline;
                bool isPure;
                bool isDeleted;
                bool isDefaulted;
                FriendKind friendKind;
                CXXOperatorKind operatorKind;
                std::string mangled;
                std::string signature;
                std::string body;
                Stmt *bodyStmt;
                CallingConvention callingConvention;
                VECTOR(Parameter *, Parameters)
                FunctionTemplateSpecialization *specializationInfo;
                Function *instantiatedFrom;
                QualifiedType qualifiedType;
            };

            class AccessSpecifierDecl;

            enum class RefQualifierKind
            {
                None,
                LValue,
                RValue
            };

            class CS_API Method : public Function
            {
            public:
                Method();
                ~Method();

                bool isVirtual;
                bool isStatic;
                bool isConst;
                bool isExplicit;
                bool isVolatile;

                CXXMethodKind methodKind;

                bool isDefaultConstructor;
                bool isCopyConstructor;
                bool isMoveConstructor;

                QualifiedType conversionType;
                RefQualifierKind refQualifier;
                VECTOR(Method *, OverriddenMethods)
            };

            class CS_API Enumeration : public DeclarationContext
            {
            public:
                DECLARE_DECL_KIND(Enumeration, Enumeration)
                ~Enumeration();

                class CS_API Item : public Declaration
                {
                public:
                    DECLARE_DECL_KIND(Item, EnumerationItem)
                    Item(const Item &);
                    ~Item();
                    std::string expression;
                    uint64_t value;
                };

                enum class CS_FLAGS EnumModifiers
                {
                    Anonymous = 1 << 0,
                    Scoped = 1 << 1,
                    Flags = 1 << 2,
                };

                EnumModifiers modifiers;
                Type *type;
                BuiltinType *builtinType;
                VECTOR(Item *, Items)

                Item *FindItemByName(const std::string &Name);
            };

            class CS_API Variable : public Declaration
            {
            public:
                DECLARE_DECL_KIND(Variable, Variable)
                ~Variable();
                bool isConstExpr;
                std::string mangled;
                QualifiedType qualifiedType;
                ExpressionObsolete *initializer;
            };

            class PreprocessedEntity;

            struct CS_API BaseClassSpecifier
            {
                BaseClassSpecifier();
                AccessSpecifier access;
                bool isVirtual;
                Type *type;
                int offset;
            };

            class Class;

            class CS_API Field : public Declaration
            {
            public:
                DECLARE_DECL_KIND(Field, Field)
                ~Field();
                QualifiedType qualifiedType;
                Class *_class;
                bool isBitField;
                unsigned bitWidth;
            };

            class CS_API AccessSpecifierDecl : public Declaration
            {
            public:
                DECLARE_DECL_KIND(AccessSpecifierDecl, AccessSpecifier)
                ~AccessSpecifierDecl();
            };

            enum class CppAbi
            {
                Itanium,
                Microsoft,
                ARM,
                iOS,
                iOS64,
                WebAssembly
            };

            enum class VTableComponentKind
            {
                VCallOffset,
                VBaseOffset,
                OffsetToTop,
                RTTI,
                FunctionPointer,
                CompleteDtorPointer,
                DeletingDtorPointer,
                UnusedFunctionPointer,
            };

            struct CS_API VTableComponent
            {
                VTableComponent();
                VTableComponentKind kind;
                unsigned offset;
                Declaration *declaration;
            };

            struct CS_API VTableLayout
            {
                VTableLayout();
                VTableLayout(const VTableLayout &);
                ~VTableLayout();
                VECTOR(VTableComponent, Components)
            };

            struct CS_API VFTableInfo
            {
                VFTableInfo();
                VFTableInfo(const VFTableInfo &);
                uint64_t VBTableIndex;
                uint32_t VFPtrOffset;
                uint32_t VFPtrFullOffset;
                VTableLayout layout;
            };

            class CS_API LayoutField
            {
            public:
                LayoutField();
                LayoutField(const LayoutField &other);
                ~LayoutField();
                unsigned offset;
                std::string name;
                QualifiedType qualifiedType;
                void *fieldPtr;
            };

            class Class;

            class CS_API LayoutBase
            {
            public:
                LayoutBase();
                LayoutBase(const LayoutBase &other);
                ~LayoutBase();
                unsigned offset;
                Class *_class;
            };

            enum class RecordArgABI
            {
                /// Pass it using the normal C aggregate rules for the ABI,
                /// potentially introducing extra copies and passing some
                /// or all of it in registers.
                Default = 0,
                /// Pass it on the stack using its defined layout.
                /// The argument must be evaluated directly into the correct
                /// stack position in the arguments area, and the call machinery
                /// must not move it or introduce extra copies.
                DirectInMemory,
                /// Pass it as a pointer to temporary memory.
                Indirect
            };

            struct CS_API ClassLayout
            {
                ClassLayout();
                CppAbi ABI;
                RecordArgABI argABI;
                VECTOR(VFTableInfo, VFTables)
                VTableLayout layout;
                bool hasOwnVFPtr;
                long VBPtrOffset;
                int alignment;
                int size;
                int dataSize;
                VECTOR(LayoutField, Fields)
                VECTOR(LayoutBase, Bases)
            };

            enum class TagKind
            {
                Struct,
                Interface,
                Union,
                Class,
                Enum
            };

            class CS_API Class : public DeclarationContext
            {
            public:
                Class();
                ~Class();

                VECTOR(BaseClassSpecifier *, Bases)
                VECTOR(Field *, Fields)
                VECTOR(Method *, Methods)
                VECTOR(AccessSpecifierDecl *, Specifiers)

                bool isPOD;
                bool isAbstract;
                bool isUnion;
                bool isDynamic;
                bool isPolymorphic;
                bool hasNonTrivialDefaultConstructor;
                bool hasNonTrivialCopyConstructor;
                bool hasNonTrivialDestructor;
                bool isExternCContext;
                bool isInjected;
                TagKind tagKind;

                ClassLayout *layout;
            };

            class CS_API Template : public Declaration
            {
            public:
                Template(DeclarationKind kind);
                DECLARE_DECL_KIND(Template, Template)
                Declaration *TemplatedDecl;
                VECTOR(Declaration *, Parameters)
            };

            template <typename T>
            T *DeclarationContext::FindTemplate(const std::string &USR)
            {
                auto foundTemplate = std::find_if(Templates.begin(), Templates.end(),
                                                  [&](Template *t)
                                                  { return t->USR == USR; });

                if (foundTemplate != Templates.end())
                    return static_cast<T *>(*foundTemplate);

                return nullptr;
            }

            class CS_API TypeAliasTemplate : public Template
            {
            public:
                TypeAliasTemplate();
                ~TypeAliasTemplate();
            };

            class CS_API TemplateParameter : public Declaration
            {
            public:
                TemplateParameter(DeclarationKind kind);
                ~TemplateParameter();
                unsigned int depth;
                unsigned int index;
                bool isParameterPack;
            };

            class CS_API TemplateTemplateParameter : public Template
            {
            public:
                TemplateTemplateParameter();
                ~TemplateTemplateParameter();

                bool isParameterPack;
                bool isPackExpansion;
                bool isExpandedParameterPack;
            };

            class CS_API TypeTemplateParameter : public TemplateParameter
            {
            public:
                TypeTemplateParameter();
                TypeTemplateParameter(const TypeTemplateParameter &);
                ~TypeTemplateParameter();

                QualifiedType defaultArgument;
            };

            class CS_API NonTypeTemplateParameter : public TemplateParameter
            {
            public:
                NonTypeTemplateParameter();
                NonTypeTemplateParameter(const NonTypeTemplateParameter &);
                ~NonTypeTemplateParameter();
                ExpressionObsolete *defaultArgument;
                Expr *defaultArgumentNew;
                unsigned int position;
                bool isPackExpansion;
                bool isExpandedParameterPack;
                QualifiedType type;
            };

            class ClassTemplateSpecialization;
            class ClassTemplatePartialSpecialization;

            class CS_API ClassTemplate : public Template
            {
            public:
                ClassTemplate();
                ~ClassTemplate();
                VECTOR(ClassTemplateSpecialization *, Specializations)
                ClassTemplateSpecialization *FindSpecialization(const std::string &usr);
                ClassTemplatePartialSpecialization *FindPartialSpecialization(const std::string &usr);
            };

            enum class TemplateSpecializationKind
            {
                Undeclared,
                ImplicitInstantiation,
                ExplicitSpecialization,
                ExplicitInstantiationDeclaration,
                ExplicitInstantiationDefinition
            };

            class CS_API ClassTemplateSpecialization : public Class
            {
            public:
                ClassTemplateSpecialization();
                ~ClassTemplateSpecialization();
                ClassTemplate *templatedDecl;
                VECTOR(TemplateArgument, Arguments)
                TemplateSpecializationKind specializationKind;
            };

            class CS_API ClassTemplatePartialSpecialization : public ClassTemplateSpecialization
            {
            public:
                ClassTemplatePartialSpecialization();
                ~ClassTemplatePartialSpecialization();
                VECTOR(Declaration *, Parameters)
            };

            class CS_API FunctionTemplate : public Template
            {
            public:
                FunctionTemplate();
                ~FunctionTemplate();
                VECTOR(FunctionTemplateSpecialization *, Specializations)
                FunctionTemplateSpecialization *FindSpecialization(const std::string &usr);
            };

            class CS_API FunctionTemplateSpecialization
            {
            public:
                FunctionTemplateSpecialization();
                ~FunctionTemplateSpecialization();
                FunctionTemplate *_template;
                VECTOR(TemplateArgument, Arguments)
                Function *specializedFunction;
                TemplateSpecializationKind specializationKind;
            };

            class VarTemplateSpecialization;
            class VarTemplatePartialSpecialization;

            class CS_API VarTemplate : public Template
            {
            public:
                VarTemplate();
                ~VarTemplate();
                VECTOR(VarTemplateSpecialization *, Specializations)
                VarTemplateSpecialization *FindSpecialization(const std::string &usr);
                VarTemplatePartialSpecialization *FindPartialSpecialization(const std::string &usr);
            };

            class CS_API VarTemplateSpecialization : public Variable
            {
            public:
                VarTemplateSpecialization();
                ~VarTemplateSpecialization();
                VarTemplate *templatedDecl;
                VECTOR(TemplateArgument, Arguments)
                TemplateSpecializationKind specializationKind;
            };

            class CS_API VarTemplatePartialSpecialization : public VarTemplateSpecialization
            {
            public:
                VarTemplatePartialSpecialization();
                ~VarTemplatePartialSpecialization();
            };

            class CS_API UnresolvedUsingTypename : public Declaration
            {
            public:
                UnresolvedUsingTypename();
                ~UnresolvedUsingTypename();
            };

            class CS_API Namespace : public DeclarationContext
            {
            public:
                Namespace();
                ~Namespace();
                bool isInline;
            };

            enum class MacroLocation
            {
                Unknown,
                ClassHead,
                ClassBody,
                FunctionHead,
                FunctionParameters,
                FunctionBody,
            };

            class CS_API PreprocessedEntity
            {
            public:
                PreprocessedEntity();
                MacroLocation macroLocation;
                void *originalPtr;
                DeclarationKind kind;
            };

            class CS_API MacroDefinition : public PreprocessedEntity
            {
            public:
                MacroDefinition();
                ~MacroDefinition();
                std::string name;
                std::string expression;
                int lineNumberStart;
                int lineNumberEnd;
            };

            class CS_API MacroExpansion : public PreprocessedEntity
            {
            public:
                MacroExpansion();
                ~MacroExpansion();
                std::string name;
                std::string text;
                MacroDefinition *definition;
            };

            class CS_API TranslationUnit : public Namespace
            {
            public:
                TranslationUnit();
                ~TranslationUnit();
                std::string fileName;
                bool isSystemHeader;
                VECTOR(MacroDefinition *, Macros)
            };

            class CS_API ASTContext
            {
            public:
                ASTContext();
                ~ASTContext();
                TranslationUnit *FindOrCreateModule(std::string File);
                VECTOR(TranslationUnit *, TranslationUnits)
            };

        }
    }
}