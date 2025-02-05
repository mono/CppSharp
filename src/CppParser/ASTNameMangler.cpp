/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "ASTNameMangler.h"

#include <clang/AST/GlobalDecl.h>
#include <clang/AST/Mangle.h>
#include <clang/Basic/TargetInfo.h>
#include <llvm/IR/Mangler.h>

using namespace clang;
using namespace CppSharp::CppParser;

namespace {
    enum ObjCKind {
        ObjCClass,
        ObjCMetaclass,
    };

    StringRef getClassSymbolPrefix(ObjCKind Kind, const ASTContext& Context) {
        if (Context.getLangOpts().ObjCRuntime.isGNUFamily())
            return Kind == ObjCMetaclass ? "_OBJC_METACLASS_" : "_OBJC_CLASS_";
        return Kind == ObjCMetaclass ? "OBJC_METACLASS_$_" : "OBJC_CLASS_$_";
    }

    void WriteObjCClassName(const ObjCInterfaceDecl* D, raw_ostream& OS) {
        OS << getClassSymbolPrefix(ObjCClass, D->getASTContext());
        OS << D->getObjCRuntimeNameAsString();
    }
}

ASTNameMangler::ASTNameMangler(ASTContext& Ctx)
    : MC(Ctx.createMangleContext())
    , DL(Ctx.getTargetInfo().getDataLayoutString())
{
}

std::string ASTNameMangler::GetName(const Decl* D) {
    std::string Name;
    {
        llvm::raw_string_ostream OS(Name);
        WriteName(D, OS);
    }
    return Name;
}

bool ASTNameMangler::WriteName(const Decl* D, raw_ostream& OS) {
    // First apply frontend mangling.
    SmallString<128> FrontendBuf;
    llvm::raw_svector_ostream FrontendBufOS(FrontendBuf);
    if (auto* FD = dyn_cast<FunctionDecl>(D)) {
        if (FD->isDependentContext())
            return true;
        if (WriteFuncOrVarName(FD, FrontendBufOS))
            return true;
    }
    else if (auto* VD = dyn_cast<VarDecl>(D)) {
        if (WriteFuncOrVarName(VD, FrontendBufOS))
            return true;
    }
    else if (auto* MD = dyn_cast<ObjCMethodDecl>(D)) {
        MC->mangleObjCMethodName(MD, OS, /*includePrefixByte=*/false,
            /*includeCategoryNamespace=*/true);
        return false;
    }
    else if (auto* ID = dyn_cast<ObjCInterfaceDecl>(D)) {
        WriteObjCClassName(ID, FrontendBufOS);
    }
    else {
        return true;
    }

    // Now apply backend mangling.
    llvm::Mangler::getNameWithPrefix(OS, FrontendBufOS.str(), DL);
    return false;
}

std::string ASTNameMangler::GetMangledStructor(const NamedDecl* ND, unsigned StructorType) {
    std::string FrontendBuf;
    llvm::raw_string_ostream FOS(FrontendBuf);

    GlobalDecl GD;
    if (const auto* CD = dyn_cast_or_null<CXXConstructorDecl>(ND))
        GD = GlobalDecl(CD, static_cast<CXXCtorType>(StructorType));
    else if (const auto* DD = dyn_cast_or_null<CXXDestructorDecl>(ND))
        GD = GlobalDecl(DD, static_cast<CXXDtorType>(StructorType));
    MC->mangleName(GD, FOS);

    std::string BackendBuf;
    llvm::raw_string_ostream BOS(BackendBuf);

    llvm::Mangler::getNameWithPrefix(BOS, FrontendBuf, DL);

    return BackendBuf;
}

std::string ASTNameMangler::GetMangledThunk(const CXXMethodDecl* MD, const ThunkInfo& T, bool /*ElideOverrideInfo*/) {
    std::string FrontendBuf;
    llvm::raw_string_ostream FOS(FrontendBuf);

    // TODO: Enable `ElideOverrideInfo` param if clang is updated to 19
    MC->mangleThunk(MD, T, /*ElideOverrideInfo,*/ FOS);

    std::string BackendBuf;
    llvm::raw_string_ostream BOS(BackendBuf);

    llvm::Mangler::getNameWithPrefix(BOS, FrontendBuf, DL);

    return BackendBuf;
}

bool ASTNameMangler::WriteFuncOrVarName(const NamedDecl* D, raw_ostream& OS) const
{
    if (!MC->shouldMangleDeclName(D)) {
        const IdentifierInfo* II = D->getIdentifier();
        if (!II)
            return true;
        OS << II->getName();
        return false;
    }

    GlobalDecl GD;
    if (const auto* CtorD = dyn_cast<CXXConstructorDecl>(D))
        GD = GlobalDecl(CtorD, Ctor_Complete);
    else if (const auto* DtorD = dyn_cast<CXXDestructorDecl>(D))
        GD = GlobalDecl(DtorD, Dtor_Complete);
    else if (D->hasAttr<CUDAGlobalAttr>())
        GD = GlobalDecl(cast<FunctionDecl>(D));
    else
        GD = GlobalDecl(D);

    MC->mangleName(GD, OS);
    return false;
}
