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
#include "APValuePrinter.h"

#include <llvm/Support/Host.h>
#include <llvm/Support/Path.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/Support/TargetSelect.h>
#include <llvm/Object/Archive.h>
#include <llvm/Object/COFF.h>
#include <llvm/Object/ObjectFile.h>
#include <llvm/Object/ELFObjectFile.h>
#include <llvm/Object/MachO.h>
#include <llvm/Option/ArgList.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/IR/Module.h>
#include <llvm/IR/DataLayout.h>
#include <clang/Basic/Builtins.h>
#include <clang/Basic/Version.h>
#include <clang/Config/config.h>
#include <clang/AST/ASTContext.h>
#include <clang/AST/Comment.h>
#include <clang/AST/DeclFriend.h>
#include <clang/AST/ExprCXX.h>
#include <clang/CodeGen/CodeGenAction.h>
#include <clang/Lex/DirectoryLookup.h>
#include <clang/Lex/HeaderSearch.h>
#include <clang/Lex/Preprocessor.h>
#include <clang/Lex/PreprocessorOptions.h>
#include <clang/Lex/PreprocessingRecord.h>
#include <clang/Parse/ParseAST.h>
#include <clang/Sema/Sema.h>
#include <clang/Sema/SemaConsumer.h>
#include <clang/Sema/Template.h>
#include <clang/Frontend/Utils.h>
#include <clang/Driver/Driver.h>
#include <clang/Driver/ToolChain.h>
#include <clang/Driver/Util.h>
#include <clang/Index/USRGeneration.h>

#include <CodeGen/TargetInfo.h>
#include <CodeGen/CGCall.h>
#include <CodeGen/CGCXXABI.h>
#include <Driver/ToolChains/Linux.h>
#include <Driver/ToolChains/MSVC.h>

#if defined(__APPLE__) || defined(__linux__)
#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif
#include <dlfcn.h>

#define HAVE_DLFCN
#endif

using namespace CppSharp::CppParser;

// We use this as a placeholder for pointer values that should be ignored.
void* IgnorePtr = reinterpret_cast<void*>(0x1);

//-----------------------------------//

Parser::Parser(CppParserOptions* Opts) : opts(Opts), index(0)
{
    for (const auto& SupportedStdType : Opts->SupportedStdTypes)
        supportedStdTypes.insert(SupportedStdType);
    supportedStdTypes.insert("allocator");
    supportedStdTypes.insert("basic_string");
    supportedFunctionTemplates = { Opts->SupportedFunctionTemplates.begin(), Opts->SupportedFunctionTemplates.end() };
}

LayoutField Parser::WalkVTablePointer(Class* Class,
    const clang::CharUnits& Offset, const std::string& prefix)
{
    LayoutField LayoutField;
    LayoutField.offset = Offset.getQuantity();
    LayoutField.name = prefix + "_" + Class->name;
    LayoutField.qualifiedType = GetQualifiedType(c->getASTContext().VoidPtrTy);
    return LayoutField;
}

static CppAbi GetClassLayoutAbi(clang::TargetCXXABI::Kind abi)
{
    switch (abi)
    {
    case clang::TargetCXXABI::Microsoft:
        return CppAbi::Microsoft;
    case clang::TargetCXXABI::GenericItanium:
        return CppAbi::Itanium;
    case clang::TargetCXXABI::GenericARM:
        return CppAbi::ARM;
    case clang::TargetCXXABI::iOS:
        return CppAbi::iOS;
    case clang::TargetCXXABI::AppleARM64:
        return CppAbi::iOS64;
    case clang::TargetCXXABI::WebAssembly:
        return CppAbi::WebAssembly;
    default:
        llvm_unreachable("Unsupported C++ ABI kind");
    }
}

void Parser::ReadClassLayout(Class* Class, const clang::RecordDecl* RD,
    clang::CharUnits Offset, bool IncludeVirtualBases)
{
    using namespace clang;

    const auto &Layout = c->getASTContext().getASTRecordLayout(RD);
    auto CXXRD = dyn_cast<CXXRecordDecl>(RD);

    auto Parent = static_cast<AST::Class*>(
        WalkDeclaration(RD));

    if (Class != Parent)
    {
        LayoutBase LayoutBase;
        LayoutBase.offset = Offset.getQuantity();
        LayoutBase._class = Parent;
        Class->layout->Bases.push_back(LayoutBase);
    }

    // Dump bases.
    if (CXXRD) {
        const CXXRecordDecl *PrimaryBase = Layout.getPrimaryBase();
        bool HasOwnVFPtr = Layout.hasOwnVFPtr();
        bool HasOwnVBPtr = Layout.hasOwnVBPtr();

        // Vtable pointer.
        if (CXXRD->isDynamicClass() && !PrimaryBase &&
            !c->getTarget().getCXXABI().isMicrosoft()) {
            auto VPtr = WalkVTablePointer(Parent, Offset, "vptr");
            Class->layout->Fields.push_back(VPtr);
        }
        else if (HasOwnVFPtr) {
            auto VTPtr = WalkVTablePointer(Parent, Offset, "vfptr");
            Class->layout->Fields.push_back(VTPtr);
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
            Class->layout->Fields.push_back(VBPtr);
        }
    }

    // Dump fields.
    uint64_t FieldNo = 0;
    for (const clang::FieldDecl* Field : RD->fields()) {
        uint64_t LocalFieldOffsetInBits = Layout.getFieldOffset(FieldNo++);
        CharUnits FieldOffset =
            Offset + c->getASTContext().toCharUnitsFromBits(LocalFieldOffsetInBits);

        auto F = WalkFieldCXX(Field, Parent);
        LayoutField LayoutField;
        LayoutField.offset = FieldOffset.getQuantity();
        LayoutField.name = F->name;
        LayoutField.qualifiedType = GetQualifiedType(Field->getType());
        LayoutField.fieldPtr = (void*)Field;
        Class->layout->Fields.push_back(LayoutField);
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
                Class->layout->Fields.push_back(VtorDisp);
            }

            ReadClassLayout(Class, VBase, VBaseOffset,
                /*IncludeVirtualBases=*/false);
        }
    }
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
        return TargetCXXABI::AppleARM64;
    }

    llvm_unreachable("Unsupported C++ ABI.");
}

