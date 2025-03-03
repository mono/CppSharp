/************************************************************************
 *
 * CppSharp
 * Licensed under the simplified BSD license. All rights reserved.
 *
 ************************************************************************/

#pragma once
#include <llvm/Support/JSON.h>

#include "ASTNameMangler.h"
#include "Types.h"
#include "clang/AST/ASTContext.h"
#include "clang/AST/ASTDumperUtils.h"
#include "clang/AST/ASTNodeTraverser.h"
#include "clang/AST/RecursiveASTVisitor.h"
#include "clang/AST/AttrVisitor.h"
#include "clang/AST/CommentCommandTraits.h"
#include "clang/AST/CommentVisitor.h"
#include "clang/AST/ExprConcepts.h"
#include "clang/AST/DeclCXX.h"
#include "clang/AST/ExprCXX.h"
#include "clang/AST/Type.h"

#include <unordered_set>

#include "Decl.h"
#include "Expr.h"
#include "Stmt.h"

namespace CppSharp::CppParser {
namespace AST {
    class Stmt;
} // namespace AST
class Parser;

class NodeStreamer
{
    bool FirstChild = true;
    bool TopLevel = true;
    llvm::SmallVector<std::function<void(bool IsLastChild)>, 32> Pending;

protected:
    llvm::json::OStream JOS;

public:
    /// Add a child of the current node.  Calls DoAddChild without arguments
    template <typename Fn>
    void AddChild(Fn DoAddChild)
    {
        return AddChild("", DoAddChild);
    }

    /// Add a child of the current node with an optional label.
    /// Calls DoAddChild without arguments.
    template <typename Fn>
    void AddChild(llvm::StringRef Label, Fn DoAddChild)
    {
        // If we're at the top level, there's nothing interesting to do; just
        // run the dumper.
        if (TopLevel)
        {
            TopLevel = false;
            JOS.objectBegin();

            DoAddChild();

            while (!Pending.empty())
            {
                Pending.back()(true);
                Pending.pop_back();
            }

            JOS.objectEnd();
            TopLevel = true;
            return;
        }

        // We need to capture an owning-string in the lambda because the lambda
        // is invoked in a deferred manner.
        std::string LabelStr(!Label.empty() ? Label : "inner");
        bool WasFirstChild = FirstChild;
        auto DumpWithIndent = [=](bool IsLastChild)
        {
            if (WasFirstChild)
            {
                JOS.attributeBegin(LabelStr);
                JOS.arrayBegin();
            }

            FirstChild = true;
            unsigned Depth = Pending.size();
            JOS.objectBegin();

            DoAddChild();

            // If any children are left, they're the last at their nesting level.
            // Dump those ones out now.
            while (Depth < Pending.size())
            {
                Pending.back()(true);
                this->Pending.pop_back();
            }

            JOS.objectEnd();

            if (IsLastChild)
            {
                JOS.arrayEnd();
                JOS.attributeEnd();
            }
        };

        if (FirstChild)
        {
            Pending.push_back(std::move(DumpWithIndent));
        }
        else
        {
            Pending.back()(false);
            Pending.back() = std::move(DumpWithIndent);
        }
        FirstChild = false;
    }

