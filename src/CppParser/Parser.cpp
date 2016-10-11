/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#ifdef DEBUG
#undef DEBUG // workaround DEBUG define messing with LLVM COFF headers
#endif

#include "Parser.h"
#include "ELFDumper.h"

#include <llvm/Support/Host.h>
#include <llvm/Support/Path.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/Object/Archive.h>
#include <llvm/Object/COFF.h>
#include <llvm/Object/ObjectFile.h>
#include <llvm/Object/ELFObjectFile.h>
#include <llvm/Object/MachO.h>
#include <llvm/Option/ArgList.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/IR/Module.h>
#include <llvm/IR/DataLayout.h>
#include <clang/Basic/Version.h>
#include <clang/Config/config.h>
#include <clang/AST/ASTContext.h>
#include <clang/AST/Comment.h>
#include <clang/AST/DeclFriend.h>
#include <clang/AST/ExprCXX.h>
#include <clang/Lex/DirectoryLookup.h>
#include <clang/Lex/HeaderSearch.h>
#include <clang/Lex/Preprocessor.h>
#include <clang/Lex/PreprocessorOptions.h>
#include <clang/Lex/PreprocessingRecord.h>
#include <clang/Parse/ParseAST.h>
#include <clang/Sema/Sema.h>
#include <clang/Sema/SemaConsumer.h>
#include <clang/Frontend/Utils.h>
#include <clang/Driver/Driver.h>
#include <clang/Driver/ToolChain.h>
#include <clang/Driver/Util.h>
#include <clang/Index/USRGeneration.h>
#include <CodeGen/CodeGenModule.h>
#include <CodeGen/CodeGenTypes.h>
#include <CodeGen/TargetInfo.h>
#include <CodeGen/CGCall.h>
#include <CodeGen/CGCXXABI.h>
#include <Driver/ToolChains.h>

#if defined(__APPLE__) || defined(__linux__)
#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif
#include <dlfcn.h>

#define HAVE_DLFCN
#endif

using namespace CppSharp::CppParser;

// We use this as a placeholder for pointer values that should be ignored.
void* IgnorePtr = (void*) 0x1;

//-----------------------------------//

Parser::Parser(CppParserOptions* Opts) : Lib(Opts->ASTContext), Opts(Opts), Index(0)
{
}

//-----------------------------------//

std::string GetCurrentLibraryDir()
{
#ifdef HAVE_DLFCN
    Dl_info dl_info;
    dladdr((void *)GetCurrentLibraryDir, &dl_info);
    return dl_info.dli_fname;
#else
    return ".";
#endif
}

LayoutField Parser::WalkVTablePointer(Class* Class,
    const clang::CharUnits& Offset, const std::string& prefix)
{
    LayoutField LayoutField;
    LayoutField.Offset = Offset.getQuantity();
    LayoutField.Name = prefix + "_" + Class->Name;
    LayoutField.QualifiedType = GetQualifiedType(C->getASTContext().VoidPtrTy);
    return LayoutField;
}

void Parser::ReadClassLayout(Class* Class, const clang::RecordDecl* RD,
    clang::CharUnits Offset, bool IncludeVirtualBases)
{
    using namespace clang;

    const auto &Layout = C->getASTContext().getASTRecordLayout(RD);
    auto CXXRD = dyn_cast<CXXRecordDecl>(RD);

    auto Parent = static_cast<AST::Class*>(
        WalkDeclaration(RD, /*IgnoreSystemDecls =*/false));

    LayoutBase LayoutBase;
    LayoutBase.Offset = Offset.getQuantity();
    LayoutBase.Class = Parent;
    Class->Layout->Bases.push_back(LayoutBase);

    // Dump bases.
    if (CXXRD) {
        const CXXRecordDecl *PrimaryBase = Layout.getPrimaryBase();
        bool HasOwnVFPtr = Layout.hasOwnVFPtr();
        bool HasOwnVBPtr = Layout.hasOwnVBPtr();

        // Vtable pointer.
        if (CXXRD->isDynamicClass() && !PrimaryBase &&
            !C->getASTContext().getTargetInfo().getCXXABI().isMicrosoft()) {
            auto VPtr = WalkVTablePointer(Parent, Offset, "vptr");
            Class->Layout->Fields.push_back(VPtr);
        }
        else if (HasOwnVFPtr) {
            auto VTPtr = WalkVTablePointer(Parent, Offset, "vfptr");
            Class->Layout->Fields.push_back(VTPtr);
        }

        // Collect nvbases.
        SmallVector<const CXXRecordDecl *, 4> Bases;
        for (const CXXBaseSpecifier &Base : CXXRD->bases()) {
            assert(!Base.getType()->isDependentType() &&
                "Cannot layout class with dependent bases.");
            if (!Base.isVirtual())
                Bases.push_back(Base.getType()->getAsCXXRecordDecl());
        }

        // Sort nvbases by offset.
        std::stable_sort(Bases.begin(), Bases.end(),
            [&](const CXXRecordDecl *L, const CXXRecordDecl *R) {
            return Layout.getBaseClassOffset(L) < Layout.getBaseClassOffset(R);
        });

        // Dump (non-virtual) bases
        for (const CXXRecordDecl *Base : Bases) {
            CharUnits BaseOffset = Offset + Layout.getBaseClassOffset(Base);
            ReadClassLayout(Class, Base, BaseOffset,
                /*IncludeVirtualBases=*/false);
        }

        // vbptr (for Microsoft C++ ABI)
        if (HasOwnVBPtr) {
            auto VBPtr = WalkVTablePointer(Parent,
                Offset + Layout.getVBPtrOffset(), "vbptr");
            Class->Layout->Fields.push_back(VBPtr);
        }
    }

    // Dump fields.
    uint64_t FieldNo = 0;
    for (RecordDecl::field_iterator I = RD->field_begin(),
        E = RD->field_end(); I != E; ++I, ++FieldNo) {
        auto Field = *I;
        uint64_t LocalFieldOffsetInBits = Layout.getFieldOffset(FieldNo);
        CharUnits FieldOffset =
            Offset + C->getASTContext().toCharUnitsFromBits(LocalFieldOffsetInBits);

        auto F = WalkFieldCXX(Field, Parent);
        LayoutField LayoutField;
        LayoutField.Offset = FieldOffset.getQuantity();
        LayoutField.Name = F->Name;
        LayoutField.QualifiedType = GetQualifiedType(Field->getType());
        LayoutField.FieldPtr = (void*)Field;
        Class->Layout->Fields.push_back(LayoutField);
    }

    // Dump virtual bases.
    if (CXXRD && IncludeVirtualBases) {
        const ASTRecordLayout::VBaseOffsetsMapTy &VtorDisps =
            Layout.getVBaseOffsetsMap();

        for (const CXXBaseSpecifier &Base : CXXRD->vbases()) {
            assert(Base.isVirtual() && "Found non-virtual class!");
            const CXXRecordDecl *VBase = Base.getType()->getAsCXXRecordDecl();

            CharUnits VBaseOffset = Offset + Layout.getVBaseClassOffset(VBase);

            if (VtorDisps.find(VBase)->second.hasVtorDisp()) {
                auto VtorDisp = WalkVTablePointer(Parent,
                    VBaseOffset - CharUnits::fromQuantity(4), "vtordisp");
                Class->Layout->Fields.push_back(VtorDisp);
            }

            ReadClassLayout(Class, VBase, VBaseOffset,
                /*IncludeVirtualBases=*/false);
        }
    }
}

static std::string GetClangResourceDir()
{
    using namespace llvm;
    using namespace clang;

    // Compute the path to the resource directory.
    StringRef ClangResourceDir(CLANG_RESOURCE_DIR);
    
    SmallString<128> P(GetCurrentLibraryDir());
    llvm::sys::path::remove_filename(P);
    
    if (ClangResourceDir != "")
        llvm::sys::path::append(P, ClangResourceDir);
    else
        llvm::sys::path::append(P, "lib", "clang", CLANG_VERSION_STRING);
    
    return P.str();
}

//-----------------------------------//

static clang::TargetCXXABI::Kind
ConvertToClangTargetCXXABI(CppSharp::CppParser::AST::CppAbi abi)
{
    using namespace clang;

    switch (abi)
    {
    case CppSharp::CppParser::AST::CppAbi::Itanium:
        return TargetCXXABI::GenericItanium;
    case CppSharp::CppParser::AST::CppAbi::Microsoft:
        return TargetCXXABI::Microsoft;
    case CppSharp::CppParser::AST::CppAbi::ARM:
        return TargetCXXABI::GenericARM;
    case CppSharp::CppParser::AST::CppAbi::iOS:
        return TargetCXXABI::iOS;
    case CppSharp::CppParser::AST::CppAbi::iOS64:
        return TargetCXXABI::iOS64;
    }

    llvm_unreachable("Unsupported C++ ABI.");
}

void Parser::SetupHeader()
{
    using namespace clang;

    std::vector<const char*> args;
    args.push_back("-cc1");

    switch (Opts->LanguageVersion)
    {
    case CppParser::LanguageVersion::C:
    case CppParser::LanguageVersion::GNUC:
        args.push_back("-xc");
        break;
    default:
        args.push_back("-xc++");
        break;
    }

    switch (Opts->LanguageVersion)
    {
    case CppParser::LanguageVersion::C:
        args.push_back("-std=c99");
        break;
    case CppParser::LanguageVersion::GNUC:
        args.push_back("-std=gnu99");
        break;
    case CppParser::LanguageVersion::CPlusPlus98:
        args.push_back("-std=c++98");
        break;
    case CppParser::LanguageVersion::GNUPlusPlus98:
        args.push_back("-std=gnu++98");
        break;
    case CppParser::LanguageVersion::CPlusPlus11:
        args.push_back(Opts->MicrosoftMode ? "-std=c++14" : "-std=c++11");
        break;
    default:
        args.push_back(Opts->MicrosoftMode ? "-std=gnu++14" : "-std=gnu++11");
        break;
    }
    args.push_back("-fno-rtti");

    for (unsigned I = 0, E = Opts->Arguments.size(); I != E; ++I)
    {
        const auto& Arg = Opts->Arguments[I];
        args.push_back(Arg.c_str());
    }

    C.reset(new CompilerInstance());
    C->createDiagnostics();

    CompilerInvocation* Inv = new CompilerInvocation();
    CompilerInvocation::CreateFromArgs(*Inv, args.data(), args.data() + args.size(),
      C->getDiagnostics());
    C->setInvocation(Inv);

    auto& TO = Inv->TargetOpts;
    TargetABI = ConvertToClangTargetCXXABI(Opts->Abi);

    TO->Triple = llvm::sys::getDefaultTargetTriple();
    if (!Opts->TargetTriple.empty())
        TO->Triple = llvm::Triple::normalize(Opts->TargetTriple);

    TargetInfo* TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), TO);
    if (!TI)
    {
        // We might have no target info due to an invalid user-provided triple.
        // Try again with the default triple.
        TO->Triple = llvm::sys::getDefaultTargetTriple();
        TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), TO);
    }

    assert(TI && "Expected valid target info");

    C->setTarget(TI);

    C->createFileManager();
    C->createSourceManager(C->getFileManager());

    auto& HSOpts = C->getHeaderSearchOpts();
    auto& PPOpts = C->getPreprocessorOpts();
    auto& LangOpts = C->getLangOpts();

    if (Opts->NoStandardIncludes)
    {
        HSOpts.UseStandardSystemIncludes = false;
        HSOpts.UseStandardCXXIncludes = false;
    }

    if (Opts->NoBuiltinIncludes)
        HSOpts.UseBuiltinIncludes = false;

    if (Opts->Verbose)
        HSOpts.Verbose = true;

    for (unsigned I = 0, E = Opts->IncludeDirs.size(); I != E; ++I)
    {
        const auto& s = Opts->IncludeDirs[I];
        HSOpts.AddPath(s, frontend::Angled, false, false);
    }

    for (unsigned I = 0, E = Opts->SystemIncludeDirs.size(); I != E; ++I)
    {
        const auto& s = Opts->SystemIncludeDirs[I];
        HSOpts.AddPath(s, frontend::System, false, false);
    }

    for (unsigned I = 0, E = Opts->Defines.size(); I != E; ++I)
    {
        const auto& define = Opts->Defines[I];
        PPOpts.addMacroDef(define);
    }

    for (unsigned I = 0, E = Opts->Undefines.size(); I != E; ++I)
    {
        const auto& undefine = Opts->Undefines[I];
        PPOpts.addMacroUndef(undefine);
    }

    // Initialize the default platform headers.
    HSOpts.ResourceDir = GetClangResourceDir();

    llvm::SmallString<128> ResourceDir(HSOpts.ResourceDir);
    llvm::sys::path::append(ResourceDir, "include");
    HSOpts.AddPath(ResourceDir.str(), clang::frontend::System, /*IsFramework=*/false,
        /*IgnoreSysRoot=*/false);

#ifdef _MSC_VER
    if (Opts->MicrosoftMode)
    {
        LangOpts.MSCompatibilityVersion = Opts->ToolSetToUse;
        if (!LangOpts.MSCompatibilityVersion) LangOpts.MSCompatibilityVersion = 1700;
    }
#endif

    llvm::opt::InputArgList Args(0, 0);
    clang::driver::Driver D("", TO->Triple, C->getDiagnostics());
    clang::driver::ToolChain *TC = nullptr;
    llvm::Triple Target(TO->Triple);
    switch (Target.getOS()) {
    // Extend this for other triples if needed, see clang's Driver::getToolChain.
    case llvm::Triple::Linux:
      TC = new clang::driver::toolchains::Linux(D, Target, Args);
      break;
    }

    if (TC && !Opts->NoStandardIncludes) {
        llvm::opt::ArgStringList Includes;
        TC->AddClangSystemIncludeArgs(Args, Includes);
        TC->AddClangCXXStdlibIncludeArgs(Args, Includes);
        for (auto& Arg : Includes) {
            if (strlen(Arg) > 0 && Arg[0] != '-')
                HSOpts.AddPath(Arg, frontend::System, /*IsFramework=*/false,
                    /*IgnoreSysRoot=*/false);
        }
    }

    // Enable preprocessing record.
    PPOpts.DetailedRecord = true;

    C->createPreprocessor(TU_Complete);

    Preprocessor& PP = C->getPreprocessor();
    PP.getBuiltinInfo().initializeBuiltins(PP.getIdentifierTable(),
        PP.getLangOpts());

    C->createASTContext();
}

//-----------------------------------//

std::string Parser::GetDeclMangledName(const clang::Decl* D)
{
    using namespace clang;

    if(!D || !isa<NamedDecl>(D))
        return "";

    bool CanMangle = isa<FunctionDecl>(D) || isa<VarDecl>(D)
        || isa<CXXConstructorDecl>(D) || isa<CXXDestructorDecl>(D);

    if (!CanMangle) return "";

    auto ND = cast<NamedDecl>(D);
    std::unique_ptr<MangleContext> MC;
    
    switch(TargetABI)
    {
    default:
       MC.reset(ItaniumMangleContext::create(*AST, AST->getDiagnostics()));
       break;
    case TargetCXXABI::Microsoft:
       MC.reset(MicrosoftMangleContext::create(*AST, AST->getDiagnostics()));
       break;
    }

    if (!MC)
        llvm_unreachable("Unknown mangling ABI");

    std::string Mangled;
    llvm::raw_string_ostream Out(Mangled);

    bool IsDependent = false;
    if (const ValueDecl *VD = dyn_cast<ValueDecl>(ND))
        IsDependent |= VD->getType()->isDependentType();

    if (!IsDependent)
        IsDependent |= ND->getDeclContext()->isDependentContext();

    if (!MC->shouldMangleDeclName(ND) || IsDependent)
        return ND->getDeclName().getAsString();

    if (const CXXConstructorDecl *CD = dyn_cast<CXXConstructorDecl>(ND))
        MC->mangleCXXCtor(CD, Ctor_Base, Out);
    else if (const CXXDestructorDecl *DD = dyn_cast<CXXDestructorDecl>(ND))
        MC->mangleCXXDtor(DD, Dtor_Base, Out);
    else
        MC->mangleName(ND, Out);

    Out.flush();

    // Strip away LLVM name marker.
    if(!Mangled.empty() && Mangled[0] == '\01')
        Mangled = Mangled.substr(1);

    return Mangled;
}

//-----------------------------------//

static std::string GetDeclName(const clang::NamedDecl* D)
{
    if (const clang::IdentifierInfo *II = D->getIdentifier())
        return II->getName();
    return D->getNameAsString();
}

static std::string GetTagDeclName(const clang::TagDecl* D)
{
    using namespace clang;

    if (auto Typedef = D->getTypedefNameForAnonDecl())
    {
        assert(Typedef->getIdentifier() && "Typedef without identifier?");
        return GetDeclName(Typedef);
    }

    return GetDeclName(D);
}

static std::string GetDeclUSR(const clang::Decl* D)
{
    using namespace clang;
    SmallString<128> usr;
    if (!index::generateUSRForDecl(D, usr))
        return usr.str();
    return "<invalid>";
}

