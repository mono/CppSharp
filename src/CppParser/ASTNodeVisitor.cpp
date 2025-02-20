/************************************************************************
 *
 * CppSharp
 * Licensed under the simplified BSD license. All rights reserved.
 *
 ************************************************************************/

#include "ASTNodeVisitor.h"

#include <clang/AST/Type.h>
#include <clang/Basic/SourceManager.h>
#include <clang/Basic/Specifiers.h>
#include <clang/Index/USRGeneration.h>
#include <clang/Lex/Lexer.h>

#include <llvm/ADT/StringExtras.h>

#include <optional>

#include "Types.h"
#include "Decl.h"
#include "Parser.h"

using namespace CppSharp::CppParser;
using namespace clang;

namespace {
AST::AccessSpecifier ConvertToAccess(clang::AccessSpecifier AS)
{
    switch (AS)
    {
        case clang::AS_private:
            return AST::AccessSpecifier::Private;
        case clang::AS_protected:
            return AST::AccessSpecifier::Protected;
        case clang::AS_public:
            return AST::AccessSpecifier::Public;
        case clang::AS_none:
            return AST::AccessSpecifier::Public;
    }

    llvm_unreachable("Unknown AccessSpecifier");
}

std::string GetDeclUSR(const clang::Decl* D)
{
    using namespace clang;
    SmallString<128> usr;
    if (!index::generateUSRForDecl(D, usr))
        return usr.c_str();
    return "<invalid>";
}

std::string GetDeclName(const clang::NamedDecl* D)
{
    if (const clang::IdentifierInfo* II = D->getIdentifier())
        return II->getName().str();
    return D->getDeclName().getAsString();
}

AST::TypeQualifiers GetTypeQualifiers(const clang::Qualifiers& quals)
{
    AST::TypeQualifiers ret;
    ret.isConst = quals.hasConst();
    ret.isRestrict = quals.hasRestrict();
    ret.isVolatile = quals.hasVolatile();
    return ret;
}
} // namespace

void ASTNodeVisitor::ConvertNamedRecord(AST::Declaration& dst, const clang::NamedDecl& src) const
{
    if (!src.getDeclName())
        return;

    dst.name = src.getNameAsString();
    dst.mangledName = GetMangledName(src);
}

std::string ASTNodeVisitor::GetMangledName(const clang::NamedDecl& ND) const
{
    // Source adapted from https://clang.llvm.org/doxygen/JSONNodeDumper_8cpp_source.html#l00845

    // If the declaration is dependent or is in a dependent context, then the
    // mangling is unlikely to be meaningful (and in some cases may cause
    // "don't know how to mangle this" assertion failures.)
    if (ND.isTemplated())
        return {};

    // FIXME: There are likely other contexts in which it makes no sense to ask
    // for a mangled name.
    if (isa<RequiresExprBodyDecl>(ND.getDeclContext()))
        return {};

    // Do not mangle template deduction guides.
    if (isa<CXXDeductionGuideDecl>(ND))
        return {};

    // Mangled names are not meaningful for locals, and may not be well-defined
    // in the case of VLAs.
    auto* VD = dyn_cast<VarDecl>(&ND);
    if (VD && VD->hasLocalStorage())
        return {};

    return NameMangler.GetName(&ND);
}