    NodeStreamer(llvm::raw_ostream& OS)
        : JOS(OS, 2)
    {
    }
};

// Dumps AST nodes in JSON format. There is no implied stability for the
// content or format of the dump between major releases of Clang, other than it
// being valid JSON output. Further, there is no requirement that the
// information dumped is a complete representation of the AST, only that the
// information presented is correct.
class ASTNodeVisitor
    : public clang::ConstAttrVisitor<ASTNodeVisitor>,
      public clang::comments::ConstCommentVisitor<ASTNodeVisitor, void, const clang::comments::FullComment*>,
      public clang::ConstTemplateArgumentVisitor<ASTNodeVisitor>,
      public clang::ConstStmtVisitor<ASTNodeVisitor>,
      public clang::TypeVisitor<ASTNodeVisitor>,
      public clang::ConstDeclVisitor<ASTNodeVisitor, AST::Declaration*>,
      public NodeStreamer
{
    friend class ASTNodeDumper;

    using InnerAttrVisitor = ConstAttrVisitor;
    using InnerCommentVisitor = ConstCommentVisitor;
    using InnerTemplateArgVisitor = ConstTemplateArgumentVisitor;
    using InnerStmtVisitor = ConstStmtVisitor;
    using InnerTypeVisitor = TypeVisitor;
    using InnerDeclVisitor = ConstDeclVisitor;

public:
    ASTNodeVisitor(llvm::raw_ostream& OS, clang::ASTContext& Ctx, Parser& parser)
        : NodeStreamer(OS)
        , parser(parser)
        , SM(Ctx.getSourceManager())
        , Ctx(Ctx)
        , NameMangler(Ctx)
        , PrintPolicy(Ctx.getPrintingPolicy())
        , Traits(Ctx.getCommentCommandTraits())
        , LastLocLine(0)
        , LastLocPresumedLine(0)
    {
        declMap.reserve(32768);
        typeMap.reserve(32768);
        stmtMap.reserve(32768);
    }

    void Visit(const clang::Attr* A);
    void Visit(const clang::Stmt* S);
    void Visit(const clang::Type* T);
    void Visit(clang::QualType T);
    AST::Declaration* Visit(const clang::Decl* D);
    void Visit(clang::TypeLoc TL);

    void Visit(const clang::comments::Comment* C, const clang::comments::FullComment* FC);
    void Visit(const clang::TemplateArgument& TA, clang::SourceRange R = {}, const clang::Decl* From = nullptr, llvm::StringRef Label = {});
    void Visit(const clang::CXXCtorInitializer* Init);
    // void Visit(const OpenACCClause* C) {}
    void Visit(const clang::OMPClause* C) {}
    void Visit(const clang::BlockDecl::Capture& C);
    void Visit(const clang::GenericSelectionExpr::ConstAssociation& A);
    void Visit(const clang::concepts::Requirement* R);
    void Visit(const clang::APValue& Value, clang::QualType Ty);
    void Visit(const clang::ConceptReference*);

    void VisitAliasAttr(const clang::AliasAttr* AA);
    void VisitCleanupAttr(const clang::CleanupAttr* CA);
    void VisitDeprecatedAttr(const clang::DeprecatedAttr* DA);
    void VisitUnavailableAttr(const clang::UnavailableAttr* UA);
    void VisitSectionAttr(const clang::SectionAttr* SA);
    void VisitVisibilityAttr(const clang::VisibilityAttr* VA);
    void VisitTLSModelAttr(const clang::TLSModelAttr* TA);

    void VisitTypedefType(const clang::TypedefType* TT);
    void VisitUsingType(const clang::UsingType* TT);
    void VisitFunctionType(const clang::FunctionType* T);
    void VisitFunctionProtoType(const clang::FunctionProtoType* T);
    void VisitRValueReferenceType(const clang::ReferenceType* RT);
    void VisitArrayType(const clang::ArrayType* AT);
    void VisitConstantArrayType(const clang::ConstantArrayType* CAT);
    void VisitDependentSizedExtVectorType(const clang::DependentSizedExtVectorType* VT);
    void VisitVectorType(const clang::VectorType* VT);
    void VisitUnresolvedUsingType(const clang::UnresolvedUsingType* UUT);
    void VisitUnaryTransformType(const clang::UnaryTransformType* UTT);
    void VisitTagType(const clang::TagType* TT);
    void VisitTemplateTypeParmType(const clang::TemplateTypeParmType* TTPT);
    void VisitSubstTemplateTypeParmType(const clang::SubstTemplateTypeParmType* STTPT);
    void VisitSubstTemplateTypeParmPackType(const clang::SubstTemplateTypeParmPackType* T);
    void VisitAutoType(const clang::AutoType* AT);
    void VisitTemplateSpecializationType(const clang::TemplateSpecializationType* TST);
    void VisitInjectedClassNameType(const clang::InjectedClassNameType* ICNT);
    void VisitObjCInterfaceType(const clang::ObjCInterfaceType* OIT);
    void VisitPackExpansionType(const clang::PackExpansionType* PET);
    void VisitElaboratedType(const clang::ElaboratedType* ET);
    void VisitMacroQualifiedType(const clang::MacroQualifiedType* MQT);
    void VisitMemberPointerType(const clang::MemberPointerType* MPT);

    AST::Declaration* VisitTranslationUnitDecl(const clang::TranslationUnitDecl* D);
    AST::Declaration* VisitNamedDecl(const clang::NamedDecl* ND);
    void HandleNamedDecl(AST::Declaration& AST_ND, const clang::NamedDecl* ND);
    AST::Declaration* VisitTypedefDecl(const clang::TypedefDecl* TD);
    AST::Declaration* VisitTypeAliasDecl(const clang::TypeAliasDecl* TAD);
    AST::Declaration* VisitNamespaceDecl(const clang::NamespaceDecl* ND);
    AST::Declaration* VisitUsingDirectiveDecl(const clang::UsingDirectiveDecl* UDD);
    AST::Declaration* VisitNamespaceAliasDecl(const clang::NamespaceAliasDecl* NAD);
    AST::Declaration* VisitUsingDecl(const clang::UsingDecl* UD);
    AST::Declaration* VisitUsingEnumDecl(const clang::UsingEnumDecl* UED);
    AST::Declaration* VisitUsingShadowDecl(const clang::UsingShadowDecl* USD);
    AST::Declaration* VisitVarDecl(const clang::VarDecl* VD);
    AST::Declaration* VisitFieldDecl(const clang::FieldDecl* FD);
    AST::Declaration* VisitFunctionDecl(const clang::FunctionDecl* FD);
    AST::Declaration* VisitEnumDecl(const clang::EnumDecl* ED);
    AST::Declaration* VisitEnumConstantDecl(const clang::EnumConstantDecl* ECD);
    AST::Declaration* VisitRecordDecl(const clang::RecordDecl* RD);
    AST::Declaration* VisitCXXRecordDecl(const clang::CXXRecordDecl* RD);
    AST::Declaration* VisitHLSLBufferDecl(const clang::HLSLBufferDecl* D);
    AST::Declaration* VisitTemplateTypeParmDecl(const clang::TemplateTypeParmDecl* D);
    AST::Declaration* VisitNonTypeTemplateParmDecl(const clang::NonTypeTemplateParmDecl* D);
    AST::Declaration* VisitTemplateTemplateParmDecl(const clang::TemplateTemplateParmDecl* D);
    AST::Declaration* VisitLinkageSpecDecl(const clang::LinkageSpecDecl* LSD);
    AST::Declaration* VisitAccessSpecDecl(const clang::AccessSpecDecl* ASD);
    AST::Declaration* VisitFriendDecl(const clang::FriendDecl* FD);

    AST::Declaration* VisitObjCIvarDecl(const clang::ObjCIvarDecl* D);
    AST::Declaration* VisitObjCMethodDecl(const clang::ObjCMethodDecl* D);
    AST::Declaration* VisitObjCTypeParamDecl(const clang::ObjCTypeParamDecl* D);
    AST::Declaration* VisitObjCCategoryDecl(const clang::ObjCCategoryDecl* D);
    AST::Declaration* VisitObjCCategoryImplDecl(const clang::ObjCCategoryImplDecl* D);
    AST::Declaration* VisitObjCProtocolDecl(const clang::ObjCProtocolDecl* D);
    AST::Declaration* VisitObjCInterfaceDecl(const clang::ObjCInterfaceDecl* D);
    AST::Declaration* VisitObjCImplementationDecl(const clang::ObjCImplementationDecl* D);
    AST::Declaration* VisitObjCCompatibleAliasDecl(const clang::ObjCCompatibleAliasDecl* D);
    AST::Declaration* VisitObjCPropertyDecl(const clang::ObjCPropertyDecl* D);
    AST::Declaration* VisitObjCPropertyImplDecl(const clang::ObjCPropertyImplDecl* D);
    AST::Declaration* VisitBlockDecl(const clang::BlockDecl* D);

    /*#define DECL(CLASS, BASE)                                                                \
        void Convert##CLASS##DeclImpl(const clang::CLASS##Decl* Src, AST::CLASS##Decl& Dst); \
        void Convert##CLASS##Decl(const clang::CLASS##Decl* Src, AST::CLASS##Decl& Dst)      \
        {                                                                                    \
            Convert##BASE##Decl(Src, Dst);                                                   \
            Convert##CLASS##DeclImpl(Src, Dst);                                              \
        }
    #include "clang/AST/DeclNodes.inc"

    // Declare Visit*() for all concrete Decl classes.
    #define ABSTRACT_DECL(DECL)
    #define DECL(CLASS, BASE)                                      \
        void Visit##CLASS##Decl(const CLASS##Decl* D)              \
        {                                                          \
            if (declMap.find(D) != declMap.end())                  \
                return;                                            \
                                                                   \
            auto res = declMap.emplace(D, new AST::CLASS##Decl()); \
            Convert##CLASS##Decl(D, res.first->second);            \
        }
    #include "clang/AST/DeclNodes.inc"
            // The above header #undefs ABSTRACT_DECL and DECL upon exit.*/


    void ConvertStmt(const clang::Stmt* Src, AST::Stmt& Dst);

#define STMT(CLASS, BASE)                                                \
    void Convert##CLASS##Impl(const clang::CLASS* Src, AST::CLASS& Dst); \
    void Convert##CLASS(const clang::CLASS* Src, AST::CLASS& Dst)        \
    {                                                                    \
        Convert##BASE(Src, Dst);                                         \
        Convert##CLASS##Impl(Src, Dst);                                  \
    }
#include "StmtNodes.inc"

// Declare Visit*() for all concrete Stmt classes.
#define ABSTRACT_STMT(STMT)
#define STMT(CLASS, BASE)                                                \
    void Visit##CLASS(const clang::CLASS* S)                             \
    {                                                                    \
        if (!S || stmtMap.find(S) != stmtMap.end())                      \
            return;                                                      \
                                                                         \
        auto res = stmtMap.emplace(S, new AST::CLASS());                 \
        Convert##CLASS(S, static_cast<AST::CLASS&>(*res.first->second)); \
    }
#include "StmtNodes.inc"
    // The above header #undefs ABSTRACT_STMT and STMT upon exit.

    void VisitNullTemplateArgument(const clang::TemplateArgument& TA);
    void VisitTypeTemplateArgument(const clang::TemplateArgument& TA);
    void VisitDeclarationTemplateArgument(const clang::TemplateArgument& TA);
    void VisitNullPtrTemplateArgument(const clang::TemplateArgument& TA);
    void VisitIntegralTemplateArgument(const clang::TemplateArgument& TA);
    void VisitTemplateTemplateArgument(const clang::TemplateArgument& TA);
    void VisitTemplateExpansionTemplateArgument(const clang::TemplateArgument& TA);
    void VisitExpressionTemplateArgument(const clang::TemplateArgument& TA);
    void VisitPackTemplateArgument(const clang::TemplateArgument& TA);

    void visitTextComment(const clang::comments::TextComment* C, const clang::comments::FullComment*);
    void visitInlineCommandComment(const clang::comments::InlineCommandComment* C, const clang::comments::FullComment*);
    void visitHTMLStartTagComment(const clang::comments::HTMLStartTagComment* C, const clang::comments::FullComment*);
    void visitHTMLEndTagComment(const clang::comments::HTMLEndTagComment* C, const clang::comments::FullComment*);
    void visitBlockCommandComment(const clang::comments::BlockCommandComment* C, const clang::comments::FullComment*);
    void visitParamCommandComment(const clang::comments::ParamCommandComment* C, const clang::comments::FullComment* FC);
    void visitTParamCommandComment(const clang::comments::TParamCommandComment* C, const clang::comments::FullComment* FC);
    void visitVerbatimBlockComment(const clang::comments::VerbatimBlockComment* C, const clang::comments::FullComment*);
    void visitVerbatimBlockLineComment(const clang::comments::VerbatimBlockLineComment* C, const clang::comments::FullComment*);
    void visitVerbatimLineComment(const clang::comments::VerbatimLineComment* C, const clang::comments::FullComment*);

private:
    void attributeOnlyIfTrue(llvm::StringRef Key, bool Value)
    {
        if (Value)
            JOS.attribute(Key, Value);
    }

    void writeIncludeStack(clang::PresumedLoc Loc, bool JustFirst = false);

    // Writes the attributes of a SourceLocation object without.
    void writeBareSourceLocation(clang::SourceLocation Loc, bool IsSpelling, bool addFileInfo);

    // Writes the attributes of a SourceLocation to JSON based on its presumed
    // spelling location. If the given location represents a macro invocation,
    // this outputs two sub-objects: one for the spelling and one for the
    // expansion location.
    void writeSourceLocation(clang::SourceLocation Loc, bool addFileInfo = true);
    void writeSourceRange(clang::SourceRange R);
    std::string createPointerRepresentation(const void* Ptr);
    llvm::json::Object createQualType(clang::QualType QT, bool Desugar = true);
    AST::QualifiedType CreateQualifiedType(clang::QualType QT, bool Desugar = true);
    llvm::json::Object createBareDeclRef(const clang::Decl* D);
    llvm::json::Object createFPOptions(clang::FPOptionsOverride FPO);
    void writeBareDeclRef(const clang::Decl* D);
    llvm::json::Object createCXXRecordDefinitionData(const clang::CXXRecordDecl* RD);
    llvm::json::Object createCXXBaseSpecifier(const clang::CXXBaseSpecifier& BS);
    std::string createAccessSpecifier(clang::AccessSpecifier AS);
    llvm::json::Array createCastPath(const clang::CastExpr* C);

    void writePreviousDeclImpl(...) {}

    template <typename T>
    void writePreviousDeclImpl(const clang::Mergeable<T>* D)
    {
        const T* First = D->getFirstDecl();
        if (First != D)
            JOS.attribute("firstRedecl", createPointerRepresentation(First));
    }

    template <typename T>
    void writePreviousDeclImpl(const clang::Redeclarable<T>* D)
    {
        const T* Prev = D->getPreviousDecl();
        if (Prev)
            JOS.attribute("previousDecl", createPointerRepresentation(Prev));
    }

    [[nodiscard]] std::string GetMangledName(const clang::NamedDecl& ND) const;
    void ConvertNamedRecord(AST::Declaration& dst, const clang::NamedDecl& src) const;

    void addPreviousDeclaration(const clang::Decl* D);

    llvm::StringRef getCommentCommandName(unsigned CommandID) const;

    template <typename Fn>
    AST::Declaration* FindOrInsertLazy(const clang::Decl* D, Fn&& inserter)
    {
        if (auto it = declMap.find(D); it != declMap.end())
            return it->second;

        auto res = declMap.emplace(D, std::invoke(inserter));
        return res.first->second;
    }

    std::unordered_map<const clang::Decl*, AST::Declaration*> declMap;
    std::unordered_set<const clang::Type*> typeMap;
    std::unordered_map<const clang::Stmt*, AST::Stmt*> stmtMap;

    Parser& parser;
    const clang::SourceManager& SM;
    clang::ASTContext& Ctx;
    ASTNameMangler NameMangler; // TODO: Remove this, or the parser one
    clang::PrintingPolicy PrintPolicy;
    const clang::comments::CommandTraits& Traits;
    llvm::StringRef LastLocFilename, LastLocPresumedFilename;
    unsigned LastLocLine, LastLocPresumedLine;
};

class ASTNodeDumper : public clang::ASTNodeTraverser<ASTNodeDumper, ASTNodeVisitor>
{
public:
    ASTNodeDumper(llvm::raw_ostream& OS, clang::ASTContext& Ctx, Parser& parser)
        : NodeVisitor(OS, Ctx, parser)
    {
        setDeserialize(true);
    }