static clang::Decl* GetPreviousDeclInContext(const clang::Decl* D)
{
    assert(!D->getLexicalDeclContext()->decls_empty());

    clang::Decl* prevDecl = nullptr;
    for(auto it =  D->getDeclContext()->decls_begin();
             it != D->getDeclContext()->decls_end(); it++)
    {
        if((*it) == D)
            return prevDecl;
        prevDecl = (*it);
    }

    return nullptr;
}

static clang::SourceLocation GetDeclStartLocation(clang::CompilerInstance* C,
                                                  const clang::Decl* D)
{
    auto& SM = C->getSourceManager();
    auto startLoc = SM.getExpansionLoc(D->getLocStart());
    auto startOffset = SM.getFileOffset(startLoc);

    if (clang::dyn_cast_or_null<clang::TranslationUnitDecl>(D) || !startLoc.isValid())
        return startLoc;

    auto lineNo = SM.getExpansionLineNumber(startLoc);
    auto lineBeginLoc = SM.translateLineCol(SM.getFileID(startLoc), lineNo, 1);
    auto lineBeginOffset = SM.getFileOffset(lineBeginLoc);
    assert(lineBeginOffset <= startOffset);

    if (D->getLexicalDeclContext()->decls_empty())
        return lineBeginLoc;

    auto prevDecl = GetPreviousDeclInContext(D);
    if(!prevDecl)
        return lineBeginLoc;

    auto prevDeclEndLoc = SM.getExpansionLoc(prevDecl->getLocEnd());
    auto prevDeclEndOffset = SM.getFileOffset(prevDeclEndLoc);

    if(SM.getFileID(prevDeclEndLoc) != SM.getFileID(startLoc))
        return lineBeginLoc;

    // TODO: Figure out why this asserts
    //assert(prevDeclEndOffset <= startOffset);

    if(prevDeclEndOffset < lineBeginOffset)
        return lineBeginLoc;

    // Declarations don't share same macro expansion
    if(SM.getExpansionLoc(prevDecl->getLocStart()) != startLoc)
        return prevDeclEndLoc;

    return GetDeclStartLocation(C, prevDecl);
}

std::string Parser::GetTypeName(const clang::Type* Type)
{
    using namespace clang;

    if(Type->isAnyPointerType() || Type->isReferenceType())
        Type = Type->getPointeeType().getTypePtr();

    if(Type->isEnumeralType() || Type->isRecordType())
    {
        const clang::TagType* Tag = Type->getAs<clang::TagType>();
        return GetTagDeclName(Tag->getDecl());
    }

    PrintingPolicy pp(C->getLangOpts());
    pp.SuppressTagKeyword = true;

    std::string TypeName;
    QualType::getAsStringInternal(Type, Qualifiers(), TypeName, pp);

    return TypeName;
}

static TypeQualifiers GetTypeQualifiers(const clang::QualType& Type)
{
    TypeQualifiers quals;
    quals.IsConst = Type.isLocalConstQualified();
    quals.IsRestrict = Type.isLocalRestrictQualified();
    quals.IsVolatile = Type.isVolatileQualified();
    return quals;
}

QualifiedType Parser::GetQualifiedType(const clang::QualType& qual, clang::TypeLoc* TL)
{
    QualifiedType qualType;
    qualType.Type = WalkType(qual, TL);
    qualType.Qualifiers = GetTypeQualifiers(qual);
    return qualType;
}

//-----------------------------------//

static AccessSpecifier ConvertToAccess(clang::AccessSpecifier AS)
{
    switch(AS)
    {
    case clang::AS_private:
        return AccessSpecifier::Private;
    case clang::AS_protected:
        return AccessSpecifier::Protected;
    case clang::AS_public:
        return AccessSpecifier::Public;
    case clang::AS_none:
        return AccessSpecifier::Public;
    }

    llvm_unreachable("Unknown AccessSpecifier");
}