void Parser::Setup(bool Compile)
{
    llvm::InitializeAllTargets();
    llvm::InitializeAllTargetMCs();
    llvm::InitializeAllAsmParsers();

    using namespace clang;

    std::vector<const char*> args;
    args.push_back("-cc1");
    if (Compile)
    {
        for (const std::string& CompilationOption : opts->CompilationOptions)
        {
            args.push_back(CompilationOption.c_str());

            if (opts->verbose)
                printf("Compiler argument: %s\n", CompilationOption.c_str());
        }
    }

    for (unsigned I = 0, E = opts->Arguments.size(); I != E; ++I)
    {
        const auto& Arg = opts->Arguments[I];
        args.push_back(Arg.c_str());

        if (opts->verbose)
            printf("Compiler argument: %s\n", Arg.c_str());
    }

    c.reset(new CompilerInstance());
    c->createDiagnostics();

    CompilerInvocation* Inv = new CompilerInvocation();
    llvm::ArrayRef<const char*> arguments(args.data(), args.data() + args.size());
    CompilerInvocation::CreateFromArgs(*Inv, arguments, c->getDiagnostics());
    c->setInvocation(std::shared_ptr<CompilerInvocation>(Inv));
    c->getLangOpts() = *Inv->LangOpts;

    auto& TO = Inv->TargetOpts;

    if (opts->targetTriple.empty())
        opts->targetTriple = llvm::sys::getDefaultTargetTriple();
    TO->Triple = llvm::Triple::normalize(opts->targetTriple);

    if (opts->verbose)
        printf("Target triple: %s\n", TO->Triple.c_str());

    TargetInfo* TI = TargetInfo::CreateTargetInfo(c->getDiagnostics(), TO);
    if (!TI)
    {
        // We might have no target info due to an invalid user-provided triple.
        // Try again with the default triple.
        opts->targetTriple = llvm::sys::getDefaultTargetTriple();
        TO->Triple = llvm::Triple::normalize(opts->targetTriple);
        TI = TargetInfo::CreateTargetInfo(c->getDiagnostics(), TO);
    }

    assert(TI && "Expected valid target info");

    c->setTarget(TI);

    c->createFileManager();
    c->createSourceManager(c->getFileManager());

    auto& HSOpts = c->getHeaderSearchOpts();
    auto& PPOpts = c->getPreprocessorOpts();
    auto& LangOpts = c->getLangOpts();

    if (opts->noStandardIncludes)
    {
        HSOpts.UseStandardSystemIncludes = false;
        HSOpts.UseStandardCXXIncludes = false;
    }

    if (opts->noBuiltinIncludes)
        HSOpts.UseBuiltinIncludes = false;

    if (opts->verbose)
        HSOpts.Verbose = true;

    for (unsigned I = 0, E = opts->IncludeDirs.size(); I != E; ++I)
    {
        const auto& s = opts->IncludeDirs[I];
        HSOpts.AddPath(s, frontend::Angled, false, false);
    }

    for (unsigned I = 0, E = opts->SystemIncludeDirs.size(); I != E; ++I)
    {
        const auto& s = opts->SystemIncludeDirs[I];
        HSOpts.AddPath(s, frontend::System, false, false);
    }

    for (unsigned I = 0, E = opts->Defines.size(); I != E; ++I)
    {
        const auto& define = opts->Defines[I];
        PPOpts.addMacroDef(define);
    }

    for (unsigned I = 0, E = opts->Undefines.size(); I != E; ++I)
    {
        const auto& undefine = opts->Undefines[I];
        PPOpts.addMacroUndef(undefine);
    }

#ifdef _MSC_VER
    if (opts->microsoftMode)
    {
        LangOpts.MSCompatibilityVersion = opts->toolSetToUse;
        if (!LangOpts.MSCompatibilityVersion) LangOpts.MSCompatibilityVersion = 1700;
    }
#endif

    llvm::opt::InputArgList Args(0, 0);
    clang::driver::Driver D("", TO->Triple, c->getDiagnostics());
    clang::driver::ToolChain *TC = nullptr;
    llvm::Triple Target(TO->Triple);

    if (Target.getOS() == llvm::Triple::Linux)
        TC = new clang::driver::toolchains::Linux(D, Target, Args);
    else if (Target.getEnvironment() == llvm::Triple::EnvironmentType::MSVC)
        TC = new clang::driver::toolchains::MSVCToolChain(D, Target, Args);

    if (TC && !opts->noStandardIncludes) {
        llvm::opt::ArgStringList Includes;
        TC->AddClangSystemIncludeArgs(Args, Includes);
        TC->AddClangCXXStdlibIncludeArgs(Args, Includes);
        for (auto& Arg : Includes) {
            if (strlen(Arg) > 0 && Arg[0] != '-')
                HSOpts.AddPath(Arg, frontend::System, /*IsFramework=*/false,
                    /*IgnoreSysRoot=*/false);
        }
    }

    if (TC)
        delete TC;

    // Enable preprocessing record.
    PPOpts.DetailedRecord = true;

    c->createPreprocessor(TU_Complete);

    Preprocessor& PP = c->getPreprocessor();
    PP.getBuiltinInfo().initializeBuiltins(PP.getIdentifierTable(),
        PP.getLangOpts());

    c->createASTContext();
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
    
    auto& AST = c->getASTContext();
    auto targetABI = c->getTarget().getCXXABI().getKind();
    switch(targetABI)
    {
    default:
       MC.reset(ItaniumMangleContext::create(AST, AST.getDiagnostics()));
       break;
    case TargetCXXABI::Microsoft:
       MC.reset(MicrosoftMangleContext::create(AST, AST.getDiagnostics()));
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

    GlobalDecl GD;
    if (const CXXConstructorDecl *CD = dyn_cast<CXXConstructorDecl>(ND))
        GD = GlobalDecl(CD, Ctor_Base);
    else if (const CXXDestructorDecl *DD = dyn_cast<CXXDestructorDecl>(ND))
        GD = GlobalDecl(DD, Dtor_Base);
    else
        GD = GlobalDecl(ND);
    MC->mangleName(GD, Out);

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
        return II->getName().str();
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
        return usr.str().str();
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

static bool IsExplicit(const clang::Decl* D)
{
    using namespace clang;

    auto CTS = llvm::dyn_cast<ClassTemplateSpecializationDecl>(D);
    return !CTS ||
        CTS->getSpecializationKind() == TSK_ExplicitSpecialization ||
        CTS->getSpecializationKind() == TSK_ExplicitInstantiationDeclaration ||
        CTS->getSpecializationKind() == TSK_ExplicitInstantiationDefinition;
}

static clang::SourceLocation GetDeclStartLocation(clang::CompilerInstance* C,
                                                  const clang::Decl* D)
{
    auto& SM = C->getSourceManager();
    auto startLoc = SM.getExpansionLoc(D->getBeginLoc());
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
    if(!prevDecl || !IsExplicit(prevDecl))
        return lineBeginLoc;

    auto prevDeclEndLoc = SM.getExpansionLoc(prevDecl->getEndLoc());
    auto prevDeclEndOffset = SM.getFileOffset(prevDeclEndLoc);

    if(SM.getFileID(prevDeclEndLoc) != SM.getFileID(startLoc))
        return lineBeginLoc;

    // TODO: Figure out why this asserts
    //assert(prevDeclEndOffset <= startOffset);

    if(prevDeclEndOffset < lineBeginOffset)
        return lineBeginLoc;

    // Declarations don't share same macro expansion
    if(SM.getExpansionLoc(prevDecl->getBeginLoc()) != startLoc)
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

    PrintingPolicy pp(c->getLangOpts());
    pp.SuppressTagKeyword = true;

    std::string TypeName;
    QualType::getAsStringInternal(Type, Qualifiers(), TypeName, pp);

    return TypeName;
}

static TypeQualifiers GetTypeQualifiers(const clang::QualType& Type)
{
    TypeQualifiers quals;
    quals.isConst = Type.isLocalConstQualified();
    quals.isRestrict = Type.isLocalRestrictQualified();
    quals.isVolatile = Type.isVolatileQualified();
    return quals;
}

QualifiedType Parser::GetQualifiedType(clang::QualType qual, const clang::TypeLoc* TL)
{
    if (qual.isNull())
        return QualifiedType();

    QualifiedType qualType;
    qualType.type = WalkType(qual, TL);
    qualType.qualifiers = GetTypeQualifiers(qual);
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
        VTC.kind = VTableComponentKind::VBaseOffset;
        VTC.offset = Component.getVCallOffset().getQuantity();
        break;
    }
    case clang::VTableComponent::CK_VBaseOffset:
    {
        VTC.kind = VTableComponentKind::VBaseOffset;
        VTC.offset = Component.getVBaseOffset().getQuantity();
        break;
    }
    case clang::VTableComponent::CK_OffsetToTop:
    {
        VTC.kind = VTableComponentKind::OffsetToTop;
        VTC.offset = Component.getOffsetToTop().getQuantity();
        break;
    }
    case clang::VTableComponent::CK_RTTI:
    {
        VTC.kind = VTableComponentKind::RTTI;
        auto RD = Component.getRTTIDecl();
        VTC.declaration = WalkRecordCXX(RD);
        break;
    }
    case clang::VTableComponent::CK_FunctionPointer:
    {
        VTC.kind = VTableComponentKind::FunctionPointer;
        auto MD = Component.getFunctionDecl();
        VTC.declaration = WalkMethodCXX(MD);
        break;
    }
    case clang::VTableComponent::CK_CompleteDtorPointer:
    {
        VTC.kind = VTableComponentKind::CompleteDtorPointer;
        auto MD = Component.getDestructorDecl();
        VTC.declaration = WalkMethodCXX(MD);
        break;
    }
    case clang::VTableComponent::CK_DeletingDtorPointer:
    {
        VTC.kind = VTableComponentKind::DeletingDtorPointer;
        auto MD = Component.getDestructorDecl();
        VTC.declaration = WalkMethodCXX(MD);
        break;
    }
    case clang::VTableComponent::CK_UnusedFunctionPointer:
    {
        VTC.kind = VTableComponentKind::UnusedFunctionPointer;
        auto MD = Component.getUnusedFunctionDecl();
        VTC.declaration = WalkMethodCXX(MD);
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

    for (const auto& VTC : VTLayout.vtable_components())
    {
        auto VTComponent = WalkVTableComponent(VTC);
        Layout.Components.push_back(VTComponent);
    }

    return Layout;
}


void Parser::WalkVTable(const clang::CXXRecordDecl* RD, Class* C)
{
    using namespace clang;

    assert(RD->isDynamicClass() && "Only dynamic classes have virtual tables");

    if (!C->layout)
        C->layout = new ClassLayout();

    auto targetABI = c->getTarget().getCXXABI().getKind();
    C->layout->ABI = GetClassLayoutAbi(targetABI);

    auto& AST = c->getASTContext();
    switch(targetABI)
    {
    case TargetCXXABI::Microsoft:
    {
        MicrosoftVTableContext VTContext(AST);

        const auto& VFPtrs = VTContext.getVFPtrOffsets(RD);
        for (const auto& VFPtrInfo : VFPtrs)
        {
            VFTableInfo Info;
            Info.VFPtrOffset = VFPtrInfo->NonVirtualOffset.getQuantity();
            Info.VFPtrFullOffset = VFPtrInfo->FullOffsetInMDC.getQuantity();

            auto& VTLayout = VTContext.getVFTableLayout(RD, VFPtrInfo->FullOffsetInMDC);
            Info.layout = WalkVTableLayout(VTLayout);

            C->layout->VFTables.push_back(Info);
        }
        break;
    }
    case TargetCXXABI::GenericItanium:
    {
        ItaniumVTableContext VTContext(AST);

        auto& VTLayout = VTContext.getVTableLayout(RD);
        C->layout->layout = WalkVTableLayout(VTLayout);
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

    if (!RC->isIncomplete || RC->completeDeclaration)
        return;

    Decl* Definition;
    if (auto CXXRecord = dyn_cast<CXXRecordDecl>(Record))
        Definition = CXXRecord->getDefinition();
    else
        Definition = Record->getDefinition();

    if (!Definition)
        return;

    RC->completeDeclaration = WalkDeclaration(Definition);
}

Class* Parser::GetRecord(const clang::RecordDecl* Record, bool& Process)
{
    using namespace clang;
    Process = false;

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
        RC = NS->FindClass(opts->unityBuild ? Record : 0, Name,
            isCompleteDefinition, /*Create=*/false);
    }

    if (RC)
        return RC;

    RC = NS->FindClass(opts->unityBuild ? Record : 0, Name,
        isCompleteDefinition, /*Create=*/true);
    RC->isInjected = Record->isInjectedClassName();
    HandleDeclaration(Record, RC);
    EnsureCompleteRecord(Record, NS, RC);

    for (auto Redecl : Record->redecls())
    {
        if (Redecl->isImplicit() || Redecl == Record)
            continue;

        RC->Redeclarations.push_back(WalkDeclaration(Redecl));
    }

    if (HasEmptyName)
    {
        auto USR = GetDeclUSR(Record);
        NS->anonymous[USR] = RC;
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

static bool IsRecordValid(const clang::RecordDecl* RC,
    std::unordered_set<const clang::RecordDecl*>& Visited)
{
    using namespace clang;

    if (Visited.find(RC) != Visited.end())
        return true;

    Visited.insert(RC);
    if (RC->isInvalidDecl())
        return false;
    for (auto Field : RC->fields())
    {
        auto Type = Field->getType()->getUnqualifiedDesugaredType();
        const auto* RD = const_cast<CXXRecordDecl*>(Type->getAsCXXRecordDecl());
        if (!RD)
            RD = Type->getPointeeCXXRecordDecl();
        if (RD && !IsRecordValid(RD, Visited))
            return false;
    }
    return true;
}

static bool IsRecordValid(const clang::RecordDecl* RC)
{
    std::unordered_set<const clang::RecordDecl*> Visited;
    return IsRecordValid(RC, Visited);
}

static clang::CXXRecordDecl* GetCXXRecordDeclFromBaseType(const clang::QualType& Ty) {
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

bool Parser::HasLayout(const clang::RecordDecl* Record)
{
    if (opts->skipLayoutInfo)
        return false;

    if (Record->isDependentType() || !Record->getDefinition() ||
        !IsRecordValid(Record))
        return false;

    if (auto CXXRecord = llvm::dyn_cast<clang::CXXRecordDecl>(Record))
        for (const clang::CXXBaseSpecifier& Base : CXXRecord->bases())
        {
            auto CXXBase = GetCXXRecordDeclFromBaseType(Base.getType());
            if (!CXXBase || !HasLayout(CXXBase))
                return false;
        }

    return true;
}

bool Parser::IsSupported(const clang::NamedDecl* ND)
{
    return !c->getSourceManager().isInSystemHeader(ND->getBeginLoc()) ||
        (llvm::isa<clang::RecordDecl>(ND) &&
         supportedStdTypes.find(ND->getName().str()) != supportedStdTypes.end());
}

bool Parser::IsSupported(const clang::CXXMethodDecl* MD)
{
    using namespace clang;

    return !c->getSourceManager().isInSystemHeader(MD->getBeginLoc()) ||
        (isa<CXXConstructorDecl>(MD) && MD->getNumParams() == 0) ||
        isa<CXXDestructorDecl>(MD) ||
        (MD->getDeclName().isIdentifier() &&
         ((MD->getName() == "data" && MD->getNumParams() == 0 && MD->isConst()) ||
          (MD->getName() == "assign" && MD->getNumParams() == 1 &&
           MD->parameters()[0]->getType()->isPointerType())) &&
         supportedStdTypes.find(MD->getParent()->getName().str()) !=
            supportedStdTypes.end());
}

static RecordArgABI GetRecordArgABI(
    clang::CodeGen::CGCXXABI::RecordArgABI argAbi)
{
    using namespace clang::CodeGen;
    switch (argAbi)
    {
    case CGCXXABI::RecordArgABI::RAA_DirectInMemory:
        return RecordArgABI::DirectInMemory;
    case CGCXXABI::RecordArgABI::RAA_Indirect:
        return RecordArgABI::Indirect;
    default:
        return RecordArgABI::Default;
    }
}

static TagKind ConvertToTagKind(clang::TagTypeKind AS)
{
    switch (AS)
    {
    case clang::TagTypeKind::TTK_Struct:
        return TagKind::Struct;
    case clang::TagTypeKind::TTK_Interface:
        return TagKind::Interface;
    case clang::TagTypeKind::TTK_Union:
        return TagKind::Union;
    case clang::TagTypeKind::TTK_Class:
        return TagKind::Class;
    case clang::TagTypeKind::TTK_Enum:
        return TagKind::Enum;
    }

    llvm_unreachable("Unknown TagKind");
}

void Parser::WalkRecord(const clang::RecordDecl* Record, Class* RC)
{
    using namespace clang;

    if (Record->isImplicit())
        return;

    if (IsExplicit(Record))
    {
        auto headStartLoc = GetDeclStartLocation(c.get(), Record);
        auto headEndLoc = Record->getLocation(); // identifier location
        auto bodyEndLoc = Record->getEndLoc();

        auto headRange = clang::SourceRange(headStartLoc, headEndLoc);
        auto bodyRange = clang::SourceRange(headEndLoc, bodyEndLoc);

        HandlePreprocessedEntities(RC, headRange, MacroLocation::ClassHead);
        HandlePreprocessedEntities(RC, bodyRange, MacroLocation::ClassBody);
    }

    auto& Sema = c->getSema();

    RC->isUnion = Record->isUnion();
    RC->isDependent = Record->isDependentType();
    RC->isExternCContext = Record->isExternCContext();
    RC->tagKind = ConvertToTagKind(Record->getTagKind());

    bool hasLayout = HasLayout(Record);

    if (hasLayout)
    {
        if (!RC->layout)
            RC->layout = new ClassLayout();

        auto targetABI = c->getTarget().getCXXABI().getKind();
        RC->layout->ABI = GetClassLayoutAbi(targetABI);

        if (auto CXXRD = llvm::dyn_cast_or_null<clang::CXXRecordDecl>(Record))
        {
            auto& CXXABI = codeGenTypes->getCXXABI();
            RC->layout->argABI = GetRecordArgABI(CXXABI.getRecordArgABI(CXXRD));
        }

        const auto& Layout = c->getASTContext().getASTRecordLayout(Record);
        RC->layout->alignment = (int)Layout.getAlignment().getQuantity();
        RC->layout->size = (int)Layout.getSize().getQuantity();
        RC->layout->dataSize = (int)Layout.getDataSize().getQuantity();

        ReadClassLayout(RC, Record, CharUnits(), true);
    }

    for (auto FD : Record->fields())
        WalkFieldCXX(FD, RC);

    if (c->getSourceManager().isInSystemHeader(Record->getBeginLoc()))
    {
        if (supportedStdTypes.find(Record->getName().str()) != supportedStdTypes.end())
        {
            for (auto D : Record->decls())
            {
                switch (D->getKind())
                {
                case Decl::CXXConstructor:
                case Decl::CXXDestructor:
                case Decl::CXXConversion:
                case Decl::CXXMethod:
                {
                    auto MD = cast<CXXMethodDecl>(D);
                    if (IsSupported(MD))
                        WalkDeclaration(MD);
                    break;
                }
                default:
                    break;
                }
            }
        }
        return;
    }

    if (opts->skipPrivateDeclarations &&
        Record->getAccess() == clang::AccessSpecifier::AS_private)
        return;

    for (auto D : Record->decls())
    {
        switch (D->getKind())
        {
        case Decl::AccessSpec:
        {
            AccessSpecDecl* AS = cast<AccessSpecDecl>(D);

            auto AccessDecl = new AccessSpecifierDecl();
            HandleDeclaration(AS, AccessDecl);

            AccessDecl->access = ConvertToAccess(AS->getAccess());
            AccessDecl->_namespace = RC;

            RC->Specifiers.push_back(AccessDecl);
            break;
        }
        case Decl::Field: // fields already handled
        case Decl::IndirectField: // FIXME: Handle indirect fields
            break;
        case Decl::CXXRecord:
            // Handle implicit records inside the class.
            if (D->isImplicit())
                continue;
            WalkDeclaration(D);
            break;
        case Decl::Friend:
        {
            FriendDecl* FD = cast<FriendDecl>(D);
            auto decl = FD->getFriendDecl();

            // Skip every friend declaration that isn't a function declaration
            if (decl && !isa<FunctionDecl>(decl))
                continue;
            WalkDeclaration(D);
            break;
        }
        case Decl::FriendTemplate:
        {
            // In this case always skip the declaration since, unlike Decl::Friend handled above,
            // it never is a declaration of a friend function or method
            break;
        }
        default:
        {
            WalkDeclaration(D);
            break;
        } }
    }
}

void Parser::WalkRecordCXX(const clang::CXXRecordDecl* Record, Class* RC)
{
    using namespace clang;

    if (Record->isImplicit())
        return;

    auto& Sema = c->getSema();
    Sema.ForceDeclarationOfImplicitMembers(const_cast<clang::CXXRecordDecl*>(Record));

    WalkRecord(Record, RC);
    
    if (!Record->hasDefinition())
        return;

    RC->isPOD = Record->isPOD();
    RC->isAbstract = Record->isAbstract();
    RC->isDynamic = Record->isDynamicClass();
    RC->isPolymorphic = Record->isPolymorphic();
    RC->hasNonTrivialDefaultConstructor = Record->hasNonTrivialDefaultConstructor();
    RC->hasNonTrivialCopyConstructor = Record->hasNonTrivialCopyConstructor();
    RC->hasNonTrivialDestructor = Record->hasNonTrivialDestructor();

    bool hasLayout = HasLayout(Record) &&
        Record->getDeclName() != c->getSema().VAListTagName;

    // Get the record layout information.
    const ASTRecordLayout* Layout = 0;
    if (hasLayout)
    {
        Layout = &c->getASTContext().getASTRecordLayout(Record);

        assert (RC->layout && "Expected a valid AST layout");
        RC->layout->hasOwnVFPtr = Layout->hasOwnVFPtr();
        RC->layout->VBPtrOffset = Layout->getVBPtrOffset().getQuantity();
    }

    // Iterate through the record bases.
    for (const CXXBaseSpecifier& BS : Record->bases())
    {
        BaseClassSpecifier* Base = new BaseClassSpecifier();
        Base->access = ConvertToAccess(BS.getAccessSpecifier());
        Base->isVirtual = BS.isVirtual();

        auto BSTL = BS.getTypeSourceInfo()->getTypeLoc();
        Base->type = WalkType(BS.getType(), &BSTL);

        auto BaseDecl = GetCXXRecordDeclFromBaseType(BS.getType());
        if (BaseDecl && Layout)
        {
            auto Offset = BS.isVirtual() ? Layout->getVBaseClassOffset(BaseDecl)
                : Layout->getBaseClassOffset(BaseDecl);
            Base->offset = Offset.getQuantity();
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

//-----------------------------------//

struct Diagnostic
{
    clang::SourceLocation Location;
    llvm::SmallString<100> Message;
    clang::DiagnosticsEngine::Level Level = clang::DiagnosticsEngine::Level::Ignored;
};

struct DiagnosticConsumer : public clang::DiagnosticConsumer
{
    virtual void HandleDiagnostic(clang::DiagnosticsEngine::Level Level,
        const clang::Diagnostic& Info) override {
        // Update the base type NumWarnings and NumErrors variables.
        if (Level == clang::DiagnosticsEngine::Warning)
            NumWarnings++;

        if (Level == clang::DiagnosticsEngine::Error ||
            Level == clang::DiagnosticsEngine::Fatal)
        {
            NumErrors++;
            if (Decl)
            {
                Decl->setInvalidDecl();
                Decl = 0;
            }
        }

        auto Diag = Diagnostic();
        Diag.Location = Info.getLocation();
        Diag.Level = Level;
        Info.FormatDiagnostic(Diag.Message);
        Diagnostics.push_back(Diag);
    }

    std::vector<Diagnostic> Diagnostics;
    clang::Decl* Decl = 0;
};

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
    TS->_namespace = NS;
    TS->name = CTS->getName().str();
    TS->templatedDecl = CT;
    TS->specializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
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
        TS->isIncomplete = true;
        if (CTS->getDefinition())
        {
            auto Complete = WalkDeclarationDef(CTS->getDefinition());
            if (!Complete->isIncomplete)
                TS->completeDeclaration = Complete;
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
    TS->_namespace = NS;
    TS->name = CTS->getName().str();
    TS->templatedDecl = CT;
    TS->specializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
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
        TS->isIncomplete = true;
        if (CTS->getDefinition())
        {
            auto Complete = WalkDeclarationDef(CTS->getDefinition());
            if (!Complete->isIncomplete)
                TS->completeDeclaration = Complete;
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
        auto TP = WalkDeclaration(ND);
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

    CT->name = GetDeclName(TD);
    CT->_namespace = NS;
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
    auto TP = walkedTemplateTemplateParameters[TTP];
    if (TP)
        return TP;

    TP = new TemplateTemplateParameter();
    HandleDeclaration(TTP, TP);
    TP->Parameters = WalkTemplateParameterList(TTP->getTemplateParameters());
    TP->isParameterPack = TTP->isParameterPack();
    TP->isPackExpansion = TTP->isPackExpansion();
    TP->isExpandedParameterPack = TTP->isExpandedParameterPack();
    if (TTP->getTemplatedDecl())
    {
        auto TD = WalkDeclaration(TTP->getTemplatedDecl());
        TP->TemplatedDecl = TD;
    }
    walkedTemplateTemplateParameters[TTP] = TP;
    return TP;
}

//-----------------------------------//

TypeTemplateParameter* Parser::WalkTypeTemplateParameter(const clang::TemplateTypeParmDecl* TTPD)
{
    auto TP = walkedTypeTemplateParameters[TTPD];
    if (TP)
        return TP;

    TP = new CppSharp::CppParser::TypeTemplateParameter();
    TP->name = GetDeclName(TTPD);
    HandleDeclaration(TTPD, TP);
    if (TTPD->hasDefaultArgument())
        TP->defaultArgument = GetQualifiedType(TTPD->getDefaultArgument());
    TP->depth = TTPD->getDepth();
    TP->index = TTPD->getIndex();
    TP->isParameterPack = TTPD->isParameterPack();
    walkedTypeTemplateParameters[TTPD] = TP;
    return TP;
}

//-----------------------------------//

NonTypeTemplateParameter* Parser::WalkNonTypeTemplateParameter(const clang::NonTypeTemplateParmDecl* NTTPD)
{
    auto NTP = walkedNonTypeTemplateParameters[NTTPD];
    if (NTP)
        return NTP;

    NTP = new CppSharp::CppParser::NonTypeTemplateParameter();
    NTP->name = GetDeclName(NTTPD);
    HandleDeclaration(NTTPD, NTP);
    if (NTTPD->hasDefaultArgument())
        NTP->defaultArgument = WalkExpressionObsolete(NTTPD->getDefaultArgument());
    NTP->depth = NTTPD->getDepth();
    NTP->index = NTTPD->getIndex();
    NTP->isParameterPack = NTTPD->isParameterPack();
    walkedNonTypeTemplateParameters[NTTPD] = NTP;
    return NTP;
}

//-----------------------------------//

UnresolvedUsingTypename* Parser::WalkUnresolvedUsingTypename(const clang::UnresolvedUsingTypenameDecl* UUTD)
{
    auto UUT = new CppSharp::CppParser::UnresolvedUsingTypename();
    HandleDeclaration(UUTD, UUT);

    return UUT;
}

//-----------------------------------//

template<typename TypeLoc>
std::vector<CppSharp::CppParser::TemplateArgument>
Parser::WalkTemplateArgumentList(const clang::TemplateArgumentList* TAL,
    TypeLoc* TSTL)
{
    using namespace clang;

    auto LocValid = TSTL && !TSTL->isNull() && TSTL->getTypePtr();

    auto params = std::vector<CppSharp::CppParser::TemplateArgument>();
    auto typeLocNumArgs = LocValid ? TSTL->getNumArgs() : 0;

    for (size_t i = 0, e = TAL->size(); i < e; i++)
    {
        const clang::TemplateArgument& TA = TAL->get(i);
        TemplateArgumentLoc TArgLoc;
        TemplateArgumentLoc *ArgLoc = 0;
        if (i < typeLocNumArgs && e == typeLocNumArgs)
        {
            TArgLoc = TSTL->getArgLoc(i);
            ArgLoc = &TArgLoc;
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
        const clang::TemplateArgument& TA = TAL->get(i);
        if (TALI)
        {
            auto ArgLoc = TALI->operator[](i);
            auto TP = WalkTemplateArgument(TA, &ArgLoc);
            params.push_back(TP);
        }
        else
        {
            auto TP = WalkTemplateArgument(TA, 0);
            params.push_back(TP);
        }
    }

    return params;
}

//-----------------------------------//

CppSharp::CppParser::TemplateArgument
Parser::WalkTemplateArgument(const clang::TemplateArgument& TA,
    clang::TemplateArgumentLoc* ArgLoc)
{
    auto Arg = CppSharp::CppParser::TemplateArgument();

    switch (TA.getKind())
    {
    case clang::TemplateArgument::Type:
    {
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Type;
        clang::TypeLoc ArgTL;
        if (ArgLoc && ArgLoc->getTypeSourceInfo())
        {
            ArgTL = ArgLoc->getTypeSourceInfo()->getTypeLoc();
        }
        auto Type = TA.getAsType();
        CompleteIfSpecializationType(Type);
        Arg.type = GetQualifiedType(Type, &ArgTL);
        break;
    }
    case clang::TemplateArgument::Declaration:
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Declaration;
        Arg.declaration = WalkDeclaration(TA.getAsDecl());
        break;
    case clang::TemplateArgument::NullPtr:
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::NullPtr;
        break;
    case clang::TemplateArgument::Integral:
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Integral;
        //Arg.Type = WalkType(TA.getIntegralType(), 0);
        {
            clang::TypeLoc ArgTL;
            if (ArgLoc && ArgLoc->getTypeSourceInfo())
                ArgTL = ArgLoc->getTypeSourceInfo()->getTypeLoc();
            Arg.type = GetQualifiedType(TA.getIntegralType(), &ArgTL);
        }
        Arg.integral = TA.getAsIntegral().getLimitedValue();
        break;
    case clang::TemplateArgument::Template:
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Template;
        break;
    case clang::TemplateArgument::TemplateExpansion:
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::TemplateExpansion;
        break;
    case clang::TemplateArgument::Expression:
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Expression;
        break;
    case clang::TemplateArgument::Pack:
        Arg.kind = CppSharp::CppParser::TemplateArgument::ArgumentKind::Pack;
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

    TA->name = GetDeclName(TD);
    NS->Templates.push_back(TA);

    TA->TemplatedDecl = WalkDeclaration(TD->getTemplatedDecl());
    TA->Parameters = WalkTemplateParameterList(TD->getTemplateParameters());

    return TA;
}

//-----------------------------------//

FunctionTemplate* Parser::WalkFunctionTemplate(const clang::FunctionTemplateDecl* TD)
{
    if (opts->skipPrivateDeclarations &&
        TD->getAccess() == clang::AccessSpecifier::AS_private)
        return nullptr;

    using namespace clang;

    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(TD);
    auto FT = NS->FindTemplate<FunctionTemplate>(USR);
    if (FT != nullptr)
        return FT;

    CppSharp::CppParser::AST::Function* F = nullptr;
    auto TemplatedDecl = TD->getTemplatedDecl();

    if (auto MD = dyn_cast<CXXMethodDecl>(TemplatedDecl))
        F = WalkMethodCXX(MD);
    else
        F = WalkFunction(TemplatedDecl);

    FT = new FunctionTemplate();
    HandleDeclaration(TD, FT);

    FT->name = GetDeclName(TD);
    FT->_namespace = NS;
    FT->TemplatedDecl = F;
    FT->Parameters = WalkTemplateParameterList(TD->getTemplateParameters());

    NS->Templates.push_back(FT);

    std::string qualifiedName;
    llvm::raw_string_ostream as(qualifiedName);
    TD->printQualifiedName(as);
    if (supportedFunctionTemplates.find(as.str()) == supportedFunctionTemplates.end())
        return FT;

    for (auto&& FD : TD->specializations())
    {
        if (auto MD = dyn_cast<CXXMethodDecl>(FD))
            WalkMethodCXX(MD);
        else
            WalkFunction(FD);
    }

    return FT;
}

//-----------------------------------//

CppSharp::CppParser::FunctionTemplateSpecialization*
Parser::WalkFunctionTemplateSpec(clang::FunctionTemplateSpecializationInfo* FTSI, CppSharp::CppParser::Function* Function)
{
    using namespace clang;

    auto FT = WalkFunctionTemplate(FTSI->getTemplate());
    auto USR = GetDeclUSR(FTSI->getFunction());
    auto FTS = FT->FindSpecialization(USR);
    if (FTS != nullptr)
        return FTS;

    FTS = new CppSharp::CppParser::FunctionTemplateSpecialization();
    FTS->specializationKind = WalkTemplateSpecializationKind(FTSI->getTemplateSpecializationKind());
    FTS->specializedFunction = Function;
    FTS->_template = WalkFunctionTemplate(FTSI->getTemplate());
    FTS->_template->Specializations.push_back(FTS);
    if (auto TSA = FTSI->TemplateArguments)
    {
        if (auto TSAW = FTSI->TemplateArgumentsAsWritten)
        {
            if (TSA->size() == TSAW->NumTemplateArgs)
            {
                FTS->Arguments = WalkTemplateArgumentList(TSA, TSAW);
                return FTS;
            }
        }
        FTS->Arguments = WalkTemplateArgumentList(TSA,
            (const clang::ASTTemplateArgumentListInfo*) 0);
    }

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

    VT->name = GetDeclName(TD);
    VT->_namespace = NS;
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
    TS->_namespace = NS;
    TS->name = VTS->getName().str();
    TS->templatedDecl = VT;
    TS->specializationKind = WalkTemplateSpecializationKind(VTS->getSpecializationKind());
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
    TS->_namespace = NS;
    TS->name = VTS->getName().str();
    TS->templatedDecl = VT;
    TS->specializationKind = WalkTemplateSpecializationKind(VTS->getSpecializationKind());
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
    case DeclarationName::CXXDeductionGuideName:
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
    const clang::CXXConstructorDecl* Ctor;
    if (opts->skipPrivateDeclarations &&
        MD->getAccess() == clang::AccessSpecifier::AS_private &&
        !MD->isVirtual() &&
        !MD->isCopyAssignmentOperator() &&
        !MD->isMoveAssignmentOperator() &&
        (!(Ctor = llvm::dyn_cast<clang::CXXConstructorDecl>(MD)) ||
         (!Ctor->isDefaultConstructor() && !Ctor->isCopyOrMoveConstructor())))
        return nullptr;

    using namespace clang;

    // We could be in a redeclaration, so process the primary context.
    if (MD->getPrimaryContext() != MD)
        return WalkMethodCXX(cast<CXXMethodDecl>(MD->getPrimaryContext()));

    auto RD = MD->getParent();
    auto Decl = WalkDeclaration(RD);

    auto Class = static_cast<CppSharp::CppParser::AST::Class*>(Decl);

    // Check for an already existing method that came from the same declaration.
    auto USR = GetDeclUSR(MD);
    for (auto& M : Class->Methods)
    {
        if (M->USR == USR)
        {
            return M;
        }
    }
    for (unsigned I = 0, E = Class->Templates.size(); I != E; ++I)
    {
        Template* Template = Class->Templates[I];
        if (Template->TemplatedDecl->USR == USR)
            return static_cast<Method*>(Template->TemplatedDecl);
    }

    auto Method = new CppSharp::CppParser::Method();
    HandleDeclaration(MD, Method);

    Method->access = ConvertToAccess(MD->getAccess());
    Method->methodKind = GetMethodKindFromDecl(MD->getDeclName());
    Method->isStatic = MD->isStatic();
    Method->isVirtual = MD->isVirtual();
    Method->isConst = MD->isConst();
    for (auto OverriddenMethod : MD->overridden_methods())
    {
        auto OM = WalkMethodCXX(OverriddenMethod);
        Method->OverriddenMethods.push_back(OM);
    }
    switch (MD->getRefQualifier())
    {
    case clang::RefQualifierKind::RQ_None:
        Method->refQualifier = RefQualifierKind::None;
        break;
    case clang::RefQualifierKind::RQ_LValue:
        Method->refQualifier = RefQualifierKind::LValue;
        break;
    case clang::RefQualifierKind::RQ_RValue:
        Method->refQualifier = RefQualifierKind::RValue;
        break;
    }

    Class->Methods.push_back(Method);

    WalkFunction(MD, Method);

    if (const CXXConstructorDecl* CD = dyn_cast<CXXConstructorDecl>(MD))
    {
        Method->isDefaultConstructor = CD->isDefaultConstructor();
        Method->isCopyConstructor = CD->isCopyConstructor();
        Method->isMoveConstructor = CD->isMoveConstructor();
        Method->isExplicit = CD->isExplicit();
    }
    else if (const CXXDestructorDecl* DD = dyn_cast<CXXDestructorDecl>(MD))
    {
    }
    else if (const CXXConversionDecl* CD = dyn_cast<CXXConversionDecl>(MD))
    {
        auto TL = MD->getTypeSourceInfo()->getTypeLoc().castAs<FunctionTypeLoc>();
        auto RTL = TL.getReturnLoc();
        Method->conversionType = GetQualifiedType(CD->getConversionType(), &RTL);
    }

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

    F->_namespace = Class;
    F->name = FD->getName().str();
    auto TL = FD->getTypeSourceInfo()->getTypeLoc();
    F->qualifiedType = GetQualifiedType(FD->getType(), &TL);
    F->access = ConvertToAccess(FD->getAccess());
    F->_class = Class;
    F->isBitField = FD->isBitField();
    if (F->isBitField && !F->isDependent && !FD->getBitWidth()->isInstantiationDependent())
        F->bitWidth = FD->getBitWidthValue(c->getASTContext());
 
    if (auto alignedAttr = FD->getAttr<clang::AlignedAttr>())
        F->alignAs = GetAlignAs(alignedAttr);

    Class->Fields.push_back(F);

    return F;
}

//-----------------------------------//

TranslationUnit* Parser::GetTranslationUnit(clang::SourceLocation Loc,
                                                      SourceLocationKind *Kind)
{
    using namespace clang;

    clang::SourceManager& SM = c->getSourceManager();

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

    auto Unit = opts->ASTContext->FindOrCreateModule(File.str());

    Unit->originalPtr = (void*) Unit;
    assert(Unit->originalPtr != nullptr);

    if (LocKind != SourceLocationKind::Invalid)
        Unit->isSystemHeader = SM.isInSystemHeader(Loc);

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
            DC = DC->FindCreateNamespace(Name.str());
            ((Namespace*)DC)->isAnonymous = ND->isAnonymousNamespace();
            ((Namespace*)DC)->isInline = ND->isInline();
            HandleDeclaration(ND, DC);
            continue;
        }
        case Decl::LinkageSpec:
        {
            const LinkageSpecDecl* LD = cast<LinkageSpecDecl>(Ctx);
            continue;
        }
        case Decl::CXXRecord:
        {
            auto RD = cast<CXXRecordDecl>(Ctx);
            DC = WalkRecordCXX(RD);
            continue;
        }
        case Decl::CXXDeductionGuide:
        {
            continue;
        }
        default:
        {
            auto D = cast<Decl>(Ctx);
            auto Decl = WalkDeclaration(D);
            DC = static_cast<DeclarationContext*>(Decl);
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

    case clang::BuiltinType::SChar: return PrimitiveType::SChar;
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
    case clang::BuiltinType::Float128: return PrimitiveType::Float128;

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

static FriendKind ConvertFriendKind(clang::Decl::FriendObjectKind FK)
{
    using namespace clang;

    switch (FK)
    {
    case Decl::FriendObjectKind::FOK_Declared:
        return FriendKind::Declared;
    case Decl::FriendObjectKind::FOK_Undeclared:
        return FriendKind::Undeclared;
    default:
        return FriendKind::None;
    }
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

static ExceptionSpecType ConvertExceptionType(clang::ExceptionSpecificationType EST)
{
    using namespace clang;

    switch (EST)
    {
    case ExceptionSpecificationType::EST_BasicNoexcept:
        return ExceptionSpecType::BasicNoexcept;
    case ExceptionSpecificationType::EST_DependentNoexcept:
        return ExceptionSpecType::DependentNoexcept;
    case ExceptionSpecificationType::EST_NoexceptFalse:
        return ExceptionSpecType::NoexceptFalse;
    case ExceptionSpecificationType::EST_NoexceptTrue:
        return ExceptionSpecType::NoexceptTrue;
    case ExceptionSpecificationType::EST_Dynamic:
        return ExceptionSpecType::Dynamic;
    case ExceptionSpecificationType::EST_DynamicNone:
        return ExceptionSpecType::DynamicNone;
    case ExceptionSpecificationType::EST_MSAny:
        return ExceptionSpecType::MSAny;
    case ExceptionSpecificationType::EST_Unevaluated:
        return ExceptionSpecType::Unevaluated;
    case ExceptionSpecificationType::EST_Uninstantiated:
        return ExceptionSpecType::Uninstantiated;
    case ExceptionSpecificationType::EST_Unparsed:
        return ExceptionSpecType::Unparsed;
    default:
        return ExceptionSpecType::None;
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

Type* Parser::WalkType(clang::QualType QualType, const clang::TypeLoc* TL,
    bool DesugarType)
{
    using namespace clang;

    if (QualType.isNull())
        return nullptr;

    auto LocValid = TL && !TL->isNull();

    const clang::Type* Type = QualType.getTypePtr();

    auto& AST = c->getASTContext();
    if (DesugarType)
    {
        clang::QualType Desugared = QualType.getDesugaredType(AST);
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
        AT->modified = GetQualifiedType(Modified, &Next);

        auto Equivalent = Attributed->getEquivalentType();
        AT->equivalent = GetQualifiedType(Equivalent, &Next);

        Ty = AT;
        break;
    }
    case clang::Type::Builtin:
    {
        auto Builtin = Type->getAs<clang::BuiltinType>();
        assert(Builtin && "Expected a builtin type");
    
        auto BT = new BuiltinType();
        BT->type = WalkBuiltinType(Builtin);
        
        Ty = BT;
        break;
    }
    case clang::Type::Enum:
    {
        auto ET = Type->getAs<clang::EnumType>();
        EnumDecl* ED = ET->getDecl();

        auto TT = new TagType();
        TT->declaration = TT->declaration = WalkDeclaration(ED);

        Ty = TT;
        break;
    }
    case clang::Type::Pointer:
    {
        auto Pointer = Type->getAs<clang::PointerType>();
        
        auto P = new PointerType();
        P->modifier = PointerType::TypeModifier::Pointer;

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Pointee = Pointer->getPointeeType();
        P->qualifiedPointee = GetQualifiedType(Pointee, &Next);

        Ty = P;
        break;
    }
    case clang::Type::Typedef:
    {
        auto TT = Type->getAs<clang::TypedefType>();
        auto TD = TT->getDecl();

        auto TTL = TD->getTypeSourceInfo()->getTypeLoc();
        auto TDD = static_cast<TypedefNameDecl*>(WalkDeclaration(TD));

        auto Type = new TypedefType();
        Type->declaration = TDD;

        Ty = Type;
        break;
    }
    case clang::Type::Decayed:
    {
        auto DT = Type->getAs<clang::DecayedType>();

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Type = new DecayedType();
        Type->decayed = GetQualifiedType(DT->getDecayedType(), &Next);
        Type->original = GetQualifiedType(DT->getOriginalType(), &Next);
        Type->pointee = GetQualifiedType(DT->getPointeeType(), &Next);

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
        TT->declaration = WalkDeclaration(RD);

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
        auto AT = AST.getAsConstantArrayType(QualType);

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        auto ElemTy = AT->getElementType();
        A->qualifiedType = GetQualifiedType(ElemTy, &Next);
        A->sizeType = ArrayType::ArraySize::Constant;
        A->size = AST.getConstantArrayElementCount(AT);

        if (!ElemTy->isDependentType() && !opts->skipLayoutInfo)
            A->elementSize = (long)AST.getTypeSize(ElemTy);

        Ty = A;
        break;
    }
    case clang::Type::IncompleteArray:
    {
        auto AT = AST.getAsIncompleteArrayType(QualType);

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        A->qualifiedType = GetQualifiedType(AT->getElementType(), &Next);
        A->sizeType = ArrayType::ArraySize::Incomplete;

        Ty = A;
        break;
    }
    case clang::Type::DependentSizedArray:
    {
        auto AT = AST.getAsDependentSizedArrayType(QualType);

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        A->qualifiedType = GetQualifiedType(AT->getElementType(), &Next);
        A->sizeType = ArrayType::ArraySize::Dependent;
        //A->Size = AT->getSizeExpr();

        Ty = A;
        break;
    }
    case clang::Type::UnresolvedUsing:
    {
        auto UT = Type->getAs<clang::UnresolvedUsingType>();

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto U = new UnresolvedUsingType();
        U->declaration = static_cast<UnresolvedUsingTypename*>(
            WalkDeclaration(UT->getDecl()));

        Ty = U;
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
        F->returnType = GetQualifiedType(FP->getReturnType(), &RL);
        F->callingConvention = ConvertCallConv(FP->getCallConv());

        Ty = F;
        break;
    }
    case clang::Type::FunctionProto:
    {
        auto FP = Type->getAs<clang::FunctionProtoType>();

        FunctionProtoTypeLoc FTL;
        TypeLoc RL;
        TypeLoc Next;
        clang::SourceLocation ParamStartLoc;
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
                ParamStartLoc = FTL.getLParenLoc();
            }
        }

        auto F = new FunctionType();
        F->returnType = GetQualifiedType(FP->getReturnType(), &RL);
        F->callingConvention = ConvertCallConv(FP->getCallConv());
        F->exceptionSpecType = ConvertExceptionType(FP->getExceptionSpecType());

        for (unsigned i = 0; i < FP->getNumParams(); ++i)
        {
            if (FTL && FTL.getParam(i))
            {
                auto PVD = FTL.getParam(i);
                auto FA = WalkParameter(PVD, ParamStartLoc);
                F->Parameters.push_back(FA);
            }
            else
            {
                auto FA = new Parameter();
                auto Arg = FP->getParamType(i);
                FA->name = "";
                FA->qualifiedType = GetQualifiedType(Arg);

                // In this case we have no valid value to use as a pointer so
                // use a special value known to the managed side to make sure
                // it gets ignored.
                FA->originalPtr = IgnorePtr;
                F->Parameters.push_back(FA);
            }
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
        MPT->pointee = GetQualifiedType(MP->getPointeeType(), &Next);
        
        Ty = MPT;
        break;
    }
    case clang::Type::TemplateSpecialization:
    {
        auto TS = Type->getAs<clang::TemplateSpecializationType>();
        auto TST = new TemplateSpecializationType();
        
        TemplateName Name = TS->getTemplateName();
        TST->_template = static_cast<Template*>(WalkDeclaration(
            Name.getAsTemplateDecl()));
        if (TS->isSugared())
            TST->desugared = GetQualifiedType(TS->getCanonicalTypeInternal(), TL);

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
            TST->desugared = GetQualifiedType(TS->getCanonicalTypeInternal(), TL);

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

        DependentTemplateSpecializationTypeLoc TSpecTL;
        DependentTemplateSpecializationTypeLoc *TSTL = 0;
        if (LocValid)
        {
            TSpecTL = TL->getAs<DependentTemplateSpecializationTypeLoc>();
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
            TPT->parameter->name = Ident->getName().str();

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

            TPT->parameter = WalkTypeTemplateParameter(TTTL.getDecl());
        }
        else if (TP->getDecl())
            TPT->parameter = WalkTypeTemplateParameter(TP->getDecl());
        TPT->depth = TP->getDepth();
        TPT->index = TP->getIndex();
        TPT->isParameterPack = TP->isParameterPack();

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
        TPT->replacement = GetQualifiedType(RepTy, &Next);
        TPT->replacedParameter = (TemplateParameterType*)
            WalkType(clang::QualType(TP->getReplacedParameter(), 0), 0);
        TPT->replacedParameter->parameter = WalkTypeTemplateParameter(
            TP->getReplacedParameter()->getDecl());

        Ty = TPT;
        break;
    }
    case clang::Type::InjectedClassName:
    {
        auto ICN = Type->getAs<clang::InjectedClassNameType>();
        auto ICNT = new InjectedClassNameType();
        ICNT->_class = static_cast<Class*>(WalkDeclaration(
            ICN->getDecl()));
        ICNT->injectedSpecializationType = GetQualifiedType(
            ICN->getInjectedSpecializationType());

        Ty = ICNT;
        break;
    }
    case clang::Type::DependentName:
    {
        auto DN = Type->getAs<clang::DependentNameType>();
        auto DNT = new DependentNameType();
        switch (DN->getQualifier()->getKind())
        {
        case clang::NestedNameSpecifier::SpecifierKind::TypeSpec:
        case clang::NestedNameSpecifier::SpecifierKind::TypeSpecWithTemplate:
        {
            const auto& Qualifier = clang::QualType(DN->getQualifier()->getAsType(), 0);
            if (LocValid)
            {
                const auto& DNTL = TL->getAs<DependentNameTypeLoc>();
                if (!DNTL.isNull())
                {
                    const auto& QL = DNTL.getQualifierLoc();
                    const auto& NNSL = QL.getTypeLoc();
                    DNT->qualifier = GetQualifiedType(Qualifier, &NNSL);
                }
                else
                {
                    DNT->qualifier = GetQualifiedType(Qualifier, 0);
                }
            }
            else
            {
                DNT->qualifier = GetQualifiedType(Qualifier, 0);
            }
            break;
        }
        default: break;
        }
        DNT->identifier = DN->getIdentifier()->getName().str();

        Ty = DNT;
        break;
    }
    case clang::Type::LValueReference:
    {
        auto LR = Type->getAs<clang::LValueReferenceType>();

        auto P = new PointerType();
        P->modifier = PointerType::TypeModifier::LVReference;

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->qualifiedPointee = GetQualifiedType(Pointee, &Next);

        Ty = P;
        break;
    }
    case clang::Type::RValueReference:
    {
        auto LR = Type->getAs<clang::RValueReferenceType>();

        auto P = new PointerType();
        P->modifier = PointerType::TypeModifier::RVReference;

        TypeLoc Next;
        if (LocValid) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->qualifiedPointee = GetQualifiedType(Pointee, &Next);

        Ty = P;
        break;
    }
    case clang::Type::UnaryTransform:
    {
        auto UT = Type->getAs<clang::UnaryTransformType>();

        TypeLoc Loc;
        if (LocValid)
        {
            clang::TypeSourceInfo* TSI = TL->getAs<UnaryTransformTypeLoc>().getUnderlyingTInfo();
            Loc = TSI->getTypeLoc();
        }

        auto UTT = new UnaryTransformType();
        UTT->desugared = GetQualifiedType(UT->isSugared() ? UT->getCanonicalTypeInternal() : UT->getBaseType(), &Loc);
        UTT->baseType = GetQualifiedType(UT->getBaseType(), &Loc);

        Ty = UTT;
        break;
    }
    case clang::Type::TypeClass::Using:
    {
        auto U = Type->getAs<clang::UsingType>();
        Ty = WalkType(U->getUnderlyingType(), TL);
        break;
    }
    case clang::Type::Vector:
    {
        auto V = Type->getAs<clang::VectorType>();

        auto VT = new VectorType();
        VT->elementType = GetQualifiedType(V->getElementType());
        VT->numElements = V->getNumElements();

        Ty = VT;
        break;
    }
    case clang::Type::PackExpansion:
    {
        // TODO: stubbed
        Ty = new PackExpansionType();
        break;
    }
    case clang::Type::Auto:
    {
        auto AT = Type->getAs<clang::AutoType>();
        if (AT->isSugared())
            Ty = WalkType(AT->getCanonicalTypeInternal());
        else
            return nullptr;
        break;
    }
    case clang::Type::Decltype:
    {
        auto DT = Type->getAs<clang::DecltypeType>();
        Ty = WalkType(DT->getUnderlyingType(), TL);
        break;
    }
    case clang::Type::MacroQualified:
    {
        auto MT = Type->getAs<clang::MacroQualifiedType>();
        Ty = WalkType(MT->getUnderlyingType(), TL);
        break;
    }
    default:
    {   
        Debug("Unhandled type class '%s'\n", Type->getTypeClassName());
        return nullptr;
    } }

    Ty->isDependent = Type->isDependentType();
    return Ty;
}

//-----------------------------------//

Enumeration* Parser::WalkEnum(const clang::EnumDecl* ED)
{
    using namespace clang;

    auto NS = GetNamespace(ED);
    assert(NS && "Expected a valid namespace");

    auto E = NS->FindEnum(ED->getCanonicalDecl());
    if (E && !E->isIncomplete)
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

    if (E && !E->isIncomplete)
        return E;

    if (!E)
    {
        auto Name = GetTagDeclName(ED);
        if (!Name.empty())
            E = NS->FindEnum(Name, /*Create=*/true);
        else
        {
            E = new Enumeration();
            E->name = Name;
            E->_namespace = NS;
            NS->Enums.push_back(E);
        }
        HandleDeclaration(ED, E);
    }

    if (ED->isScoped())
        E->modifiers = (Enumeration::EnumModifiers)
            ((int)E->modifiers | (int)Enumeration::EnumModifiers::Scoped);

    // Get the underlying integer backing the enum.
    clang::QualType IntType = ED->getIntegerType();
    E->type = WalkType(IntType, 0);
    E->builtinType = static_cast<BuiltinType*>(WalkType(IntType, 0,
        /*DesugarType=*/true));

    if (!ED->isThisDeclarationADefinition())
    {
        E->isIncomplete = true;
        return E;
    }

    E->isIncomplete = false;
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

    EnumItem->name = ECD->getNameAsString();
    auto Value = ECD->getInitVal();
    EnumItem->value = Value.isSigned() ? Value.getSExtValue()
        : Value.getZExtValue();
    EnumItem->_namespace = GetNamespace(ECD);

    std::string Text;
    if (GetDeclText(ECD->getSourceRange(), Text))
        EnumItem->expression = Text;

    return EnumItem;
}

//-----------------------------------//

static const clang::CodeGen::CGFunctionInfo& GetCodeGenFunctionInfo(
    clang::CodeGen::CodeGenTypes* CodeGenTypes, const clang::FunctionDecl* FD)
{
    auto FTy = FD->getType()->getCanonicalTypeUnqualified();
    return CodeGenTypes->arrangeFreeFunctionType(
        FTy.castAs<clang::FunctionProtoType>());
}

bool Parser::CanCheckCodeGenInfo(const clang::Type* Ty)
{
    auto FinalType = GetFinalType(Ty);

    if (FinalType->isDependentType() ||
        FinalType->isInstantiationDependentType() ||
        FinalType->isUndeducedType())
        return false;

    if (FinalType->isFunctionType())
    {
        auto FTy = FinalType->getAs<clang::FunctionType>();
        auto CanCheck = CanCheckCodeGenInfo(FTy->getReturnType().getTypePtr());
        if (!CanCheck)
            return false;

        if (FinalType->isFunctionProtoType())
        {
            auto FPTy = FinalType->getAs<clang::FunctionProtoType>();
            for (const auto& ParamType : FPTy->getParamTypes())
            {
                auto CanCheck = CanCheckCodeGenInfo(ParamType.getTypePtr());
                if (!CanCheck)
                    return false;
            }
        }
    }

    if (auto RT = FinalType->getAs<clang::RecordType>())
        if (!HasLayout(RT->getDecl()))
            return false;

    // Lock in the MS inheritance model if we have a member pointer to a class,
    // else we get an assertion error inside Clang's codegen machinery.
    if (c->getTarget().getCXXABI().isMicrosoft())
    {
        if (auto MPT = Ty->getAs<clang::MemberPointerType>())
            if (!MPT->isDependentType())
                c->getSema().RequireCompleteType(clang::SourceLocation(),
                    clang::QualType(Ty, 0), 1);
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
    default:
        break;
    }

    return Loc;
}

void Parser::CompleteIfSpecializationType(const clang::QualType& QualType)
{
    using namespace clang;

    auto Type = QualType->getUnqualifiedDesugaredType();
    auto RD = Type->getAsCXXRecordDecl();
    if (!RD)
        RD = const_cast<CXXRecordDecl*>(Type->getPointeeCXXRecordDecl());
    ClassTemplateSpecializationDecl* CTS;
    if (!RD ||
        !(CTS = llvm::dyn_cast<ClassTemplateSpecializationDecl>(RD)))
        return;

    auto existingClient = c->getSema().getDiagnostics().getClient();
    std::unique_ptr<::DiagnosticConsumer> SemaDiagnostics(new ::DiagnosticConsumer());
    SemaDiagnostics->Decl = CTS;
    c->getSema().getDiagnostics().setClient(SemaDiagnostics.get(), false);

    Scope Scope(nullptr, Scope::ScopeFlags::ClassScope, c->getSema().getDiagnostics());
    c->getSema().TUScope = &Scope;

    if (!CTS->isCompleteDefinition())
        c->getSema().InstantiateClassTemplateSpecialization(CTS->getBeginLoc(),
            CTS, clang::TemplateSpecializationKind::TSK_ImplicitInstantiation, false);

    c->getSema().getDiagnostics().setClient(existingClient, false);
    c->getSema().TUScope = nullptr;

    auto CT = WalkClassTemplate(CTS->getSpecializedTemplate());
    auto USR = GetDeclUSR(CTS);
    auto TS = CT->FindSpecialization(USR);
    if (TS != nullptr && TS->isIncomplete)
    {
        TS->isIncomplete = false;
        TS->specializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
        WalkRecordCXX(CTS, TS);
    }
}

Parameter* Parser::WalkParameter(const clang::ParmVarDecl* PVD,
    const clang::SourceLocation& ParamStartLoc)
{
    using namespace clang;

    auto P = walkedParameters[PVD];
    if (P)
        return P;

    P = new Parameter();
    P->name = PVD->getNameAsString();

    TypeLoc PTL;
    if (auto TSI = PVD->getTypeSourceInfo())
        PTL = TSI->getTypeLoc();

    auto paramRange = PVD->getSourceRange();
    paramRange.setBegin(ParamStartLoc);

    HandlePreprocessedEntities(P, paramRange, MacroLocation::FunctionParameters);

    const auto& Type = PVD->getOriginalType();
    auto Function = PVD->getParentFunctionOrMethod();
    if (Function && cast<NamedDecl>(Function)->isExternallyVisible())
        CompleteIfSpecializationType(Type);
    P->qualifiedType = GetQualifiedType(Type, &PTL);
    P->hasDefaultValue = PVD->hasDefaultArg();
    P->index = PVD->getFunctionScopeIndex();
    if (PVD->hasDefaultArg() && !PVD->hasUnparsedDefaultArg())
    {
        if (PVD->hasUninstantiatedDefaultArg())
            P->defaultArgument = WalkExpressionObsolete(PVD->getUninstantiatedDefaultArg());
        else
            P->defaultArgument = WalkExpressionObsolete(PVD->getDefaultArg());
    }
    HandleDeclaration(PVD, P);
    walkedParameters[PVD] = P;
    auto Context = cast<Decl>(PVD->getDeclContext());
    P->_namespace = static_cast<DeclarationContext*>(WalkDeclaration(Context));

    return P;
}

void Parser::SetBody(const clang::FunctionDecl* FD, Function* F)
{
    F->body = GetFunctionBody(FD);
    F->isInline = FD->isInlined();
    if (!F->body.empty() && F->isInline)
        return;
    for (const auto& R : FD->redecls())
    {
        if (F->body.empty())
            F->body = GetFunctionBody(R);
        F->isInline |= R->isInlined();
        if (!F->body.empty() && F->isInline)
            break;
    }
}

static bool IsInvalid(clang::Stmt* Body, std::unordered_set<clang::Stmt*>& Bodies)
{
    using namespace clang;

    if (Bodies.find(Body) != Bodies.end())
        return false;
    Bodies.insert(Body);

    if (auto E = dyn_cast<clang::Expr>(Body))
        if (E->containsErrors())
            return true;

    Decl* D = 0;
    switch (Body->getStmtClass())
    {
    case clang::Stmt::StmtClass::DeclRefExprClass:
        D = cast<clang::DeclRefExpr>(Body)->getDecl();
        break;
    case clang::Stmt::StmtClass::MemberExprClass:
        D = cast<clang::MemberExpr>(Body)->getMemberDecl();
        break;
    default:
        break;
    }
    if (D)
    {
        if (D->isInvalidDecl())
            return true;
        if (auto F = dyn_cast<FunctionDecl>(D))
            if (IsInvalid(F->getBody(), Bodies))
                return true;
    }
    for (auto C : Body->children())
        if (IsInvalid(C, Bodies))
            return true;
    return false;
}

void Parser::MarkValidity(Function* F)
{
    using namespace clang;

    auto FD = static_cast<FunctionDecl*>(F->originalPtr);

    if (!FD->isImplicit() &&
        (!FD->getTemplateInstantiationPattern() || !FD->isExternallyVisible()))
        return;

    auto existingClient = c->getSema().getDiagnostics().getClient();
    std::unique_ptr<::DiagnosticConsumer> SemaDiagnostics(new ::DiagnosticConsumer());
    SemaDiagnostics->Decl = FD;
    c->getSema().getDiagnostics().setClient(SemaDiagnostics.get(), false);

    Scope Scope(nullptr, Scope::ScopeFlags::FnScope, c->getSema().getDiagnostics());
    c->getSema().TUScope = &Scope;

    c->getSema().InstantiateFunctionDefinition(FD->getBeginLoc(), FD,
        /*Recursive*/true);
    F->isInvalid = FD->isInvalidDecl();
    if (!F->isInvalid)
    {
        std::unordered_set<clang::Stmt*> Bodies{ 0 };
        F->isInvalid = IsInvalid(FD->getBody(), Bodies);
    }

    if (!F->isInvalid)
    {
        DeclContext* Context = FD->getDeclContext();
        while (Context)
        {
            F->isInvalid = cast<Decl>(Context)->isInvalidDecl();
            if (F->isInvalid)
                break;
            Context = Context->getParent();
        }
    }

    c->getSema().getDiagnostics().setClient(existingClient, false);
    c->getSema().TUScope = nullptr;
}

void Parser::WalkFunction(const clang::FunctionDecl* FD, Function* F)
{
    using namespace clang;

    assert(FD->getBuiltinID() == 0);
    auto FT = FD->getType()->getAs<clang::FunctionType>();

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    F->name = FD->getNameAsString();
    F->_namespace = NS;
    F->isConstExpr = FD->isConstexpr();
    F->isVariadic = FD->isVariadic();
    F->isDependent = FD->isDependentContext();
    F->isPure = FD->isPure();
    F->isDeleted = FD->isDeleted();
    F->isDefaulted = FD->isDefaulted();
    SetBody(FD, F);
    if (auto InstantiatedFrom = FD->getTemplateInstantiationPattern())
        F->instantiatedFrom = static_cast<Function*>(WalkDeclaration(InstantiatedFrom));

    auto FK = FD->getFriendObjectKind();
    F->friendKind = ConvertFriendKind(FK);
    auto CC = FT->getCallConv();
    F->callingConvention = ConvertCallConv(CC);

    F->operatorKind = GetOperatorKindFromDecl(FD->getDeclName());

    TypeLoc RTL;
    FunctionTypeLoc FTL;
    if (auto TSI = FD->getTypeSourceInfo())
    {
        auto Loc = DesugarTypeLoc(TSI->getTypeLoc());
        FTL = Loc.getAs<FunctionTypeLoc>();
        if (FTL)
        {
            RTL = FTL.getReturnLoc();

            auto& SM = c->getSourceManager();
            auto headStartLoc = GetDeclStartLocation(c.get(), FD);
            auto headEndLoc = SM.getExpansionLoc(FTL.getLParenLoc());
            auto headRange = clang::SourceRange(headStartLoc, headEndLoc);

            HandlePreprocessedEntities(F, headRange, MacroLocation::FunctionHead);
            HandlePreprocessedEntities(F, FTL.getParensRange(), MacroLocation::FunctionParameters);
        }
    }

    auto ReturnType = FD->getReturnType();
    if (FD->isExternallyVisible())
        CompleteIfSpecializationType(ReturnType);
    F->returnType = GetQualifiedType(ReturnType, &RTL);

    const auto& Mangled = GetDeclMangledName(FD);
    F->mangled = Mangled;

    const auto& Body = GetFunctionBody(FD);
    F->body = Body;

    clang::SourceLocation ParamStartLoc = FD->getBeginLoc();
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
            assert(!FTInfo.isNull());

            ParamStartLoc = FTInfo.getLParenLoc();
            ResultLoc = FTInfo.getReturnLoc().getBeginLoc();
        }
    }

    clang::SourceLocation BeginLoc = FD->getBeginLoc();
    if (ResultLoc.isValid())
        BeginLoc = ResultLoc;

    clang::SourceRange Range(BeginLoc, FD->getEndLoc());

    std::string Sig;
    if (GetDeclText(Range, Sig))
        F->signature = Sig;

    for (auto VD : FD->parameters())
    {
        auto P = WalkParameter(VD, ParamStartLoc);
        F->Parameters.push_back(P);

        ParamStartLoc = VD->getEndLoc();
    }

    if (!opts->skipFunctionBodies && FD->hasBody())
    {
        if (auto Body = FD->getBody())
            F->bodyStmt = WalkStatement(Body);
    }

    auto& CXXABI = codeGenTypes->getCXXABI();
    bool HasThisReturn = false;
    if (auto CD = dyn_cast<CXXConstructorDecl>(FD))
        HasThisReturn = CXXABI.HasThisReturn(GlobalDecl(CD, Ctor_Complete));
    else if (auto DD = dyn_cast<CXXDestructorDecl>(FD))
        HasThisReturn = CXXABI.HasThisReturn(GlobalDecl(DD, Dtor_Complete));
    else
        HasThisReturn = CXXABI.HasThisReturn(FD);

    F->hasThisReturn = HasThisReturn;

    if (auto FTSI = FD->getTemplateSpecializationInfo())
        F->specializationInfo = WalkFunctionTemplateSpec(FTSI, F);

    MarkValidity(F);
    F->qualifiedType = GetQualifiedType(FD->getType(), &FTL);

    const CXXMethodDecl* MD;
    if (FD->isDependentContext() ||
        ((MD = dyn_cast<CXXMethodDecl>(FD)) && !MD->isStatic() &&
            !HasLayout(cast<CXXRecordDecl>(MD->getDeclContext()))) ||
        !CanCheckCodeGenInfo(FD->getReturnType().getTypePtr()) ||
        std::any_of(FD->parameters().begin(), FD->parameters().end(),
            [this](auto* P) { return !CanCheckCodeGenInfo(P->getType().getTypePtr()); }))
    {
        return;
    }

    auto& CGInfo = GetCodeGenFunctionInfo(codeGenTypes.get(), FD);
    F->isReturnIndirect = CGInfo.getReturnInfo().isIndirect() ||
        CGInfo.getReturnInfo().isInAlloca();

    unsigned Index = 0;
    for (const auto& Arg : CGInfo.arguments())
    {
        F->Parameters[Index++]->isIndirect =
            Arg.info.isIndirect() && !Arg.info.getIndirectByVal();
    }
}

Function* Parser::WalkFunction(const clang::FunctionDecl* FD)
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
    NS->Functions.push_back(F);
    WalkFunction(FD, F);

    return F;
}

//-----------------------------------//

SourceLocationKind Parser::GetLocationKind(const clang::SourceLocation& Loc)
{
    using namespace clang;

    clang::SourceManager& SM = c->getSourceManager();
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

void Parser::WalkAST(clang::TranslationUnitDecl* TU)
{
    for (auto D : TU->decls())
    {
        if (D->getBeginLoc().isValid() &&
            !c->getSourceManager().isInSystemHeader(D->getBeginLoc()))
            WalkDeclarationDef(D);
    }
}

//-----------------------------------//

void Parser::WalkVariable(const clang::VarDecl* VD, Variable* Var)
{
    HandleDeclaration(VD, Var);

    Var->isConstExpr = VD->isConstexpr();
    Var->name = VD->getName().str();
    Var->access = ConvertToAccess(VD->getAccess());

    auto Init = VD->getAnyInitializer();
    Var->initializer = (Init && !Init->getType()->isDependentType()) ?
        WalkVariableInitializerExpression(Init) : nullptr;

    auto TL = VD->getTypeSourceInfo()->getTypeLoc();
    Var->qualifiedType = GetQualifiedType(VD->getType(), &TL);

    auto Mangled = GetDeclMangledName(VD);
    Var->mangled = Mangled;
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
    Var->_namespace = NS;

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
    F->_namespace = NS;

    if (FriendDecl)
    {
        F->declaration = GetDeclarationFromFriend(FriendDecl);
    }

    NS->Friends.push_back(F);

    return F;
}

//-----------------------------------//

bool Parser::GetDeclText(clang::SourceRange SR, std::string& Text)
{
    using namespace clang;
    clang::SourceManager& SM = c->getSourceManager();
    const LangOptions &LangOpts = c->getLangOpts();

    auto Range = CharSourceRange::getTokenRange(SR);

    bool Invalid;
    Text = Lexer::getSourceText(Range, SM, LangOpts, &Invalid).str();

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
        if (Entity->originalPtr == PPEntity)
            return Entity;
    }

    auto& P = c->getPreprocessor();

    PreprocessedEntity* Entity = 0;

    switch(PPEntity->getKind())
    {
    case clang::PreprocessedEntity::MacroExpansionKind:
    {
        auto ME = cast<clang::MacroExpansion>(PPEntity);
        auto Expansion = new MacroExpansion();
        auto MD = ME->getDefinition();
        if (MD && MD->getKind() != clang::PreprocessedEntity::InvalidKind)
            Expansion->definition = (MacroDefinition*)
                WalkPreprocessedEntity(Decl, ME->getDefinition());
        Entity = Expansion;

        std::string Text;
        GetDeclText(PPEntity->getSourceRange(), Text);

        static_cast<MacroExpansion*>(Entity)->text = Text;
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

        if (!MI || MI->isBuiltinMacro())
            break;

        clang::SourceManager& SM = c->getSourceManager();
        const LangOptions &LangOpts = c->getLangOpts();

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
        Definition->lineNumberStart = SM.getExpansionLineNumber(MD->getLocation());
        Definition->lineNumberEnd = SM.getExpansionLineNumber(MD->getLocation());
        Entity = Definition;

        Definition->name = II->getName().trim().str();
        Definition->expression = Expression.trim().str();
    }
    case clang::PreprocessedEntity::InclusionDirectiveKind:
        // nothing to be done for InclusionDirectiveKind
        break;
    default:
        llvm_unreachable("Unknown PreprocessedEntity");
    }

    if (!Entity)
        return nullptr;

    Entity->originalPtr = PPEntity;
    auto Namespace = GetTranslationUnit(PPEntity->getSourceRange().getBegin());

    if (Decl->kind == CppSharp::CppParser::AST::DeclarationKind::TranslationUnit)
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
    auto PPRecord = c->getPreprocessor().getPreprocessingRecord();

    for (auto it = PPRecord->begin(); it != PPRecord->end(); ++it)
    {
        clang::PreprocessedEntity* PPEntity = (*it);
        auto Entity = WalkPreprocessedEntity(Decl, PPEntity);
    }
}

AST::ExpressionObsolete* Parser::WalkExpressionObsolete(const clang::Expr* Expr)
{
    using namespace clang;

    switch (Expr->getStmtClass())
    {
    case clang::Stmt::BinaryOperatorClass:
    {
        auto BinaryOperator = cast<clang::BinaryOperator>(Expr);
        return new AST::BinaryOperatorObsolete(GetStringFromStatement(Expr),
            WalkExpressionObsolete(BinaryOperator->getLHS()), WalkExpressionObsolete(BinaryOperator->getRHS()),
            BinaryOperator->getOpcodeStr().str());
    }
    case clang::Stmt::CallExprClass:
    {
        auto CallExpr = cast<clang::CallExpr>(Expr);
        auto CallExpression = new AST::CallExprObsolete(GetStringFromStatement(Expr),
            CallExpr->getCalleeDecl() ? WalkDeclaration(CallExpr->getCalleeDecl()) : 0);
        for (auto arg : CallExpr->arguments())
        {
            CallExpression->Arguments.push_back(WalkExpressionObsolete(arg));
        }
        return CallExpression;
    }
    case clang::Stmt::DeclRefExprClass:
        return new AST::ExpressionObsolete(GetStringFromStatement(Expr), StatementClassObsolete::DeclRefExprClass,
            WalkDeclaration(cast<clang::DeclRefExpr>(Expr)->getDecl()));
    case clang::Stmt::CStyleCastExprClass:
    case clang::Stmt::CXXConstCastExprClass:
    case clang::Stmt::CXXDynamicCastExprClass:
    case clang::Stmt::CXXFunctionalCastExprClass:
    case clang::Stmt::CXXReinterpretCastExprClass:
    case clang::Stmt::CXXStaticCastExprClass:
    case clang::Stmt::ImplicitCastExprClass:
        return WalkExpressionObsolete(cast<clang::CastExpr>(Expr)->getSubExprAsWritten());
    case clang::Stmt::CXXOperatorCallExprClass:
    {
        auto OperatorCallExpr = cast<clang::CXXOperatorCallExpr>(Expr);
        return new AST::ExpressionObsolete(GetStringFromStatement(Expr), StatementClassObsolete::CXXOperatorCallExpr,
            OperatorCallExpr->getCalleeDecl() ? WalkDeclaration(OperatorCallExpr->getCalleeDecl()) : 0);
    }
    case clang::Stmt::CXXConstructExprClass:
    case clang::Stmt::CXXTemporaryObjectExprClass:
    {
        auto ConstructorExpr = cast<clang::CXXConstructExpr>(Expr);
        if (ConstructorExpr->getNumArgs() == 1)
        {
            auto Arg = ConstructorExpr->getArg(0);
            auto TemporaryExpr = dyn_cast<clang::MaterializeTemporaryExpr>(Arg);
            if (TemporaryExpr)
            {
                auto SubTemporaryExpr = TemporaryExpr->getSubExpr();
                auto Cast = dyn_cast<clang::CastExpr>(SubTemporaryExpr);
                if (!Cast ||
                    (Cast->getSubExprAsWritten()->getStmtClass() != clang::Stmt::IntegerLiteralClass &&
                     Cast->getSubExprAsWritten()->getStmtClass() != clang::Stmt::CXXNullPtrLiteralExprClass))
                    return WalkExpressionObsolete(SubTemporaryExpr);
                return new AST::CXXConstructExprObsolete(GetStringFromStatement(Expr),
                    WalkDeclaration(ConstructorExpr->getConstructor()));
            }
        }
        auto ConstructorExpression = new AST::CXXConstructExprObsolete(GetStringFromStatement(Expr),
            WalkDeclaration(ConstructorExpr->getConstructor()));
        for (auto arg : ConstructorExpr->arguments())
        {
            ConstructorExpression->Arguments.push_back(WalkExpressionObsolete(arg));
        }
        return ConstructorExpression;
    }
    case clang::Stmt::CXXBindTemporaryExprClass:
        return WalkExpressionObsolete(cast<clang::CXXBindTemporaryExpr>(Expr)->getSubExpr());
    case clang::Stmt::CXXDefaultArgExprClass:
        return WalkExpressionObsolete(cast<clang::CXXDefaultArgExpr>(Expr)->getExpr());
    case clang::Stmt::MaterializeTemporaryExprClass:
        return WalkExpressionObsolete(cast<clang::MaterializeTemporaryExpr>(Expr)->getSubExpr());
    default:
        break;
    }

    if (!Expr->isValueDependent())
    {
        clang::Expr::EvalResult integer;
        if (Expr->getStmtClass() == clang::Stmt::CharacterLiteralClass)
        {
            auto result = GetStringFromStatement(Expr);
            if (!result.empty() && 
                result.front() != '\'' && 
                Expr->EvaluateAsInt(integer, c->getASTContext()))
            {
                result = llvm::toString(integer.Val.getInt(), 10);
            }

            return new AST::ExpressionObsolete(result);
        }
        else if (Expr->getStmtClass() != clang::Stmt::CXXBoolLiteralExprClass &&
            Expr->getStmtClass() != clang::Stmt::UnaryExprOrTypeTraitExprClass &&
            Expr->EvaluateAsInt(integer, c->getASTContext())
            )
        {
            return new AST::ExpressionObsolete(llvm::toString(integer.Val.getInt(), 10));
        }
    }

    return new AST::ExpressionObsolete(GetStringFromStatement(Expr));
}

AST::ExpressionObsolete* Parser::WalkVariableInitializerExpression(const clang::Expr* Expr)
{
    using namespace clang;

    if (IsCastStmt(Expr->getStmtClass()))
        return WalkVariableInitializerExpression(cast<clang::CastExpr>(Expr)->getSubExprAsWritten());

    if (IsLiteralStmt(Expr->getStmtClass()))
      return WalkExpressionObsolete(Expr);

    clang::Expr::EvalResult result;
    if (!Expr->isValueDependent() &&
        Expr->EvaluateAsConstantExpr(result, c->getASTContext()))
    {
        std::string s;
        llvm::raw_string_ostream out(s);
        APValuePrinter printer{c->getASTContext(), out};

        if (printer.Print(result.Val, Expr->getType()))
            return new AST::ExpressionObsolete(out.str());
    }
    
    return WalkExpressionObsolete(Expr);
}

bool Parser::IsCastStmt(clang::Stmt::StmtClass stmt) 
{
    switch (stmt)
    {
    case clang::Stmt::CStyleCastExprClass:
    case clang::Stmt::CXXConstCastExprClass:
    case clang::Stmt::CXXDynamicCastExprClass:
    case clang::Stmt::CXXFunctionalCastExprClass:
    case clang::Stmt::CXXReinterpretCastExprClass:
    case clang::Stmt::CXXStaticCastExprClass:
    case clang::Stmt::ImplicitCastExprClass:
        return true;
    default:
        return false;
    }
}

bool Parser::IsLiteralStmt(clang::Stmt::StmtClass stmt) 
{
    switch (stmt)
    {
    case clang::Stmt::CharacterLiteralClass:
    case clang::Stmt::FixedPointLiteralClass:
    case clang::Stmt::FloatingLiteralClass:
    case clang::Stmt::IntegerLiteralClass:
    case clang::Stmt::StringLiteralClass:
    case clang::Stmt::ImaginaryLiteralClass:
    case clang::Stmt::UserDefinedLiteralClass:
    case clang::Stmt::CXXNullPtrLiteralExprClass:
    case clang::Stmt::CXXBoolLiteralExprClass:
        return true;
    default:
        return false;
    }
}

std::string Parser::GetStringFromStatement(const clang::Stmt* Statement)
{
    using namespace clang;

    PrintingPolicy Policy(c->getLangOpts());
    std::string s;
    llvm::raw_string_ostream as(s);
    Statement->printPretty(as, 0, Policy);
    return as.str();
}

std::string Parser::GetFunctionBody(const clang::FunctionDecl* FD)
{
    if (!FD->getBody())
        return "";

    clang::PrintingPolicy Policy(c->getLangOpts());
    std::string s;
    llvm::raw_string_ostream as(s);
    FD->getBody()->printPretty(as, 0, Policy);
    return as.str();
}

void Parser::HandlePreprocessedEntities(Declaration* Decl,
                                        clang::SourceRange sourceRange,
                                        MacroLocation macroLocation)
{
    if (sourceRange.isInvalid()) return;

    auto& SourceMgr = c->getSourceManager();
    auto isBefore = SourceMgr.isBeforeInTranslationUnit(sourceRange.getEnd(),
        sourceRange.getBegin());

    if (isBefore) return;

    assert(!SourceMgr.isBeforeInTranslationUnit(sourceRange.getEnd(),
        sourceRange.getBegin()));

    using namespace clang;
    auto PPRecord = c->getPreprocessor().getPreprocessingRecord();

    auto Range = PPRecord->getPreprocessedEntitiesInRange(sourceRange);

    for (auto PPEntity : Range)
    {
        auto Entity = WalkPreprocessedEntity(Decl, PPEntity);
        if (!Entity) continue;
 
        if (Entity->macroLocation == MacroLocation::Unknown)
            Entity->macroLocation = macroLocation;
    }
}

void Parser::HandleOriginalText(const clang::Decl* D, Declaration* Decl)
{
    auto& SM = c->getSourceManager();
    auto& LangOpts = c->getLangOpts();

    auto Range = clang::CharSourceRange::getTokenRange(D->getSourceRange());

    bool Invalid;
    auto DeclText = clang::Lexer::getSourceText(Range, SM, LangOpts, &Invalid);
    
    if (!Invalid)
        Decl->debugText = DeclText.str();
}

void Parser::HandleDeclaration(const clang::Decl* D, Declaration* Decl)
{
    if (Decl->originalPtr != nullptr)
        return;

    Decl->originalPtr = (void*) D;
    Decl->USR = GetDeclUSR(D);
    Decl->isImplicit = D->isImplicit();
    Decl->location = SourceLocation(D->getLocation().getRawEncoding());
    auto IsDeclExplicit = IsExplicit(D);
    if (IsDeclExplicit)
    {
        Decl->lineNumberStart = c->getSourceManager().getExpansionLineNumber(D->getBeginLoc());
        Decl->lineNumberEnd = c->getSourceManager().getExpansionLineNumber(D->getEndLoc());
    }
    else
    {
        Decl->lineNumberStart = -1;
        Decl->lineNumberEnd = -1;
    }

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
        else if (IsDeclExplicit)
        {
            auto startLoc = GetDeclStartLocation(c.get(), D);
            auto endLoc = D->getEndLoc();
            auto range = clang::SourceRange(startLoc, endLoc);

            HandlePreprocessedEntities(Decl, range);
        }
    }

    if (IsDeclExplicit)
        HandleOriginalText(D, Decl);
    HandleComments(D, Decl);

    if (const clang::ValueDecl *VD = clang::dyn_cast_or_null<clang::ValueDecl>(D))
        Decl->isDependent = VD->getType()->isDependentType();

    if (const clang::DeclContext *DC = clang::dyn_cast_or_null<clang::DeclContext>(D))
        Decl->isDependent |= DC->isDependentContext();

    Decl->access = ConvertToAccess(D->getAccess());
}

//-----------------------------------//

Declaration* Parser::WalkDeclarationDef(clang::Decl* D)
{
    auto Decl = WalkDeclaration(D);
    if (!Decl || Decl->definitionOrder > 0)
        return Decl;
    // We store a definition order index into the declarations.
    // This is needed because declarations are added to their contexts as
    // soon as they are referenced and we need to know the original order
    // of the declarations.
    clang::RecordDecl* RecordDecl;
    if ((RecordDecl = llvm::dyn_cast<clang::RecordDecl>(D)) &&
        RecordDecl->isCompleteDefinition())
        Decl->definitionOrder = index++;
    return Decl;
}

Declaration* Parser::WalkDeclaration(const clang::Decl* D)
{
    using namespace clang;

    if (D == nullptr)
        return nullptr;

    Declaration* Decl = nullptr;

    auto Kind = D->getKind();
    switch(D->getKind())
    {
    case Decl::Record:
    {
        auto RD = cast<RecordDecl>(D);
        Decl = WalkRecord(RD);
        break;
    }
    case Decl::CXXRecord:
    {
        auto RD = cast<CXXRecordDecl>(D);
        Decl = WalkRecordCXX(RD);
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
        Typedef->qualifiedType = GetQualifiedType(TD->getUnderlyingType(), &TTL);
        AST::TypedefDecl* Existing;
        // if the typedef was added along the way, the just created one is useless, delete it
        if ((Existing = NS->FindTypedef(Name, /*Create=*/false)))
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
        TypeAlias->qualifiedType = GetQualifiedType(TD->getUnderlyingType(), &TTL);
        AST::TypeAlias* Existing;
        if ((Existing = NS->FindTypeAlias(Name, /*Create=*/false)))
            delete TypeAlias;
        else
            NS->TypeAliases.push_back(Existing = TypeAlias);

        if (auto TAT = TD->getDescribedAliasTemplate())
            TypeAlias->describedAliasTemplate = WalkTypeAliasTemplate(TAT);

        Decl = Existing;
        break;
    }
    case Decl::TranslationUnit:
    {
        Decl = GetTranslationUnit(D);
        break;
    }
    case Decl::Namespace:
    {
        auto ND = cast<NamespaceDecl>(D);

        for (auto D : ND->decls())
        {
            if (!isa<NamedDecl>(D) || IsSupported(cast<NamedDecl>(D)))
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
    case Decl::CXXDestructor:
    case Decl::CXXConversion:
    case Decl::CXXMethod:
    {
        auto MD = cast<CXXMethodDecl>(D);
        Decl = WalkMethodCXX(MD);
        if (Decl == nullptr)
            return Decl;

        auto NS = GetNamespace(MD);
        Decl->_namespace = NS;
        break;
    }
    case Decl::Field:
    {
        auto FD = cast<FieldDecl>(D);
        auto _Class = static_cast<Class*>(WalkDeclaration(FD->getParent()));
        Decl = WalkFieldCXX(FD, _Class);
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
    case Decl::UnresolvedUsingTypename:
    {
        auto UUTD = cast<UnresolvedUsingTypenameDecl>(D);
        Decl = WalkUnresolvedUsingTypename(UUTD);
        break;
    }
    case Decl::BuiltinTemplate:
    case Decl::ClassScopeFunctionSpecialization:
    case Decl::PragmaComment:
    case Decl::PragmaDetectMismatch:
    case Decl::Empty:
    case Decl::AccessSpec:
    case Decl::Using:
    case Decl::UsingDirective:
    case Decl::UsingShadow:
    case Decl::ConstructorUsingShadow:
    case Decl::UnresolvedUsingValue:
    case Decl::IndirectField:
    case Decl::StaticAssert:
    case Decl::NamespaceAlias:
        break;
    default:
    {
        Debug("Unhandled declaration kind: %s\n", D->getDeclKindName());

        auto& SM = c->getSourceManager();
        auto Loc = D->getLocation();
        auto FileName = SM.getFilename(Loc);
        auto Offset = SM.getFileOffset(Loc);
        auto LineNo = SM.getLineNumber(SM.getFileID(Loc), Offset);
        Debug("  %s (line %u)\n", FileName.str().c_str(), LineNo);

        break;
    } };

    if (Decl && D->hasAttrs())
    {
        for (auto it = D->attr_begin(); it != D->attr_end(); ++it)
        {
            Attr* Attr = (*it);
            switch(Attr->getKind())
            {
                case clang::attr::Kind::MaxFieldAlignment:
                {
                    auto MFA = cast<clang::MaxFieldAlignmentAttr>(Attr);
                    Decl->maxFieldAlignment = MFA->getAlignment() / 8; // bits to bytes.
                    break;
                }
                case clang::attr::Kind::Deprecated:
                {
                    auto DA = cast<clang::DeprecatedAttr>(Attr);
                    Decl->isDeprecated = true;
                    break;
                }
                case clang::attr::Kind::Aligned:
                    Decl->alignAs = GetAlignAs(cast<clang::AlignedAttr>(Attr));
                    break;
                default:
                    break;
            }
        }
    }

    return Decl;
}

int Parser::GetAlignAs(const clang::AlignedAttr* alignedAttr)
{
    return alignedAttr->isAlignas() &&
        !alignedAttr->isAlignmentErrorDependent() &&
        !alignedAttr->isAlignmentDependent()
        ? alignedAttr->getAlignment(c->getASTContext())
        : 0;
}

void Parser::HandleDiagnostics(ParserResult* res)
{
    auto DiagClient = (DiagnosticConsumer&) c->getDiagnosticClient();
    auto& Diags = DiagClient.Diagnostics;

    // Convert the diagnostics to the managed types
    for (unsigned I = 0, E = Diags.size(); I != E; ++I)
    {
        auto& Diag = DiagClient.Diagnostics[I];
        auto& Source = c->getSourceManager();
        auto FileName = Source.getFilename(Source.getFileLoc(Diag.Location));

        auto PDiag = ParserDiagnostic();
        PDiag.fileName = FileName.str();
        PDiag.message = Diag.Message.str().str();
        PDiag.lineNumber = 0;
        PDiag.columnNumber = 0;

        if( !Diag.Location.isInvalid() )
        {
             clang::PresumedLoc PLoc = Source.getPresumedLoc(Diag.Location);
             if( PLoc.isValid() )
             {
                PDiag.lineNumber = PLoc.getLine();
                PDiag.columnNumber = PLoc.getColumn();
             }
        }

        switch( Diag.Level )
        {
        case clang::DiagnosticsEngine::Ignored: 
            PDiag.level = ParserDiagnosticLevel::Ignored;
            break;
        case clang::DiagnosticsEngine::Note:
            PDiag.level = ParserDiagnosticLevel::Note;
            break;
        case clang::DiagnosticsEngine::Warning:
            PDiag.level = ParserDiagnosticLevel::Warning;
            break;
        case clang::DiagnosticsEngine::Error:
            PDiag.level = ParserDiagnosticLevel::Error;
            break;
        case clang::DiagnosticsEngine::Fatal:
            PDiag.level = ParserDiagnosticLevel::Fatal;
            break;
        default:
            assert(0);
        }

        res->Diagnostics.push_back(PDiag);
    }
}

void Parser::SetupLLVMCodegen()
{
    // Initialize enough Clang codegen machinery so we can get at ABI details.
    LLVMModule.reset(new llvm::Module("", LLVMCtx));

    LLVMModule->setTargetTriple(c->getTarget().getTriple().getTriple());
    LLVMModule->setDataLayout(c->getTarget().getDataLayoutString());

    CGM.reset(new clang::CodeGen::CodeGenModule(c->getASTContext(),
        c->getHeaderSearchOpts(), c->getPreprocessorOpts(),
        c->getCodeGenOpts(), *LLVMModule, c->getDiagnostics()));

    codeGenTypes.reset(new clang::CodeGen::CodeGenTypes(*CGM.get()));
}

bool Parser::SetupSourceFiles(const std::vector<std::string>& SourceFiles,
    std::vector<const clang::FileEntry*>& FileEntries)
{
    // Check that the file is reachable.
    const clang::DirectoryLookup *Dir;
    llvm::SmallVector<
        std::pair<const clang::FileEntry *, const clang::DirectoryEntry *>,
        0> Includers;

    for (const auto& SourceFile : SourceFiles)
    {
        auto FileEntry = c->getPreprocessor().getHeaderSearchInfo().LookupFile(SourceFile,
            clang::SourceLocation(), /*isAngled*/true,
            nullptr, Dir, Includers, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr);

        if (!FileEntry)
            return false;

        FileEntries.push_back(&FileEntry.getPointer()->getFileEntry());
    }

    // Create a virtual file that includes the header. This gets rid of some
    // Clang warnings about parsing an header file as the main file.

    std::string source;
    for (const auto& SourceFile : SourceFiles)
    {
        source += "#include \"" + SourceFile + "\"" + "\n";
    }
    source += "\0";

    auto buffer = llvm::MemoryBuffer::getMemBufferCopy(source);
    auto& SM = c->getSourceManager();
    SM.setMainFileID(SM.createFileID(std::move(buffer)));

    return true;
}

class SemaConsumer : public clang::SemaConsumer {
    CppSharp::CppParser::Parser& Parser;
    std::vector<const clang::FileEntry*>& FileEntries;
public:
    SemaConsumer(CppSharp::CppParser::Parser& parser,
        std::vector<const clang::FileEntry*>& entries)
        : Parser(parser), FileEntries(entries) {}
    virtual void HandleTranslationUnit(clang::ASTContext& Ctx) override;
};

void SemaConsumer::HandleTranslationUnit(clang::ASTContext& Ctx)
{
    auto FileEntry = FileEntries[0];
    auto FileName = FileEntry->getName();
    auto Unit = Parser.opts->ASTContext->FindOrCreateModule(FileName.str());

    auto TU = Ctx.getTranslationUnitDecl();
    Parser.HandleDeclaration(TU, Unit);

    if (Unit->originalPtr == nullptr)
        Unit->originalPtr = (void*)FileEntry;

    Parser.WalkAST(TU);
}

ParserResult* Parser::Parse(const std::vector<std::string>& SourceFiles)
{
    assert(opts->ASTContext && "Expected a valid ASTContext");

    auto res = new ParserResult();

    if (SourceFiles.empty())
    {
        res->kind = ParserResultKind::FileNotFound;
        return res;
    }

    Setup();
    SetupLLVMCodegen();

    std::vector<const clang::FileEntry*> FileEntries;
    if (!SetupSourceFiles(SourceFiles, FileEntries))
    {
        res->kind = ParserResultKind::FileNotFound;
        return res;
    }

    std::unique_ptr<SemaConsumer> SC(new SemaConsumer(*this, FileEntries));
    c->setASTConsumer(std::move(SC));

    c->createSema(clang::TU_Complete, 0);

    std::unique_ptr<::DiagnosticConsumer> DiagClient(new ::DiagnosticConsumer());
    c->getDiagnostics().setClient(DiagClient.get(), false);

    DiagClient->BeginSourceFile(c->getLangOpts(), &c->getPreprocessor());

    ParseAST(c->getSema());

    DiagClient->EndSourceFile();

    HandleDiagnostics(res);

    if(DiagClient->getNumErrors() != 0)
    {
        res->kind = ParserResultKind::Error;
        return res;
    }

    res->targetInfo = GetTargetInfo();

    res->kind = ParserResultKind::Success;
    return res;
 }

ParserResultKind Parser::ParseArchive(const std::string& File,
                                      llvm::object::Archive* Archive,
                                      std::vector<CppSharp::CppParser::NativeLibrary*>& NativeLibs)
{
    auto NativeLib = new NativeLibrary();
    NativeLib->fileName = File;

    for (const auto& Symbol : Archive->symbols())
    {
        llvm::StringRef SymRef = Symbol.getName();
        NativeLib->Symbols.push_back(SymRef.str());
    }
    NativeLibs.push_back(NativeLib);

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
static void ReadELFDependencies(const llvm::object::ELFFile<ELFT>& ELFFile, CppSharp::CppParser::NativeLibrary*& NativeLib)
{
    ELFDumper<ELFT> ELFDumper(&ELFFile);
    for (const auto& Dependency : ELFDumper.getNeededLibraries())
        NativeLib->Dependencies.push_back(Dependency.str());
}

ParserResultKind Parser::ParseSharedLib(const std::string& File,
                                        llvm::object::ObjectFile* ObjectFile,
                                        std::vector<CppSharp::CppParser::NativeLibrary*>& NativeLibs)
{
    auto NativeLib = new NativeLibrary();
    NativeLib->fileName = File;
    NativeLib->archType = ConvertArchType(ObjectFile->getArch());
    NativeLibs.push_back(NativeLib);

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
        for (const auto& ExportedSymbol : COFFObjectFile->export_directories())
        {
            llvm::StringRef Symbol;
            if (!ExportedSymbol.getSymbolName(Symbol))
                NativeLib->Symbols.push_back(Symbol.str());
        }
        for (const auto& ImportedSymbol : COFFObjectFile->import_directories())
        {
            llvm::StringRef Name;
            if (!ImportedSymbol.getName(Name) && (Name.endswith(".dll") || Name.endswith(".DLL")))
                NativeLib->Dependencies.push_back(Name.str());
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
                NativeLib->Dependencies.push_back(lib.str());
            }
        }
        // HACK: the correct way is with exported(Err) but it crashes with msvc 32
        // see https://bugs.llvm.org/show_bug.cgi?id=44433
        for (const auto& Symbol : MachOObjectFile->symbols())
        {
            if (Symbol.getName().takeError() || Symbol.getFlags().takeError())
                return ParserResultKind::Error;

            if ((Symbol.getFlags().get() & llvm::object::BasicSymbolRef::Flags::SF_Exported) &&
                !(Symbol.getFlags().get() & llvm::object::BasicSymbolRef::Flags::SF_Undefined))
                NativeLib->Symbols.push_back(Symbol.getName().get().str());
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
    NativeLib->fileName = LibName.str();

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

ParserResult* Parser::ParseLibrary(const CppLinkerOptions* Opts)
{
    auto res = new ParserResult();

    for (const auto& Lib : Opts->Libraries)
    {
        if (Lib.empty())
        {
            res->kind = ParserResultKind::FileNotFound;
            return res;
        }

        std::string PrefixedLib = "lib" + Lib;
        std::string FileName;
        std::string FileEntry;

        using namespace llvm::sys;
        for (const auto& LibDir : Opts->LibraryDirs)
        {
            std::error_code ErrorCode;
            fs::directory_iterator Dir(LibDir, ErrorCode);
            for (const auto& File = Dir;
                 Dir != fs::directory_iterator() && !ErrorCode;
                 Dir = Dir.increment(ErrorCode))
            {
                FileName = path::filename(File->path()).str();
                if (FileName == Lib ||
                    FileName == PrefixedLib ||
                    path::stem(FileName) == Lib ||
                    path::stem(FileName) == PrefixedLib ||
                    path::stem(path::stem(FileName)) == Lib ||
                    path::stem(path::stem(FileName)) == PrefixedLib)
                {
                    FileEntry = File->path();
                    goto found;
                }
            }
        }

        if (FileEntry.empty())
        {
            res->kind = ParserResultKind::FileNotFound;
            return res;
        }

    found:
        auto BinaryOrErr = llvm::object::createBinary(FileEntry);
        if (!BinaryOrErr)
        {
            auto Error = BinaryOrErr.takeError();
            res->kind = ParserResultKind::Error;
            return res;
        }

        auto OwningBinary = std::move(BinaryOrErr.get());
        auto Bin = OwningBinary.getBinary();
        if (auto Archive = llvm::dyn_cast<llvm::object::Archive>(Bin)) {
            res->kind = ParseArchive(FileName, Archive, res->Libraries);
            if (res->kind == ParserResultKind::Error)
                return res;
        }

        if (auto ObjectFile = llvm::dyn_cast<llvm::object::ObjectFile>(Bin))
        {
            res->kind = ParseSharedLib(FileName, ObjectFile, res->Libraries);
            if (res->kind == ParserResultKind::Error)
                return res;
        }
    }

    res->kind = ParserResultKind::Success;
    return res;
}

ParserResult* Parser::Build(const CppLinkerOptions* LinkerOptions, const std::string& File, bool Last)
{
    ParserResult* error = Compile(File);
    if (error)
        return error;

    bool LinkingError = !Link(File, LinkerOptions);

    if (Last)
        llvm::llvm_shutdown();

    auto res = new ParserResult();
    HandleDiagnostics(res);
    if (LinkingError)
    {
        ParserDiagnostic PD;
        PD.level = ParserDiagnosticLevel::Error;
        PD.lineNumber = PD.columnNumber = -1;
        res->addDiagnostics(PD);
    }
    return res;
}

ParserResult* ClangParser::ParseHeader(CppParserOptions* Opts)
{
    if (!Opts)
        return nullptr;

    auto& Headers = Opts->SourceFiles;
    if (Opts->unityBuild)
    {
        Parser parser(Opts);
        return parser.Parse(Headers);
    }

    ParserResult* res = 0;
    std::vector<Parser*> parsers;
    for (size_t i = 0; i < Headers.size(); i++)
    {
        auto parser = new Parser(Opts);
        parsers.push_back(parser);
        std::vector<std::string> Header{Headers[i]};
        if (i < Headers.size() - 1)
            delete parser->Parse(Header);
        else
            res = parser->Parse(Header);
    }

    for (auto parser : parsers)
        delete parser;

    return res;
}

ParserResult* ClangParser::ParseLibrary(CppLinkerOptions* Opts)
{
    if (!Opts)
        return nullptr;

    return Parser::ParseLibrary(Opts);
}

ParserResult* ClangParser::Build(CppParserOptions* Opts,
    const CppLinkerOptions* LinkerOptions, const std::string& File, bool Last)
{
    if (!Opts)
        return 0;

    Parser Parser(Opts);
    return Parser.Build(LinkerOptions, File, Last);
}

ParserResult* ClangParser::Compile(CppParserOptions* Opts,
    const std::string& File)
{
    if (!Opts)
        return 0;

    Parser Parser(Opts);
    return Parser.Compile(File);
}

ParserResult* ClangParser::Link(CppParserOptions* Opts,
    const CppLinkerOptions* LinkerOptions, const std::string& File, bool Last)
{
    if (!Opts)
        return 0;

    Parser Parser(Opts);
    bool LinkingError = !Parser.Link(File, LinkerOptions);

    if (Last)
        llvm::llvm_shutdown();

    auto res = new ParserResult();
    if (LinkingError)
    {
        ParserDiagnostic PD;
        PD.level = ParserDiagnosticLevel::Error;
        PD.lineNumber = PD.columnNumber = -1;
        res->addDiagnostics(PD);
    }
    return res;
}

ParserResult* Parser::Compile(const std::string& File)
{
    llvm::InitializeAllAsmPrinters();
    llvm::StringRef Stem = llvm::sys::path::stem(File);
    Setup(/* Compile */ true);

    c->getDiagnostics().setClient(new ::DiagnosticConsumer());

    c->getFrontendOpts().Inputs.clear();
    c->getFrontendOpts().Inputs.push_back(clang::FrontendInputFile(File, clang::Language::CXX));

    const llvm::Triple Triple = c->getTarget().getTriple();
    llvm::StringRef Dir(llvm::sys::path::parent_path(File));
    llvm::SmallString<1024> Object(Dir);
    llvm::sys::path::append(Object,
        (Triple.isOSWindows() ? "" : "lib") + Stem + ".o");
    c->getFrontendOpts().OutputFile = std::string(Object);

    llvm::LLVMContext context;
    auto action = std::make_unique<clang::EmitObjAction>(&context);
    if (!c->ExecuteAction(*action))
    {
        auto res = new ParserResult();
        HandleDiagnostics(res);
        return res;
    }
    return 0;
}

ParserTargetInfo* Parser::GetTargetInfo()
{
    auto parserTargetInfo = new ParserTargetInfo();

    auto& TI = c->getTarget();
    parserTargetInfo->ABI = TI.getABI().str();

    parserTargetInfo->char16Type = ConvertIntType(TI.getChar16Type());
    parserTargetInfo->char32Type = ConvertIntType(TI.getChar32Type());
    parserTargetInfo->int64Type = ConvertIntType(TI.getInt64Type());
    parserTargetInfo->intMaxType = ConvertIntType(TI.getIntMaxType());
    parserTargetInfo->intPtrType = ConvertIntType(TI.getIntPtrType());
    parserTargetInfo->sizeType = ConvertIntType(TI.getSizeType());
    parserTargetInfo->uIntMaxType = ConvertIntType(TI.getUIntMaxType());
    parserTargetInfo->wCharType = ConvertIntType(TI.getWCharType());
    parserTargetInfo->wIntType = ConvertIntType(TI.getWIntType());

    parserTargetInfo->boolAlign = TI.getBoolAlign();
    parserTargetInfo->boolWidth = TI.getBoolWidth();
    parserTargetInfo->charAlign = TI.getCharAlign();
    parserTargetInfo->charWidth = TI.getCharWidth();
    parserTargetInfo->char16Align = TI.getChar16Align();
    parserTargetInfo->char16Width = TI.getChar16Width();
    parserTargetInfo->char32Align = TI.getChar32Align();
    parserTargetInfo->char32Width = TI.getChar32Width();
    parserTargetInfo->halfAlign = TI.getHalfAlign();
    parserTargetInfo->halfWidth = TI.getHalfWidth();
    parserTargetInfo->floatAlign = TI.getFloatAlign();
    parserTargetInfo->floatWidth = TI.getFloatWidth();
    parserTargetInfo->doubleAlign = TI.getDoubleAlign();
    parserTargetInfo->doubleWidth = TI.getDoubleWidth();
    parserTargetInfo->shortAlign = TI.getShortAlign();
    parserTargetInfo->shortWidth = TI.getShortWidth();
    parserTargetInfo->intAlign = TI.getIntAlign();
    parserTargetInfo->intWidth = TI.getIntWidth();
    parserTargetInfo->intMaxTWidth = TI.getIntMaxTWidth();
    parserTargetInfo->longAlign = TI.getLongAlign();
    parserTargetInfo->longWidth = TI.getLongWidth();
    parserTargetInfo->longDoubleAlign = TI.getLongDoubleAlign();
    parserTargetInfo->longDoubleWidth = TI.getLongDoubleWidth();
    parserTargetInfo->longLongAlign = TI.getLongLongAlign();
    parserTargetInfo->longLongWidth = TI.getLongLongWidth();
    parserTargetInfo->pointerAlign = TI.getPointerAlign(0);
    parserTargetInfo->pointerWidth = TI.getPointerWidth(0);
    parserTargetInfo->wCharAlign = TI.getWCharAlign();
    parserTargetInfo->wCharWidth = TI.getWCharWidth();
    parserTargetInfo->float128Align = TI.getFloat128Align();
    parserTargetInfo->float128Width = TI.getFloat128Width();

    return parserTargetInfo;
}

Declaration* Parser::GetDeclarationFromFriend(clang::NamedDecl* FriendDecl)
{
    Declaration* Decl = WalkDeclarationDef(FriendDecl);
    if (!Decl) return nullptr;

    int MinLineNumberStart = std::numeric_limits<int>::max();
    int MinLineNumberEnd = std::numeric_limits<int>::max();
    auto& SM = c->getSourceManager();
    for (auto it = FriendDecl->redecls_begin(); it != FriendDecl->redecls_end(); it++)
    {
        if (it->getLocation() != FriendDecl->getLocation())
        {
            auto DecomposedLocStart = SM.getDecomposedLoc(it->getLocation());
            int NewLineNumberStart = SM.getLineNumber(DecomposedLocStart.first, DecomposedLocStart.second);
            auto DecomposedLocEnd = SM.getDecomposedLoc(it->getEndLoc());
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
        Decl->lineNumberStart = MinLineNumberStart;
        Decl->lineNumberEnd = MinLineNumberEnd;
    }
    return Decl;
}