    ASTNodeVisitor& doGetNodeDelegate() { return NodeVisitor; }

    void VisitFunctionTemplateDecl(const clang::FunctionTemplateDecl* FTD)
    {
        writeTemplateDecl(FTD, true);
    }
    void VisitClassTemplateDecl(const clang::ClassTemplateDecl* CTD)
    {
        writeTemplateDecl(CTD, false);
    }
    void VisitVarTemplateDecl(const clang::VarTemplateDecl* VTD)
    {
        writeTemplateDecl(VTD, false);
    }

private:
    template <typename SpecializationDecl>
    void writeTemplateDeclSpecialization(const SpecializationDecl* SD, bool DumpExplicitInst, bool DumpRefOnly)
    {
        bool DumpedAny = false;
        for (const auto* RedeclWithBadType : SD->redecls())
        {
            // FIXME: The redecls() range sometimes has elements of a less-specific
            // type. (In particular, ClassTemplateSpecializationDecl::redecls() gives
            // us TagDecls, and should give CXXRecordDecls).
            const auto* Redecl = clang::dyn_cast<SpecializationDecl>(RedeclWithBadType);
            if (!Redecl)
            {
                // Found the injected-class-name for a class template. This will be
                // dumped as part of its surrounding class so we don't need to dump it
                // here.
                assert(clang::isa<clang::CXXRecordDecl>(RedeclWithBadType) &&
                       "expected an injected-class-name");
                continue;
            }

            switch (Redecl->getTemplateSpecializationKind())
            {
                case clang::TSK_ExplicitInstantiationDeclaration:
                case clang::TSK_ExplicitInstantiationDefinition:
                    if (!DumpExplicitInst)
                        break;
                    [[fallthrough]];
                case clang::TSK_Undeclared:
                case clang::TSK_ImplicitInstantiation:
                    if (DumpRefOnly)
                        NodeVisitor.AddChild([=]
                                             {
                                                 NodeVisitor.writeBareDeclRef(Redecl);
                                             });
                    else
                        Visit(Redecl);
                    DumpedAny = true;
                    break;
                case clang::TSK_ExplicitSpecialization:
                    break;
            }
        }

        // Ensure we dump at least one decl for each specialization.
        if (!DumpedAny)
            NodeVisitor.AddChild([=]
                                 {
                                     NodeVisitor.writeBareDeclRef(SD);
                                 });
    }