void ASTNodeVisitor::addPreviousDeclaration(const Decl* D)
{
    switch (D->getKind())
    {
#define DECL(DERIVED, BASE) \
    case Decl::DERIVED:     \
        return writePreviousDeclImpl(cast<DERIVED##Decl>(D));
#define ABSTRACT_DECL(DECL)
#include "clang/AST/DeclNodes.inc"

#undef ABSTRACT_DECL
#undef DECL
    }
    llvm_unreachable("Decl that isn't part of DeclNodes.inc!");
}

void ASTNodeVisitor::Visit(const Attr* A)
{
    const char* AttrName = nullptr;
    switch (A->getKind())
    {
#define ATTR(X)               \
    case attr::X:             \
        AttrName = #X "Attr"; \
        break;
#include "clang/Basic/AttrList.inc"

#undef ATTR
    }
    JOS.attribute("id", createPointerRepresentation(A));
    JOS.attribute("kind", AttrName);
    JOS.attributeObject("range", [A, this]
                        {
                            writeSourceRange(A->getRange());
                        });
    attributeOnlyIfTrue("inherited", A->isInherited());
    attributeOnlyIfTrue("implicit", A->isImplicit());

    // FIXME: it would be useful for us to output the spelling kind as well as
    // the actual spelling. This would allow us to distinguish between the
    // various attribute syntaxes, but we don't currently track that information
    // within the AST.
    // JOS.attribute("spelling", A->getSpelling());

    InnerAttrVisitor::Visit(A);
}

void ASTNodeVisitor::Visit(const Stmt* S)
{
    JOS.attribute("id", createPointerRepresentation(S));

    if (!S)
        return;

    if (auto it = stmtMap.find(S); it != stmtMap.end())
        return;
    stmtMap.emplace(S);

    JOS.attribute("kind", S->getStmtClassName());
    JOS.attributeObject("range",
                        [S, this]
                        {
                            writeSourceRange(S->getSourceRange());
                        });

    if (const auto* E = dyn_cast<Expr>(S))
    {
        JOS.attribute("type", createQualType(E->getType()));
        const char* Category = nullptr;
        switch (E->getValueKind())
        {
            case VK_LValue: Category = "lvalue"; break;
            case VK_XValue: Category = "xvalue"; break;
            case VK_PRValue:
                Category = "prvalue";
                break;
        }
        JOS.attribute("valueCategory", Category);
    }
    InnerStmtVisitor::Visit(S);
}

void ASTNodeVisitor::Visit(const Type* T)
{
    JOS.attribute("id", createPointerRepresentation(T));

    if (!T)
        return;

    if (auto it = typeMap.find(T); it != typeMap.end())
        return;
    typeMap.emplace(T);

    JOS.attribute("kind", (llvm::Twine(T->getTypeClassName()) + "Type").str());
    JOS.attribute("type", createQualType(QualType(T, 0), /*Desugar=*/false));
    attributeOnlyIfTrue("containsErrors", T->containsErrors());
    attributeOnlyIfTrue("isDependent", T->isDependentType());
    attributeOnlyIfTrue("isInstantiationDependent",
                        T->isInstantiationDependentType());
    attributeOnlyIfTrue("isVariablyModified", T->isVariablyModifiedType());
    attributeOnlyIfTrue("containsUnexpandedPack",
                        T->containsUnexpandedParameterPack());
    attributeOnlyIfTrue("isImported", T->isFromAST());
    InnerTypeVisitor::Visit(T);
}

void ASTNodeVisitor::Visit(QualType T)
{
    JOS.attribute("id", createPointerRepresentation(T.getAsOpaquePtr()));
    JOS.attribute("kind", "QualType");
    JOS.attribute("type", createQualType(T));
    JOS.attribute("qualifiers", T.split().Quals.getAsString());
}

void ASTNodeVisitor::Visit(TypeLoc TL)
{
    if (TL.isNull())
        return;
    JOS.attribute("kind",
                  (llvm::Twine(TL.getTypeLocClass() == TypeLoc::Qualified ? "Qualified" : TL.getTypePtr()->getTypeClassName()) +
                   "TypeLoc")
                      .str());
    JOS.attribute("type",
                  createQualType(QualType(TL.getType()), /*Desugar=*/false));
    JOS.attributeObject("range",
                        [TL, this]
                        {
                            writeSourceRange(TL.getSourceRange());
                        });
}

AST::Declaration* ASTNodeVisitor::Visit(const Decl* D)
{
    JOS.attribute("id", createPointerRepresentation(D));

    if (!D)
        return nullptr;

    if (auto it = declMap.find(D); it != declMap.end())
        return it->second;

    JOS.attribute("kind", (llvm::Twine(D->getDeclKindName()) + "Decl").str());
    JOS.attributeObject("loc",
                        [D, this]
                        {
                            writeSourceLocation(D->getLocation());
                        });
    JOS.attributeObject("range",
                        [D, this]
                        {
                            writeSourceRange(D->getSourceRange());
                        });
    attributeOnlyIfTrue("isImplicit", D->isImplicit());
    attributeOnlyIfTrue("isInvalid", D->isInvalidDecl());

    if (D->isUsed())
        JOS.attribute("isUsed", true);
    else if (D->isThisDeclarationReferenced())
        JOS.attribute("isReferenced", true);

    if (D->getLexicalDeclContext() != D->getDeclContext())
    {
        // Because of multiple inheritance, a DeclContext pointer does not produce
        // the same pointer representation as a Decl pointer that references the
        // same AST Node.
        const auto* ParentDeclContextDecl = dyn_cast<Decl>(D->getDeclContext());
        JOS.attribute("parentDeclContextId",
                      createPointerRepresentation(ParentDeclContextDecl));
    }

    addPreviousDeclaration(D);
    AST::Declaration* AST_D = InnerDeclVisitor::Visit(D);
    if (!AST_D)
        return nullptr;

    AST_D->originalPtr = (void*)D;
    AST_D->kind = (AST::DeclarationKind)D->getKind();
    AST_D->location = SourceLocation(D->getLocation().getRawEncoding());

    AST_D->isImplicit = D->isImplicit();
    AST_D->isInvalid = D->isInvalidDecl();
    AST_D->isUsed = D->isUsed();
    AST_D->isReferenced = D->isUsed() && D->isThisDeclarationReferenced();

    AST_D->USR = GetDeclUSR(D);
    AST_D->access = ConvertToAccess(D->getAccess());

    return AST_D;
}

void ASTNodeVisitor::Visit(const comments::Comment* C, const comments::FullComment* FC)
{
    if (!C)
        return;

    JOS.attribute("id", createPointerRepresentation(C));
    JOS.attribute("kind", C->getCommentKindName());
    JOS.attributeObject("loc",
                        [C, this]
                        {
                            writeSourceLocation(C->getLocation());
                        });
    JOS.attributeObject("range",
                        [C, this]
                        {
                            writeSourceRange(C->getSourceRange());
                        });

    InnerCommentVisitor::visit(C, FC);
}

void ASTNodeVisitor::Visit(const TemplateArgument& TA, clang::SourceRange R, const Decl* From, StringRef Label)
{
    JOS.attribute("kind", "TemplateArgument");
    if (R.isValid())
        JOS.attributeObject("range", [R, this]
                            {
                                writeSourceRange(R);
                            });

    if (From)
        JOS.attribute(Label.empty() ? "fromDecl" : Label, createBareDeclRef(From));

    InnerTemplateArgVisitor::Visit(TA);
}

void ASTNodeVisitor::Visit(const CXXCtorInitializer* Init)
{
    JOS.attribute("kind", "CXXCtorInitializer");
    if (Init->isAnyMemberInitializer())
        JOS.attribute("anyInit", createBareDeclRef(Init->getAnyMember()));
    else if (Init->isBaseInitializer())
        JOS.attribute("baseInit",
                      createQualType(QualType(Init->getBaseClass(), 0)));
    else if (Init->isDelegatingInitializer())
        JOS.attribute("delegatingInit",
                      createQualType(Init->getTypeSourceInfo()->getType()));
    else
        llvm_unreachable("Unknown initializer type");
}

void ASTNodeVisitor::Visit(const BlockDecl::Capture& C)
{
    JOS.attribute("kind", "Capture");
    attributeOnlyIfTrue("byref", C.isByRef());
    attributeOnlyIfTrue("nested", C.isNested());
    if (C.getVariable())
        JOS.attribute("var", createBareDeclRef(C.getVariable()));
}

void ASTNodeVisitor::Visit(const GenericSelectionExpr::ConstAssociation& A)
{
    JOS.attribute("associationKind", A.getTypeSourceInfo() ? "case" : "default");
    attributeOnlyIfTrue("selected", A.isSelected());
}

void ASTNodeVisitor::Visit(const concepts::Requirement* R)
{
    if (!R)
        return;

    switch (R->getKind())
    {
        case concepts::Requirement::RK_Type:
            JOS.attribute("kind", "TypeRequirement");
            break;
        case concepts::Requirement::RK_Simple:
            JOS.attribute("kind", "SimpleRequirement");
            break;
        case concepts::Requirement::RK_Compound:
            JOS.attribute("kind", "CompoundRequirement");
            break;
        case concepts::Requirement::RK_Nested:
            JOS.attribute("kind", "NestedRequirement");
            break;
    }

    if (auto* ER = dyn_cast<concepts::ExprRequirement>(R))
        attributeOnlyIfTrue("noexcept", ER->hasNoexceptRequirement());

    attributeOnlyIfTrue("isDependent", R->isDependent());
    if (!R->isDependent())
        JOS.attribute("satisfied", R->isSatisfied());
    attributeOnlyIfTrue("containsUnexpandedPack",
                        R->containsUnexpandedParameterPack());
}

void ASTNodeVisitor::Visit(const APValue& Value, QualType Ty)
{
    std::string Str;
    llvm::raw_string_ostream OS(Str);
    Value.printPretty(OS, Ctx, Ty);
    JOS.attribute("value", Str);
}

void ASTNodeVisitor::Visit(const ConceptReference* CR)
{
    JOS.attribute("kind", "ConceptReference");
    JOS.attribute("id", createPointerRepresentation(CR->getNamedConcept()));
    if (const auto* Args = CR->getTemplateArgsAsWritten())
    {
        JOS.attributeArray("templateArgsAsWritten", [Args, this]
                           {
                               for (const TemplateArgumentLoc& TAL : Args->arguments())
                                   JOS.object(
                                       [&TAL, this]
                                       {
                                           Visit(TAL.getArgument(), TAL.getSourceRange());
                                       });
                           });
    }
    /*JOS.attributeObject("loc",
        [CR, this] { writeSourceLocation(CR->getLocation()); });
    JOS.attributeObject("range",
        [CR, this] { writeSourceRange(CR->getSourceRange()); });*/
}

void ASTNodeVisitor::writeIncludeStack(PresumedLoc Loc, bool JustFirst)
{
    if (Loc.isInvalid())
        return;

    JOS.attributeBegin("includedFrom");
    JOS.objectBegin();

    if (!JustFirst)
    {
        // Walk the stack recursively, then print out the presumed location.
        writeIncludeStack(SM.getPresumedLoc(Loc.getIncludeLoc()));
    }

    JOS.attribute("file", Loc.getFilename());
    JOS.objectEnd();
    JOS.attributeEnd();
}

void ASTNodeVisitor::writeBareSourceLocation(clang::SourceLocation Loc, bool IsSpelling, bool addFileInfo)
{
    PresumedLoc Presumed = SM.getPresumedLoc(Loc);
    unsigned ActualLine = IsSpelling ? SM.getSpellingLineNumber(Loc) : SM.getExpansionLineNumber(Loc);
    StringRef ActualFile = SM.getBufferName(Loc);

    if (!Presumed.isValid())
        return;

    JOS.attribute("offset", SM.getDecomposedLoc(Loc).second);
    if (LastLocFilename != ActualFile)
    {
        if (addFileInfo)
            JOS.attribute("file", ActualFile);
        JOS.attribute("line", ActualLine);
    }
    else if (LastLocLine != ActualLine)
        JOS.attribute("line", ActualLine);

    StringRef PresumedFile = Presumed.getFilename();
    if (PresumedFile != ActualFile && LastLocPresumedFilename != PresumedFile && addFileInfo)
        JOS.attribute("presumedFile", PresumedFile);

    unsigned PresumedLine = Presumed.getLine();
    if (ActualLine != PresumedLine && LastLocPresumedLine != PresumedLine)
        JOS.attribute("presumedLine", PresumedLine);

    JOS.attribute("col", Presumed.getColumn());
    JOS.attribute("tokLen",
                  Lexer::MeasureTokenLength(Loc, SM, Ctx.getLangOpts()));
    LastLocFilename = ActualFile;
    LastLocPresumedFilename = PresumedFile;
    LastLocPresumedLine = PresumedLine;
    LastLocLine = ActualLine;

    if (addFileInfo)
    {
        // Orthogonal to the file, line, and column de-duplication is whether the
        // given location was a result of an include. If so, print where the
        // include location came from.
        writeIncludeStack(SM.getPresumedLoc(Presumed.getIncludeLoc()),
                          /*JustFirst*/ true);
    }
}

void ASTNodeVisitor::writeSourceLocation(clang::SourceLocation Loc, bool addFileInfo)
{
    clang::SourceLocation Spelling = SM.getSpellingLoc(Loc);
    clang::SourceLocation Expansion = SM.getExpansionLoc(Loc);

    if (Expansion != Spelling)
    {
        // If the expansion and the spelling are different, output subobjects
        // describing both locations.
        JOS.attributeObject("spellingLoc", [Spelling, addFileInfo, this]
                            {
                                writeBareSourceLocation(Spelling, /*IsSpelling*/ true, addFileInfo);
                            });
        JOS.attributeObject("expansionLoc", [Expansion, addFileInfo, Loc, this]
                            {
                                writeBareSourceLocation(Expansion, /*IsSpelling*/ false, addFileInfo);
                                // If there is a macro expansion, add extra information if the interesting
                                // bit is the macro arg expansion.
                                if (SM.isMacroArgExpansion(Loc))
                                    JOS.attribute("isMacroArgExpansion", true);
                            });
    }
    else
        writeBareSourceLocation(Spelling, /*IsSpelling*/ true, addFileInfo);
}

void ASTNodeVisitor::writeSourceRange(clang::SourceRange R)
{
    JOS.attributeObject("begin",
                        [R, this]
                        {
                            writeSourceLocation(R.getBegin(), false);
                        });

    JOS.attributeObject("end", [R, this]
                        {
                            writeSourceLocation(R.getEnd(), false);
                        });
}

std::string ASTNodeVisitor::createPointerRepresentation(const void* Ptr)
{
    // Because JSON stores integer values as signed 64-bit integers, trying to
    // represent them as such makes for very ugly pointer values in the resulting
    // output. Instead, we convert the value to hex and treat it as a string.
    return "0x" + llvm::utohexstr(reinterpret_cast<uint64_t>(Ptr), true);
}

llvm::json::Object ASTNodeVisitor::createQualType(QualType QT, bool Desugar)
{
    SplitQualType SQT = QT.split();
    std::string SQTS = QualType::getAsString(SQT, PrintPolicy);
    llvm::json::Object Ret{ { "qualType", SQTS } };

    if (Desugar && !QT.isNull())
    {
        SplitQualType DSQT = QT.getSplitDesugaredType();
        if (DSQT != SQT)
        {
            std::string DSQTS = QualType::getAsString(DSQT, PrintPolicy);
            if (DSQTS != SQTS)
                Ret["desugaredQualType"] = DSQTS;
        }
        if (const auto* TT = QT->getAs<TypedefType>())
            Ret["typeAliasDeclId"] = createPointerRepresentation(TT->getDecl());
    }
    return Ret;
}

AST::QualifiedType ASTNodeVisitor::CreateQualifiedType(QualType QT, bool Desugar)
{
    if (QT.isNull())
        return {};

    SplitQualType SQT = QT.split();
    AST::QualifiedType Ret;
    Ret.qualifiers = GetTypeQualifiers(SQT.Quals);
    // Ret.type = Visit(SQT.Ty); // TODO: Implement Visit

    if (Desugar)
    {
        SplitQualType DSQT = QT.getSplitDesugaredType();
        if (DSQT != SQT)
        {
            // Ret.desugaredType = Visit(DSQT.Ty);
        }

        if (const auto* TT = QT->getAs<TypedefType>())
            Ret.typeAliasDeclId = (void*)TT->getDecl();
    }
    return Ret;
}

void ASTNodeVisitor::writeBareDeclRef(const Decl* D)
{
    JOS.attribute("id", createPointerRepresentation(D));
    if (!D)
        return;

    JOS.attribute("kind", (llvm::Twine(D->getDeclKindName()) + "Decl").str());
    if (const auto* ND = dyn_cast<NamedDecl>(D))
        JOS.attribute("name", ND->getDeclName().getAsString());
    if (const auto* VD = dyn_cast<ValueDecl>(D))
        JOS.attribute("type", createQualType(VD->getType()));
}

llvm::json::Object ASTNodeVisitor::createBareDeclRef(const Decl* D)
{
    llvm::json::Object Ret{ { "id", createPointerRepresentation(D) } };
    if (!D)
        return Ret;

    Ret["kind"] = (llvm::Twine(D->getDeclKindName()) + "Decl").str();
    if (const auto* ND = dyn_cast<NamedDecl>(D))
        Ret["name"] = ND->getDeclName().getAsString();
    if (const auto* VD = dyn_cast<ValueDecl>(D))
        Ret["type"] = createQualType(VD->getType());
    return Ret;
}

llvm::json::Array ASTNodeVisitor::createCastPath(const CastExpr* C)
{
    llvm::json::Array Ret;
    if (C->path_empty())
        return Ret;

    for (auto I = C->path_begin(), E = C->path_end(); I != E; ++I)
    {
        const CXXBaseSpecifier* Base = *I;
        const auto* RD =
            cast<CXXRecordDecl>(Base->getType()->castAs<RecordType>()->getDecl());

        llvm::json::Object Val{ { "name", RD->getName() } };
        if (Base->isVirtual())
            Val["isVirtual"] = true;
        Ret.push_back(std::move(Val));
    }
    return Ret;
}

#define FIELD2(Name, Flag) \
    if (RD->Flag())        \
    Ret[Name] = true
#define FIELD1(Flag) FIELD2(#Flag, Flag)

static llvm::json::Object createDefaultConstructorDefinitionData(const CXXRecordDecl* RD)
{
    llvm::json::Object Ret;

    FIELD2("exists", hasDefaultConstructor);
    FIELD2("trivial", hasTrivialDefaultConstructor);
    FIELD2("nonTrivial", hasNonTrivialDefaultConstructor);
    FIELD2("userProvided", hasUserProvidedDefaultConstructor);
    FIELD2("isConstexpr", hasConstexprDefaultConstructor);
    FIELD2("needsImplicit", needsImplicitDefaultConstructor);
    FIELD2("defaultedIsConstexpr", defaultedDefaultConstructorIsConstexpr);

    return Ret;
}

static llvm::json::Object createCopyConstructorDefinitionData(const CXXRecordDecl* RD)
{
    llvm::json::Object Ret;

    FIELD2("simple", hasSimpleCopyConstructor);
    FIELD2("trivial", hasTrivialCopyConstructor);
    FIELD2("nonTrivial", hasNonTrivialCopyConstructor);
    FIELD2("userDeclared", hasUserDeclaredCopyConstructor);
    FIELD2("hasConstParam", hasCopyConstructorWithConstParam);
    FIELD2("implicitHasConstParam", implicitCopyConstructorHasConstParam);
    FIELD2("needsImplicit", needsImplicitCopyConstructor);
    FIELD2("needsOverloadResolution", needsOverloadResolutionForCopyConstructor);
    if (!RD->needsOverloadResolutionForCopyConstructor())
        FIELD2("defaultedIsDeleted", defaultedCopyConstructorIsDeleted);

    return Ret;
}

static llvm::json::Object createMoveConstructorDefinitionData(const CXXRecordDecl* RD)
{
    llvm::json::Object Ret;

    FIELD2("exists", hasMoveConstructor);
    FIELD2("simple", hasSimpleMoveConstructor);
    FIELD2("trivial", hasTrivialMoveConstructor);
    FIELD2("nonTrivial", hasNonTrivialMoveConstructor);
    FIELD2("userDeclared", hasUserDeclaredMoveConstructor);
    FIELD2("needsImplicit", needsImplicitMoveConstructor);
    FIELD2("needsOverloadResolution", needsOverloadResolutionForMoveConstructor);
    if (!RD->needsOverloadResolutionForMoveConstructor())
        FIELD2("defaultedIsDeleted", defaultedMoveConstructorIsDeleted);

    return Ret;
}

static llvm::json::Object createCopyAssignmentDefinitionData(const CXXRecordDecl* RD)
{
    llvm::json::Object Ret;

    FIELD2("simple", hasSimpleCopyAssignment);
    FIELD2("trivial", hasTrivialCopyAssignment);
    FIELD2("nonTrivial", hasNonTrivialCopyAssignment);
    FIELD2("hasConstParam", hasCopyAssignmentWithConstParam);
    FIELD2("implicitHasConstParam", implicitCopyAssignmentHasConstParam);
    FIELD2("userDeclared", hasUserDeclaredCopyAssignment);
    FIELD2("needsImplicit", needsImplicitCopyAssignment);
    FIELD2("needsOverloadResolution", needsOverloadResolutionForCopyAssignment);

    return Ret;
}

static llvm::json::Object createMoveAssignmentDefinitionData(const CXXRecordDecl* RD)
{
    llvm::json::Object Ret;

    FIELD2("exists", hasMoveAssignment);
    FIELD2("simple", hasSimpleMoveAssignment);
    FIELD2("trivial", hasTrivialMoveAssignment);
    FIELD2("nonTrivial", hasNonTrivialMoveAssignment);
    FIELD2("userDeclared", hasUserDeclaredMoveAssignment);
    FIELD2("needsImplicit", needsImplicitMoveAssignment);
    FIELD2("needsOverloadResolution", needsOverloadResolutionForMoveAssignment);

    return Ret;
}

static llvm::json::Object createDestructorDefinitionData(const CXXRecordDecl* RD)
{
    llvm::json::Object Ret;

    FIELD2("simple", hasSimpleDestructor);
    FIELD2("irrelevant", hasIrrelevantDestructor);
    FIELD2("trivial", hasTrivialDestructor);
    FIELD2("nonTrivial", hasNonTrivialDestructor);
    FIELD2("userDeclared", hasUserDeclaredDestructor);
    FIELD2("needsImplicit", needsImplicitDestructor);
    FIELD2("needsOverloadResolution", needsOverloadResolutionForDestructor);
    if (!RD->needsOverloadResolutionForDestructor())
        FIELD2("defaultedIsDeleted", defaultedDestructorIsDeleted);

    return Ret;
}

llvm::json::Object ASTNodeVisitor::createCXXRecordDefinitionData(const CXXRecordDecl* RD)
{
    llvm::json::Object Ret;

    // This data is common to all C++ classes.
    FIELD1(isGenericLambda);
    FIELD1(isLambda);
    FIELD1(isEmpty);
    FIELD1(isAggregate);
    FIELD1(isStandardLayout);
    FIELD1(isTriviallyCopyable);
    FIELD1(isPOD);
    FIELD1(isTrivial);
    FIELD1(isPolymorphic);
    FIELD1(isAbstract);
    FIELD1(isLiteral);
    FIELD1(canPassInRegisters);
    FIELD1(hasUserDeclaredConstructor);
    FIELD1(hasConstexprNonCopyMoveConstructor);
    FIELD1(hasMutableFields);
    FIELD1(hasVariantMembers);
    FIELD2("canConstDefaultInit", allowConstDefaultInit);

    Ret["defaultCtor"] = createDefaultConstructorDefinitionData(RD);
    Ret["copyCtor"] = createCopyConstructorDefinitionData(RD);
    Ret["moveCtor"] = createMoveConstructorDefinitionData(RD);
    Ret["copyAssign"] = createCopyAssignmentDefinitionData(RD);
    Ret["moveAssign"] = createMoveAssignmentDefinitionData(RD);
    Ret["dtor"] = createDestructorDefinitionData(RD);

    return Ret;
}

#undef FIELD1
#undef FIELD2

std::string ASTNodeVisitor::createAccessSpecifier(AccessSpecifier AS)
{
    const auto AccessSpelling = getAccessSpelling(AS);
    if (AccessSpelling.empty())
        return "none";
    return AccessSpelling.str();
}

llvm::json::Object ASTNodeVisitor::createCXXBaseSpecifier(const CXXBaseSpecifier& BS)
{
    llvm::json::Object Ret;

    Ret["type"] = createQualType(BS.getType());
    Ret["access"] = createAccessSpecifier(BS.getAccessSpecifier());
    Ret["writtenAccess"] =
        createAccessSpecifier(BS.getAccessSpecifierAsWritten());
    if (BS.isVirtual())
        Ret["isVirtual"] = true;
    if (BS.isPackExpansion())
        Ret["isPackExpansion"] = true;

    return Ret;
}

void ASTNodeVisitor::VisitAliasAttr(const AliasAttr* AA)
{
    JOS.attribute("aliasee", AA->getAliasee());
}

void ASTNodeVisitor::VisitCleanupAttr(const CleanupAttr* CA)
{
    JOS.attribute("cleanup_function", createBareDeclRef(CA->getFunctionDecl()));
}

void ASTNodeVisitor::VisitDeprecatedAttr(const DeprecatedAttr* DA)
{
    if (!DA->getMessage().empty())
        JOS.attribute("message", DA->getMessage());
    if (!DA->getReplacement().empty())
        JOS.attribute("replacement", DA->getReplacement());
}

void ASTNodeVisitor::VisitUnavailableAttr(const UnavailableAttr* UA)
{
    if (!UA->getMessage().empty())
        JOS.attribute("message", UA->getMessage());
}

void ASTNodeVisitor::VisitSectionAttr(const SectionAttr* SA)
{
    JOS.attribute("section_name", SA->getName());
}

void ASTNodeVisitor::VisitVisibilityAttr(const VisibilityAttr* VA)
{
    JOS.attribute("visibility", VisibilityAttr::ConvertVisibilityTypeToStr(
                                    VA->getVisibility()));
}

void ASTNodeVisitor::VisitTLSModelAttr(const TLSModelAttr* TA)
{
    JOS.attribute("tls_model", TA->getModel());
}

void ASTNodeVisitor::VisitTypedefType(const TypedefType* TT)
{
    JOS.attribute("decl", createBareDeclRef(TT->getDecl()));
    if (!TT->typeMatchesDecl())
        JOS.attribute("type", createQualType(TT->desugar()));
}

void ASTNodeVisitor::VisitUsingType(const UsingType* TT)
{
    JOS.attribute("decl", createBareDeclRef(TT->getFoundDecl()));
    if (!TT->typeMatchesDecl())
        JOS.attribute("type", createQualType(TT->desugar()));
}

void ASTNodeVisitor::VisitFunctionType(const FunctionType* T)
{
    FunctionType::ExtInfo E = T->getExtInfo();
    attributeOnlyIfTrue("noreturn", E.getNoReturn());
    attributeOnlyIfTrue("producesResult", E.getProducesResult());
    if (E.getHasRegParm())
        JOS.attribute("regParm", E.getRegParm());
    JOS.attribute("cc", FunctionType::getNameForCallConv(E.getCC()));
}

void ASTNodeVisitor::VisitFunctionProtoType(const FunctionProtoType* T)
{
    FunctionProtoType::ExtProtoInfo E = T->getExtProtoInfo();
    attributeOnlyIfTrue("trailingReturn", E.HasTrailingReturn);
    attributeOnlyIfTrue("const", T->isConst());
    attributeOnlyIfTrue("volatile", T->isVolatile());
    attributeOnlyIfTrue("restrict", T->isRestrict());
    attributeOnlyIfTrue("variadic", E.Variadic);
    switch (E.RefQualifier)
    {
        case RQ_LValue: JOS.attribute("refQualifier", "&"); break;
        case RQ_RValue: JOS.attribute("refQualifier", "&&"); break;
        case RQ_None: break;
    }
    switch (E.ExceptionSpec.Type)
    {
        case EST_DynamicNone:
        case EST_Dynamic:
        {
            JOS.attribute("exceptionSpec", "throw");
            llvm::json::Array Types;
            for (QualType QT : E.ExceptionSpec.Exceptions)
                Types.push_back(createQualType(QT));
            JOS.attribute("exceptionTypes", std::move(Types));
        }
        break;
        case EST_MSAny:
            JOS.attribute("exceptionSpec", "throw");
            JOS.attribute("throwsAny", true);
            break;
        case EST_BasicNoexcept:
            JOS.attribute("exceptionSpec", "noexcept");
            break;
        case EST_NoexceptTrue:
        case EST_NoexceptFalse:
            JOS.attribute("exceptionSpec", "noexcept");
            JOS.attribute("conditionEvaluatesTo",
                          E.ExceptionSpec.Type == EST_NoexceptTrue);
            // JOS.attributeWithCall("exceptionSpecExpr",
            //                     [this, E]() { Visit(E.ExceptionSpec.NoexceptExpr); });
            break;
        case EST_NoThrow:
            JOS.attribute("exceptionSpec", "nothrow");
            break;
            // FIXME: I cannot find a way to trigger these cases while dumping the AST. I
            // suspect you can only run into them when executing an AST dump from within
            // the debugger, which is not a use case we worry about for the JSON dumping
            // feature.
        case EST_DependentNoexcept:
        case EST_Unevaluated:
        case EST_Uninstantiated:
        case EST_Unparsed:
        case EST_None: break;
    }
    VisitFunctionType(T);
}

void ASTNodeVisitor::VisitRValueReferenceType(const ReferenceType* RT)
{
    attributeOnlyIfTrue("spelledAsLValue", RT->isSpelledAsLValue());
}

void ASTNodeVisitor::VisitArrayType(const ArrayType* AT)
{
    switch (AT->getSizeModifier())
    {
        case ArrayType::ArraySizeModifier::Star:
            JOS.attribute("sizeModifier", "*");
            break;
        case ArrayType::ArraySizeModifier::Static:
            JOS.attribute("sizeModifier", "static");
            break;
        case ArrayType::ArraySizeModifier::Normal:
            break;
    }

    std::string Str = AT->getIndexTypeQualifiers().getAsString();
    if (!Str.empty())
        JOS.attribute("indexTypeQualifiers", Str);
}

void ASTNodeVisitor::VisitConstantArrayType(const ConstantArrayType* CAT)
{
    // FIXME: this should use ZExt instead of SExt, but JSON doesn't allow a
    // narrowing conversion to int64_t so it cannot be expressed.
    JOS.attribute("size", CAT->getSize().getSExtValue());
    VisitArrayType(CAT);
}

void ASTNodeVisitor::VisitDependentSizedExtVectorType(
    const DependentSizedExtVectorType* VT)
{
    JOS.attributeObject(
        "attrLoc", [VT, this]
        {
            writeSourceLocation(VT->getAttributeLoc());
        });
}

void ASTNodeVisitor::VisitVectorType(const VectorType* VT)
{
    JOS.attribute("numElements", VT->getNumElements());
    switch (VT->getVectorKind())
    {
        case VectorType::VectorKind::GenericVector:
            break;
        case VectorType::VectorKind::AltiVecVector:
            JOS.attribute("vectorKind", "altivec");
            break;
        case VectorType::VectorKind::AltiVecPixel:
            JOS.attribute("vectorKind", "altivec pixel");
            break;
        case VectorType::VectorKind::AltiVecBool:
            JOS.attribute("vectorKind", "altivec bool");
            break;
        case VectorType::VectorKind::NeonVector:
            JOS.attribute("vectorKind", "neon");
            break;
        case VectorType::VectorKind::NeonPolyVector:
            JOS.attribute("vectorKind", "neon poly");
            break;
        case VectorType::VectorKind::SveFixedLengthDataVector:
            JOS.attribute("vectorKind", "fixed-length sve data vector");
            break;
        case VectorType::VectorKind::SveFixedLengthPredicateVector:
            JOS.attribute("vectorKind", "fixed-length sve predicate vector");
            break;
        case VectorType::VectorKind::RVVFixedLengthDataVector:
            JOS.attribute("vectorKind", "fixed-length rvv data vector");
            break;
            /*case VectorType::VectorKind::RVVFixedLengthMask:
            case VectorType::VectorKind::RVVFixedLengthMask_1:
            case VectorType::VectorKind::RVVFixedLengthMask_2:
            case VectorType::VectorKind::RVVFixedLengthMask_4:
                JOS.attribute("vectorKind", "fixed-length rvv mask vector");
                break;*/
    }
}

void ASTNodeVisitor::VisitUnresolvedUsingType(const UnresolvedUsingType* UUT)
{
    JOS.attribute("decl", createBareDeclRef(UUT->getDecl()));
}

void ASTNodeVisitor::VisitUnaryTransformType(const UnaryTransformType* UTT)
{
    switch (UTT->getUTTKind())
    {
#define TRANSFORM_TYPE_TRAIT_DEF(Enum, Trait)   \
    case UnaryTransformType::Enum:              \
        JOS.attribute("transformKind", #Trait); \
        break;
#include "clang/Basic/TransformTypeTraits.def"
    }
}

void ASTNodeVisitor::VisitTagType(const TagType* TT)
{
    JOS.attribute("decl", createBareDeclRef(TT->getDecl()));
}

void ASTNodeVisitor::VisitTemplateTypeParmType(
    const TemplateTypeParmType* TTPT)
{
    JOS.attribute("depth", TTPT->getDepth());
    JOS.attribute("index", TTPT->getIndex());
    attributeOnlyIfTrue("isPack", TTPT->isParameterPack());
    JOS.attribute("decl", createBareDeclRef(TTPT->getDecl()));
}

void ASTNodeVisitor::VisitSubstTemplateTypeParmType(
    const SubstTemplateTypeParmType* STTPT)
{
    JOS.attribute("index", STTPT->getIndex());
    if (auto PackIndex = STTPT->getPackIndex())
        JOS.attribute("pack_index", *PackIndex);
}

void ASTNodeVisitor::VisitSubstTemplateTypeParmPackType(
    const SubstTemplateTypeParmPackType* T)
{
    JOS.attribute("index", T->getIndex());
}

void ASTNodeVisitor::VisitAutoType(const AutoType* AT)
{
    JOS.attribute("undeduced", !AT->isDeduced());
    switch (AT->getKeyword())
    {
        case AutoTypeKeyword::Auto:
            JOS.attribute("typeKeyword", "auto");
            break;
        case AutoTypeKeyword::DecltypeAuto:
            JOS.attribute("typeKeyword", "decltype(auto)");
            break;
        case AutoTypeKeyword::GNUAutoType:
            JOS.attribute("typeKeyword", "__auto_type");
            break;
    }
}

void ASTNodeVisitor::VisitTemplateSpecializationType(
    const TemplateSpecializationType* TST)
{
    attributeOnlyIfTrue("isAlias", TST->isTypeAlias());

    std::string Str;
    llvm::raw_string_ostream OS(Str);
    TST->getTemplateName().print(OS, PrintPolicy);
    JOS.attribute("templateName", Str);
}

void ASTNodeVisitor::VisitInjectedClassNameType(
    const InjectedClassNameType* ICNT)
{
    JOS.attribute("decl", createBareDeclRef(ICNT->getDecl()));
}

void ASTNodeVisitor::VisitObjCInterfaceType(const ObjCInterfaceType* OIT)
{
    JOS.attribute("decl", createBareDeclRef(OIT->getDecl()));
}

void ASTNodeVisitor::VisitPackExpansionType(const PackExpansionType* PET)
{
    if (std::optional<unsigned> N = PET->getNumExpansions())
        JOS.attribute("numExpansions", *N);
}

void ASTNodeVisitor::VisitElaboratedType(const ElaboratedType* ET)
{
    if (const NestedNameSpecifier* NNS = ET->getQualifier())
    {
        std::string Str;
        llvm::raw_string_ostream OS(Str);
        NNS->print(OS, PrintPolicy, /*ResolveTemplateArgs*/ true);
        JOS.attribute("qualifier", Str);
    }
    if (const TagDecl* TD = ET->getOwnedTagDecl())
        JOS.attribute("ownedTagDecl", createBareDeclRef(TD));
}

void ASTNodeVisitor::VisitMacroQualifiedType(const MacroQualifiedType* MQT)
{
    JOS.attribute("macroName", MQT->getMacroIdentifier()->getName());
}

void ASTNodeVisitor::VisitMemberPointerType(const MemberPointerType* MPT)
{
    attributeOnlyIfTrue("isData", MPT->isMemberDataPointer());
    attributeOnlyIfTrue("isFunction", MPT->isMemberFunctionPointer());
}

AST::Declaration* ASTNodeVisitor::VisitTranslationUnitDecl(const clang::TranslationUnitDecl* D)
{
    AST::TranslationUnit* TU = parser.GetOrCreateTranslationUnit(D);
    declMap.emplace(D, TU);
    return TU;
}

AST::Declaration* ASTNodeVisitor::VisitNamedDecl(const NamedDecl* ND)
{
    if (!ND)
        return nullptr;

    if (!ND->getDeclName())
        return nullptr;

    JOS.attribute("name", ND->getNameAsString());

    std::string MangledName = parser.GetDeclMangledName(ND);
    if (!MangledName.empty())
        JOS.attribute("mangledName", MangledName);

    declMap.try_emplace(ND, nullptr);

    return nullptr;
}

void ASTNodeVisitor::HandleNamedDecl(AST::Declaration& AST_ND, const NamedDecl* ND)
{
    if (!ND)
        return;

    declMap.emplace(ND, &AST_ND);

    AST_ND.name = ND->getName();
    AST_ND.mangledName = parser.GetDeclMangledName(ND);
    AST_ND.isHidden = !ND->isUnconditionallyVisible(); // TODO: Sema::isVisible()?
}

AST::Declaration* ASTNodeVisitor::VisitTypedefDecl(const TypedefDecl* TD)
{
    AST::Declaration* Decl = VisitNamedDecl(TD);
    JOS.attribute("type", createQualType(TD->getUnderlyingType()));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitTypeAliasDecl(const TypeAliasDecl* TAD)
{
    auto NS = parser.GetNamespace(TAD);
    auto Name = GetDeclName(TAD);

    auto AST_TAD = NS->FindTypeAlias(Name, /*Create=*/false);

    if (AST_TAD)
        return AST_TAD;

    AST_TAD = NS->FindTypeAlias(Name, /*Create=*/true);
    HandleNamedDecl(*AST_TAD, TAD);

    VisitNamedDecl(TAD);
    JOS.attribute("type", createQualType(TAD->getUnderlyingType()));
    return AST_TAD;
}

AST::Declaration* ASTNodeVisitor::VisitNamespaceDecl(const NamespaceDecl* ND)
{
    AST::TranslationUnit* Unit = parser.GetOrCreateTranslationUnit(cast<Decl>(ND->getDeclContext()));
    AST::Namespace& NS = Unit->FindCreateNamespace(ND->getName());

    NS.isInline = ND->isInline();
    NS.isNested = ND->isNested();
    NS.isAnonymous = ND->isAnonymousNamespace();

    HandleNamedDecl(NS, ND);

    VisitNamedDecl(ND);
    attributeOnlyIfTrue("isInline", ND->isInline());
    attributeOnlyIfTrue("isNested", ND->isNested());
    if (!ND->isFirstDecl())
        JOS.attribute("originalNamespace", createBareDeclRef(ND->getFirstDecl()));

    return &NS;
}

AST::Declaration* ASTNodeVisitor::VisitUsingDirectiveDecl(const UsingDirectiveDecl* UDD)
{
    JOS.attribute("nominatedNamespace",
                  createBareDeclRef(UDD->getNominatedNamespace()));
    return nullptr;
}

AST::Declaration* ASTNodeVisitor::VisitNamespaceAliasDecl(const NamespaceAliasDecl* NAD)
{
    AST::Declaration* Decl = VisitNamedDecl(NAD);
    JOS.attribute("aliasedNamespace",
                  createBareDeclRef(NAD->getAliasedNamespace()));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitUsingDecl(const UsingDecl* UD)
{
    std::string Name;
    if (const NestedNameSpecifier* NNS = UD->getQualifier())
    {
        llvm::raw_string_ostream SOS(Name);
        NNS->print(SOS, UD->getASTContext().getPrintingPolicy());
    }
    Name += UD->getNameAsString();
    JOS.attribute("name", Name);
    return nullptr;
}

AST::Declaration* ASTNodeVisitor::VisitUsingEnumDecl(const UsingEnumDecl* UED)
{
    JOS.attribute("target", createBareDeclRef(UED->getEnumDecl()));
    return nullptr;
}

AST::Declaration* ASTNodeVisitor::VisitUsingShadowDecl(const UsingShadowDecl* USD)
{
    JOS.attribute("target", createBareDeclRef(USD->getTargetDecl()));
    return nullptr;
}

AST::Declaration* ASTNodeVisitor::VisitVarDecl(const VarDecl* VD)
{
    AST::Declaration* Decl = VisitNamedDecl(VD);
    JOS.attribute("type", createQualType(VD->getType()));

    // TODO: clang 19
    // if (const auto* P = dyn_cast<ParmVarDecl>(VD))
    //    attributeOnlyIfTrue("explicitObjectParameter", P-isExplicitObjectParameter());

    StorageClass SC = VD->getStorageClass();
    if (SC != SC_None)
        JOS.attribute("storageClass", VarDecl::getStorageClassSpecifierString(SC));
    switch (VD->getTLSKind())
    {
        case VarDecl::TLS_Dynamic: JOS.attribute("tls", "dynamic"); break;
        case VarDecl::TLS_Static: JOS.attribute("tls", "static"); break;
        case VarDecl::TLS_None: break;
    }
    attributeOnlyIfTrue("nrvo", VD->isNRVOVariable());
    attributeOnlyIfTrue("inline", VD->isInline());
    attributeOnlyIfTrue("constexpr", VD->isConstexpr());
    attributeOnlyIfTrue("modulePrivate", VD->isModulePrivate());
    if (VD->hasInit())
    {
        switch (VD->getInitStyle())
        {
            case VarDecl::CInit: JOS.attribute("init", "c"); break;
            case VarDecl::CallInit: JOS.attribute("init", "call"); break;
            case VarDecl::ListInit: JOS.attribute("init", "list"); break;
            case VarDecl::ParenListInit:
                JOS.attribute("init", "paren-list");
                break;
        }
    }
    attributeOnlyIfTrue("isParameterPack", VD->isParameterPack());
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitFieldDecl(const FieldDecl* FD)
{
    AST::Declaration* Decl = VisitNamedDecl(FD);
    JOS.attribute("type", createQualType(FD->getType()));
    attributeOnlyIfTrue("mutable", FD->isMutable());
    attributeOnlyIfTrue("modulePrivate", FD->isModulePrivate());
    attributeOnlyIfTrue("isBitfield", FD->isBitField());
    attributeOnlyIfTrue("hasInClassInitializer", FD->hasInClassInitializer());
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitFunctionDecl(const FunctionDecl* FD)
{
    AST::Declaration* Decl = VisitNamedDecl(FD);
    JOS.attribute("type", createQualType(FD->getType()));
    StorageClass SC = FD->getStorageClass();
    if (SC != SC_None)
        JOS.attribute("storageClass", VarDecl::getStorageClassSpecifierString(SC));
    attributeOnlyIfTrue("inline", FD->isInlineSpecified());
    attributeOnlyIfTrue("virtual", FD->isVirtualAsWritten());
    attributeOnlyIfTrue("pure", FD->isPure()); // // TODO: clang 19 isPureVirtual
    attributeOnlyIfTrue("explicitlyDeleted", FD->isDeletedAsWritten());
    attributeOnlyIfTrue("constexpr", FD->isConstexpr());
    attributeOnlyIfTrue("variadic", FD->isVariadic());
    attributeOnlyIfTrue("immediate", FD->isImmediateFunction());

    if (FD->isDefaulted())
        JOS.attribute("explicitlyDefaulted",
                      FD->isDeleted() ? "deleted" : "default");

    // TODO: clang 19
    /*if (StringLiteral* Msg = FD->getDeletedMessage())
        JOS.attribute("deletedMessage", Msg->getString());*/
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitEnumDecl(const EnumDecl* ED)
{
    AST::Declaration* Decl = VisitNamedDecl(ED);
    if (ED->isFixed())
        JOS.attribute("fixedUnderlyingType", createQualType(ED->getIntegerType()));
    if (ED->isScoped())
        JOS.attribute("scopedEnumTag",
                      ED->isScopedUsingClassTag() ? "class" : "struct");
    return Decl;
}
AST::Declaration* ASTNodeVisitor::VisitEnumConstantDecl(const EnumConstantDecl* ECD)
{
    AST::Declaration* Decl = VisitNamedDecl(ECD);
    JOS.attribute("type", createQualType(ECD->getType()));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitRecordDecl(const RecordDecl* RD)
{
    AST::Declaration* Decl = VisitNamedDecl(RD);
    JOS.attribute("tagUsed", RD->getKindName());
    attributeOnlyIfTrue("completeDefinition", RD->isCompleteDefinition());
    return Decl;
}
AST::Declaration* ASTNodeVisitor::VisitCXXRecordDecl(const CXXRecordDecl* RD)
{
    return FindOrInsertLazy(RD, [&]() -> AST::Class*
                            {
                                VisitRecordDecl(RD);

                                // All other information requires a complete definition.
                                if (!RD->isCompleteDefinition())
                                    return nullptr;

                                JOS.attribute("definitionData", createCXXRecordDefinitionData(RD));
                                if (RD->getNumBases())
                                {
                                    JOS.attributeArray("bases", [this, RD]
                                                       {
                                                           for (const auto& Spec : RD->bases())
                                                               JOS.value(createCXXBaseSpecifier(Spec));
                                                       });
                                }

                                bool Process = false;
                                AST::Class* RC = parser.GetRecord(RD, Process);

                                if (!RC || !Process)
                                    return RC;

                                parser.WalkRecordCXX(RD, RC);
                                return RC;
                            });
}

AST::Declaration* ASTNodeVisitor::VisitHLSLBufferDecl(const HLSLBufferDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("bufferKind", D->isCBuffer() ? "cbuffer" : "tbuffer");
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitTemplateTypeParmDecl(const TemplateTypeParmDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("tagUsed", D->wasDeclaredWithTypename() ? "typename" : "class");
    JOS.attribute("depth", D->getDepth());
    JOS.attribute("index", D->getIndex());
    attributeOnlyIfTrue("isParameterPack", D->isParameterPack());

    // TODO: clang 19
    /*if (D->hasDefaultArgument())
        JOS.attributeObject("defaultArg", [=] {
        Visit(D->getDefaultArgument().getArgument(), SourceRange(),
            D->getDefaultArgStorage().getInheritedFrom(),
            D->defaultArgumentWasInherited() ? "inherited from" : "previous");
            });*/
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitNonTypeTemplateParmDecl(const NonTypeTemplateParmDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("type", createQualType(D->getType()));
    JOS.attribute("depth", D->getDepth());
    JOS.attribute("index", D->getIndex());
    attributeOnlyIfTrue("isParameterPack", D->isParameterPack());

    // TODO: clang 19
    /*if (D->hasDefaultArgument())
        JOS.attributeObject("defaultArg", [=] {
        Visit(D->getDefaultArgument().getArgument(), SourceRange(),
            D->getDefaultArgStorage().getInheritedFrom(),
            D->defaultArgumentWasInherited() ? "inherited from" : "previous");
            });*/
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitTemplateTemplateParmDecl(const TemplateTemplateParmDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("depth", D->getDepth());
    JOS.attribute("index", D->getIndex());
    attributeOnlyIfTrue("isParameterPack", D->isParameterPack());

    if (D->hasDefaultArgument())
        JOS.attributeObject("defaultArg", [&]
                            {
                                const auto* InheritedFrom = D->getDefaultArgStorage().getInheritedFrom();
                                Visit(D->getDefaultArgument().getArgument(),
                                      InheritedFrom ? InheritedFrom->getSourceRange() : clang::SourceLocation{},
                                      InheritedFrom,
                                      D->defaultArgumentWasInherited() ? "inherited from" : "previous");
                            });
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitLinkageSpecDecl(const LinkageSpecDecl* LSD)
{
    StringRef Lang;
    switch (LSD->getLanguage())
    {
        case LinkageSpecDecl::LanguageIDs::lang_c:
            Lang = "C";
            break;
        case LinkageSpecDecl::LanguageIDs::lang_cxx:
            Lang = "C++";
            break;
    }
    JOS.attribute("language", Lang);
    attributeOnlyIfTrue("hasBraces", LSD->hasBraces());
    return nullptr;
}

AST::Declaration* ASTNodeVisitor::VisitAccessSpecDecl(const AccessSpecDecl* ASD)
{
    JOS.attribute("access", createAccessSpecifier(ASD->getAccess()));
    return nullptr;
}

AST::Declaration* ASTNodeVisitor::VisitFriendDecl(const FriendDecl* FD)
{
    if (const TypeSourceInfo* T = FD->getFriendType())
        JOS.attribute("type", createQualType(T->getType()));
    // TODO: clang 19
    // attributeOnlyIfTrue("isPackExpansion", FD->isPackExpansion());
    return nullptr;
}

AST::Declaration* ASTNodeVisitor::VisitObjCIvarDecl(const ObjCIvarDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("type", createQualType(D->getType()));
    attributeOnlyIfTrue("synthesized", D->getSynthesize());
    switch (D->getAccessControl())
    {
        case ObjCIvarDecl::None: JOS.attribute("access", "none"); break;
        case ObjCIvarDecl::Private: JOS.attribute("access", "private"); break;
        case ObjCIvarDecl::Protected: JOS.attribute("access", "protected"); break;
        case ObjCIvarDecl::Public: JOS.attribute("access", "public"); break;
        case ObjCIvarDecl::Package: JOS.attribute("access", "package"); break;
    }
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCMethodDecl(const ObjCMethodDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("returnType", createQualType(D->getReturnType()));
    JOS.attribute("instance", D->isInstanceMethod());
    attributeOnlyIfTrue("variadic", D->isVariadic());
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCTypeParamDecl(const ObjCTypeParamDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("type", createQualType(D->getUnderlyingType()));
    attributeOnlyIfTrue("bounded", D->hasExplicitBound());
    switch (D->getVariance())
    {
        case ObjCTypeParamVariance::Invariant:
            break;
        case ObjCTypeParamVariance::Covariant:
            JOS.attribute("variance", "covariant");
            break;
        case ObjCTypeParamVariance::Contravariant:
            JOS.attribute("variance", "contravariant");
            break;
    }
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCCategoryDecl(const ObjCCategoryDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("interface", createBareDeclRef(D->getClassInterface()));
    JOS.attribute("implementation", createBareDeclRef(D->getImplementation()));

    llvm::json::Array Protocols;
    for (const auto* P : D->protocols())
        Protocols.push_back(createBareDeclRef(P));
    if (!Protocols.empty())
        JOS.attribute("protocols", std::move(Protocols));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCCategoryImplDecl(const ObjCCategoryImplDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("interface", createBareDeclRef(D->getClassInterface()));
    JOS.attribute("categoryDecl", createBareDeclRef(D->getCategoryDecl()));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCProtocolDecl(const ObjCProtocolDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);

    llvm::json::Array Protocols;
    for (const auto* P : D->protocols())
        Protocols.push_back(createBareDeclRef(P));
    if (!Protocols.empty())
        JOS.attribute("protocols", std::move(Protocols));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCInterfaceDecl(const ObjCInterfaceDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("super", createBareDeclRef(D->getSuperClass()));
    JOS.attribute("implementation", createBareDeclRef(D->getImplementation()));

    llvm::json::Array Protocols;
    for (const auto* P : D->protocols())
        Protocols.push_back(createBareDeclRef(P));
    if (!Protocols.empty())
        JOS.attribute("protocols", std::move(Protocols));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCImplementationDecl(
    const ObjCImplementationDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("super", createBareDeclRef(D->getSuperClass()));
    JOS.attribute("interface", createBareDeclRef(D->getClassInterface()));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCCompatibleAliasDecl(
    const ObjCCompatibleAliasDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("interface", createBareDeclRef(D->getClassInterface()));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCPropertyDecl(const ObjCPropertyDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D);
    JOS.attribute("type", createQualType(D->getType()));

    switch (D->getPropertyImplementation())
    {
        case ObjCPropertyDecl::None: break;
        case ObjCPropertyDecl::Required: JOS.attribute("control", "required"); break;
        case ObjCPropertyDecl::Optional: JOS.attribute("control", "optional"); break;
    }

    ObjCPropertyAttribute::Kind Attrs = D->getPropertyAttributes();
    if (Attrs != ObjCPropertyAttribute::kind_noattr)
    {
        if (Attrs & ObjCPropertyAttribute::kind_getter)
            JOS.attribute("getter", createBareDeclRef(D->getGetterMethodDecl()));
        if (Attrs & ObjCPropertyAttribute::kind_setter)
            JOS.attribute("setter", createBareDeclRef(D->getSetterMethodDecl()));
        attributeOnlyIfTrue("readonly",
                            Attrs & ObjCPropertyAttribute::kind_readonly);
        attributeOnlyIfTrue("assign", Attrs & ObjCPropertyAttribute::kind_assign);
        attributeOnlyIfTrue("readwrite",
                            Attrs & ObjCPropertyAttribute::kind_readwrite);
        attributeOnlyIfTrue("retain", Attrs & ObjCPropertyAttribute::kind_retain);
        attributeOnlyIfTrue("copy", Attrs & ObjCPropertyAttribute::kind_copy);
        attributeOnlyIfTrue("nonatomic",
                            Attrs & ObjCPropertyAttribute::kind_nonatomic);
        attributeOnlyIfTrue("atomic", Attrs & ObjCPropertyAttribute::kind_atomic);
        attributeOnlyIfTrue("weak", Attrs & ObjCPropertyAttribute::kind_weak);
        attributeOnlyIfTrue("strong", Attrs & ObjCPropertyAttribute::kind_strong);
        attributeOnlyIfTrue("unsafe_unretained",
                            Attrs & ObjCPropertyAttribute::kind_unsafe_unretained);
        attributeOnlyIfTrue("class", Attrs & ObjCPropertyAttribute::kind_class);
        attributeOnlyIfTrue("direct", Attrs & ObjCPropertyAttribute::kind_direct);
        attributeOnlyIfTrue("nullability",
                            Attrs & ObjCPropertyAttribute::kind_nullability);
        attributeOnlyIfTrue("null_resettable",
                            Attrs & ObjCPropertyAttribute::kind_null_resettable);
    }
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitObjCPropertyImplDecl(const ObjCPropertyImplDecl* D)
{
    AST::Declaration* Decl = VisitNamedDecl(D->getPropertyDecl());
    JOS.attribute("implKind", D->getPropertyImplementation() ==
                                      ObjCPropertyImplDecl::Synthesize ?
                                  "synthesize" :
                                  "dynamic");
    JOS.attribute("propertyDecl", createBareDeclRef(D->getPropertyDecl()));
    JOS.attribute("ivarDecl", createBareDeclRef(D->getPropertyIvarDecl()));
    return Decl;
}

AST::Declaration* ASTNodeVisitor::VisitBlockDecl(const BlockDecl* D)
{
    attributeOnlyIfTrue("variadic", D->isVariadic());
    attributeOnlyIfTrue("capturesThis", D->capturesCXXThis());
    return nullptr;
}

void ASTNodeVisitor::VisitAtomicExpr(const AtomicExpr* AE)
{
    // JOS.attribute("name", AE->getOp());
}

void ASTNodeVisitor::VisitObjCEncodeExpr(const ObjCEncodeExpr* OEE)
{
    JOS.attribute("encodedType", createQualType(OEE->getEncodedType()));
}

void ASTNodeVisitor::VisitObjCMessageExpr(const ObjCMessageExpr* OME)
{
    std::string Str;
    llvm::raw_string_ostream OS(Str);

    OME->getSelector().print(OS);
    JOS.attribute("selector", Str);

    switch (OME->getReceiverKind())
    {
        case ObjCMessageExpr::Instance:
            JOS.attribute("receiverKind", "instance");
            break;
        case ObjCMessageExpr::Class:
            JOS.attribute("receiverKind", "class");
            JOS.attribute("classType", createQualType(OME->getClassReceiver()));
            break;
        case ObjCMessageExpr::SuperInstance:
            JOS.attribute("receiverKind", "super (instance)");
            JOS.attribute("superType", createQualType(OME->getSuperType()));
            break;
        case ObjCMessageExpr::SuperClass:
            JOS.attribute("receiverKind", "super (class)");
            JOS.attribute("superType", createQualType(OME->getSuperType()));
            break;
    }

    QualType CallReturnTy = OME->getCallReturnType(Ctx);
    if (OME->getType() != CallReturnTy)
        JOS.attribute("callReturnType", createQualType(CallReturnTy));
}

void ASTNodeVisitor::VisitObjCBoxedExpr(const ObjCBoxedExpr* OBE)
{
    if (const ObjCMethodDecl* MD = OBE->getBoxingMethod())
    {
        std::string Str;
        llvm::raw_string_ostream OS(Str);

        MD->getSelector().print(OS);
        JOS.attribute("selector", Str);
    }
}

void ASTNodeVisitor::VisitObjCSelectorExpr(const ObjCSelectorExpr* OSE)
{
    std::string Str;
    llvm::raw_string_ostream OS(Str);

    OSE->getSelector().print(OS);
    JOS.attribute("selector", Str);
}

void ASTNodeVisitor::VisitObjCProtocolExpr(const ObjCProtocolExpr* OPE)
{
    JOS.attribute("protocol", createBareDeclRef(OPE->getProtocol()));
}

void ASTNodeVisitor::VisitObjCPropertyRefExpr(const ObjCPropertyRefExpr* OPRE)
{
    if (OPRE->isImplicitProperty())
    {
        JOS.attribute("propertyKind", "implicit");
        if (const ObjCMethodDecl* MD = OPRE->getImplicitPropertyGetter())
            JOS.attribute("getter", createBareDeclRef(MD));
        if (const ObjCMethodDecl* MD = OPRE->getImplicitPropertySetter())
            JOS.attribute("setter", createBareDeclRef(MD));
    }
    else
    {
        JOS.attribute("propertyKind", "explicit");
        JOS.attribute("property", createBareDeclRef(OPRE->getExplicitProperty()));
    }

    attributeOnlyIfTrue("isSuperReceiver", OPRE->isSuperReceiver());
    attributeOnlyIfTrue("isMessagingGetter", OPRE->isMessagingGetter());
    attributeOnlyIfTrue("isMessagingSetter", OPRE->isMessagingSetter());
}

void ASTNodeVisitor::VisitObjCSubscriptRefExpr(
    const ObjCSubscriptRefExpr* OSRE)
{
    JOS.attribute("subscriptKind",
                  OSRE->isArraySubscriptRefExpr() ? "array" : "dictionary");

    if (const ObjCMethodDecl* MD = OSRE->getAtIndexMethodDecl())
        JOS.attribute("getter", createBareDeclRef(MD));
    if (const ObjCMethodDecl* MD = OSRE->setAtIndexMethodDecl())
        JOS.attribute("setter", createBareDeclRef(MD));
}

void ASTNodeVisitor::VisitObjCIvarRefExpr(const ObjCIvarRefExpr* OIRE)
{
    JOS.attribute("decl", createBareDeclRef(OIRE->getDecl()));
    attributeOnlyIfTrue("isFreeIvar", OIRE->isFreeIvar());
    JOS.attribute("isArrow", OIRE->isArrow());
}

void ASTNodeVisitor::VisitObjCBoolLiteralExpr(const ObjCBoolLiteralExpr* OBLE)
{
    JOS.attribute("value", OBLE->getValue() ? "__objc_yes" : "__objc_no");
}

void ASTNodeVisitor::VisitDeclRefExpr(const DeclRefExpr* DRE)
{
    JOS.attribute("referencedDecl", createBareDeclRef(DRE->getDecl()));
    if (DRE->getDecl() != DRE->getFoundDecl())
        JOS.attribute("foundReferencedDecl",
                      createBareDeclRef(DRE->getFoundDecl()));
    switch (DRE->isNonOdrUse())
    {
        case NOUR_None: break;
        case NOUR_Unevaluated: JOS.attribute("nonOdrUseReason", "unevaluated"); break;
        case NOUR_Constant: JOS.attribute("nonOdrUseReason", "constant"); break;
        case NOUR_Discarded: JOS.attribute("nonOdrUseReason", "discarded"); break;
    }
    attributeOnlyIfTrue("isImmediateEscalating", DRE->isImmediateEscalating());
}

void ASTNodeVisitor::VisitSYCLUniqueStableNameExpr(
    const SYCLUniqueStableNameExpr* E)
{
    JOS.attribute("typeSourceInfo",
                  createQualType(E->getTypeSourceInfo()->getType()));
}

void ASTNodeVisitor::VisitPredefinedExpr(const PredefinedExpr* PE)
{
    JOS.attribute("name", PredefinedExpr::getIdentKindName(PE->getIdentKind()));
}

void ASTNodeVisitor::VisitUnaryOperator(const UnaryOperator* UO)
{
    JOS.attribute("isPostfix", UO->isPostfix());
    JOS.attribute("opcode", UnaryOperator::getOpcodeStr(UO->getOpcode()));
    if (!UO->canOverflow())
        JOS.attribute("canOverflow", false);
}

void ASTNodeVisitor::VisitBinaryOperator(const BinaryOperator* BO)
{
    JOS.attribute("opcode", BinaryOperator::getOpcodeStr(BO->getOpcode()));
}

void ASTNodeVisitor::VisitCompoundAssignOperator(
    const CompoundAssignOperator* CAO)
{
    VisitBinaryOperator(CAO);
    JOS.attribute("computeLHSType", createQualType(CAO->getComputationLHSType()));
    JOS.attribute("computeResultType",
                  createQualType(CAO->getComputationResultType()));
}

void ASTNodeVisitor::VisitMemberExpr(const MemberExpr* ME)
{
    // Note, we always write this Boolean field because the information it conveys
    // is critical to understanding the AST node.
    ValueDecl* VD = ME->getMemberDecl();
    JOS.attribute("name", VD && VD->getDeclName() ? VD->getNameAsString() : "");
    JOS.attribute("isArrow", ME->isArrow());
    JOS.attribute("referencedMemberDecl", createPointerRepresentation(VD));
    switch (ME->isNonOdrUse())
    {
        case NOUR_None: break;
        case NOUR_Unevaluated: JOS.attribute("nonOdrUseReason", "unevaluated"); break;
        case NOUR_Constant: JOS.attribute("nonOdrUseReason", "constant"); break;
        case NOUR_Discarded: JOS.attribute("nonOdrUseReason", "discarded"); break;
    }
}

void ASTNodeVisitor::VisitCXXNewExpr(const CXXNewExpr* NE)
{
    attributeOnlyIfTrue("isGlobal", NE->isGlobalNew());
    attributeOnlyIfTrue("isArray", NE->isArray());
    attributeOnlyIfTrue("isPlacement", NE->getNumPlacementArgs() != 0);
    switch (NE->getInitializationStyle())
    {
        case CXXNewExpr::InitializationStyle::NoInit:
            break;
        case CXXNewExpr::InitializationStyle::CallInit: // Parens
            JOS.attribute("initStyle", "call");
            break;
        case CXXNewExpr::InitializationStyle::ListInit: // Braces
            JOS.attribute("initStyle", "list");
            break;
    }
    if (const FunctionDecl* FD = NE->getOperatorNew())
        JOS.attribute("operatorNewDecl", createBareDeclRef(FD));
    if (const FunctionDecl* FD = NE->getOperatorDelete())
        JOS.attribute("operatorDeleteDecl", createBareDeclRef(FD));
}
void ASTNodeVisitor::VisitCXXDeleteExpr(const CXXDeleteExpr* DE)
{
    attributeOnlyIfTrue("isGlobal", DE->isGlobalDelete());
    attributeOnlyIfTrue("isArray", DE->isArrayForm());
    attributeOnlyIfTrue("isArrayAsWritten", DE->isArrayFormAsWritten());
    if (const FunctionDecl* FD = DE->getOperatorDelete())
        JOS.attribute("operatorDeleteDecl", createBareDeclRef(FD));
}

void ASTNodeVisitor::VisitCXXThisExpr(const CXXThisExpr* TE)
{
    attributeOnlyIfTrue("implicit", TE->isImplicit());
}

void ASTNodeVisitor::VisitCastExpr(const CastExpr* CE)
{
    JOS.attribute("castKind", CE->getCastKindName());
    llvm::json::Array Path = createCastPath(CE);
    if (!Path.empty())
        JOS.attribute("path", std::move(Path));
    // FIXME: This may not be useful information as it can be obtusely gleaned
    // from the inner[] array.
    if (const NamedDecl* ND = CE->getConversionFunction())
        JOS.attribute("conversionFunc", createBareDeclRef(ND));
}

void ASTNodeVisitor::VisitImplicitCastExpr(const ImplicitCastExpr* ICE)
{
    VisitCastExpr(ICE);
    attributeOnlyIfTrue("isPartOfExplicitCast", ICE->isPartOfExplicitCast());
}

void ASTNodeVisitor::VisitCallExpr(const CallExpr* CE)
{
    attributeOnlyIfTrue("adl", CE->usesADL());
}

void ASTNodeVisitor::VisitUnaryExprOrTypeTraitExpr(
    const UnaryExprOrTypeTraitExpr* TTE)
{
    JOS.attribute("name", getTraitSpelling(TTE->getKind()));
    if (TTE->isArgumentType())
        JOS.attribute("argType", createQualType(TTE->getArgumentType()));
}

void ASTNodeVisitor::VisitSizeOfPackExpr(const SizeOfPackExpr* SOPE)
{
    VisitNamedDecl(SOPE->getPack());
}

void ASTNodeVisitor::VisitUnresolvedLookupExpr(
    const UnresolvedLookupExpr* ULE)
{
    JOS.attribute("usesADL", ULE->requiresADL());
    JOS.attribute("name", ULE->getName().getAsString());

    JOS.attributeArray("lookups", [this, ULE]
                       {
                           for (const NamedDecl* D : ULE->decls())
                               JOS.value(createBareDeclRef(D));
                       });
}

void ASTNodeVisitor::VisitAddrLabelExpr(const AddrLabelExpr* ALE)
{
    JOS.attribute("name", ALE->getLabel()->getName());
    JOS.attribute("labelDeclId", createPointerRepresentation(ALE->getLabel()));
}

void ASTNodeVisitor::VisitCXXTypeidExpr(const CXXTypeidExpr* CTE)
{
    if (CTE->isTypeOperand())
    {
        QualType Adjusted = CTE->getTypeOperand(Ctx);
        QualType Unadjusted = CTE->getTypeOperandSourceInfo()->getType();
        JOS.attribute("typeArg", createQualType(Unadjusted));
        if (Adjusted != Unadjusted)
            JOS.attribute("adjustedTypeArg", createQualType(Adjusted));
    }
}

void ASTNodeVisitor::VisitConstantExpr(const ConstantExpr* CE)
{
    if (CE->getResultAPValueKind() != APValue::None)
        Visit(CE->getAPValueResult(), CE->getType());
}

void ASTNodeVisitor::VisitInitListExpr(const InitListExpr* ILE)
{
    if (const FieldDecl* FD = ILE->getInitializedFieldInUnion())
        JOS.attribute("field", createBareDeclRef(FD));
}

void ASTNodeVisitor::VisitGenericSelectionExpr(
    const GenericSelectionExpr* GSE)
{
    attributeOnlyIfTrue("resultDependent", GSE->isResultDependent());
}

void ASTNodeVisitor::VisitCXXUnresolvedConstructExpr(
    const CXXUnresolvedConstructExpr* UCE)
{
    if (UCE->getType() != UCE->getTypeAsWritten())
        JOS.attribute("typeAsWritten", createQualType(UCE->getTypeAsWritten()));
    attributeOnlyIfTrue("list", UCE->isListInitialization());
}

void ASTNodeVisitor::VisitCXXConstructExpr(const CXXConstructExpr* CE)
{
    CXXConstructorDecl* Ctor = CE->getConstructor();
    JOS.attribute("ctorType", createQualType(Ctor->getType()));
    attributeOnlyIfTrue("elidable", CE->isElidable());
    attributeOnlyIfTrue("list", CE->isListInitialization());
    attributeOnlyIfTrue("initializer_list", CE->isStdInitListInitialization());
    attributeOnlyIfTrue("zeroing", CE->requiresZeroInitialization());
    attributeOnlyIfTrue("hadMultipleCandidates", CE->hadMultipleCandidates());
    attributeOnlyIfTrue("isImmediateEscalating", CE->isImmediateEscalating());

    switch (CE->getConstructionKind())
    {
        case CXXConstructExpr::ConstructionKind::CK_Complete:
            JOS.attribute("constructionKind", "complete");
            break;
        case CXXConstructExpr::ConstructionKind::CK_Delegating:
            JOS.attribute("constructionKind", "delegating");
            break;
        case CXXConstructExpr::ConstructionKind::CK_NonVirtualBase:
            JOS.attribute("constructionKind", "non-virtual base");
            break;
        case CXXConstructExpr::ConstructionKind::CK_VirtualBase:
            JOS.attribute("constructionKind", "virtual base");
            break;
    }
}

void ASTNodeVisitor::VisitExprWithCleanups(const ExprWithCleanups* EWC)
{
    attributeOnlyIfTrue("cleanupsHaveSideEffects",
                        EWC->cleanupsHaveSideEffects());
    if (EWC->getNumObjects())
    {
        JOS.attributeArray("cleanups", [this, EWC]
                           {
                               for (const ExprWithCleanups::CleanupObject& CO : EWC->getObjects())
                                   if (auto* BD = CO.dyn_cast<BlockDecl*>())
                                   {
                                       JOS.value(createBareDeclRef(BD));
                                   }
                                   else if (auto* CLE = CO.dyn_cast<CompoundLiteralExpr*>())
                                   {
                                       llvm::json::Object Obj;
                                       Obj["id"] = createPointerRepresentation(CLE);
                                       Obj["kind"] = CLE->getStmtClassName();
                                       JOS.value(std::move(Obj));
                                   }
                                   else
                                   {
                                       llvm_unreachable("unexpected cleanup object type");
                                   }
                           });
    }
}

void ASTNodeVisitor::VisitCXXBindTemporaryExpr(
    const CXXBindTemporaryExpr* BTE)
{
    const CXXTemporary* Temp = BTE->getTemporary();
    JOS.attribute("temp", createPointerRepresentation(Temp));
    if (const CXXDestructorDecl* Dtor = Temp->getDestructor())
        JOS.attribute("dtor", createBareDeclRef(Dtor));
}

void ASTNodeVisitor::VisitMaterializeTemporaryExpr(
    const MaterializeTemporaryExpr* MTE)
{
    if (const ValueDecl* VD = MTE->getExtendingDecl())
        JOS.attribute("extendingDecl", createBareDeclRef(VD));

    switch (MTE->getStorageDuration())
    {
        case SD_Automatic:
            JOS.attribute("storageDuration", "automatic");
            break;
        case SD_Dynamic:
            JOS.attribute("storageDuration", "dynamic");
            break;
        case SD_FullExpression:
            JOS.attribute("storageDuration", "full expression");
            break;
        case SD_Static:
            JOS.attribute("storageDuration", "static");
            break;
        case SD_Thread:
            JOS.attribute("storageDuration", "thread");
            break;
    }

    attributeOnlyIfTrue("boundToLValueRef", MTE->isBoundToLvalueReference());
}

void ASTNodeVisitor::VisitCXXDefaultArgExpr(const CXXDefaultArgExpr* Node)
{
    attributeOnlyIfTrue("hasRewrittenInit", Node->hasRewrittenInit());
}

void ASTNodeVisitor::VisitCXXDefaultInitExpr(const CXXDefaultInitExpr* Node)
{
    attributeOnlyIfTrue("hasRewrittenInit", Node->hasRewrittenInit());
}

void ASTNodeVisitor::VisitCXXDependentScopeMemberExpr(
    const CXXDependentScopeMemberExpr* DSME)
{
    JOS.attribute("isArrow", DSME->isArrow());
    JOS.attribute("member", DSME->getMember().getAsString());
    attributeOnlyIfTrue("hasTemplateKeyword", DSME->hasTemplateKeyword());
    attributeOnlyIfTrue("hasExplicitTemplateArgs",
                        DSME->hasExplicitTemplateArgs());

    if (DSME->getNumTemplateArgs())
    {
        JOS.attributeArray("explicitTemplateArgs", [DSME, this]
                           {
                               for (const TemplateArgumentLoc& TAL : DSME->template_arguments())
                                   JOS.object(
                                       [&TAL, this]
                                       {
                                           Visit(TAL.getArgument(), TAL.getSourceRange());
                                       });
                           });
    }
}

void ASTNodeVisitor::VisitRequiresExpr(const RequiresExpr* RE)
{
    if (!RE->isValueDependent())
        JOS.attribute("satisfied", RE->isSatisfied());
}

void ASTNodeVisitor::VisitIntegerLiteral(const IntegerLiteral* IL)
{
    llvm::SmallString<16> Buffer;
    IL->getValue().toString(Buffer,
                            /*Radix=*/10, IL->getType()->isSignedIntegerType());
    JOS.attribute("value", Buffer);
}
void ASTNodeVisitor::VisitCharacterLiteral(const CharacterLiteral* CL)
{
    // FIXME: This should probably print the character literal as a string,
    // rather than as a numerical value. It would be nice if the behavior matched
    // what we do to print a string literal; right now, it is impossible to tell
    // the difference between 'a' and L'a' in C from the JSON output.
    JOS.attribute("value", CL->getValue());
}
void ASTNodeVisitor::VisitFixedPointLiteral(const FixedPointLiteral* FPL)
{
    JOS.attribute("value", FPL->getValueAsString(/*Radix=*/10));
}
void ASTNodeVisitor::VisitFloatingLiteral(const FloatingLiteral* FL)
{
    llvm::SmallString<16> Buffer;
    FL->getValue().toString(Buffer);
    JOS.attribute("value", Buffer);
}
void ASTNodeVisitor::VisitStringLiteral(const StringLiteral* SL)
{
    std::string Buffer;
    llvm::raw_string_ostream SS(Buffer);
    SL->outputString(SS);
    JOS.attribute("value", Buffer);
}
void ASTNodeVisitor::VisitCXXBoolLiteralExpr(const CXXBoolLiteralExpr* BLE)
{
    JOS.attribute("value", BLE->getValue());
}

void ASTNodeVisitor::VisitIfStmt(const IfStmt* IS)
{
    attributeOnlyIfTrue("hasInit", IS->hasInitStorage());
    attributeOnlyIfTrue("hasVar", IS->hasVarStorage());
    attributeOnlyIfTrue("hasElse", IS->hasElseStorage());
    attributeOnlyIfTrue("isConstexpr", IS->isConstexpr());
    attributeOnlyIfTrue("isConsteval", IS->isConsteval());
    attributeOnlyIfTrue("constevalIsNegated", IS->isNegatedConsteval());
}

void ASTNodeVisitor::VisitSwitchStmt(const SwitchStmt* SS)
{
    attributeOnlyIfTrue("hasInit", SS->hasInitStorage());
    attributeOnlyIfTrue("hasVar", SS->hasVarStorage());
}
void ASTNodeVisitor::VisitCaseStmt(const CaseStmt* CS)
{
    attributeOnlyIfTrue("isGNURange", CS->caseStmtIsGNURange());
}

void ASTNodeVisitor::VisitLabelStmt(const LabelStmt* LS)
{
    JOS.attribute("name", LS->getName());
    JOS.attribute("declId", createPointerRepresentation(LS->getDecl()));
    attributeOnlyIfTrue("sideEntry", LS->isSideEntry());
}
void ASTNodeVisitor::VisitGotoStmt(const GotoStmt* GS)
{
    JOS.attribute("targetLabelDeclId",
                  createPointerRepresentation(GS->getLabel()));
}

void ASTNodeVisitor::VisitWhileStmt(const WhileStmt* WS)
{
    attributeOnlyIfTrue("hasVar", WS->hasVarStorage());
}

void ASTNodeVisitor::VisitObjCAtCatchStmt(const ObjCAtCatchStmt* OACS)
{
    // FIXME: it would be nice for the ASTNodeTraverser would handle the catch
    // parameter the same way for C++ and ObjC rather. In this case, C++ gets a
    // null child node and ObjC gets no child node.
    attributeOnlyIfTrue("isCatchAll", OACS->getCatchParamDecl() == nullptr);
}

void ASTNodeVisitor::VisitNullTemplateArgument(const TemplateArgument& TA)
{
    JOS.attribute("isNull", true);
}
void ASTNodeVisitor::VisitTypeTemplateArgument(const TemplateArgument& TA)
{
    JOS.attribute("type", createQualType(TA.getAsType()));
}
void ASTNodeVisitor::VisitDeclarationTemplateArgument(
    const TemplateArgument& TA)
{
    JOS.attribute("decl", createBareDeclRef(TA.getAsDecl()));
}
void ASTNodeVisitor::VisitNullPtrTemplateArgument(const TemplateArgument& TA)
{
    JOS.attribute("isNullptr", true);
}
void ASTNodeVisitor::VisitIntegralTemplateArgument(const TemplateArgument& TA)
{
    JOS.attribute("value", TA.getAsIntegral().getSExtValue());
}
void ASTNodeVisitor::VisitTemplateTemplateArgument(const TemplateArgument& TA)
{
    // FIXME: cannot just call dump() on the argument, as that doesn't specify
    // the output format.
}
void ASTNodeVisitor::VisitTemplateExpansionTemplateArgument(
    const TemplateArgument& TA)
{
    // FIXME: cannot just call dump() on the argument, as that doesn't specify
    // the output format.
}
void ASTNodeVisitor::VisitExpressionTemplateArgument(
    const TemplateArgument& TA)
{
    JOS.attribute("isExpr", true);
}
void ASTNodeVisitor::VisitPackTemplateArgument(const TemplateArgument& TA)
{
    JOS.attribute("isPack", true);
}

StringRef ASTNodeVisitor::getCommentCommandName(unsigned CommandID) const
{
    return Traits.getCommandInfo(CommandID)->Name;
}

void ASTNodeVisitor::visitTextComment(const comments::TextComment* C,
                                      const comments::FullComment*)
{
    JOS.attribute("text", C->getText());
}

void ASTNodeVisitor::visitInlineCommandComment(
    const comments::InlineCommandComment* C,
    const comments::FullComment*)
{
    JOS.attribute("name", getCommentCommandName(C->getCommandID()));

    switch (C->getRenderKind())
    {
        case comments::InlineCommandComment::RenderKind::RenderNormal:
            JOS.attribute("renderKind", "normal");
            break;
        case comments::InlineCommandComment::RenderKind::RenderBold:
            JOS.attribute("renderKind", "bold");
            break;
        case comments::InlineCommandComment::RenderKind::RenderEmphasized:
            JOS.attribute("renderKind", "emphasized");
            break;
        case comments::InlineCommandComment::RenderKind::RenderMonospaced:
            JOS.attribute("renderKind", "monospaced");
            break;
        case comments::InlineCommandComment::RenderKind::RenderAnchor:
            JOS.attribute("renderKind", "anchor");
            break;
    }

    llvm::json::Array Args;
    for (unsigned I = 0, E = C->getNumArgs(); I < E; ++I)
        Args.push_back(C->getArgText(I));

    if (!Args.empty())
        JOS.attribute("args", std::move(Args));
}

void ASTNodeVisitor::visitHTMLStartTagComment(
    const comments::HTMLStartTagComment* C,
    const comments::FullComment*)
{
    JOS.attribute("name", C->getTagName());
    attributeOnlyIfTrue("selfClosing", C->isSelfClosing());
    attributeOnlyIfTrue("malformed", C->isMalformed());

    llvm::json::Array Attrs;
    for (unsigned I = 0, E = C->getNumAttrs(); I < E; ++I)
        Attrs.push_back(
            { { "name", C->getAttr(I).Name }, { "value", C->getAttr(I).Value } });

    if (!Attrs.empty())
        JOS.attribute("attrs", std::move(Attrs));
}

void ASTNodeVisitor::visitHTMLEndTagComment(
    const comments::HTMLEndTagComment* C,
    const comments::FullComment*)
{
    JOS.attribute("name", C->getTagName());
}

void ASTNodeVisitor::visitBlockCommandComment(
    const comments::BlockCommandComment* C,
    const comments::FullComment*)
{
    JOS.attribute("name", getCommentCommandName(C->getCommandID()));

    llvm::json::Array Args;
    for (unsigned I = 0, E = C->getNumArgs(); I < E; ++I)
        Args.push_back(C->getArgText(I));

    if (!Args.empty())
        JOS.attribute("args", std::move(Args));
}

void ASTNodeVisitor::visitParamCommandComment(
    const comments::ParamCommandComment* C,
    const comments::FullComment* FC)
{
    switch (C->getDirection())
    {
        case comments::ParamCommandComment::PassDirection::In:
            JOS.attribute("direction", "in");
            break;
        case comments::ParamCommandComment::PassDirection::Out:
            JOS.attribute("direction", "out");
            break;
        case comments::ParamCommandComment::PassDirection::InOut:
            JOS.attribute("direction", "in,out");
            break;
    }
    attributeOnlyIfTrue("explicit", C->isDirectionExplicit());

    if (C->hasParamName())
        JOS.attribute("param", C->isParamIndexValid() ? C->getParamName(FC) : C->getParamNameAsWritten());

    if (C->isParamIndexValid() && !C->isVarArgParam())
        JOS.attribute("paramIdx", C->getParamIndex());
}

void ASTNodeVisitor::visitTParamCommandComment(
    const comments::TParamCommandComment* C,
    const comments::FullComment* FC)
{
    if (C->hasParamName())
        JOS.attribute("param", C->isPositionValid() ? C->getParamName(FC) : C->getParamNameAsWritten());
    if (C->isPositionValid())
    {
        llvm::json::Array Positions;
        for (unsigned I = 0, E = C->getDepth(); I < E; ++I)
            Positions.push_back(C->getIndex(I));

        if (!Positions.empty())
            JOS.attribute("positions", std::move(Positions));
    }
}

void ASTNodeVisitor::visitVerbatimBlockComment(
    const comments::VerbatimBlockComment* C,
    const comments::FullComment*)
{
    JOS.attribute("name", getCommentCommandName(C->getCommandID()));
    JOS.attribute("closeName", C->getCloseName());
}

void ASTNodeVisitor::visitVerbatimBlockLineComment(
    const comments::VerbatimBlockLineComment* C,
    const comments::FullComment*)
{
    JOS.attribute("text", C->getText());
}

void ASTNodeVisitor::visitVerbatimLineComment(
    const comments::VerbatimLineComment* C,
    const comments::FullComment*)
{
    JOS.attribute("text", C->getText());
}

llvm::json::Object ASTNodeVisitor::createFPOptions(FPOptionsOverride FPO)
{
    llvm::json::Object Ret;
#define OPTION(NAME, TYPE, WIDTH, PREVIOUS) \
    if (FPO.has##NAME##Override())          \
        Ret.try_emplace(#NAME, static_cast<unsigned>(FPO.get##NAME##Override()));
#include "clang/Basic/FPOptions.def"

    return Ret;
}

void ASTNodeVisitor::VisitCompoundStmt(const CompoundStmt* S)
{
    VisitStmt(S);
    if (S->hasStoredFPFeatures())
        JOS.attribute("fpoptions", createFPOptions(S->getStoredFPFeatures()));
}