VTableComponent
Parser::WalkVTableComponent(const clang::VTableComponent& Component)
{
    using namespace clang;
    VTableComponent VTC;

    switch(Component.getKind())
    {
    case clang::VTableComponent::CK_VCallOffset:
    {
        VTC.Kind = VTableComponentKind::VBaseOffset;
        VTC.Offset = Component.getVCallOffset().getQuantity();
        break;
    }
    case clang::VTableComponent::CK_VBaseOffset:
    {
        VTC.Kind = VTableComponentKind::VBaseOffset;
        VTC.Offset = Component.getVBaseOffset().getQuantity();
        break;
    }
    case clang::VTableComponent::CK_OffsetToTop:
    {
        VTC.Kind = VTableComponentKind::OffsetToTop;
        VTC.Offset = Component.getOffsetToTop().getQuantity();
        break;
    }
    case clang::VTableComponent::CK_RTTI:
    {
        VTC.Kind = VTableComponentKind::RTTI;
        auto RD = Component.getRTTIDecl();
        VTC.Declaration = WalkRecordCXX(RD);
        break;
    }
    case clang::VTableComponent::CK_FunctionPointer:
    {
        VTC.Kind = VTableComponentKind::FunctionPointer;
        auto MD = Component.getFunctionDecl();
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    case clang::VTableComponent::CK_CompleteDtorPointer:
    {
        VTC.Kind = VTableComponentKind::CompleteDtorPointer;
        auto MD = Component.getDestructorDecl();
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    case clang::VTableComponent::CK_DeletingDtorPointer:
    {
        VTC.Kind = VTableComponentKind::DeletingDtorPointer;
        auto MD = Component.getDestructorDecl();
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    case clang::VTableComponent::CK_UnusedFunctionPointer:
    {
        VTC.Kind = VTableComponentKind::UnusedFunctionPointer;
        auto MD = Component.getUnusedFunctionDecl();
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    default:
        llvm_unreachable("Unknown vtable component kind");
    }

    return VTC;
}

VTableLayout Parser::WalkVTableLayout(const clang::VTableLayout& VTLayout)
{
    auto Layout = VTableLayout();

    for (auto I = VTLayout.vtable_component_begin(),
              E = VTLayout.vtable_component_end(); I != E; ++I)
    {
        auto VTComponent = WalkVTableComponent(*I);
        Layout.Components.push_back(VTComponent);
    }

    return Layout;
}


void Parser::WalkVTable(const clang::CXXRecordDecl* RD, Class* C)
{
    using namespace clang;

    assert(RD->isDynamicClass() && "Only dynamic classes have virtual tables");

    if (!C->Layout)
        C->Layout = new ClassLayout();

    switch(TargetABI)
    {
    case TargetCXXABI::Microsoft:
    {
        C->Layout->ABI = CppAbi::Microsoft;
        MicrosoftVTableContext VTContext(*AST);

        auto VFPtrs = VTContext.getVFPtrOffsets(RD);
        for (auto I = VFPtrs.begin(), E = VFPtrs.end(); I != E; ++I)
        {
            auto& VFPtrInfo = *I;

            VFTableInfo Info;
            Info.VFPtrOffset = VFPtrInfo->NonVirtualOffset.getQuantity();
            Info.VFPtrFullOffset = VFPtrInfo->FullOffsetInMDC.getQuantity();

            auto& VTLayout = VTContext.getVFTableLayout(RD, VFPtrInfo->FullOffsetInMDC);
            Info.Layout = WalkVTableLayout(VTLayout);

            C->Layout->VFTables.push_back(Info);
        }
        break;
    }
    case TargetCXXABI::GenericItanium:
    {
        C->Layout->ABI = CppAbi::Itanium;
        ItaniumVTableContext VTContext(*AST);

        auto& VTLayout = VTContext.getVTableLayout(RD);
        C->Layout->Layout = WalkVTableLayout(VTLayout);
        break;
    }
    default:
        llvm_unreachable("Unsupported C++ ABI kind");
    }
}

void Parser::EnsureCompleteRecord(const clang::RecordDecl* Record,
    DeclarationContext* NS, Class* RC)
{
    using namespace clang;

    if (!RC->IsIncomplete || RC->CompleteDeclaration)
        return;

    auto Complete = NS->FindClass(Record->getName(),
        /*IsComplete=*/true, /*Create=*/false);
    if (Complete)
    {
        RC->CompleteDeclaration = Complete;
        return;
    }

    auto Definition = Record->getDefinition();
    bool isCXX = false;
    if (auto CXXRecord = dyn_cast<CXXRecordDecl>(Record))
    {
        Definition = CXXRecord->getDefinition();
        isCXX = true;
    }

    if (!Definition)
        return;

    auto DC = GetNamespace(Definition);
    Complete = DC->FindClass(Record->getName(),
        /*IsComplete=*/true, /*Create=*/false);
    if (Complete)
    {
        RC->CompleteDeclaration = Complete;
        return;
    }
    Complete = DC->FindClass(Record->getName(),
        /*IsComplete=*/true, /*Create=*/true);
    if (isCXX)
        WalkRecordCXX(cast<CXXRecordDecl>(Definition), Complete);
    else
        WalkRecord(Definition, Complete);
    HandleDeclaration(Definition, Complete);
    RC->CompleteDeclaration = Complete;
}

Class* Parser::GetRecord(const clang::RecordDecl* Record, bool& Process)
{
    using namespace clang;
    Process = false;

    if (Record->isInjectedClassName())
        return nullptr;

    auto NS = GetNamespace(Record);
    assert(NS && "Expected a valid namespace");

    bool isCompleteDefinition = Record->isCompleteDefinition();

    Class* RC = nullptr;

    auto Name = GetTagDeclName(Record);
    auto HasEmptyName = Record->getDeclName().isEmpty();

    if (HasEmptyName)
    {
        auto USR = GetDeclUSR(Record);
        if (auto AR = NS->FindAnonymous(USR))
            RC = static_cast<Class*>(AR);
    }
    else
    {
        RC = NS->FindClass(Name, isCompleteDefinition, /*Create=*/false);
    }

    if (RC)
        return RC;

    RC = NS->FindClass(Name, isCompleteDefinition, /*Create=*/true);
    HandleDeclaration(Record, RC);
    EnsureCompleteRecord(Record, NS, RC);

    if (HasEmptyName)
    {
        auto USR = GetDeclUSR(Record);
        NS->Anonymous[USR] = RC;
    }

    if (!isCompleteDefinition)
        return RC;

    Process = true;
    return RC;
}

Class* Parser::WalkRecord(const clang::RecordDecl* Record)
{
    bool Process;
    auto RC = GetRecord(Record, Process);

    if (!RC || !Process)
        return RC;

    WalkRecord(Record, RC);

    return RC;
}

Class* Parser::WalkRecordCXX(const clang::CXXRecordDecl* Record)
{
    bool Process;
    auto RC = GetRecord(Record, Process);

    if (!RC || !Process)
        return RC;

    WalkRecordCXX(Record, RC);

    return RC;
}

static int I = 0;

void Parser::WalkRecord(const clang::RecordDecl* Record, Class* RC)
{
    using namespace clang;

    if (Record->isImplicit())
        return;

    auto headStartLoc = GetDeclStartLocation(C.get(), Record);
    auto headEndLoc = Record->getLocation(); // identifier location
    auto bodyEndLoc = Record->getLocEnd();

    auto headRange = clang::SourceRange(headStartLoc, headEndLoc);
    auto bodyRange = clang::SourceRange(headEndLoc, bodyEndLoc);

    HandlePreprocessedEntities(RC, headRange, MacroLocation::ClassHead);
    HandlePreprocessedEntities(RC, bodyRange, MacroLocation::ClassBody);

    auto& Sema = C->getSema();

    RC->IsUnion = Record->isUnion();
    RC->IsDependent = Record->isDependentType();
    RC->IsExternCContext = Record->isExternCContext();

    bool hasLayout = !Record->isDependentType() && !Record->isInvalidDecl();

    if (hasLayout)
    {
        const auto& Layout = C->getASTContext().getASTRecordLayout(Record);
        if (!RC->Layout)
            RC->Layout = new ClassLayout();
        RC->Layout->Alignment = (int)Layout.getAlignment().getQuantity();
        RC->Layout->Size = (int)Layout.getSize().getQuantity();
        RC->Layout->DataSize = (int)Layout.getDataSize().getQuantity();
        ReadClassLayout(RC, Record, CharUnits(), true);
    }

    for(auto it = Record->decls_begin(); it != Record->decls_end(); ++it)
    {
        auto D = *it;

        switch(D->getKind())
        {
        case Decl::CXXConstructor:
        case Decl::CXXDestructor:
        case Decl::CXXConversion:
        case Decl::CXXMethod:
        {
            auto MD = cast<CXXMethodDecl>(D);
            WalkMethodCXX(MD);
            break;
        }
        case Decl::Field:
        {
            auto FD = cast<FieldDecl>(D);
            WalkFieldCXX(FD, RC);
            break;
        }
        case Decl::AccessSpec:
        {
            AccessSpecDecl* AS = cast<AccessSpecDecl>(D);

            auto AccessDecl = new AccessSpecifierDecl();
            HandleDeclaration(AS, AccessDecl);

            AccessDecl->Access = ConvertToAccess(AS->getAccess());
            AccessDecl->_Namespace = RC;

            RC->Specifiers.push_back(AccessDecl);
            break;
        }
        case Decl::IndirectField: // FIXME: Handle indirect fields
            break;
        default:
        {
            WalkDeclaration(D);
            break;
        } }
    }
}

static clang::CXXRecordDecl* GetCXXRecordDeclFromBaseType(const clang::Type* Ty) {
    using namespace clang;

    if (auto RT = Ty->getAs<clang::RecordType>())
        return dyn_cast<clang::CXXRecordDecl>(RT->getDecl());
    else if (auto TST = Ty->getAs<clang::TemplateSpecializationType>())
        return dyn_cast<clang::CXXRecordDecl>(
            TST->getTemplateName().getAsTemplateDecl()->getTemplatedDecl());
    else if (auto Injected = Ty->getAs<clang::InjectedClassNameType>())
        return Injected->getDecl();

    assert("Could not get base CXX record from type");
    return nullptr;
}

void Parser::WalkRecordCXX(const clang::CXXRecordDecl* Record, Class* RC)
{
    using namespace clang;

    if (Record->isImplicit())
        return;

    auto& Sema = C->getSema();
    Sema.ForceDeclarationOfImplicitMembers(const_cast<clang::CXXRecordDecl*>(Record));

    WalkRecord(Record, RC);

    RC->IsPOD = Record->isPOD();
    RC->IsAbstract = Record->isAbstract();
    RC->IsDynamic = Record->isDynamicClass();
    RC->IsPolymorphic = Record->isPolymorphic();
    RC->HasNonTrivialDefaultConstructor = Record->hasNonTrivialDefaultConstructor();
    RC->HasNonTrivialCopyConstructor = Record->hasNonTrivialCopyConstructor();
    RC->HasNonTrivialDestructor = Record->hasNonTrivialDestructor();

    bool hasLayout = !Record->isDependentType() && !Record->isInvalidDecl() &&
        Record->getDeclName() != C->getSema().VAListTagName;

    // Get the record layout information.
    const ASTRecordLayout* Layout = 0;
    if (hasLayout)
    {
        Layout = &C->getASTContext().getASTRecordLayout(Record);

        assert (RC->Layout && "Expected a valid AST layout");
        RC->Layout->HasOwnVFPtr = Layout->hasOwnVFPtr();
        RC->Layout->VBPtrOffset = Layout->getVBPtrOffset().getQuantity();
    }

    // Iterate through the record bases.
    for (auto it = Record->bases_begin(); it != Record->bases_end(); ++it)
    {
        auto& BS = *it;

        BaseClassSpecifier* Base = new BaseClassSpecifier();
        Base->Access = ConvertToAccess(BS.getAccessSpecifier());
        Base->IsVirtual = BS.isVirtual();

        auto BSTL = BS.getTypeSourceInfo()->getTypeLoc();
        Base->Type = WalkType(BS.getType(), &BSTL);

        auto BaseDecl = GetCXXRecordDeclFromBaseType(BS.getType().getTypePtr());
        if (BaseDecl && Layout)
        {
            auto Offset = BS.isVirtual() ? Layout->getVBaseClassOffset(BaseDecl)
                : Layout->getBaseClassOffset(BaseDecl);
            Base->Offset = Offset.getQuantity();
        }

        RC->Bases.push_back(Base);
    }

    // Process the vtables
    if (hasLayout && Record->isDynamicClass())
        WalkVTable(Record, RC);
}

//-----------------------------------//

static TemplateSpecializationKind
WalkTemplateSpecializationKind(clang::TemplateSpecializationKind Kind)
{
    switch(Kind)
    {
    case clang::TSK_Undeclared:
        return TemplateSpecializationKind::Undeclared;
    case clang::TSK_ImplicitInstantiation:
        return TemplateSpecializationKind::ImplicitInstantiation;
    case clang::TSK_ExplicitSpecialization:
        return TemplateSpecializationKind::ExplicitSpecialization;
    case clang::TSK_ExplicitInstantiationDeclaration:
        return TemplateSpecializationKind::ExplicitInstantiationDeclaration;
    case clang::TSK_ExplicitInstantiationDefinition:
        return TemplateSpecializationKind::ExplicitInstantiationDefinition;
    }

    llvm_unreachable("Unknown template specialization kind");
}

ClassTemplateSpecialization*
Parser::WalkClassTemplateSpecialization(const clang::ClassTemplateSpecializationDecl* CTS)
{
    using namespace clang;

    auto CT = WalkClassTemplate(CTS->getSpecializedTemplate());
    auto USR = GetDeclUSR(CTS);
    auto TS = CT->FindSpecialization(USR);
    if (TS != nullptr)
        return TS;

    TS = new ClassTemplateSpecialization();
    HandleDeclaration(CTS, TS);

    auto NS = GetNamespace(CTS);
    assert(NS && "Expected a valid namespace");
    TS->_Namespace = NS;
    TS->Name = CTS->getName();
    TS->TemplatedDecl = CT;
    TS->SpecializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
    CT->Specializations.push_back(TS);

    auto& TAL = CTS->getTemplateArgs();
    auto TSI = CTS->getTypeAsWritten();
    if (TSI)
    {
        auto TL = TSI->getTypeLoc();
        auto TSL = TL.getAs<TemplateSpecializationTypeLoc>();
        TS->Arguments = WalkTemplateArgumentList(&TAL, &TSL);
    }
    else
    {
        TS->Arguments = WalkTemplateArgumentList(&TAL, (clang::TemplateSpecializationTypeLoc*) 0);
    }

    if (CTS->isCompleteDefinition())
    {
        WalkRecordCXX(CTS, TS);
    }
    else
    {
        TS->IsIncomplete = true;
        if (CTS->getDefinition())
        {
            auto Complete = WalkDeclaration(CTS->getDefinition(), /*CanBeDefinition=*/true);
            if (!Complete->IsIncomplete)
                TS->CompleteDeclaration = Complete;
        }
    }

    return TS;
}

//-----------------------------------//

ClassTemplatePartialSpecialization*
Parser::WalkClassTemplatePartialSpecialization(const clang::ClassTemplatePartialSpecializationDecl* CTS)
{
    using namespace clang;

    auto CT = WalkClassTemplate(CTS->getSpecializedTemplate());
    auto USR = GetDeclUSR(CTS);
    auto TS = CT->FindPartialSpecialization(USR);
    if (TS != nullptr)
        return TS;

    TS = new ClassTemplatePartialSpecialization();
    HandleDeclaration(CTS, TS);

    auto NS = GetNamespace(CTS);
    assert(NS && "Expected a valid namespace");
    TS->_Namespace = NS;
    TS->Name = CTS->getName();
    TS->TemplatedDecl = CT;
    TS->SpecializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
    CT->Specializations.push_back(TS);

    auto& TAL = CTS->getTemplateArgs();
    if (auto TSI = CTS->getTypeAsWritten())
    {
        auto TL = TSI->getTypeLoc();
        auto TSL = TL.getAs<TemplateSpecializationTypeLoc>();
        TS->Arguments = WalkTemplateArgumentList(&TAL, &TSL);
    }

    if (CTS->isCompleteDefinition())
    {
        WalkRecordCXX(CTS, TS);
    }
    else
    {
        TS->IsIncomplete = true;
        if (CTS->getDefinition())
        {
            auto Complete = WalkDeclaration(CTS->getDefinition(), /*CanBeDefinition=*/true);
            if (!Complete->IsIncomplete)
                TS->CompleteDeclaration = Complete;
        }
    }

    return TS;
}

//-----------------------------------//

std::vector<Declaration*> Parser::WalkTemplateParameterList(const clang::TemplateParameterList* TPL)
{
    auto params = std::vector<CppSharp::CppParser::Declaration*>();

    for (auto it = TPL->begin(); it != TPL->end(); ++it)
    {
        auto ND = *it;
        auto TP = WalkDeclaration(ND, /*IgnoreSystemDecls=*/false);
        params.push_back(TP);
    }

    return params;
}

//-----------------------------------//

ClassTemplate* Parser::WalkClassTemplate(const clang::ClassTemplateDecl* TD)
{
    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(TD);
    auto CT = NS->FindTemplate<ClassTemplate>(USR);
    if (CT != nullptr)
        return CT;

    CT = new ClassTemplate();
    HandleDeclaration(TD, CT);

    CT->Name = GetDeclName(TD);
    CT->_Namespace = NS;
    NS->Templates.push_back(CT);

    bool Process;
    auto RC = GetRecord(TD->getTemplatedDecl(), Process);
    CT->TemplatedDecl = RC;

    if (Process)
        WalkRecordCXX(TD->getTemplatedDecl(), RC);

    CT->Parameters = WalkTemplateParameterList(TD->getTemplateParameters());

    return CT;
}

//-----------------------------------//

TemplateTemplateParameter* Parser::WalkTemplateTemplateParameter(const clang::TemplateTemplateParmDecl* TTP)
{
    auto TP = new TemplateTemplateParameter();
    HandleDeclaration(TTP, TP);
    TP->Parameters = WalkTemplateParameterList(TTP->getTemplateParameters());
    TP->IsParameterPack = TTP->isParameterPack();
    TP->IsPackExpansion = TTP->isPackExpansion();
    TP->IsExpandedParameterPack = TTP->isExpandedParameterPack();
    if (TTP->getTemplatedDecl())
    {
        auto TD = WalkDeclaration(TTP->getTemplatedDecl(), /*IgnoreSystemDecls=*/false);
        TP->TemplatedDecl = TD;
    }
    return TP;
}

//-----------------------------------//

TypeTemplateParameter* Parser::WalkTypeTemplateParameter(const clang::TemplateTypeParmDecl* TTPD)
{
    auto TP = new CppSharp::CppParser::TypeTemplateParameter();
    TP->Name = GetDeclName(TTPD);
    HandleDeclaration(TTPD, TP);
    if (TTPD->hasDefaultArgument())
        TP->DefaultArgument = GetQualifiedType(TTPD->getDefaultArgument());
    TP->Depth = TTPD->getDepth();
    TP->Index = TTPD->getIndex();
    TP->IsParameterPack = TTPD->isParameterPack();
    return TP;
}

//-----------------------------------//

NonTypeTemplateParameter* Parser::WalkNonTypeTemplateParameter(const clang::NonTypeTemplateParmDecl* NTTPD)
{
    auto NTP = new CppSharp::CppParser::NonTypeTemplateParameter();
    NTP->Name = GetDeclName(NTTPD);
    HandleDeclaration(NTTPD, NTP);
    if (NTTPD->hasDefaultArgument())
        NTP->DefaultArgument = WalkExpression(NTTPD->getDefaultArgument());
    NTP->Depth = NTTPD->getDepth();
    NTP->Index = NTTPD->getIndex();
    NTP->IsParameterPack = NTTPD->isParameterPack();
    return NTP;
}

//-----------------------------------//

std::vector<CppSharp::CppParser::TemplateArgument>
Parser::WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL,
    clang::TemplateSpecializationTypeLoc* TSTL)
{
    using namespace clang;

    auto params = std::vector<CppSharp::CppParser::TemplateArgument>();

    for (size_t i = 0, e = TAL->size(); i < e; i++)
    {
        auto TA = TAL->get(i);
        TemplateArgumentLoc TAL;
        TemplateArgumentLoc *ArgLoc = 0;
        if (TSTL && TSTL->getTypePtr() && i < TSTL->getNumArgs())
        {
            TAL = TSTL->getArgLoc(i);
            ArgLoc = &TAL;
        }
        auto Arg = WalkTemplateArgument(TA, ArgLoc);
        params.push_back(Arg);
    }

    return params;
}

//-----------------------------------//

std::vector<CppSharp::CppParser::TemplateArgument>
Parser::WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL, 
    const clang::ASTTemplateArgumentListInfo* TALI)
{
    using namespace clang;

    auto params = std::vector<CppSharp::CppParser::TemplateArgument>();

    for (size_t i = 0, e = TAL->size(); i < e; i++)
    {
        auto TA = TAL->get(i);
        auto ArgLoc = TALI->operator[](i);
        auto TP = WalkTemplateArgument(TA, &ArgLoc);
        params.push_back(TP);
    }

    return params;
}

//-----------------------------------//

CppSharp::CppParser::TemplateArgument
Parser::WalkTemplateArgument(const clang::TemplateArgument& TA, clang::TemplateArgumentLoc* ArgLoc)
{
    auto Arg = CppSharp::CppParser::TemplateArgument();

    switch (TA.getKind())
    {
    case clang::TemplateArgument::Type:
    {
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Type;
        if (ArgLoc && ArgLoc->getTypeSourceInfo())
        {
            auto ArgTL = ArgLoc->getTypeSourceInfo()->getTypeLoc();
            Arg.Type = GetQualifiedType(TA.getAsType(), &ArgTL);
        }
        else
        {
            Arg.Type = GetQualifiedType(TA.getAsType());
        }
        break;
    }
    case clang::TemplateArgument::Declaration:
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Declaration;
        Arg.Declaration = WalkDeclaration(TA.getAsDecl(), 0);
        break;
    case clang::TemplateArgument::NullPtr:
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::NullPtr;
        break;
    case clang::TemplateArgument::Integral:
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Integral;
        //Arg.Type = WalkType(TA.getIntegralType(), 0);
        Arg.Integral = TA.getAsIntegral().getLimitedValue();
        break;
    case clang::TemplateArgument::Template:
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Template;
        break;
    case clang::TemplateArgument::TemplateExpansion:
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::TemplateExpansion;
        break;
    case clang::TemplateArgument::Expression:
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Expression;
        break;
    case clang::TemplateArgument::Pack:
        Arg.Kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Pack;
        break;
    case clang::TemplateArgument::Null:
    default:
        llvm_unreachable("Unknown TemplateArgument");
    }

    return Arg;
}

//-----------------------------------//

TypeAliasTemplate* Parser::WalkTypeAliasTemplate(
    const clang::TypeAliasTemplateDecl* TD)
{
    using namespace clang;

    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(TD);
    auto TA = NS->FindTemplate<TypeAliasTemplate>(USR);
    if (TA != nullptr)
        return TA;

    TA = new TypeAliasTemplate();
    HandleDeclaration(TD, TA);

    TA->Name = GetDeclName(TD);
    TA->TemplatedDecl = WalkDeclaration(TD->getTemplatedDecl());
    TA->Parameters = WalkTemplateParameterList(TD->getTemplateParameters());

    NS->Templates.push_back(TA);

    return TA;
}

//-----------------------------------//

FunctionTemplate* Parser::WalkFunctionTemplate(const clang::FunctionTemplateDecl* TD)
{
    using namespace clang;

    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(TD);
    auto FT = NS->FindTemplate<FunctionTemplate>(USR);
    if (FT != nullptr)
        return FT;

    CppSharp::CppParser::AST::Function* Function = nullptr;
    auto TemplatedDecl = TD->getTemplatedDecl();

    if (auto MD = dyn_cast<CXXMethodDecl>(TemplatedDecl))
        Function = WalkMethodCXX(MD);
    else
        Function = WalkFunction(TemplatedDecl, /*IsDependent=*/true,
                                            /*AddToNamespace=*/false);

    FT = new FunctionTemplate();
    HandleDeclaration(TD, FT);

    FT->Name = GetDeclName(TD);
    FT->_Namespace = NS;
    FT->TemplatedDecl = Function;
    FT->Parameters = WalkTemplateParameterList(TD->getTemplateParameters());

    NS->Templates.push_back(FT);

    return FT;
}

//-----------------------------------//

CppSharp::CppParser::FunctionTemplateSpecialization*
Parser::WalkFunctionTemplateSpec(clang::FunctionTemplateSpecializationInfo* FTSI, CppSharp::CppParser::Function* Function)
{
    using namespace clang;

    auto FTS = new CppSharp::CppParser::FunctionTemplateSpecialization();
    FTS->SpecializationKind = WalkTemplateSpecializationKind(FTSI->getTemplateSpecializationKind());
    FTS->SpecializedFunction = Function;
    // HACK: walking template arguments crashes when generating the parser bindings for OS X
    // so let's disable it for function templates which we do not support yet anyway 
    //if (auto TALI = FTSI->TemplateArgumentsAsWritten)
    //    FTS->Arguments = WalkTemplateArgumentList(FTSI->TemplateArguments, TALI);
    FTS->Template = WalkFunctionTemplate(FTSI->getTemplate());
    FTS->Template->Specializations.push_back(FTS);

    return FTS;
}

//-----------------------------------//

VarTemplate* Parser::WalkVarTemplate(const clang::VarTemplateDecl* TD)
{
    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(TD);
    auto VT = NS->FindTemplate<VarTemplate>(USR);
    if (VT != nullptr)
        return VT;

    VT = new VarTemplate();
    HandleDeclaration(TD, VT);

    VT->Name = GetDeclName(TD);
    VT->_Namespace = NS;
    NS->Templates.push_back(VT);

    auto RC = WalkVariable(TD->getTemplatedDecl());
    VT->TemplatedDecl = RC;
    VT->Parameters = WalkTemplateParameterList(TD->getTemplateParameters());

    return VT;
}

VarTemplateSpecialization*
Parser::WalkVarTemplateSpecialization(const clang::VarTemplateSpecializationDecl* VTS)
{
    using namespace clang;

    auto VT = WalkVarTemplate(VTS->getSpecializedTemplate());
    auto USR = GetDeclUSR(VTS);
    auto TS = VT->FindSpecialization(USR);
    if (TS != nullptr)
        return TS;

    TS = new VarTemplateSpecialization();
    HandleDeclaration(VTS, TS);

    auto NS = GetNamespace(VTS);
    assert(NS && "Expected a valid namespace");
    TS->_Namespace = NS;
    TS->Name = VTS->getName();
    TS->TemplatedDecl = VT;
    TS->SpecializationKind = WalkTemplateSpecializationKind(VTS->getSpecializationKind());
    VT->Specializations.push_back(TS);

    auto& TAL = VTS->getTemplateArgs();
    auto TSI = VTS->getTypeAsWritten();
    if (TSI)
    {
        auto TL = TSI->getTypeLoc();
        auto TSL = TL.getAs<TemplateSpecializationTypeLoc>();
        TS->Arguments = WalkTemplateArgumentList(&TAL, &TSL);
    }
    else
    {
        TS->Arguments = WalkTemplateArgumentList(&TAL, (clang::TemplateSpecializationTypeLoc*) 0);
    }

    WalkVariable(VTS, TS);

    return TS;
}

VarTemplatePartialSpecialization*
Parser::WalkVarTemplatePartialSpecialization(const clang::VarTemplatePartialSpecializationDecl* VTS)
{
    using namespace clang;

    auto VT = WalkVarTemplate(VTS->getSpecializedTemplate());
    auto USR = GetDeclUSR(VTS);
    auto TS = VT->FindPartialSpecialization(USR);
    if (TS != nullptr)
        return TS;

    TS = new VarTemplatePartialSpecialization();
    HandleDeclaration(VTS, TS);

    auto NS = GetNamespace(VTS);
    assert(NS && "Expected a valid namespace");
    TS->_Namespace = NS;
    TS->Name = VTS->getName();
    TS->TemplatedDecl = VT;
    TS->SpecializationKind = WalkTemplateSpecializationKind(VTS->getSpecializationKind());
    VT->Specializations.push_back(TS);

    auto& TAL = VTS->getTemplateArgs();
    if (auto TSI = VTS->getTypeAsWritten())
    {
        auto TL = TSI->getTypeLoc();
        auto TSL = TL.getAs<TemplateSpecializationTypeLoc>();
        TS->Arguments = WalkTemplateArgumentList(&TAL, &TSL);
    }

    WalkVariable(VTS, TS);

    return TS;
}

//-----------------------------------//

static CXXMethodKind GetMethodKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    switch(Name.getNameKind())
    {
    case DeclarationName::Identifier:
    case DeclarationName::ObjCZeroArgSelector:
    case DeclarationName::ObjCOneArgSelector:
    case DeclarationName::ObjCMultiArgSelector:
        return CXXMethodKind::Normal;
    case DeclarationName::CXXConstructorName:
        return CXXMethodKind::Constructor;
    case DeclarationName::CXXDestructorName:
        return CXXMethodKind::Destructor;
    case DeclarationName::CXXConversionFunctionName:
        return CXXMethodKind::Conversion;
    case DeclarationName::CXXOperatorName:
    case DeclarationName::CXXLiteralOperatorName:
        return CXXMethodKind::Operator;
    case DeclarationName::CXXUsingDirective:
        return CXXMethodKind::UsingDirective;
    }
    return CXXMethodKind::Normal;
}

static CXXOperatorKind GetOperatorKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    if (Name.getNameKind() != DeclarationName::CXXOperatorName)
        return CXXOperatorKind::None;

    switch(Name.getCXXOverloadedOperator())
    {
    case OO_None:
        return CXXOperatorKind::None;
    case NUM_OVERLOADED_OPERATORS:
        break;

    #define OVERLOADED_OPERATOR(Name,Spelling,Token,Unary,Binary,MemberOnly) \
    case OO_##Name: return CXXOperatorKind::Name;
    #include "clang/Basic/OperatorKinds.def"
    }

    llvm_unreachable("Unknown OverloadedOperator");
}