    template <typename TemplateDecl>
    void writeTemplateDecl(const TemplateDecl* TD, bool DumpExplicitInst)
    {
        // FIXME: it would be nice to dump template parameters and specializations
        // to their own named arrays rather than shoving them into the "inner"
        // array. However, template declarations are currently being handled at the
        // wrong "level" of the traversal hierarchy and so it is difficult to
        // achieve without losing information elsewhere.

        dumpTemplateParameters(TD->getTemplateParameters());

        Visit(TD->getTemplatedDecl());

        // TODO: Fixme. Crash in specializations iterator (clang bug?)
        // for (const auto* Child : TD->specializations())
        //    writeTemplateDeclSpecialization(Child, DumpExplicitInst, !TD->isCanonicalDecl());
    }

    ASTNodeVisitor NodeVisitor;
};

class ASTParser : public clang::RecursiveASTVisitor<ASTParser>
{
    bool shouldVisitTemplateInstantiations() const { return true; }
    bool shouldWalkTypesOfTypeLocs() const { return true; }
    bool shouldVisitImplicitCode() const { return true; }
    bool shouldVisitLambdaBody() const { return true; }
    bool shouldTraversePostOrder() const { return true; }

    /*void HandleNamedDecl(AST::Declaration& AST_ND, const clang::NamedDecl* ND);

    bool VisitTranslationUnitDecl(const clang::TranslationUnitDecl* D);
    bool VisitNamedDecl(const clang::NamedDecl* ND);
    bool VisitTypedefDecl(const clang::TypedefDecl* TD);
    bool VisitTypeAliasDecl(const clang::TypeAliasDecl* TAD);
    bool VisitNamespaceDecl(const clang::NamespaceDecl* ND);
    bool VisitUsingDirectiveDecl(const clang::UsingDirectiveDecl* UDD);
    bool VisitNamespaceAliasDecl(const clang::NamespaceAliasDecl* NAD);
    bool VisitUsingDecl(const clang::UsingDecl* UD);
    bool VisitUsingEnumDecl(const clang::UsingEnumDecl* UED);
    bool VisitUsingShadowDecl(const clang::UsingShadowDecl* USD);
    bool VisitVarDecl(const clang::VarDecl* VD);
    bool VisitFieldDecl(const clang::FieldDecl* FD);
    bool VisitFunctionDecl(const clang::FunctionDecl* FD);
    bool VisitEnumDecl(const clang::EnumDecl* ED);
    bool VisitEnumConstantDecl(const clang::EnumConstantDecl* ECD);
    bool VisitRecordDecl(const clang::RecordDecl* RD);
    bool VisitCXXRecordDecl(const clang::CXXRecordDecl* RD);
    bool VisitHLSLBufferDecl(const clang::HLSLBufferDecl* D);
    bool VisitTemplateTypeParmDecl(const clang::TemplateTypeParmDecl* D);
    bool VisitNonTypeTemplateParmDecl(const clang::NonTypeTemplateParmDecl* D);
    bool VisitTemplateTemplateParmDecl(const clang::TemplateTemplateParmDecl* D);
    bool VisitLinkageSpecDecl(const clang::LinkageSpecDecl* LSD);
    bool VisitAccessSpecDecl(const clang::AccessSpecDecl* ASD);
    bool VisitFriendDecl(const clang::FriendDecl* FD);*/
};
} // namespace CppSharp::CppParser