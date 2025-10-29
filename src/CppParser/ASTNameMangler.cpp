/************************************************************************
 *
 * CppSharp
 * Licensed under the simplified BSD license. All rights reserved.
 *
 ************************************************************************/

#include "ASTNameMangler.h"

#include <clang/AST/GlobalDecl.h>
#include <clang/AST/Mangle.h>
#include <clang/AST/VTableBuilder.h>
#include <clang/Basic/TargetInfo.h>
#include <llvm/IR/Mangler.h>

using namespace clang;
using namespace CppSharp::CppParser;

namespace {
enum ObjCKind
{
    ObjCClass,
    ObjCMetaclass,
};

StringRef getClassSymbolPrefix(ObjCKind Kind, const ASTContext& Context)
{
    if (Context.getLangOpts().ObjCRuntime.isGNUFamily())
        return Kind == ObjCMetaclass ? "_OBJC_METACLASS_" : "_OBJC_CLASS_";
    return Kind == ObjCMetaclass ? "OBJC_METACLASS_$_" : "OBJC_CLASS_$_";
}

void WriteObjCClassName(const ObjCInterfaceDecl* D, raw_ostream& OS)
{
    OS << getClassSymbolPrefix(ObjCClass, D->getASTContext());
    OS << D->getObjCRuntimeNameAsString();
}
} // namespace

ASTNameMangler::ASTNameMangler(ASTContext& Ctx)
    : DL(Ctx.getTargetInfo().getDataLayoutString())
    , MC(Ctx.createMangleContext())
{
}

std::string ASTNameMangler::GetName(const Decl* D) const
{
    std::string Name;
    {
        llvm::raw_string_ostream OS(Name);
        WriteName(D, OS);
    }
    return Name;
}

bool ASTNameMangler::WriteName(const Decl* D, raw_ostream& OS) const
{
    if (auto* FD = dyn_cast<FunctionDecl>(D))
    {
        if (FD->isDependentContext())
            return true;
        if (WriteFuncOrVarName(FD, OS))
            return true;
    }
    else if (auto* VD = dyn_cast<VarDecl>(D))
    {
        if (WriteFuncOrVarName(VD, OS))
            return true;
    }
    else if (auto* MD = dyn_cast<ObjCMethodDecl>(D))
    {
        MC->mangleObjCMethodName(MD, OS, /*includePrefixByte=*/false, /*includeCategoryNamespace=*/true);
        return false;
    }
    else if (auto* ID = dyn_cast<ObjCInterfaceDecl>(D))
    {
        WriteObjCClassName(ID, OS);
    }
    else
    {
        return true;
    }

    return false;
}

std::string ASTNameMangler::GetMangledStructor(const NamedDecl* ND, unsigned StructorType) const
{
    std::string FrontendBuf;
    llvm::raw_string_ostream FOS(FrontendBuf);

    GlobalDecl GD;
    if (const auto* CD = dyn_cast_or_null<CXXConstructorDecl>(ND))
        GD = GlobalDecl(CD, static_cast<CXXCtorType>(StructorType));
    else if (const auto* DD = dyn_cast_or_null<CXXDestructorDecl>(ND))
        GD = GlobalDecl(DD, static_cast<CXXDtorType>(StructorType));
    MC->mangleName(GD, FOS);

    return FrontendBuf;
}

std::string ASTNameMangler::GetMangledThunk(const CXXMethodDecl* MD, const ThunkInfo& T, bool ElideOverrideInfo) const
{
    std::string FrontendBuf;
    llvm::raw_string_ostream FOS(FrontendBuf);

    // TODO: Enable `ElideOverrideInfo` param if clang is updated to 19

#if LLVM_VERSION_MAJOR >= 19
    MC->mangleThunk(MD, T, ElideOverrideInfo, FOS);
#else
    MC->mangleThunk(MD, T, FOS);
#endif
    return FrontendBuf;
}

bool ASTNameMangler::WriteFuncOrVarName(const NamedDecl* D, raw_ostream& OS) const
{
    if (!MC->shouldMangleDeclName(D))
    {
        const IdentifierInfo* II = D->getIdentifier();
        if (!II)
            return true;
        OS << II->getName();
        return false;
    }

    GlobalDecl GD;
    if (const auto* CtorD = dyn_cast<CXXConstructorDecl>(D))
        GD = GlobalDecl(CtorD, Ctor_Base);
    else if (const auto* DtorD = dyn_cast<CXXDestructorDecl>(D))
        GD = GlobalDecl(DtorD, Dtor_Base);
    else if (D->hasAttr<CUDAGlobalAttr>())
        GD = GlobalDecl(cast<FunctionDecl>(D));
    else
        GD = GlobalDecl(D);

    MC->mangleName(GD, OS);
    return false;
}