Method* Parser::WalkMethodCXX(const clang::CXXMethodDecl* MD)
{
    using namespace clang;

    // We could be in a redeclaration, so process the primary context.
    if (MD->getPrimaryContext() != MD)
        return WalkMethodCXX(cast<CXXMethodDecl>(MD->getPrimaryContext()));

    auto RD = MD->getParent();
    auto Decl = WalkDeclaration(RD, /*IgnoreSystemDecls=*/false);

    auto Class = static_cast<CppSharp::CppParser::AST::Class*>(Decl);

    // Check for an already existing method that came from the same declaration.
    auto USR = GetDeclUSR(MD);
    for (unsigned I = 0, E = Class->Methods.size(); I != E; ++I)
    {
         Method* Method = Class->Methods[I];
         if (Method->USR == USR)
             return Method;
    }
    for (unsigned I = 0, E = Class->Templates.size(); I != E; ++I)
    {
        Template* Template = Class->Templates[I];
        if (Template->TemplatedDecl->USR == USR)
            return static_cast<Method*>(Template->TemplatedDecl);
    }

    auto Method = new CppSharp::CppParser::Method();
    HandleDeclaration(MD, Method);

    Method->Access = ConvertToAccess(MD->getAccess());
    Method->MethodKind = GetMethodKindFromDecl(MD->getDeclName());
    Method->IsStatic = MD->isStatic();
    Method->IsVirtual = MD->isVirtual();
    Method->IsConst = MD->isConst();
    Method->IsOverride = MD->size_overridden_methods() > 0;
    switch (MD->getRefQualifier())
    {
    case clang::RefQualifierKind::RQ_None:
        Method->RefQualifier = RefQualifierKind::None;
        break;
    case clang::RefQualifierKind::RQ_LValue:
        Method->RefQualifier = RefQualifierKind::LValue;
        break;
    case clang::RefQualifierKind::RQ_RValue:
        Method->RefQualifier = RefQualifierKind::RValue;
        break;
    }

    WalkFunction(MD, Method);

    for (auto& M : Class->Methods)
    {
        if (M->USR == USR)
        {
            delete Method;
            return M;
        }
    }

    if (const CXXConstructorDecl* CD = dyn_cast<CXXConstructorDecl>(MD))
    {
        Method->IsDefaultConstructor = CD->isDefaultConstructor();
        Method->IsCopyConstructor = CD->isCopyConstructor();
        Method->IsMoveConstructor = CD->isMoveConstructor();
        Method->IsExplicit = CD->isExplicit();
    }
    else if (const CXXDestructorDecl* DD = dyn_cast<CXXDestructorDecl>(MD))
    {
    }
    else if (const CXXConversionDecl* CD = dyn_cast<CXXConversionDecl>(MD))
    {
        auto TL = MD->getTypeSourceInfo()->getTypeLoc().castAs<FunctionTypeLoc>();
        auto RTL = TL.getReturnLoc();
        Method->ConversionType = GetQualifiedType(CD->getConversionType(), &RTL);
    }
    
    Class->Methods.push_back(Method);

    return Method;
}

//-----------------------------------//

Field* Parser::WalkFieldCXX(const clang::FieldDecl* FD, Class* Class)
{
    using namespace clang;

    const auto& USR = GetDeclUSR(FD);

    auto FoundField = std::find_if(Class->Fields.begin(), Class->Fields.end(),
        [&](Field* Field) { return Field->USR == USR; });

    if (FoundField != Class->Fields.end())
        return *FoundField;

    auto F = new Field();
    HandleDeclaration(FD, F);

    F->_Namespace = Class;
    F->Name = FD->getName();
    auto TL = FD->getTypeSourceInfo()->getTypeLoc();
    F->QualifiedType = GetQualifiedType(FD->getType(), &TL);
    F->Access = ConvertToAccess(FD->getAccess());
    F->Class = Class;
    F->IsBitField = FD->isBitField();
    if (F->IsBitField && !F->IsDependent && !FD->getBitWidth()->isInstantiationDependent())
        F->BitWidth = FD->getBitWidthValue(C->getASTContext());

    Class->Fields.push_back(F);

    return F;
}

//-----------------------------------//

TranslationUnit* Parser::GetTranslationUnit(clang::SourceLocation Loc,
                                                      SourceLocationKind *Kind)
{
    using namespace clang;

    clang::SourceManager& SM = C->getSourceManager();

    if (Loc.isMacroID())
        Loc = SM.getExpansionLoc(Loc);

    StringRef File;

    auto LocKind = GetLocationKind(Loc);
    switch(LocKind)
    {
    case SourceLocationKind::Invalid:
        File = "<invalid>";
        break;
    case SourceLocationKind::Builtin:
        File = "<built-in>";
        break;
    case SourceLocationKind::CommandLine:
        File = "<command-line>";
        break;
    default:
        File = SM.getFilename(Loc);
        assert(!File.empty() && "Expected to find a valid file");
        break;
    }

    if (Kind)
        *Kind = LocKind;

    auto Unit = Lib->FindOrCreateModule(File);

    Unit->OriginalPtr = (void*) Unit;
    assert(Unit->OriginalPtr != nullptr);

    if (LocKind != SourceLocationKind::Invalid)
        Unit->IsSystemHeader = SM.isInSystemHeader(Loc);

    return Unit;
}

//-----------------------------------//

TranslationUnit* Parser::GetTranslationUnit(const clang::Decl* D)
{
    clang::SourceLocation Loc = D->getLocation();

    SourceLocationKind Kind;
    TranslationUnit* Unit = GetTranslationUnit(Loc, &Kind);

    return Unit;
}

DeclarationContext* Parser::GetNamespace(const clang::Decl* D,
    const clang::DeclContext *Ctx)
{
    using namespace clang;

    auto Context = Ctx;

    // If the declaration is at global scope, just early exit.
    if (Context->isTranslationUnit())
        return GetTranslationUnit(D);

    TranslationUnit* Unit = GetTranslationUnit(cast<Decl>(Context));

    // Else we need to do a more expensive check to get all the namespaces,
    // and then perform a reverse iteration to get the namespaces in order.
    typedef SmallVector<const DeclContext *, 8> ContextsTy;
    ContextsTy Contexts;

    for(; Context != nullptr; Context = Context->getParent())
        Contexts.push_back(Context);

    assert(Contexts.back()->isTranslationUnit());
    Contexts.pop_back();

    DeclarationContext* DC = Unit;

    for (auto I = Contexts.rbegin(), E = Contexts.rend(); I != E; ++I)
    {
        const auto* Ctx = *I;

        switch(Ctx->getDeclKind())
        {
        case Decl::Namespace:
        {
            auto ND = cast<NamespaceDecl>(Ctx);
            if (ND->isAnonymousNamespace())
                continue;
            auto Name = ND->getName();
            DC = DC->FindCreateNamespace(Name);
            ((Namespace*)DC)->IsAnonymous = ND->isAnonymousNamespace();
            ((Namespace*)DC)->IsInline = ND->isInline();
            HandleDeclaration(ND, DC);
            continue;
        }
        case Decl::LinkageSpec:
        {
            const LinkageSpecDecl* LD = cast<LinkageSpecDecl>(Ctx);
            continue;
        }
        case Decl::Record:
        {
            auto RD = cast<RecordDecl>(Ctx);
            DC = WalkRecord(RD);
            continue;
        }
        case Decl::CXXRecord:
        {
            auto RD = cast<CXXRecordDecl>(Ctx);
            DC = WalkRecordCXX(RD);
            continue;
        }
        case Decl::ClassTemplateSpecialization:
        {
            auto CTSpec = cast<ClassTemplateSpecializationDecl>(Ctx);
            DC = WalkClassTemplateSpecialization(CTSpec);
            continue;
        }
        case Decl::ClassTemplatePartialSpecialization:
        {
            auto CTPSpec = cast<ClassTemplatePartialSpecializationDecl>(Ctx);
            DC = WalkClassTemplatePartialSpecialization(CTPSpec);
            continue;
        }
        case Decl::Enum:
        {
            auto CTPSpec = cast<EnumDecl>(Ctx);
            DC = WalkEnum(CTPSpec);
            continue;
        }
        default:
        {
            StringRef Kind = Ctx->getDeclKindName();
            printf("Unhandled declaration context kind: %s\n", Kind.str().c_str());
            assert(0 && "Unhandled declaration context kind");
        } }
    }

    return DC;
}

DeclarationContext* Parser::GetNamespace(const clang::Decl *D)
{
    return GetNamespace(D, D->getDeclContext());
}

static PrimitiveType WalkBuiltinType(const clang::BuiltinType* Builtin)
{
    assert(Builtin && "Expected a builtin type");

    switch(Builtin->getKind())
    {
    case clang::BuiltinType::Void: return PrimitiveType::Void;
    case clang::BuiltinType::Bool: return PrimitiveType::Bool;

    case clang::BuiltinType::SChar:
    case clang::BuiltinType::Char_S: return PrimitiveType::Char;
    
    case clang::BuiltinType::UChar:
    case clang::BuiltinType::Char_U: return PrimitiveType::UChar;

    case clang::BuiltinType::WChar_S:
    case clang::BuiltinType::WChar_U: return PrimitiveType::WideChar;

    case clang::BuiltinType::Char16: return PrimitiveType::Char16;
    case clang::BuiltinType::Char32: return PrimitiveType::Char32;

    case clang::BuiltinType::Short: return PrimitiveType::Short;
    case clang::BuiltinType::UShort: return PrimitiveType::UShort;

    case clang::BuiltinType::Int: return PrimitiveType::Int;
    case clang::BuiltinType::UInt: return PrimitiveType::UInt;

    case clang::BuiltinType::Long: return PrimitiveType::Long;
    case clang::BuiltinType::ULong: return PrimitiveType::ULong;
    
    case clang::BuiltinType::LongLong: return PrimitiveType::LongLong;
    case clang::BuiltinType::ULongLong: return PrimitiveType::ULongLong;

    case clang::BuiltinType::Int128: return PrimitiveType::Int128;
    case clang::BuiltinType::UInt128: return PrimitiveType::UInt128;

    case clang::BuiltinType::Half: return PrimitiveType::Half;
    case clang::BuiltinType::Float: return PrimitiveType::Float;
    case clang::BuiltinType::Double: return PrimitiveType::Double;
    case clang::BuiltinType::LongDouble: return PrimitiveType::LongDouble;
    case clang::BuiltinType::Float128: return PrimitiveType::Half;

    case clang::BuiltinType::NullPtr: return PrimitiveType::Null;

    default: break;
    }

    return PrimitiveType::Null;
}

//-----------------------------------//

clang::TypeLoc ResolveTypeLoc(clang::TypeLoc TL, clang::TypeLoc::TypeLocClass Class)
{
    using namespace clang;

    auto TypeLocClass = TL.getTypeLocClass();

    if (TypeLocClass == Class)
    {
        return TL;
    }
    if (TypeLocClass == TypeLoc::Qualified)
    {
        auto UTL = TL.getUnqualifiedLoc();
        TL = UTL;
    }
    else if (TypeLocClass == TypeLoc::Elaborated)
    {
        auto ETL = TL.getAs<ElaboratedTypeLoc>();
        auto ITL = ETL.getNextTypeLoc();
        TL = ITL;
    }
    else if (TypeLocClass == TypeLoc::Paren)
    {
        auto PTL = TL.getAs<ParenTypeLoc>();
        TL = PTL.getNextTypeLoc();
    }

    assert(TL.getTypeLocClass() == Class);
    return TL;
}

static CallingConvention ConvertCallConv(clang::CallingConv CC)
{
    using namespace clang;

    switch(CC)
    {
    case CC_C:
        return CallingConvention::C;
    case CC_X86StdCall:
        return CallingConvention::StdCall;
    case CC_X86FastCall:
        return CallingConvention::FastCall;
    case CC_X86ThisCall:
        return CallingConvention::ThisCall;
    default:
        return CallingConvention::Unknown;
    }
}

static ParserIntType ConvertIntType(clang::TargetInfo::IntType IT)
{
    switch (IT)
    {
    case clang::TargetInfo::IntType::NoInt:
        return ParserIntType::NoInt;
    case clang::TargetInfo::IntType::SignedChar:
        return ParserIntType::SignedChar;
    case clang::TargetInfo::IntType::UnsignedChar:
        return ParserIntType::UnsignedChar;
    case clang::TargetInfo::IntType::SignedShort:
        return ParserIntType::SignedShort;
    case clang::TargetInfo::IntType::UnsignedShort:
        return ParserIntType::UnsignedShort;
    case clang::TargetInfo::IntType::SignedInt:
        return ParserIntType::SignedInt;
    case clang::TargetInfo::IntType::UnsignedInt:
        return ParserIntType::UnsignedInt;
    case clang::TargetInfo::IntType::SignedLong:
        return ParserIntType::SignedLong;
    case clang::TargetInfo::IntType::UnsignedLong:
        return ParserIntType::UnsignedLong;
    case clang::TargetInfo::IntType::SignedLongLong:
        return ParserIntType::SignedLongLong;
    case clang::TargetInfo::IntType::UnsignedLongLong:
        return ParserIntType::UnsignedLongLong;
    }

    llvm_unreachable("Unknown parser integer type");
}

static const clang::Type* GetFinalType(const clang::Type* Ty)
{
    auto FinalType = Ty;
    while (true)
    {
        FinalType = FinalType->getUnqualifiedDesugaredType();
        if (FinalType->getPointeeType().isNull())
            return FinalType;
        FinalType = FinalType->getPointeeType().getTypePtr();
    }
}

bool Parser::ShouldCompleteType(const clang::QualType& QualType, bool LocValid)
{
    // HACK: the completion of types is temporarily suspended because of problems with QtWidgets; will restore when it's time to wrap functions in template types
    return false;
    auto FinalType = GetFinalType(QualType.getTypePtr());
    if (auto Tag = FinalType->getAsTagDecl())
    {
        if (auto CTS = llvm::dyn_cast<clang::ClassTemplateSpecializationDecl>(Tag))
        {
            // we cannot get a location in some cases of template arguments
            if (!LocValid)
                return false;

            auto TAL = &CTS->getTemplateArgs();
            for (size_t i = 0; i < TAL->size(); i++)
            {
                auto TA = TAL->get(i);
                if (TA.getKind() == clang::TemplateArgument::ArgKind::Type)
                {
                    auto Type = TA.getAsType();
                    if (Type->isVoidType())
                        return false;
                }
            }
        }
        auto Unit = GetTranslationUnit(Tag);
        // HACK: completing all system types overflows the managed stack
        // while running the AST converter since the latter is a giant indirect recursion
        // this solution is a hack because we might need to complete system template specialisations
        // such as std:string or std::vector in order to represent them in the target language
        return !Unit->IsSystemHeader;
    }
    return true;
}

Type* Parser::WalkType(clang::QualType QualType, clang::TypeLoc* TL,
    bool DesugarType)
{
    using namespace clang;

    if (QualType.isNull())
        return nullptr;

    auto LocValid = TL && !TL->isNull();

    auto CompleteType = ShouldCompleteType(QualType, LocValid);
    if (CompleteType)
        C->getSema().RequireCompleteType(
            LocValid ? TL->getLocStart() : clang::SourceLocation(), QualType, 1);

    const clang::Type* Type = QualType.getTypePtr();

    if (DesugarType)
    {
        clang::QualType Desugared = QualType.getDesugaredType(*AST);
        assert(!Desugared.isNull() && "Expected a valid desugared type");
        Type = Desugared.getTypePtr();
    }

    CppSharp::CppParser::AST::Type* Ty = nullptr;

    assert(Type && "Expected a valid type");
    switch(Type->getTypeClass())
    {
    case clang::Type::Atomic:
    {
        auto Atomic = Type->getAs<clang::AtomicType>();
        assert(Atomic && "Expected an atomic type");

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        Ty = WalkType(Atomic->getValueType(), &Next);
        break;
    }
    case clang::Type::Attributed:
    {
        auto Attributed = Type->getAs<clang::AttributedType>();
        assert(Attributed && "Expected an attributed type");

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto AT = new AttributedType();

        auto Modified = Attributed->getModifiedType();
        AT->Modified = GetQualifiedType(Modified, &Next);

        auto Equivalent = Attributed->getEquivalentType();
        AT->Equivalent = GetQualifiedType(Equivalent, &Next);

        Ty = AT;
        break;
    }
    case clang::Type::Builtin:
    {
        auto Builtin = Type->getAs<clang::BuiltinType>();
        assert(Builtin && "Expected a builtin type");
    
        auto BT = new BuiltinType();
        BT->Type = WalkBuiltinType(Builtin);
        
        Ty = BT;
        break;
    }
    case clang::Type::Enum:
    {
        auto ET = Type->getAs<clang::EnumType>();
        EnumDecl* ED = ET->getDecl();

        auto TT = new TagType();
        TT->Declaration = TT->Declaration = WalkDeclaration(ED, /*IgnoreSystemDecls=*/false);

        Ty = TT;
        break;
    }
    case clang::Type::Pointer:
    {
        auto Pointer = Type->getAs<clang::PointerType>();
        
        auto P = new PointerType();
        P->Modifier = PointerType::TypeModifier::Pointer;

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Pointee = Pointer->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, &Next);

        Ty = P;
        break;
    }
    case clang::Type::Typedef:
    {
        auto TT = Type->getAs<clang::TypedefType>();
        auto TD = TT->getDecl();

        auto TTL = TD->getTypeSourceInfo()->getTypeLoc();
        auto TDD = static_cast<TypedefNameDecl*>(WalkDeclaration(TD,
            /*IgnoreSystemDecls=*/false));

        auto Type = new TypedefType();
        Type->Declaration = TDD;

        Ty = Type;
        break;
    }
    case clang::Type::Decayed:
    {
        auto DT = Type->getAs<clang::DecayedType>();

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Type = new DecayedType();
        Type->Decayed = GetQualifiedType(DT->getDecayedType(), &Next);
        Type->Original = GetQualifiedType(DT->getOriginalType(), &Next);
        Type->Pointee = GetQualifiedType(DT->getPointeeType(), &Next);

        Ty = Type;
        break;
    }
    case clang::Type::Elaborated:
    {
        auto ET = Type->getAs<clang::ElaboratedType>();

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        Ty = WalkType(ET->getNamedType(), &Next);
        break;
    }
    case clang::Type::Record:
    {
        auto RT = Type->getAs<clang::RecordType>();
        RecordDecl* RD = RT->getDecl();

        auto TT = new TagType();
        TT->Declaration = WalkDeclaration(RD, /*IgnoreSystemDecls=*/false);

        Ty = TT;
        break;
    }
    case clang::Type::Paren:
    {
        auto PT = Type->getAs<clang::ParenType>();

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        Ty = WalkType(PT->getInnerType(), &Next);
        break;
    }
    case clang::Type::ConstantArray:
    {
        auto AT = AST->getAsConstantArrayType(QualType);

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        auto ElemTy = AT->getElementType();
        A->QualifiedType = GetQualifiedType(ElemTy, &Next);
        A->SizeType = ArrayType::ArraySize::Constant;
        A->Size = AST->getConstantArrayElementCount(AT);
        if (!ElemTy->isDependentType())
            A->ElementSize = (long)AST->getTypeSize(ElemTy);

        Ty = A;
        break;
    }
    case clang::Type::IncompleteArray:
    {
        auto AT = AST->getAsIncompleteArrayType(QualType);

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        A->QualifiedType = GetQualifiedType(AT->getElementType(), &Next);
        A->SizeType = ArrayType::ArraySize::Incomplete;

        Ty = A;
        break;
    }
    case clang::Type::DependentSizedArray:
    {
        auto AT = AST->getAsDependentSizedArrayType(QualType);

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        A->QualifiedType = GetQualifiedType(AT->getElementType(), &Next);
        A->SizeType = ArrayType::ArraySize::Dependent;
        //A->Size = AT->getSizeExpr();

        Ty = A;
        break;
    }
    case clang::Type::FunctionNoProto:
    {
        auto FP = Type->getAs<clang::FunctionNoProtoType>();

        FunctionNoProtoTypeLoc FTL;
        TypeLoc RL;
        TypeLoc Next;
        if (LocValid)
        {
            while (!TL->isNull() && TL->getTypeLocClass() != TypeLoc::FunctionNoProto)
            {
                Next = TL->getNextTypeLoc();
                TL = &Next;
            }

            if (!TL->isNull() && TL->getTypeLocClass() == TypeLoc::FunctionNoProto)
            {
                FTL = TL->getAs<FunctionNoProtoTypeLoc>();
                RL = FTL.getReturnLoc();
            }
        }

        auto F = new FunctionType();
        F->ReturnType = GetQualifiedType(FP->getReturnType(), &RL);
        F->CallingConvention = ConvertCallConv(FP->getCallConv());

        Ty = F;
        break;
    }
    case clang::Type::FunctionProto:
    {
        auto FP = Type->getAs<clang::FunctionProtoType>();

        FunctionProtoTypeLoc FTL;
        TypeLoc RL;
        TypeLoc Next;
        if (LocValid)
        {
            while (!TL->isNull() && TL->getTypeLocClass() != TypeLoc::FunctionProto)
            {
                Next = TL->getNextTypeLoc();
                TL = &Next;
            }

            if (!TL->isNull() && TL->getTypeLocClass() == TypeLoc::FunctionProto)
            {
                FTL = TL->getAs<FunctionProtoTypeLoc>();
                RL = FTL.getReturnLoc();
            }
        }

        auto F = new FunctionType();
        F->ReturnType = GetQualifiedType(FP->getReturnType(), &RL);
        F->CallingConvention = ConvertCallConv(FP->getCallConv());

        for (unsigned i = 0; i < FP->getNumParams(); ++i)
        {
            auto FA = new Parameter();
            if (FTL && FTL.getParam(i))
            {
                auto PVD = FTL.getParam(i);
                HandleDeclaration(PVD, FA);

                auto PTL = PVD->getTypeSourceInfo()->getTypeLoc();

                FA->Name = PVD->getNameAsString();
                FA->QualifiedType = GetQualifiedType(PVD->getOriginalType(), &PTL);
            }
            else
            {
                auto Arg = FP->getParamType(i);
                FA->Name = "";
                FA->QualifiedType = GetQualifiedType(Arg);

                // In this case we have no valid value to use as a pointer so
                // use a special value known to the managed side to make sure
                // it gets ignored.
                FA->OriginalPtr = IgnorePtr;
            }
            F->Parameters.push_back(FA);
        }

        Ty = F;
        break;
    }
    case clang::Type::TypeOf:
    {
        auto TO = Type->getAs<clang::TypeOfType>();

        Ty = WalkType(TO->getUnderlyingType());
        break;
    }
    case clang::Type::TypeOfExpr:
    {
        auto TO = Type->getAs<clang::TypeOfExprType>();

        Ty = WalkType(TO->getUnderlyingExpr()->getType());
        break;
    }
    case clang::Type::MemberPointer:
    {
        auto MP = Type->getAs<clang::MemberPointerType>();

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto MPT = new MemberPointerType();
        MPT->Pointee = GetQualifiedType(MP->getPointeeType(), &Next);
        
        Ty = MPT;
        break;
    }
    case clang::Type::TemplateSpecialization:
    {
        auto TS = Type->getAs<clang::TemplateSpecializationType>();
        auto TST = new TemplateSpecializationType();
        
        TemplateName Name = TS->getTemplateName();
        TST->Template = static_cast<Template*>(WalkDeclaration(
            Name.getAsTemplateDecl(), 0));
        if (TS->isSugared())
            TST->Desugared = GetQualifiedType(TS->desugar(), TL);

        TypeLoc UTL, ETL, ITL;

        if (LocValid)
        {
            auto TypeLocClass = TL->getTypeLocClass();
            if (TypeLocClass == TypeLoc::Qualified)
            {
                UTL = TL->getUnqualifiedLoc();
                TL = &UTL;
            }
            else if (TypeLocClass == TypeLoc::Elaborated)
            {
                ETL = TL->getAs<ElaboratedTypeLoc>();
                ITL = ETL.getNextTypeLoc();
                TL = &ITL;
            }

            assert(TL->getTypeLocClass() == TypeLoc::TemplateSpecialization);
        }

        TemplateSpecializationTypeLoc TSpecTL;
        TemplateSpecializationTypeLoc *TSTL = 0;
        if (LocValid)
        {
            TSpecTL = TL->getAs<TemplateSpecializationTypeLoc>();
            TSTL = &TSpecTL;
        }

        ArrayRef<clang::TemplateArgument> TSArgs(TS->getArgs(), TS->getNumArgs());
        TemplateArgumentList TArgs(TemplateArgumentList::OnStack, TSArgs);
        TST->Arguments = WalkTemplateArgumentList(&TArgs, TSTL);

        Ty = TST;
        break;
    }
    case clang::Type::DependentTemplateSpecialization:
    {
        auto TS = Type->getAs<clang::DependentTemplateSpecializationType>();
        auto TST = new DependentTemplateSpecializationType();

        if (TS->isSugared())
            TST->Desugared = GetQualifiedType(TS->desugar(), TL);

        TypeLoc UTL, ETL, ITL;

        if (LocValid)
        {
            auto TypeLocClass = TL->getTypeLocClass();
            if (TypeLocClass == TypeLoc::Qualified)
            {
                UTL = TL->getUnqualifiedLoc();
                TL = &UTL;
            }
            else if (TypeLocClass == TypeLoc::Elaborated)
            {
                ETL = TL->getAs<ElaboratedTypeLoc>();
                ITL = ETL.getNextTypeLoc();
                TL = &ITL;
            }

            assert(TL->getTypeLocClass() == TypeLoc::DependentTemplateSpecialization);
        }

        TemplateSpecializationTypeLoc TSpecTL;
        TemplateSpecializationTypeLoc *TSTL = 0;
        if (LocValid)
        {
            TSpecTL = TL->getAs<TemplateSpecializationTypeLoc>();
            TSTL = &TSpecTL;
        }

        ArrayRef<clang::TemplateArgument> TSArgs(TS->getArgs(), TS->getNumArgs());
        TemplateArgumentList TArgs(TemplateArgumentList::OnStack, TSArgs);
        TST->Arguments = WalkTemplateArgumentList(&TArgs, TSTL);

        Ty = TST;
        break;
    }
    case clang::Type::TemplateTypeParm:
    {
        auto TP = Type->getAs<TemplateTypeParmType>();

        auto TPT = new CppSharp::CppParser::TemplateParameterType();

        if (auto Ident = TP->getIdentifier())
            TPT->Parameter->Name = Ident->getName();

        TypeLoc UTL, ETL, ITL, Next;

        if (LocValid)
        {
            auto TypeLocClass = TL->getTypeLocClass();
            if (TypeLocClass == TypeLoc::Qualified)
            {
                UTL = TL->getUnqualifiedLoc();
                TL = &UTL;
            }
            else if (TypeLocClass == TypeLoc::Elaborated)
            {
                ETL = TL->getAs<ElaboratedTypeLoc>();
                ITL = ETL.getNextTypeLoc();
                TL = &ITL;
            }

            while (TL->getTypeLocClass() != TypeLoc::TemplateTypeParm)
            {
                Next = TL->getNextTypeLoc();
                TL = &Next;
            }

            assert(TL->getTypeLocClass() == TypeLoc::TemplateTypeParm);
            auto TTTL = TL->getAs<TemplateTypeParmTypeLoc>();

            TPT->Parameter = WalkTypeTemplateParameter(TTTL.getDecl());
        }
        TPT->Depth = TP->getDepth();
        TPT->Index = TP->getIndex();
        TPT->IsParameterPack = TP->isParameterPack();

        Ty = TPT;
        break;
    }
    case clang::Type::SubstTemplateTypeParm:
    {
        auto TP = Type->getAs<SubstTemplateTypeParmType>();
        auto TPT = new TemplateParameterSubstitutionType();

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto RepTy = TP->getReplacementType();
        TPT->Replacement = GetQualifiedType(RepTy, &Next);

        Ty = TPT;
        break;
    }
    case clang::Type::InjectedClassName:
    {
        auto ICN = Type->getAs<clang::InjectedClassNameType>();
        auto ICNT = new InjectedClassNameType();
        ICNT->Class = static_cast<Class*>(WalkDeclaration(
            ICN->getDecl(), 0));
        ICNT->InjectedSpecializationType = GetQualifiedType(
            ICN->getInjectedSpecializationType());

        Ty = ICNT;
        break;
    }
    case clang::Type::DependentName:
    {
        auto DN = Type->getAs<clang::DependentNameType>();
        auto DNT = new DependentNameType();
        if (DN->isSugared())
            DNT->Desugared = GetQualifiedType(DN->desugar(), TL);

        Ty = DNT;
        break;
    }
    case clang::Type::LValueReference:
    {
        auto LR = Type->getAs<clang::LValueReferenceType>();

        auto P = new PointerType();
        P->Modifier = PointerType::TypeModifier::LVReference;

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, &Next);

        Ty = P;
        break;
    }
    case clang::Type::RValueReference:
    {
        auto LR = Type->getAs<clang::RValueReferenceType>();

        auto P = new PointerType();
        P->Modifier = PointerType::TypeModifier::RVReference;

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, &Next);

        Ty = P;
        break;
    }
    case clang::Type::UnaryTransform:
    {
        auto UT = Type->getAs<clang::UnaryTransformType>();

        auto UTT = new UnaryTransformType();
        auto Loc = TL->getAs<UnaryTransformTypeLoc>().getUnderlyingTInfo()->getTypeLoc();
        UTT->Desugared = GetQualifiedType(UT->isSugared() ? UT->desugar() : UT->getBaseType(), &Loc);
        UTT->BaseType = GetQualifiedType(UT->getBaseType(), &Loc);

        Ty = UTT;
        break;
    }
    case clang::Type::Vector:
    {
        auto V = Type->getAs<clang::VectorType>();

        auto VT = new VectorType();
        VT->ElementType = GetQualifiedType(V->getElementType());
        VT->NumElements = V->getNumElements();

        Ty = VT;
        break;
    }
    case clang::Type::PackExpansion:
    {
        // TODO: stubbed
        Ty = new PackExpansionType();
        break;
    }
    case clang::Type::Decltype:
    {
        auto DT = Type->getAs<clang::DecltypeType>();
        Ty = WalkType(DT->getUnderlyingType(), TL);
        break;
    }
    default:
    {   
        Debug("Unhandled type class '%s'\n", Type->getTypeClassName());
        return nullptr;
    } }

    Ty->IsDependent = Type->isDependentType();
    return Ty;
}

//-----------------------------------//

Enumeration* Parser::WalkEnum(const clang::EnumDecl* ED)
{
    using namespace clang;

    auto NS = GetNamespace(ED);
    assert(NS && "Expected a valid namespace");

    auto E = NS->FindEnum(ED->getCanonicalDecl());
    if (E && !E->IsIncomplete)
        return E;

    if (!E)
    {
        auto Name = GetTagDeclName(ED);
        if (!Name.empty())
            E = NS->FindEnum(Name, /*Create=*/false);
        else
        {
            // Enum with no identifier - try to find existing enum through enum items
            for (auto it = ED->enumerator_begin(); it != ED->enumerator_end(); ++it)
            {
                EnumConstantDecl* ECD = (*it);
                auto EnumItemName = ECD->getNameAsString();
                E = NS->FindEnumWithItem(EnumItemName);
                break;
            }
        }
    }

    if (E && !E->IsIncomplete)
        return E;

    if (!E)
    {
        auto Name = GetTagDeclName(ED);
        if (!Name.empty())
            E = NS->FindEnum(Name, /*Create=*/true);
        else
        {
            E = new Enumeration();
            E->Name = Name;
            E->_Namespace = NS;
            NS->Enums.push_back(E);
        }
        HandleDeclaration(ED, E);
    }

    if (ED->isScoped())
        E->Modifiers = (Enumeration::EnumModifiers)
            ((int)E->Modifiers | (int)Enumeration::EnumModifiers::Scoped);

    // Get the underlying integer backing the enum.
    clang::QualType IntType = ED->getIntegerType();
    E->Type = WalkType(IntType, 0);
    E->BuiltinType = static_cast<BuiltinType*>(WalkType(IntType, 0,
        /*DesugarType=*/true));

    if (!ED->isThisDeclarationADefinition())
    {
        E->IsIncomplete = true;
        return E;
    }

    E->IsIncomplete = false;
    for(auto it = ED->enumerator_begin(); it != ED->enumerator_end(); ++it)
    {
        E->Items.push_back(WalkEnumItem(*it));
    }

    return E;
}

Enumeration::Item* Parser::WalkEnumItem(clang::EnumConstantDecl* ECD)
{
    auto EnumItem = new Enumeration::Item();
    HandleDeclaration(ECD, EnumItem);

    EnumItem->Name = ECD->getNameAsString();
    auto Value = ECD->getInitVal();
    EnumItem->Value = Value.isSigned() ? Value.getSExtValue()
        : Value.getZExtValue();
    EnumItem->_Namespace = GetNamespace(ECD);

    std::string Text;
    if (GetDeclText(ECD->getSourceRange(), Text))
        EnumItem->Expression = Text;

    return EnumItem;
}

//-----------------------------------//

static const clang::CodeGen::CGFunctionInfo& GetCodeGenFuntionInfo(
    clang::CodeGen::CodeGenTypes* CodeGenTypes, const clang::FunctionDecl* FD)
{
    using namespace clang;
    if (auto CD = dyn_cast<clang::CXXConstructorDecl>(FD)) {
        return CodeGenTypes->arrangeCXXStructorDeclaration(CD, clang::CodeGen::StructorType::Base);
    } else if (auto DD = dyn_cast<clang::CXXDestructorDecl>(FD)) {
        return CodeGenTypes->arrangeCXXStructorDeclaration(DD, clang::CodeGen::StructorType::Base);
    }

    return CodeGenTypes->arrangeFunctionDeclaration(FD);
}

bool Parser::CanCheckCodeGenInfo(clang::Sema& S, const clang::Type* Ty)
{
    auto FinalType = GetFinalType(Ty);

    if (Ty->isDependentType() || FinalType->isDependentType() ||
        FinalType->isInstantiationDependentType())
        return false;

    if (auto RT = FinalType->getAs<clang::RecordType>())
        return RT->getDecl()->getDefinition() != 0;

    // Lock in the MS inheritance model if we have a member pointer to a class,
    // else we get an assertion error inside Clang's codegen machinery.
    if (C->getASTContext().getTargetInfo().getCXXABI().isMicrosoft())
    {
        if (auto MPT = Ty->getAs<clang::MemberPointerType>())
            if (!MPT->isDependentType())
                S.RequireCompleteType(clang::SourceLocation(), clang::QualType(Ty, 0), 1);
    }

    return true;
}

static clang::TypeLoc DesugarTypeLoc(const clang::TypeLoc& Loc)
{
    using namespace clang;

    switch (Loc.getTypeLocClass())
    {
    case TypeLoc::TypeLocClass::Attributed:
    {
        auto ATL = Loc.getAs<AttributedTypeLoc>();
        return ATL.getModifiedLoc();
    }
    case TypeLoc::TypeLocClass::Paren:
    {
        auto PTL = Loc.getAs<ParenTypeLoc>();
        return PTL.getInnerLoc();
    }
    }

    return Loc;
}

void Parser::WalkFunction(const clang::FunctionDecl* FD, Function* F,
                          bool IsDependent)
{
    using namespace clang;

    assert (FD->getBuiltinID() == 0);
    auto FT = FD->getType()->getAs<clang::FunctionType>();

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    F->Name = FD->getNameAsString();
    F->_Namespace = NS;
    F->IsVariadic = FD->isVariadic();
    F->IsInline = FD->isInlined();
    F->IsDependent = FD->isDependentContext();
    F->IsPure = FD->isPure();
    F->IsDeleted = FD->isDeleted();
    if (auto InstantiatedFrom = FD->getInstantiatedFromMemberFunction())
        F->InstantiatedFrom = static_cast<Function*>(WalkDeclaration(InstantiatedFrom));

    auto CC = FT->getCallConv();
    F->CallingConvention = ConvertCallConv(CC);

    F->OperatorKind = GetOperatorKindFromDecl(FD->getDeclName());

    TypeLoc RTL;
    if (auto TSI = FD->getTypeSourceInfo())
    {
        auto Loc = DesugarTypeLoc(TSI->getTypeLoc());
        auto FTL = Loc.getAs<FunctionTypeLoc>();
        if (FTL)
        {
            RTL = FTL.getReturnLoc();

            auto& SM = C->getSourceManager();
            auto headStartLoc = GetDeclStartLocation(C.get(), FD);
            auto headEndLoc = SM.getExpansionLoc(FTL.getLParenLoc());
            auto headRange = clang::SourceRange(headStartLoc, headEndLoc);

            HandlePreprocessedEntities(F, headRange, MacroLocation::FunctionHead);
            HandlePreprocessedEntities(F, FTL.getParensRange(), MacroLocation::FunctionParameters);
        }
    }

    F->ReturnType = GetQualifiedType(FD->getReturnType(), &RTL);

    const auto& Mangled = GetDeclMangledName(FD);
    F->Mangled = Mangled;

    clang::SourceLocation ParamStartLoc = FD->getLocStart();
    clang::SourceLocation ResultLoc;

    auto FTSI = FD->getTypeSourceInfo();
    if (FTSI)
    {
        auto FTL = FTSI->getTypeLoc();
        while (FTL && !FTL.getAs<FunctionTypeLoc>())
            FTL = FTL.getNextTypeLoc();

        if (FTL)
        {
            auto FTInfo = FTL.castAs<FunctionTypeLoc>();
            assert (!FTInfo.isNull());

            ParamStartLoc = FTInfo.getLParenLoc();
            ResultLoc = FTInfo.getReturnLoc().getLocStart();
        }
    }

    clang::SourceLocation BeginLoc = FD->getLocStart();
    if (ResultLoc.isValid())
        BeginLoc = ResultLoc;

    clang::SourceRange Range(BeginLoc, FD->getLocEnd());

    std::string Sig;
    if (GetDeclText(Range, Sig))
        F->Signature = Sig;

    for (const auto& VD : FD->parameters())
    {
        auto P = new Parameter();
        P->Name = VD->getNameAsString();

        TypeLoc PTL;
        if (auto TSI = VD->getTypeSourceInfo())
            PTL = VD->getTypeSourceInfo()->getTypeLoc();

        auto paramRange = VD->getSourceRange();
        paramRange.setBegin(ParamStartLoc);

        HandlePreprocessedEntities(P, paramRange, MacroLocation::FunctionParameters);

        P->QualifiedType = GetQualifiedType(VD->getOriginalType(), &PTL);
        P->HasDefaultValue = VD->hasDefaultArg();
        P->_Namespace = NS;
        P->Index = VD->getFunctionScopeIndex();
        if (VD->hasDefaultArg() && !VD->hasUnparsedDefaultArg())
        {
            if (VD->hasUninstantiatedDefaultArg())
                P->DefaultArgument = WalkExpression(VD->getUninstantiatedDefaultArg());
            else
                P->DefaultArgument = WalkExpression(VD->getDefaultArg());
        }
        HandleDeclaration(VD, P);

        F->Parameters.push_back(P);

        ParamStartLoc = VD->getLocEnd();
    }

    auto& CXXABI = CodeGenTypes->getCXXABI();
    bool HasThisReturn = false;
    if (auto CD = dyn_cast<CXXConstructorDecl>(FD))
        HasThisReturn = CXXABI.HasThisReturn(GlobalDecl(CD, Ctor_Complete));
    else if (auto DD = dyn_cast<CXXDestructorDecl>(FD))
        HasThisReturn = CXXABI.HasThisReturn(GlobalDecl(DD, Dtor_Complete));
    else
        HasThisReturn = CXXABI.HasThisReturn(FD);

    F->HasThisReturn = HasThisReturn;

    if (auto FTSI = FD->getTemplateSpecializationInfo())
        F->SpecializationInfo = WalkFunctionTemplateSpec(FTSI, F);

    const CXXMethodDecl* MD;
    if ((MD = dyn_cast<CXXMethodDecl>(FD)) && !MD->isStatic() &&
        !CanCheckCodeGenInfo(C->getSema(), MD->getThisType(C->getASTContext()).getTypePtr()))
        return;

    if (!CanCheckCodeGenInfo(C->getSema(), FD->getReturnType().getTypePtr()))
        return;

    for (const auto& P : FD->parameters())
        if (!CanCheckCodeGenInfo(C->getSema(), P->getType().getTypePtr()))
            return;

    auto& CGInfo = GetCodeGenFuntionInfo(CodeGenTypes, FD);
    F->IsReturnIndirect = CGInfo.getReturnInfo().isIndirect();

    unsigned Index = 0;
    for (auto I = CGInfo.arg_begin(), E = CGInfo.arg_end(); I != E; I++)
    {
        // Skip the first argument as it's the return type.
        if (I == CGInfo.arg_begin())
            continue;
        if (Index >= F->Parameters.size())
            continue;
        F->Parameters[Index++]->IsIndirect = I->info.isIndirect();
    }
}

Function* Parser::WalkFunction(const clang::FunctionDecl* FD, bool IsDependent,
                                     bool AddToNamespace)
{
    using namespace clang;

    assert (FD->getBuiltinID() == 0);

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(FD);
    auto F = NS->FindFunction(USR);
    if (F != nullptr)
        return F;

    F = new Function();
    HandleDeclaration(FD, F);

    WalkFunction(FD, F, IsDependent);

    if (AddToNamespace)
        NS->Functions.push_back(F);

    return F;
}

//-----------------------------------//

SourceLocationKind Parser::GetLocationKind(const clang::SourceLocation& Loc)
{
    using namespace clang;

    clang::SourceManager& SM = C->getSourceManager();
    clang::PresumedLoc PLoc = SM.getPresumedLoc(Loc);

    if (PLoc.isInvalid())
        return SourceLocationKind::Invalid;

    const char *FileName = PLoc.getFilename();

    if(strcmp(FileName, "<built-in>") == 0)
        return SourceLocationKind::Builtin;

    if(strcmp(FileName, "<command line>") == 0)
        return SourceLocationKind::CommandLine;

    if(SM.getFileCharacteristic(Loc) == clang::SrcMgr::C_User)
        return SourceLocationKind::User;

    return SourceLocationKind::System;
}

bool Parser::IsValidDeclaration(const clang::SourceLocation& Loc)
{
    auto Kind = GetLocationKind(Loc);

    return Kind == SourceLocationKind::User;
}

//-----------------------------------//

void Parser::WalkAST()
{
    auto TU = AST->getTranslationUnitDecl();
    for(auto it = TU->decls_begin(); it != TU->decls_end(); ++it)
    {
        clang::Decl* D = (*it);
        WalkDeclarationDef(D);
    }
}

//-----------------------------------//

void Parser::WalkVariable(const clang::VarDecl* VD, Variable* Var)
{
    HandleDeclaration(VD, Var);

    Var->Name = VD->getName();
    Var->Access = ConvertToAccess(VD->getAccess());

    auto TL = VD->getTypeSourceInfo()->getTypeLoc();
    Var->QualifiedType = GetQualifiedType(VD->getType(), &TL);

    auto Mangled = GetDeclMangledName(VD);
    Var->Mangled = Mangled;
}

Variable* Parser::WalkVariable(const clang::VarDecl *VD)
{
    using namespace clang;

    auto NS = GetNamespace(VD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(VD);
    if (auto Var = NS->FindVariable(USR))
        return Var;

    auto Var = new Variable();
    Var->_Namespace = NS;

    WalkVariable(VD, Var);

    NS->Variables.push_back(Var);

    return Var;
}

//-----------------------------------//

Friend* Parser::WalkFriend(const clang::FriendDecl *FD)
{
    using namespace clang;

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    auto FriendDecl = FD->getFriendDecl();

    // Work around clangIndex's lack of USR handling for friends and pass the
    // pointed to friend declaration instead.
    auto USR = GetDeclUSR(FriendDecl ? ((Decl*)FriendDecl) : FD);
    if (auto F = NS->FindFriend(USR))
        return F;

    auto F = new Friend();
    HandleDeclaration(FD, F);
    F->_Namespace = NS;

    if (FriendDecl)
    {
        F->Declaration = GetDeclarationFromFriend(FriendDecl);
    }

    NS->Friends.push_back(F);

    return F;
}

//-----------------------------------//

bool Parser::GetDeclText(clang::SourceRange SR, std::string& Text)
{
    using namespace clang;
    clang::SourceManager& SM = C->getSourceManager();
    const LangOptions &LangOpts = C->getLangOpts();

    auto Range = CharSourceRange::getTokenRange(SR);

    bool Invalid;
    Text = Lexer::getSourceText(Range, SM, LangOpts, &Invalid);

    return !Invalid && !Text.empty();
}

PreprocessedEntity* Parser::WalkPreprocessedEntity(
    Declaration* Decl, clang::PreprocessedEntity* PPEntity)
{
    using namespace clang;

    for (unsigned I = 0, E = Decl->PreprocessedEntities.size();
        I != E; ++I)
    {
        auto Entity = Decl->PreprocessedEntities[I];
        if (Entity->OriginalPtr == PPEntity)
            return Entity;
    }

    auto& P = C->getPreprocessor();

    PreprocessedEntity* Entity = 0;

    switch(PPEntity->getKind())
    {
    case clang::PreprocessedEntity::MacroExpansionKind:
    {
        auto ME = cast<clang::MacroExpansion>(PPEntity);
        auto Expansion = new MacroExpansion();
        auto MD = ME->getDefinition();
        if (MD && MD->getKind() != clang::PreprocessedEntity::InvalidKind)
            Expansion->Definition = (MacroDefinition*)
                WalkPreprocessedEntity(Decl, ME->getDefinition());
        Entity = Expansion;

        std::string Text;
        GetDeclText(PPEntity->getSourceRange(), Text);

        static_cast<MacroExpansion*>(Entity)->Text = Text;
        break;
    }
    case clang::PreprocessedEntity::MacroDefinitionKind:
    {
        auto MD = cast<clang::MacroDefinitionRecord>(PPEntity);

        if (!IsValidDeclaration(MD->getLocation()))
            break;

        const IdentifierInfo* II = MD->getName();
        assert(II && "Expected valid identifier info");

        MacroInfo* MI = P.getMacroInfo((IdentifierInfo*)II);

        if (!MI || MI->isBuiltinMacro() || MI->isFunctionLike())
            break;

        clang::SourceManager& SM = C->getSourceManager();
        const LangOptions &LangOpts = C->getLangOpts();

        auto Loc = MI->getDefinitionLoc();

        if (!IsValidDeclaration(Loc))
            break;

        clang::SourceLocation BeginExpr =
            Lexer::getLocForEndOfToken(Loc, 0, SM, LangOpts);

        auto Range = clang::CharSourceRange::getTokenRange(
            BeginExpr, MI->getDefinitionEndLoc());

        bool Invalid;
        StringRef Expression = Lexer::getSourceText(Range, SM, LangOpts,
            &Invalid);

        if (Invalid || Expression.empty())
            break;

        auto Definition = new MacroDefinition();
        Definition->LineNumberStart = SM.getExpansionLineNumber(MD->getLocation());
        Definition->LineNumberEnd = SM.getExpansionLineNumber(MD->getLocation());
        Entity = Definition;

        Definition->Name = II->getName().trim();
        Definition->Expression = Expression.trim();
    }
    case clang::PreprocessedEntity::InclusionDirectiveKind:
        // nothing to be done for InclusionDirectiveKind
        break;
    default:
        llvm_unreachable("Unknown PreprocessedEntity");
    }

    if (!Entity)
        return nullptr;

    Entity->OriginalPtr = PPEntity;
    auto Namespace = GetTranslationUnit(PPEntity->getSourceRange().getBegin());

    if (Decl->Kind == CppSharp::CppParser::AST::DeclarationKind::TranslationUnit)
    {
        Namespace->PreprocessedEntities.push_back(Entity);
    }
    else
    {
        Decl->PreprocessedEntities.push_back(Entity);
    }

    return Entity;
}

void Parser::HandlePreprocessedEntities(Declaration* Decl)
{
    using namespace clang;
    auto PPRecord = C->getPreprocessor().getPreprocessingRecord();

    for (auto it = PPRecord->begin(); it != PPRecord->end(); ++it)
    {
        clang::PreprocessedEntity* PPEntity = (*it);
        auto Entity = WalkPreprocessedEntity(Decl, PPEntity);
    }
}

AST::Expression* Parser::WalkExpression(clang::Expr* Expr)
{
    using namespace clang;

    switch (Expr->getStmtClass())
    {
    case Stmt::BinaryOperatorClass:
    {
        auto BinaryOperator = cast<clang::BinaryOperator>(Expr);
        return new AST::BinaryOperator(GetStringFromStatement(Expr),
            WalkExpression(BinaryOperator->getLHS()), WalkExpression(BinaryOperator->getRHS()),
            BinaryOperator->getOpcodeStr().str());
    }
    case Stmt::CallExprClass:
    {
        auto CallExpr = cast<clang::CallExpr>(Expr);
        auto CallExpression = new AST::CallExpr(GetStringFromStatement(Expr),
            CallExpr->getCalleeDecl() ? WalkDeclaration(CallExpr->getCalleeDecl()) : 0);
        for (auto arg : CallExpr->arguments())
        {
            CallExpression->Arguments.push_back(WalkExpression(arg));
        }
        return CallExpression;
    }
    case Stmt::DeclRefExprClass:
        return new AST::Expression(GetStringFromStatement(Expr), StatementClass::DeclRefExprClass,
            WalkDeclaration(cast<DeclRefExpr>(Expr)->getDecl()));
    case Stmt::CStyleCastExprClass:
    case Stmt::CXXConstCastExprClass:
    case Stmt::CXXDynamicCastExprClass:
    case Stmt::CXXFunctionalCastExprClass:
    case Stmt::CXXReinterpretCastExprClass:
    case Stmt::CXXStaticCastExprClass:
    case Stmt::ImplicitCastExprClass:
        return WalkExpression(cast<CastExpr>(Expr)->getSubExprAsWritten());
    case Stmt::CXXOperatorCallExprClass:
    {
        auto OperatorCallExpr = cast<CXXOperatorCallExpr>(Expr);
        return new AST::Expression(GetStringFromStatement(Expr), StatementClass::CXXOperatorCallExpr,
            OperatorCallExpr->getCalleeDecl() ? WalkDeclaration(OperatorCallExpr->getCalleeDecl()) : 0);
    }
    case Stmt::CXXConstructExprClass:
    case Stmt::CXXTemporaryObjectExprClass:
    {
        auto ConstructorExpr = cast<clang::CXXConstructExpr>(Expr);
        if (ConstructorExpr->getNumArgs() == 1)
        {
            auto Arg = ConstructorExpr->getArg(0);
            auto TemporaryExpr = dyn_cast<MaterializeTemporaryExpr>(Arg);
            if (TemporaryExpr)
            {
                auto SubTemporaryExpr = TemporaryExpr->GetTemporaryExpr();
                auto Cast = dyn_cast<CastExpr>(SubTemporaryExpr);
                if (!Cast ||
                    (Cast->getSubExprAsWritten()->getStmtClass() != Stmt::IntegerLiteralClass &&
                     Cast->getSubExprAsWritten()->getStmtClass() != Stmt::CXXNullPtrLiteralExprClass))
                    return WalkExpression(SubTemporaryExpr);
                return new AST::CXXConstructExpr(GetStringFromStatement(Expr),
                    WalkDeclaration(ConstructorExpr->getConstructor()));
            }
        }
        auto ConstructorExpression = new AST::CXXConstructExpr(GetStringFromStatement(Expr),
            WalkDeclaration(ConstructorExpr->getConstructor()));
        for (clang::Expr* arg : ConstructorExpr->arguments())
        {
            ConstructorExpression->Arguments.push_back(WalkExpression(arg));
        }
        return ConstructorExpression;
    }
    case Stmt::CXXBindTemporaryExprClass:
        return WalkExpression(cast<CXXBindTemporaryExpr>(Expr)->getSubExpr());
    case Stmt::MaterializeTemporaryExprClass:
        return WalkExpression(cast<MaterializeTemporaryExpr>(Expr)->GetTemporaryExpr());
    default:
        break;
    }
    llvm::APSInt integer;
    if (Expr->getStmtClass() != Stmt::CharacterLiteralClass &&
        Expr->getStmtClass() != Stmt::CXXBoolLiteralExprClass &&
        Expr->getStmtClass() != Stmt::UnaryExprOrTypeTraitExprClass &&
        !Expr->isValueDependent() &&
        Expr->EvaluateAsInt(integer, C->getASTContext()))
        return new AST::Expression(integer.toString(10));
    return new AST::Expression(GetStringFromStatement(Expr));
}

std::string Parser::GetStringFromStatement(const clang::Stmt* Statement)
{
    using namespace clang;

    PrintingPolicy Policy(C->getLangOpts());
    std::string s;
    llvm::raw_string_ostream as(s);
    Statement->printPretty(as, 0, Policy);
    return as.str();
}

void Parser::HandlePreprocessedEntities(Declaration* Decl,
                                        clang::SourceRange sourceRange,
                                        MacroLocation macroLocation)
{
    if (sourceRange.isInvalid()) return;

    auto& SourceMgr = C->getSourceManager();
    auto isBefore = SourceMgr.isBeforeInTranslationUnit(sourceRange.getEnd(),
        sourceRange.getBegin());

    if (isBefore) return;

    assert(!SourceMgr.isBeforeInTranslationUnit(sourceRange.getEnd(),
        sourceRange.getBegin()));

    using namespace clang;
    auto PPRecord = C->getPreprocessor().getPreprocessingRecord();

    auto Range = PPRecord->getPreprocessedEntitiesInRange(sourceRange);

    for (auto PPEntity : Range)
    {
        auto Entity = WalkPreprocessedEntity(Decl, PPEntity);
        if (!Entity) continue;
 
        if (Entity->MacroLocation == MacroLocation::Unknown)
            Entity->MacroLocation = macroLocation;
    }
}

void Parser::HandleOriginalText(const clang::Decl* D, Declaration* Decl)
{
    auto& SM = C->getSourceManager();
    auto& LangOpts = C->getLangOpts();

    auto Range = clang::CharSourceRange::getTokenRange(D->getSourceRange());

    bool Invalid;
    auto DeclText = clang::Lexer::getSourceText(Range, SM, LangOpts, &Invalid);
    
    if (!Invalid)
        Decl->DebugText = DeclText;
}

void Parser::HandleDeclaration(const clang::Decl* D, Declaration* Decl)
{
    if (Decl->OriginalPtr != nullptr)
        return;

    Decl->OriginalPtr = (void*) D;
    Decl->USR = GetDeclUSR(D);
    Decl->IsImplicit = D->isImplicit();
    Decl->Location = SourceLocation(D->getLocation().getRawEncoding());
    Decl->LineNumberStart = C->getSourceManager().getExpansionLineNumber(D->getLocStart());
    Decl->LineNumberEnd = C->getSourceManager().getExpansionLineNumber(D->getLocEnd());

    if (Decl->PreprocessedEntities.empty() && !D->isImplicit())
    {
        if (clang::dyn_cast<clang::TranslationUnitDecl>(D))
        {
            HandlePreprocessedEntities(Decl);
        }
        else if (clang::dyn_cast<clang::ParmVarDecl>(D))
        {
            // Ignore function parameters as we already walk their preprocessed entities.
        }
        else
        {
            auto startLoc = GetDeclStartLocation(C.get(), D);
            auto endLoc = D->getLocEnd();
            auto range = clang::SourceRange(startLoc, endLoc);

            HandlePreprocessedEntities(Decl, range);
        }
    }

    HandleOriginalText(D, Decl);
    HandleComments(D, Decl);

    if (const clang::ValueDecl *VD = clang::dyn_cast_or_null<clang::ValueDecl>(D))
        Decl->IsDependent = VD->getType()->isDependentType();

    if (const clang::DeclContext *DC = clang::dyn_cast_or_null<clang::DeclContext>(D))
        Decl->IsDependent |= DC->isDependentContext();

    Decl->Access = ConvertToAccess(D->getAccess());
}

//-----------------------------------//

Declaration* Parser::WalkDeclarationDef(clang::Decl* D)
{
    return WalkDeclaration(D, /*CanBeDefinition=*/true);
}

Declaration* Parser::WalkDeclaration(const clang::Decl* D,
                                           bool CanBeDefinition)
{
    using namespace clang;

    if (D->hasAttrs())
    {
        for (auto it = D->attr_begin(); it != D->attr_end(); ++it)
        {
            Attr* Attr = (*it);

            if (Attr->getKind() != clang::attr::Annotate)
                continue;

            AnnotateAttr* Annotation = cast<AnnotateAttr>(Attr);
            assert(Annotation != nullptr);

            StringRef AnnotationText = Annotation->getAnnotation();
        }
    }

    Declaration* Decl = nullptr;

    auto Kind = D->getKind();
    switch(D->getKind())
    {
    case Decl::Record:
    {
        auto RD = cast<RecordDecl>(D);

        auto Record = WalkRecord(RD);

        // We store a definition order index into the declarations.
        // This is needed because declarations are added to their contexts as
        // soon as they are referenced and we need to know the original order
        // of the declarations.

        if (CanBeDefinition && Record->DefinitionOrder == 0 &&
            RD->isCompleteDefinition())
        {
            Record->DefinitionOrder = Index++;
            //Debug("%d: %s\n", Index++, GetTagDeclName(RD).c_str());
        }

        Decl = Record;
        break;
    }
    case Decl::CXXRecord:
    {
        auto RD = cast<CXXRecordDecl>(D);

        auto Class = WalkRecordCXX(RD);

        // We store a definition order index into the declarations.
        // This is needed because declarations are added to their contexts as
        // soon as they are referenced and we need to know the original order
        // of the declarations.

        if (CanBeDefinition && Class->DefinitionOrder == 0 &&
            RD->isCompleteDefinition())
        {
            Class->DefinitionOrder = Index++;
            //Debug("%d: %s\n", Index++, GetTagDeclName(RD).c_str());
        }

        Decl = Class;
        break;
    }
    case Decl::ClassTemplate:
    {
        auto TD = cast<ClassTemplateDecl>(D);
        auto Template = WalkClassTemplate(TD); 
        
        Decl = Template;
        break;
    }
    case Decl::ClassTemplateSpecialization:
    {
        auto TS = cast<ClassTemplateSpecializationDecl>(D);
        auto CT = WalkClassTemplateSpecialization(TS);

        Decl = CT;
        break;
    }
    case Decl::ClassTemplatePartialSpecialization:
    {
        auto TS = cast<ClassTemplatePartialSpecializationDecl>(D);
        auto CT = WalkClassTemplatePartialSpecialization(TS);

        Decl = CT;
        break;
    }
    case Decl::FunctionTemplate:
    {
        auto TD = cast<FunctionTemplateDecl>(D);
        auto FT = WalkFunctionTemplate(TD);

        Decl = FT;
        break;
    }
    case Decl::VarTemplate:
    {
        auto TD = cast<VarTemplateDecl>(D);
        auto Template = WalkVarTemplate(TD);

        Decl = Template;
        break;
    }
    case Decl::VarTemplateSpecialization:
    {
        auto TS = cast<VarTemplateSpecializationDecl>(D);
        auto CT = WalkVarTemplateSpecialization(TS);

        Decl = CT;
        break;
    }
    case Decl::VarTemplatePartialSpecialization:
    {
        auto TS = cast<VarTemplatePartialSpecializationDecl>(D);
        auto CT = WalkVarTemplatePartialSpecialization(TS);

        Decl = CT;
        break;
    }
    case Decl::TypeAliasTemplate:
    {
        auto TD = cast<TypeAliasTemplateDecl>(D);
        auto TA = WalkTypeAliasTemplate(TD);

        Decl = TA;
        break;
    }
    case Decl::Enum:
    {
        auto ED = cast<EnumDecl>(D);
        Decl = WalkEnum(ED);
        break;
    }
    case Decl::EnumConstant:
    {
        auto ED = cast<EnumConstantDecl>(D);
        auto E = static_cast<Enumeration*>(GetNamespace(ED));
        assert(E && "Expected a valid enumeration");
        Decl = E->FindItemByName(ED->getNameAsString());
        break;
    }
    case Decl::Function:
    {
        auto FD = cast<FunctionDecl>(D);

        // Check for and ignore built-in functions.
        if (FD->getBuiltinID() != 0)
            break;

        Decl = WalkFunction(FD);
        break;
    }
    case Decl::LinkageSpec:
    {
        auto LS = cast<LinkageSpecDecl>(D);
        
        for (auto it = LS->decls_begin(); it != LS->decls_end(); ++it)
        {
            clang::Decl* D = (*it);
            Decl = WalkDeclarationDef(D);
        }
        
        break;
    }
    case Decl::Typedef:
    {
        auto TD = cast<clang::TypedefDecl>(D);

        auto NS = GetNamespace(TD);
        auto Name = GetDeclName(TD);
        auto Typedef = NS->FindTypedef(Name, /*Create=*/false);
        if (Typedef) return Typedef;

        Typedef = NS->FindTypedef(Name, /*Create=*/true);
        HandleDeclaration(TD, Typedef);

        auto TTL = TD->getTypeSourceInfo()->getTypeLoc();
        // resolve the typedef before adding it to the list otherwise it might be found and returned prematurely
        // see "typedef _Aligned<16, char>::type type;" and the related classes in Common.h in the tests
        Typedef->QualifiedType = GetQualifiedType(TD->getUnderlyingType(), &TTL);
        AST::TypedefDecl* Existing;
        // if the typedef was added along the way, the just created one is useless, delete it
        if (Existing = NS->FindTypedef(Name, /*Create=*/false))
            delete Typedef;
        else
            NS->Typedefs.push_back(Existing = Typedef);

        Decl = Existing;
        break;
    }
    case Decl::TypeAlias:
    {
        auto TD = cast<clang::TypeAliasDecl>(D);

        auto NS = GetNamespace(TD);
        auto Name = GetDeclName(TD);
        auto TypeAlias = NS->FindTypeAlias(Name, /*Create=*/false);
        if (TypeAlias) return TypeAlias;

        TypeAlias = NS->FindTypeAlias(Name, /*Create=*/true);
        HandleDeclaration(TD, TypeAlias);

        auto TTL = TD->getTypeSourceInfo()->getTypeLoc();
        // see above the case for "Typedef"
        TypeAlias->QualifiedType = GetQualifiedType(TD->getUnderlyingType(), &TTL);
        AST::TypeAlias* Existing;
        if (Existing = NS->FindTypeAlias(Name, /*Create=*/false))
            delete TypeAlias;
        else
            NS->TypeAliases.push_back(Existing = TypeAlias);

        if (auto TAT = TD->getDescribedAliasTemplate())
            TypeAlias->DescribedAliasTemplate = WalkTypeAliasTemplate(TAT);

        Decl = Existing;
        break;
    }
    case Decl::Namespace:
    {
        auto ND = cast<NamespaceDecl>(D);

        for (auto it = ND->decls_begin(); it != ND->decls_end(); ++it)
        {
            clang::Decl* D = (*it);
            Decl = WalkDeclarationDef(D);
        }

        break;
    }
    case Decl::Var:
    {
        auto VD = cast<VarDecl>(D);
        Decl = WalkVariable(VD);
        break;
    }
    case Decl::CXXConstructor:
    case Decl::CXXMethod:
    {
        auto MD = cast<CXXMethodDecl>(D);
        Decl = WalkMethodCXX(MD);

        auto NS = GetNamespace(MD);
        Decl->_Namespace = NS;
        break;
    }
    case Decl::Friend:
    {
        auto FD = cast<FriendDecl>(D);
        Decl = WalkFriend(FD);
        break;
    }
    case Decl::TemplateTemplateParm:
    {
        auto TTP = cast<TemplateTemplateParmDecl>(D);
        Decl = WalkTemplateTemplateParameter(TTP);
        break;
    }
    case Decl::TemplateTypeParm:
    {
        auto TTPD = cast<TemplateTypeParmDecl>(D);
        Decl = WalkTypeTemplateParameter(TTPD);
        break;
    }
    case Decl::NonTypeTemplateParm:
    {
        auto NTTPD = cast<NonTypeTemplateParmDecl>(D);
        Decl = WalkNonTypeTemplateParameter(NTTPD);
        break;
    }
    // Ignore these declarations since they must have been declared in
    // a class already.
    case Decl::CXXDestructor:
    case Decl::CXXConversion:
        break;
    case Decl::PragmaComment:
    case Decl::PragmaDetectMismatch:
    case Decl::Empty:
    case Decl::AccessSpec:
    case Decl::Using:
    case Decl::UsingDirective:
    case Decl::UsingShadow:
    case Decl::ConstructorUsingShadow:
    case Decl::UnresolvedUsingTypename:
    case Decl::UnresolvedUsingValue:
    case Decl::IndirectField:
    case Decl::StaticAssert:
    case Decl::NamespaceAlias:
        break;
    default:
    {
        Debug("Unhandled declaration kind: %s\n", D->getDeclKindName());

        auto& SM = C->getSourceManager();
        auto Loc = D->getLocation();
        auto FileName = SM.getFilename(Loc);
        auto Offset = SM.getFileOffset(Loc);
        auto LineNo = SM.getLineNumber(SM.getFileID(Loc), Offset);
        Debug("  %s (line %u)\n", FileName.str().c_str(), LineNo);

        break;
    } };

    return Decl;
}

//-----------------------------------//

struct Diagnostic
{
    clang::SourceLocation Location;
    llvm::SmallString<100> Message;
    clang::DiagnosticsEngine::Level Level;
};

struct DiagnosticConsumer : public clang::DiagnosticConsumer
{
    virtual ~DiagnosticConsumer() { }

    virtual void HandleDiagnostic(clang::DiagnosticsEngine::Level Level,
                                  const clang::Diagnostic& Info) override {
        // Update the base type NumWarnings and NumErrors variables.
        if (Level == clang::DiagnosticsEngine::Warning)
            NumWarnings++;

        if (Level == clang::DiagnosticsEngine::Error ||
            Level == clang::DiagnosticsEngine::Fatal)
            NumErrors++;

        auto Diag = Diagnostic();
        Diag.Location = Info.getLocation();
        Diag.Level = Level;
        Info.FormatDiagnostic(Diag.Message);
        Diagnostics.push_back(Diag);
    }

    std::vector<Diagnostic> Diagnostics;
};

void Parser::HandleDiagnostics(ParserResult* res)
{
    auto DiagClient = (DiagnosticConsumer&) C->getDiagnosticClient();
    auto& Diags = DiagClient.Diagnostics;

    // Convert the diagnostics to the managed types
    for (unsigned I = 0, E = Diags.size(); I != E; ++I)
    {
        auto& Diag = DiagClient.Diagnostics[I];
        auto& Source = C->getSourceManager();
        auto FileName = Source.getFilename(Source.getFileLoc(Diag.Location));

        auto PDiag = ParserDiagnostic();
        PDiag.FileName = FileName.str();
        PDiag.Message = Diag.Message.str();
        PDiag.LineNumber = 0;
        PDiag.ColumnNumber = 0;

        if( !Diag.Location.isInvalid() )
        {
             clang::PresumedLoc PLoc = Source.getPresumedLoc(Diag.Location);
             if( PLoc.isValid() )
             {
                PDiag.LineNumber = PLoc.getLine();
                PDiag.ColumnNumber = PLoc.getColumn();
             }
        }

        switch( Diag.Level )
        {
        case clang::DiagnosticsEngine::Ignored: 
            PDiag.Level = ParserDiagnosticLevel::Ignored;
            break;
        case clang::DiagnosticsEngine::Note:
            PDiag.Level = ParserDiagnosticLevel::Note;
            break;
        case clang::DiagnosticsEngine::Warning:
            PDiag.Level = ParserDiagnosticLevel::Warning;
            break;
        case clang::DiagnosticsEngine::Error:
            PDiag.Level = ParserDiagnosticLevel::Error;
            break;
        case clang::DiagnosticsEngine::Fatal:
            PDiag.Level = ParserDiagnosticLevel::Fatal;
            break;
        default:
            assert(0);
        }

        res->Diagnostics.push_back(PDiag);
    }
}

ParserResult* Parser::ParseHeader(const std::vector<std::string>& SourceFiles, ParserResult* res)
{
    assert(Opts->ASTContext && "Expected a valid ASTContext");

    res->ASTContext = Lib;

    if (SourceFiles.empty())
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    SetupHeader();

    std::unique_ptr<clang::SemaConsumer> SC(new clang::SemaConsumer());
    C->setASTConsumer(std::move(SC));

    C->createSema(clang::TU_Complete, 0);

    auto DiagClient = new DiagnosticConsumer();
    C->getDiagnostics().setClient(DiagClient);

    // Check that the file is reachable.
    const clang::DirectoryLookup *Dir;
    llvm::SmallVector<
        std::pair<const clang::FileEntry *, const clang::DirectoryEntry *>,
        0> Includers;

    std::vector<const clang::FileEntry*> FileEntries;
    for (const auto& SourceFile : SourceFiles)
    {
        auto FileEntry = C->getPreprocessor().getHeaderSearchInfo().LookupFile(SourceFile,
            clang::SourceLocation(), /*isAngled*/true,
            nullptr, Dir, Includers, nullptr, nullptr, nullptr, nullptr);
        if (!FileEntry)
        {
            res->Kind = ParserResultKind::FileNotFound;
            return res;
        }
        FileEntries.push_back(FileEntry);
    }

    // Create a virtual file that includes the header. This gets rid of some
    // Clang warnings about parsing an header file as the main file.

    std::string str;
    for (const auto& SourceFile : SourceFiles)
    {
        str += "#include \"" + SourceFile + "\"" + "\n";
    }
    str += "\0";

    auto buffer = llvm::MemoryBuffer::getMemBuffer(str);
    auto& SM = C->getSourceManager();
    SM.setMainFileID(SM.createFileID(std::move(buffer)));

    clang::DiagnosticConsumer* client = C->getDiagnostics().getClient();
    client->BeginSourceFile(C->getLangOpts(), &C->getPreprocessor());

    ParseAST(C->getSema(), /*PrintStats=*/false, /*SkipFunctionBodies=*/true);

    client->EndSourceFile();

    HandleDiagnostics(res);

    if(client->getNumErrors() != 0)
    {
        res->Kind = ParserResultKind::Error;
        return res;
    }

    AST = &C->getASTContext();

    auto FileEntry = FileEntries[0];
    auto FileName = FileEntry->getName();
    auto Unit = Lib->FindOrCreateModule(FileName);

    auto TU = AST->getTranslationUnitDecl();
    HandleDeclaration(TU, Unit);

    if (Unit->OriginalPtr == nullptr)
        Unit->OriginalPtr = (void*)FileEntry;

    // Initialize enough Clang codegen machinery so we can get at ABI details.
    llvm::LLVMContext Ctx;
    std::unique_ptr<llvm::Module> M(new llvm::Module("", Ctx));

    M->setTargetTriple(AST->getTargetInfo().getTriple().getTriple());
    M->setDataLayout(AST->getTargetInfo().getDataLayout());

    std::unique_ptr<clang::CodeGen::CodeGenModule> CGM(
        new clang::CodeGen::CodeGenModule(C->getASTContext(), C->getHeaderSearchOpts(),
        C->getPreprocessorOpts(), C->getCodeGenOpts(), *M, C->getDiagnostics()));

    std::unique_ptr<clang::CodeGen::CodeGenTypes> CGT(
        new clang::CodeGen::CodeGenTypes(*CGM.get()));

    CodeGenTypes = CGT.get();

    WalkAST();

    res->Kind = ParserResultKind::Success;
    return res;
 }

ParserResultKind Parser::ParseArchive(llvm::StringRef File,
                                      llvm::object::Archive* Archive,
                                      CppSharp::CppParser::NativeLibrary*& NativeLib)
{
    auto LibName = File;
    NativeLib = new NativeLibrary();
    NativeLib->FileName = LibName;

    for(auto it = Archive->symbol_begin(); it != Archive->symbol_end(); ++it)
    {
        llvm::StringRef SymRef = it->getName();
        NativeLib->Symbols.push_back(SymRef);
    }

    return ParserResultKind::Success;
}

static ArchType ConvertArchType(unsigned int archType)
{
    switch (archType)
    {
    case llvm::Triple::ArchType::x86:
        return ArchType::x86;
    case llvm::Triple::ArchType::x86_64:
        return ArchType::x86_64;
    }
    return ArchType::UnknownArch;
}

template<class ELFT>
static void ReadELFDependencies(const llvm::object::ELFFile<ELFT>* ELFFile, CppSharp::CppParser::NativeLibrary*& NativeLib)
{
    ELFDumper<ELFT> ELFDumper(ELFFile);
    for (const auto& Dependency : ELFDumper.getNeededLibraries())
        NativeLib->Dependencies.push_back(Dependency);
}

ParserResultKind Parser::ParseSharedLib(llvm::StringRef File,
                                        llvm::object::ObjectFile* ObjectFile,
                                        CppSharp::CppParser::NativeLibrary*& NativeLib)
{
    auto LibName = File;
    NativeLib = new NativeLibrary();
    NativeLib->FileName = LibName;
    NativeLib->ArchType = ConvertArchType(ObjectFile->getArch());

    if (ObjectFile->isELF())
    {
        auto IDyn = llvm::cast<llvm::object::ELFObjectFileBase>(ObjectFile)->getDynamicSymbolIterators();
        for (auto it = IDyn.begin(); it != IDyn.end(); ++it)
        {
            std::string Sym;
            llvm::raw_string_ostream SymStream(Sym);

            if (it->printName(SymStream))
                continue;

            SymStream.flush();
            if (!Sym.empty())
                NativeLib->Symbols.push_back(Sym);
        }
        if (auto ELFObjectFile = llvm::dyn_cast<llvm::object::ELF32LEObjectFile>(ObjectFile))
        {
            ReadELFDependencies(ELFObjectFile->getELFFile(), NativeLib);
        }
        else if (auto ELFObjectFile = llvm::dyn_cast<llvm::object::ELF32BEObjectFile>(ObjectFile))
        {
            ReadELFDependencies(ELFObjectFile->getELFFile(), NativeLib);
        }
        else if (auto ELFObjectFile = llvm::dyn_cast<llvm::object::ELF64LEObjectFile>(ObjectFile))
        {
            ReadELFDependencies(ELFObjectFile->getELFFile(), NativeLib);
        }
        else if (auto ELFObjectFile = llvm::dyn_cast<llvm::object::ELF64BEObjectFile>(ObjectFile))
        {
            ReadELFDependencies(ELFObjectFile->getELFFile(), NativeLib);
        }
        return ParserResultKind::Success;
    }

    if (ObjectFile->isCOFF())
    {
        auto COFFObjectFile = static_cast<llvm::object::COFFObjectFile*>(ObjectFile);
        for (auto ExportedSymbol : COFFObjectFile->export_directories())
        {
            llvm::StringRef Symbol;
            if (!ExportedSymbol.getSymbolName(Symbol))
                NativeLib->Symbols.push_back(Symbol);
        }
        for (auto ImportedSymbol : COFFObjectFile->import_directories())
        {
            llvm::StringRef Name;
            if (!ImportedSymbol.getName(Name) && (Name.endswith(".dll") || Name.endswith(".DLL")))
                NativeLib->Dependencies.push_back(Name);
        }
        return ParserResultKind::Success;
    }

    if (ObjectFile->isMachO())
    {
        auto MachOObjectFile = static_cast<llvm::object::MachOObjectFile*>(ObjectFile);
        for (const auto& Load : MachOObjectFile->load_commands())
        {
            if (Load.C.cmd == llvm::MachO::LC_ID_DYLIB ||
                Load.C.cmd == llvm::MachO::LC_LOAD_DYLIB ||
                Load.C.cmd == llvm::MachO::LC_LOAD_WEAK_DYLIB ||
                Load.C.cmd == llvm::MachO::LC_REEXPORT_DYLIB ||
                Load.C.cmd == llvm::MachO::LC_LAZY_LOAD_DYLIB ||
                Load.C.cmd == llvm::MachO::LC_LOAD_UPWARD_DYLIB)
            {
                auto dl = MachOObjectFile->getDylibIDLoadCommand(Load);
                auto lib = llvm::sys::path::filename(Load.Ptr + dl.dylib.name);
                NativeLib->Dependencies.push_back(lib);
            }
        }
        for (const auto& Entry : MachOObjectFile->exports())
        {
            NativeLib->Symbols.push_back(Entry.name());
        }
        return ParserResultKind::Success;
    }

    return ParserResultKind::Error;
}

ParserResultKind Parser::ReadSymbols(llvm::StringRef File,
                                     llvm::object::basic_symbol_iterator Begin,
                                     llvm::object::basic_symbol_iterator End,
                                     CppSharp::CppParser::NativeLibrary*& NativeLib)
{
    auto LibName = File;
    NativeLib = new NativeLibrary();
    NativeLib->FileName = LibName;

    for (auto it = Begin; it != End; ++it)
    {
        std::string Sym;
        llvm::raw_string_ostream SymStream(Sym);

        if (it->printName(SymStream))
             continue;

        SymStream.flush();
        if (!Sym.empty())
            NativeLib->Symbols.push_back(Sym);
    }

    return ParserResultKind::Success;
}

ParserResult* Parser::ParseLibrary(const std::string& File, ParserResult* res)
{
    if (File.empty())
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    llvm::StringRef FileEntry;

    for (unsigned I = 0, E = Opts->LibraryDirs.size(); I != E; ++I)
    {
        auto& LibDir = Opts->LibraryDirs[I];
        llvm::SmallString<256> Path(LibDir);
        llvm::sys::path::append(Path, File);

        if (!(FileEntry = Path.str()).empty() && llvm::sys::fs::exists(FileEntry))
            break;
    }

    if (FileEntry.empty())
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    auto BinaryOrErr = llvm::object::createBinary(FileEntry);
    if (!BinaryOrErr)
    {
        res->Kind = ParserResultKind::Error;
        return res;
    }
    auto OwningBinary = std::move(BinaryOrErr.get());
    auto Bin = OwningBinary.getBinary();
    if (auto Archive = llvm::dyn_cast<llvm::object::Archive>(Bin)) {
        res->Kind = ParseArchive(File, Archive, res->Library);
        if (res->Kind == ParserResultKind::Success)
            return res;
    }
    if (auto ObjectFile = llvm::dyn_cast<llvm::object::ObjectFile>(Bin))
    {
        res->Kind = ParseSharedLib(File, ObjectFile, res->Library);
        if (res->Kind == ParserResultKind::Success)
            return res;
    }
    res->Kind = ParserResultKind::Error;
    return res;
}

ParserResult* ClangParser::ParseHeader(CppParserOptions* Opts)
{
    if (!Opts)
        return nullptr;

    auto res = new ParserResult();
    res->CodeParser = new Parser(Opts);
    return res->CodeParser->ParseHeader(Opts->SourceFiles, res);
}

ParserResult* ClangParser::ParseLibrary(CppParserOptions* Opts)
{
    if (!Opts)
        return nullptr;

    auto res = new ParserResult();
    res->CodeParser = new Parser(Opts);
    return res->CodeParser->ParseLibrary(Opts->LibraryFile, res);
}

ParserTargetInfo* ClangParser::GetTargetInfo(CppParserOptions* Opts)
{
    if (!Opts)
        return nullptr;

    Parser parser(Opts);
    return parser.GetTargetInfo();
}

ParserTargetInfo* Parser::GetTargetInfo()
{
    assert(Opts->ASTContext && "Expected a valid ASTContext");

    SetupHeader();

    std::unique_ptr<clang::SemaConsumer> SC(new clang::SemaConsumer());
    C->setASTConsumer(std::move(SC));

    C->createSema(clang::TU_Complete, 0);

    auto DiagClient = new DiagnosticConsumer();
    C->getDiagnostics().setClient(DiagClient);

    AST = &C->getASTContext();

    // Initialize enough Clang codegen machinery so we can get at ABI details.
    llvm::LLVMContext Ctx;
    std::unique_ptr<llvm::Module> M(new llvm::Module("", Ctx));

    M->setTargetTriple(AST->getTargetInfo().getTriple().getTriple());
    M->setDataLayout(AST->getTargetInfo().getDataLayout());

    std::unique_ptr<clang::CodeGen::CodeGenModule> CGM(
        new clang::CodeGen::CodeGenModule(C->getASTContext(), C->getHeaderSearchOpts(),
        C->getPreprocessorOpts(), C->getCodeGenOpts(), *M, C->getDiagnostics()));

    std::unique_ptr<clang::CodeGen::CodeGenTypes> CGT(
        new clang::CodeGen::CodeGenTypes(*CGM.get()));

    CodeGenTypes = CGT.get();

    auto parserTargetInfo = new ParserTargetInfo();

    auto& TI = AST->getTargetInfo();
    parserTargetInfo->ABI = TI.getABI();

    parserTargetInfo->Char16Type = ConvertIntType(TI.getChar16Type());
    parserTargetInfo->Char32Type = ConvertIntType(TI.getChar32Type());
    parserTargetInfo->Int64Type = ConvertIntType(TI.getInt64Type());
    parserTargetInfo->IntMaxType = ConvertIntType(TI.getIntMaxType());
    parserTargetInfo->IntPtrType = ConvertIntType(TI.getIntPtrType());
    parserTargetInfo->SizeType = ConvertIntType(TI.getSizeType());
    parserTargetInfo->UIntMaxType = ConvertIntType(TI.getUIntMaxType());
    parserTargetInfo->WCharType = ConvertIntType(TI.getWCharType());
    parserTargetInfo->WIntType = ConvertIntType(TI.getWIntType());

    parserTargetInfo->BoolAlign = TI.getBoolAlign();
    parserTargetInfo->BoolWidth = TI.getBoolWidth();
    parserTargetInfo->CharAlign = TI.getCharAlign();
    parserTargetInfo->CharWidth = TI.getCharWidth();
    parserTargetInfo->Char16Align = TI.getChar16Align();
    parserTargetInfo->Char16Width = TI.getChar16Width();
    parserTargetInfo->Char32Align = TI.getChar32Align();
    parserTargetInfo->Char32Width = TI.getChar32Width();
    parserTargetInfo->HalfAlign = TI.getHalfAlign();
    parserTargetInfo->HalfWidth = TI.getHalfWidth();
    parserTargetInfo->FloatAlign = TI.getFloatAlign();
    parserTargetInfo->FloatWidth = TI.getFloatWidth();
    parserTargetInfo->DoubleAlign = TI.getDoubleAlign();
    parserTargetInfo->DoubleWidth = TI.getDoubleWidth();
    parserTargetInfo->ShortAlign = TI.getShortAlign();
    parserTargetInfo->ShortWidth = TI.getShortWidth();
    parserTargetInfo->IntAlign = TI.getIntAlign();
    parserTargetInfo->IntWidth = TI.getIntWidth();
    parserTargetInfo->IntMaxTWidth = TI.getIntMaxTWidth();
    parserTargetInfo->LongAlign = TI.getLongAlign();
    parserTargetInfo->LongWidth = TI.getLongWidth();
    parserTargetInfo->LongDoubleAlign = TI.getLongDoubleAlign();
    parserTargetInfo->LongDoubleWidth = TI.getLongDoubleWidth();
    parserTargetInfo->LongLongAlign = TI.getLongLongAlign();
    parserTargetInfo->LongLongWidth = TI.getLongLongWidth();
    parserTargetInfo->PointerAlign = TI.getPointerAlign(0);
    parserTargetInfo->PointerWidth = TI.getPointerWidth(0);
    parserTargetInfo->WCharAlign = TI.getWCharAlign();
    parserTargetInfo->WCharWidth = TI.getWCharWidth();

    return parserTargetInfo;
}

Declaration* Parser::GetDeclarationFromFriend(clang::NamedDecl* FriendDecl)
{
    Declaration* Decl = WalkDeclarationDef(FriendDecl);
    if (!Decl) return nullptr;

    int MinLineNumberStart = std::numeric_limits<int>::max();
    int MinLineNumberEnd = std::numeric_limits<int>::max();
    auto& SM = C->getSourceManager();
    for (auto it = FriendDecl->redecls_begin(); it != FriendDecl->redecls_end(); it++)
    {
        if (it->getLocation() != FriendDecl->getLocation())
        {
            auto DecomposedLocStart = SM.getDecomposedLoc(it->getLocation());
            int NewLineNumberStart = SM.getLineNumber(DecomposedLocStart.first, DecomposedLocStart.second);
            auto DecomposedLocEnd = SM.getDecomposedLoc(it->getLocEnd());
            int NewLineNumberEnd = SM.getLineNumber(DecomposedLocEnd.first, DecomposedLocEnd.second);
            if (NewLineNumberStart < MinLineNumberStart)
            {
                MinLineNumberStart = NewLineNumberStart;
                MinLineNumberEnd = NewLineNumberEnd;
            }
        }
    }
    if (MinLineNumberStart < std::numeric_limits<int>::max())
    {
        Decl->LineNumberStart = MinLineNumberStart;
        Decl->LineNumberEnd = MinLineNumberEnd;
    }
    return Decl;
}