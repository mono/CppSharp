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
#include <llvm/Option/ArgList.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/IR/Module.h>
#include <llvm/IR/DataLayout.h>
#include <clang/Basic/Version.h>
#include <clang/Config/config.h>
#include <clang/AST/ASTContext.h>
#include <clang/AST/Comment.h>
#include <clang/AST/DeclFriend.h>
#include <clang/AST/DeclTemplate.h>
#include <clang/AST/ExprCXX.h>
#include <clang/Lex/DirectoryLookup.h>
#include <clang/Lex/HeaderSearch.h>
#include <clang/Lex/Preprocessor.h>
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

Parser::Parser(ParserOptions* Opts) : Lib(Opts->ASTContext), Opts(Opts), Index(0)
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

// Defined in Targets.cpp
clang::TargetInfo * CreateTargetInfo(clang::DiagnosticsEngine &Diags,
    const std::shared_ptr<clang::TargetOptions> &Opts);

void Parser::SetupHeader()
{
    using namespace clang;

    std::vector<const char*> args;
    args.push_back("-cc1");

    switch (Opts->LanguageVersion)
    {
    case CppParser::LanguageVersion::C:
        args.push_back("-xc");
    case CppParser::LanguageVersion::CPlusPlus98:
    case CppParser::LanguageVersion::CPlusPlus11:
        args.push_back("-xc++");
    }

    switch (Opts->LanguageVersion)
    {
    case CppParser::LanguageVersion::C:
        args.push_back("-std=gnu99");
        break;
    case CppParser::LanguageVersion::CPlusPlus98:
        args.push_back("-std=gnu++98");
        break;
    default:
        args.push_back("-std=gnu++11");
        break;
    }
    args.push_back("-fno-rtti");

    for (unsigned I = 0, E = Opts->Arguments.size(); I != E; ++I)
    {
        const String& Arg = Opts->Arguments[I];
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

    TargetInfo* TI = CreateTargetInfo(C->getDiagnostics(), TO);
    if (!TI)
    {
        // We might have no target info due to an invalid user-provided triple.
        // Try again with the default triple.
        TO->Triple = llvm::sys::getDefaultTargetTriple();
        TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), TO);
    }

    assert(TI && "Expected valid target info");

    TI->setCXXABI(TargetABI);
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
        const String& s = Opts->IncludeDirs[I];
        HSOpts.AddPath(s, frontend::Angled, false, false);
    }

    for (unsigned I = 0, E = Opts->SystemIncludeDirs.size(); I != E; ++I)
    {
        String s = Opts->SystemIncludeDirs[I];
        HSOpts.AddPath(s, frontend::System, false, false);
    }

    for (unsigned I = 0, E = Opts->Defines.size(); I != E; ++I)
    {
        const String& define = Opts->Defines[I];
        PPOpts.addMacroDef(define);
    }

    for (unsigned I = 0, E = Opts->Undefines.size(); I != E; ++I)
    {
        const String& undefine = Opts->Undefines[I];
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

std::string Parser::GetDeclMangledName(clang::Decl* D)
{
    using namespace clang;

    if(!D || !isa<NamedDecl>(D))
        return "";

    bool CanMangle = isa<FunctionDecl>(D) || isa<VarDecl>(D)
        || isa<CXXConstructorDecl>(D) || isa<CXXDestructorDecl>(D);

    if (!CanMangle) return "";

    NamedDecl* ND = cast<NamedDecl>(D);
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

    if (TypedefNameDecl *Typedef = D->getTypedefNameForAnonDecl())
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
    auto &SM = C->getSourceManager();
    auto startLoc = SM.getExpansionLoc(D->getLocStart());
    auto startOffset = SM.getFileOffset(startLoc);

    if (clang::dyn_cast_or_null<clang::TranslationUnitDecl>(D))
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

static TypeQualifiers GetTypeQualifiers(clang::QualType Type)
{
    TypeQualifiers quals;
    quals.IsConst = Type.isLocalConstQualified();
    quals.IsRestrict = Type.isLocalRestrictQualified();
    quals.IsVolatile = Type.isVolatileQualified();
    return quals;
}

static QualifiedType
GetQualifiedType(clang::QualType qual, Type* type)
{
    QualifiedType qualType;
    qualType.Type = type;
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
        auto RD = const_cast<CXXRecordDecl*>(Component.getRTTIDecl());
        VTC.Declaration = WalkRecordCXX(RD);
        break;
    }
    case clang::VTableComponent::CK_FunctionPointer:
    {
        VTC.Kind = VTableComponentKind::FunctionPointer;
        auto MD = const_cast<CXXMethodDecl*>(Component.getFunctionDecl());
        VTC.Declaration = WalkMethodCXX(MD, /*AddToClass=*/false);
        break;
    }
    case clang::VTableComponent::CK_CompleteDtorPointer:
    {
        VTC.Kind = VTableComponentKind::CompleteDtorPointer;
        auto MD = const_cast<CXXDestructorDecl*>(Component.getDestructorDecl());
        VTC.Declaration = WalkMethodCXX(MD, /*AddToClass=*/false);
        break;
    }
    case clang::VTableComponent::CK_DeletingDtorPointer:
    {
        VTC.Kind = VTableComponentKind::DeletingDtorPointer;
        auto MD = const_cast<CXXDestructorDecl*>(Component.getDestructorDecl());
        VTC.Declaration = WalkMethodCXX(MD, /*AddToClass=*/false);
        break;
    }
    case clang::VTableComponent::CK_UnusedFunctionPointer:
    {
        VTC.Kind = VTableComponentKind::UnusedFunctionPointer;
        auto MD = const_cast<CXXMethodDecl*>(Component.getUnusedFunctionDecl());
        VTC.Declaration = WalkMethodCXX(MD, /*AddToClass=*/false);
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


void Parser::WalkVTable(clang::CXXRecordDecl* RD, Class* C)
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

Class* Parser::GetRecord(clang::RecordDecl* Record, bool& Process)
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

Class* Parser::WalkRecord(clang::RecordDecl* Record)
{
    bool Process;
    auto RC = GetRecord(Record, Process);

    if (!RC || !Process)
        return RC;

    WalkRecord(Record, RC);

    return RC;
}

Class* Parser::WalkRecordCXX(clang::CXXRecordDecl* Record)
{
    bool Process;
    auto RC = GetRecord(Record, Process);

    if (!RC || !Process)
        return RC;

    WalkRecordCXX(Record, RC);

    return RC;
}

static int I = 0;

void Parser::WalkRecord(clang::RecordDecl* Record, Class* RC)
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

    auto &Sema = C->getSema();

    RC->IsUnion = Record->isUnion();
    RC->IsDependent = Record->isDependentType();
    RC->IsExternCContext = Record->isExternCContext();

    bool hasLayout = !Record->isDependentType() && !Record->isInvalidDecl();

    // Get the record layout information.
    const ASTRecordLayout* Layout = 0;
    if (hasLayout)
    {
        Layout = &C->getASTContext().getASTRecordLayout(Record);
        if (!RC->Layout)
            RC->Layout = new ClassLayout();
        RC->Layout->Alignment = (int)Layout-> getAlignment().getQuantity();
        RC->Layout->Size = (int)Layout->getSize().getQuantity();
        RC->Layout->DataSize = (int)Layout->getDataSize().getQuantity();
    }

    AccessSpecifierDecl* AccessDecl = nullptr;

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
            auto Method = WalkMethodCXX(MD);
            Method->AccessDecl = AccessDecl;
            break;
        }
        case Decl::Field:
        {
            auto FD = cast<FieldDecl>(D);
            auto Field = WalkFieldCXX(FD, RC);

            if (Layout)
                Field->Offset = Layout->getFieldOffset(FD->getFieldIndex());

            break;
        }
        case Decl::AccessSpec:
        {
            AccessSpecDecl* AS = cast<AccessSpecDecl>(D);

            AccessDecl = new AccessSpecifierDecl();
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
            auto Decl = WalkDeclaration(D);
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

void Parser::WalkRecordCXX(clang::CXXRecordDecl* Record, Class* RC)
{
    using namespace clang;

    auto &Sema = C->getSema();
    Sema.ForceDeclarationOfImplicitMembers(Record);

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
    for(auto it = Record->bases_begin(); it != Record->bases_end(); ++it)
    {
        clang::CXXBaseSpecifier &BS = *it;

        BaseClassSpecifier* Base = new BaseClassSpecifier();
        Base->Access = ConvertToAccess(BS.getAccessSpecifier());
        Base->IsVirtual = BS.isVirtual();

        auto BSTL = BS.getTypeSourceInfo()->getTypeLoc();
        Base->Type = WalkType(BS.getType(), &BSTL);

        auto BaseDecl = GetCXXRecordDeclFromBaseType(BS.getType().getTypePtr());
        if (BaseDecl && Layout)
        {
            auto Offset = Layout->getBaseClassOffset(BaseDecl);
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
Parser::WalkClassTemplateSpecialization(clang::ClassTemplateSpecializationDecl* CTS)
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
    auto &TAL = CTS->getTemplateArgs();
    if (auto TSI = CTS->getTypeAsWritten())
    {
        auto TL = TSI->getTypeLoc();
        auto TSL = TL.getAs<TemplateSpecializationTypeLoc>();
        TS->Arguments = WalkTemplateArgumentList(&TAL, &TSL);
    }
    CT->Specializations.push_back(TS);

    if (CTS->isCompleteDefinition())
        WalkRecordCXX(CTS, TS);

    return TS;
}

//-----------------------------------//

ClassTemplatePartialSpecialization*
Parser::WalkClassTemplatePartialSpecialization(clang::ClassTemplatePartialSpecializationDecl* CTS)
{
    using namespace clang;

    auto CT = WalkClassTemplate(CTS->getSpecializedTemplate());
    auto USR = GetDeclUSR(CTS);
    auto TS = CT->FindPartialSpecialization(USR);
    if (TS != nullptr)
        return TS;

    TS = new ClassTemplatePartialSpecialization();
    HandleDeclaration(CTS, TS);

    TS->Name = CTS->getName();

    auto NS = GetNamespace(CTS);
    assert(NS && "Expected a valid namespace");
    TS->_Namespace = NS;

    TS->TemplatedDecl = CT;
    TS->SpecializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
    auto &TAL = CTS->getTemplateArgs();
    if (auto TSI = CTS->getTypeAsWritten())
    {
        auto TL = TSI->getTypeLoc();
        auto TSL = TL.getAs<TemplateSpecializationTypeLoc>();
        TS->Arguments = WalkTemplateArgumentList(&TAL, &TSL);
    }
    CT->Specializations.push_back(TS);

    if (CTS->isCompleteDefinition())
        WalkRecordCXX(CTS, TS);

    return TS;
}

//-----------------------------------//

CppSharp::CppParser::TemplateParameter
WalkTemplateParameter(const clang::NamedDecl* D)
{
    using namespace clang;

    auto TP = CppSharp::CppParser::TemplateParameter();
    if (D == nullptr)
        return TP;

    TP.Name = GetDeclName(D);
    switch (D->getKind())
    {
    case Decl::TemplateTypeParm:
    {
        auto TTPD = cast<TemplateTypeParmDecl>(D);
        TP.IsTypeParameter = true;
        break;
    }
    default:
        break;
    }
    return TP;
}

//-----------------------------------//

static std::vector<CppSharp::CppParser::TemplateParameter>
WalkTemplateParameterList(const clang::TemplateParameterList* TPL)
{
    auto params = std::vector<CppSharp::CppParser::TemplateParameter>();

    for (auto it = TPL->begin(); it != TPL->end(); ++it)
    {
        auto ND = *it;
        auto TP = WalkTemplateParameter(ND);
        params.push_back(TP);
    }

    return params;
}

//-----------------------------------//

ClassTemplate* Parser::WalkClassTemplate(clang::ClassTemplateDecl* TD)
{
    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(TD);
    auto CT = NS->FindClassTemplate(USR);
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
        if (TSTL && i < TSTL->getNumArgs())
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
        if (ArgLoc)
        {
            auto ArgTL = ArgLoc->getTypeSourceInfo()->getTypeLoc();
            Arg.Type = GetQualifiedType(TA.getAsType(), WalkType(TA.getAsType(), &ArgTL));
        }
        else
        {
            Arg.Type = GetQualifiedType(TA.getAsType(), WalkType(TA.getAsType()));
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

FunctionTemplate* Parser::WalkFunctionTemplate(clang::FunctionTemplateDecl* TD)
{
    using namespace clang;

    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(TD);
    auto FT = NS->FindFunctionTemplate(USR);
    if (FT != nullptr)
        return FT;

    CppSharp::CppParser::AST::Function* Function = nullptr;
    auto TemplatedDecl = TD->getTemplatedDecl();

    if (auto MD = dyn_cast<CXXMethodDecl>(TemplatedDecl))
        Function = WalkMethodCXX(MD, /*AddToClass=*/false);
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
    if (auto TALI = FTSI->TemplateArgumentsAsWritten)
        FTS->Arguments = WalkTemplateArgumentList(FTSI->TemplateArguments, TALI);
    FTS->Template = WalkFunctionTemplate(FTSI->getTemplate());
    FTS->Template->Specializations.push_back(FTS);

    return FTS;
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

Method* Parser::WalkMethodCXX(clang::CXXMethodDecl* MD, bool AddToClass)
{
    using namespace clang;

    // We could be in a redeclaration, so process the primary context.
    if (MD->getPrimaryContext() != MD)
        return WalkMethodCXX(cast<CXXMethodDecl>(MD->getPrimaryContext()), AddToClass);

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

    WalkFunction(MD, Method);

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
        auto ConvTy = WalkType(CD->getConversionType(), &RTL);
        Method->ConversionType = GetQualifiedType(CD->getConversionType(), ConvTy);
    }

    if (AddToClass)
        Class->Methods.push_back(Method);

    return Method;
}

//-----------------------------------//

Field* Parser::WalkFieldCXX(clang::FieldDecl* FD, Class* Class)
{
    using namespace clang;

    Field* F = new Field();
    HandleDeclaration(FD, F);

    F->_Namespace = Class;
    F->Name = FD->getName();
    auto TL = FD->getTypeSourceInfo()->getTypeLoc();
    F->QualifiedType = GetQualifiedType(FD->getType(), WalkType(FD->getType(), &TL));
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

DeclarationContext* Parser::GetNamespace(clang::Decl* D,
                                                   clang::DeclContext *Ctx)
{
    using namespace clang;

    // If the declaration is at global scope, just early exit.
    if (Ctx->isTranslationUnit())
        return GetTranslationUnit(D);

    TranslationUnit* Unit = GetTranslationUnit(cast<Decl>(Ctx));

    // Else we need to do a more expensive check to get all the namespaces,
    // and then perform a reverse iteration to get the namespaces in order.
    typedef SmallVector<DeclContext *, 8> ContextsTy;
    ContextsTy Contexts;

    for(; Ctx != nullptr; Ctx = Ctx->getParent())
        Contexts.push_back(Ctx);

    assert(Contexts.back()->isTranslationUnit());
    Contexts.pop_back();

    DeclarationContext* DC = Unit;

    for (auto I = Contexts.rbegin(), E = Contexts.rend(); I != E; ++I)
    {
        DeclContext* Ctx = *I;

        switch(Ctx->getDeclKind())
        {
        case Decl::Namespace:
        {
            NamespaceDecl* ND = cast<NamespaceDecl>(Ctx);
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

DeclarationContext* Parser::GetNamespace(clang::Decl *D)
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

    case clang::BuiltinType::Short: return PrimitiveType::Short;
    case clang::BuiltinType::UShort: return PrimitiveType::UShort;

    case clang::BuiltinType::Int: return PrimitiveType::Int;
    case clang::BuiltinType::UInt: return PrimitiveType::UInt;

    case clang::BuiltinType::Long: return PrimitiveType::Long;
    case clang::BuiltinType::ULong: return PrimitiveType::ULong;
    
    case clang::BuiltinType::LongLong: return PrimitiveType::LongLong;
    case clang::BuiltinType::ULongLong: return PrimitiveType::ULongLong;

    case clang::BuiltinType::Float: return PrimitiveType::Float;
    case clang::BuiltinType::Double: return PrimitiveType::Double;

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

Type* Parser::WalkType(clang::QualType QualType, clang::TypeLoc* TL,
    bool DesugarType)
{
    using namespace clang;

    if (QualType.isNull())
        return nullptr;

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
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        Ty = WalkType(Atomic->getValueType(), &Next);
        break;
    }
    case clang::Type::Attributed:
    {
        auto Attributed = Type->getAs<clang::AttributedType>();
        assert(Attributed && "Expected an attributed type");

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto AT = new AttributedType();

        auto Modified = Attributed->getModifiedType();
        AT->Modified = GetQualifiedType(Modified, WalkType(Modified, &Next));

        auto Equivalent = Attributed->getEquivalentType();
        AT->Equivalent = GetQualifiedType(Equivalent, WalkType(Equivalent, &Next));

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
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Pointee = Pointer->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        Ty = P;
        break;
    }
    case clang::Type::Typedef:
    {
        auto TT = Type->getAs<clang::TypedefType>();
        TypedefNameDecl* TD = TT->getDecl();

        auto TTL = TD->getTypeSourceInfo()->getTypeLoc();
        auto TDD = static_cast<TypedefDecl*>(WalkDeclaration(TD,
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
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Type = new DecayedType();
        Type->Decayed = GetQualifiedType(DT->getDecayedType(),
            WalkType(DT->getDecayedType(), &Next));
        Type->Original = GetQualifiedType(DT->getOriginalType(),
            WalkType(DT->getOriginalType(), &Next));
        Type->Pointee = GetQualifiedType(DT->getPointeeType(),
            WalkType(DT->getPointeeType(), &Next));

        Ty = Type;
        break;
    }
    case clang::Type::Elaborated:
    {
        auto ET = Type->getAs<clang::ElaboratedType>();

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

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
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        Ty = WalkType(PT->getInnerType(), &Next);
        break;
    }
    case clang::Type::ConstantArray:
    {
        auto AT = AST->getAsConstantArrayType(QualType);

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        auto ElemTy = AT->getElementType();
        A->QualifiedType = GetQualifiedType(ElemTy, WalkType(ElemTy, &Next));
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
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        A->QualifiedType = GetQualifiedType(AT->getElementType(),
            WalkType(AT->getElementType(), &Next));
        A->SizeType = ArrayType::ArraySize::Incomplete;

        Ty = A;
        break;
    }
    case clang::Type::DependentSizedArray:
    {
        auto AT = AST->getAsDependentSizedArrayType(QualType);

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto A = new ArrayType();
        A->QualifiedType = GetQualifiedType(AT->getElementType(),
            WalkType(AT->getElementType(), &Next));
        A->SizeType = ArrayType::ArraySize::Dependent;
        //A->Size = AT->getSizeExpr();

        Ty = A;
        break;
    }
    case clang::Type::FunctionProto:
    {
        auto FP = Type->getAs<clang::FunctionProtoType>();

        FunctionProtoTypeLoc FTL;
        TypeLoc RL;
        TypeLoc Next;
        if (TL && !TL->isNull())
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
        F->ReturnType = GetQualifiedType(FP->getReturnType(),
            WalkType(FP->getReturnType(), &RL));
        F->CallingConvention = ConvertCallConv(FP->getCallConv());

        for (unsigned i = 0; i < FP->getNumParams(); ++i)
        {
            auto FA = new Parameter();
            if (FTL)
            {
                auto PVD = FTL.getParam(i);
                HandleDeclaration(PVD, FA);

                auto PTL = PVD->getTypeSourceInfo()->getTypeLoc();

                FA->Name = PVD->getNameAsString();
                FA->QualifiedType = GetQualifiedType(PVD->getType(), WalkType(PVD->getType(), &PTL));
            }
            else
            {
                auto Arg = FP->getParamType(i);
                FA->Name = "";
                FA->QualifiedType = GetQualifiedType(Arg, WalkType(Arg));

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
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto MPT = new MemberPointerType();
        MPT->Pointee = GetQualifiedType(MP->getPointeeType(),
            WalkType(MP->getPointeeType(), &Next));
        
        Ty = MPT;
        break;
    }
    case clang::Type::TemplateSpecialization:
    {
        auto TS = Type->getAs<clang::TemplateSpecializationType>();
        auto TST = new TemplateSpecializationType();
        
        TemplateName Name = TS->getTemplateName();
        TST->Template = static_cast<Template*>(WalkDeclaration(
            Name.getAsTemplateDecl(), 0, /*IgnoreSystemDecls=*/false));
        if (TS->isSugared())
            TST->Desugared = WalkType(TS->desugar());

        TypeLoc UTL, ETL, ITL;

        if (TL && !TL->isNull())
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
        if (TL && !TL->isNull())
        {
            TSpecTL = TL->getAs<TemplateSpecializationTypeLoc>();
            TSTL = &TSpecTL;
        }

        TemplateArgumentList TArgs(TemplateArgumentList::OnStack, TS->getArgs(),
            TS->getNumArgs());
        TST->Arguments = WalkTemplateArgumentList(&TArgs, TSTL);

        Ty = TST;
        break;
    }
    case clang::Type::TemplateTypeParm:
    {
        auto TP = Type->getAs<TemplateTypeParmType>();

        auto TPT = new CppSharp::CppParser::TemplateParameterType();

        if (auto Ident = TP->getIdentifier())
            TPT->Parameter.Name = Ident->getName();

        TypeLoc UTL, ETL, ITL, Next;

        if (TL && !TL->isNull())
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

            TPT->Parameter = WalkTemplateParameter(TTTL.getDecl());
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
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto RepTy = TP->getReplacementType();
        TPT->Replacement = GetQualifiedType(RepTy, WalkType(RepTy, &Next));

        Ty = TPT;
        break;
    }
    case clang::Type::InjectedClassName:
    {
        auto ICN = Type->getAs<clang::InjectedClassNameType>();
        auto ICNT = new InjectedClassNameType();
        ICNT->Class = static_cast<Class*>(WalkDeclaration(
            ICN->getDecl(), 0, /*IgnoreSystemDecls=*/false));

        Ty = ICNT;
        break;
    }
    case clang::Type::DependentName:
    {
        auto DN = Type->getAs<clang::DependentNameType>();
        auto DNT = new DependentNameType();

        Ty = DNT;
        break;
    }
    case clang::Type::LValueReference:
    {
        auto LR = Type->getAs<clang::LValueReferenceType>();

        auto P = new PointerType();
        P->Modifier = PointerType::TypeModifier::LVReference;

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        Ty = P;
        break;
    }
    case clang::Type::RValueReference:
    {
        auto LR = Type->getAs<clang::RValueReferenceType>();

        auto P = new PointerType();
        P->Modifier = PointerType::TypeModifier::RVReference;

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        Ty = P;
        break;
    }
    case clang::Type::Vector:
    {
        // GCC-specific / __attribute__((vector_size(n))
        return nullptr;
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
        Ty = WalkType(DT->getUnderlyingType());
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

Enumeration* Parser::WalkEnum(clang::EnumDecl* ED)
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
        E->Items.push_back(*WalkEnumItem(*it));
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
    clang::CodeGen::CodeGenTypes* CodeGenTypes, clang::FunctionDecl* FD)
{
    using namespace clang;
    if (auto CD = dyn_cast<clang::CXXConstructorDecl>(FD)) {
        return CodeGenTypes->arrangeCXXStructorDeclaration(CD, clang::CodeGen::StructorType::Base);
    } else if (auto DD = dyn_cast<clang::CXXDestructorDecl>(FD)) {
        return CodeGenTypes->arrangeCXXStructorDeclaration(DD, clang::CodeGen::StructorType::Base);
    }

    return CodeGenTypes->arrangeFunctionDeclaration(FD);
}

static bool CanCheckCodeGenInfo(clang::Sema& S,
                                const clang::Type* Ty, bool IsMicrosoftABI)
{
    bool CheckCodeGenInfo = true;

    if (auto RT = Ty->getAs<clang::RecordType>())
        CheckCodeGenInfo &= RT->getDecl()->getDefinition() != 0;

    if (auto RD = Ty->getAsCXXRecordDecl())
        CheckCodeGenInfo &= RD->hasDefinition();

    // Lock in the MS inheritance model if we have a member pointer to a class,
    // else we get an assertion error inside Clang's codegen machinery.
    if (IsMicrosoftABI)
    {
        if (auto MPT = Ty->getAs<clang::MemberPointerType>())
            if (!MPT->isDependentType())
                S.RequireCompleteType(clang::SourceLocation(), clang::QualType(Ty, 0), 0);
    }

    return CheckCodeGenInfo;
}

void Parser::WalkFunction(clang::FunctionDecl* FD, Function* F,
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

    auto CC = FT->getCallConv();
    F->CallingConvention = ConvertCallConv(CC);

    F->OperatorKind = GetOperatorKindFromDecl(FD->getDeclName());

    TypeLoc RTL;
    if (auto TSI = FD->getTypeSourceInfo())
    {
        FunctionTypeLoc FTL = TSI->getTypeLoc().getAs<FunctionTypeLoc>();
        if (FTL)
        {
            RTL = FTL.getReturnLoc();

            auto &SM = C->getSourceManager();
            auto headStartLoc = GetDeclStartLocation(C.get(), FD);
            auto headEndLoc = SM.getExpansionLoc(FTL.getLParenLoc());
            auto headRange = clang::SourceRange(headStartLoc, headEndLoc);

            HandlePreprocessedEntities(F, headRange, MacroLocation::FunctionHead);
            HandlePreprocessedEntities(F, FTL.getParensRange(), MacroLocation::FunctionParameters);
            //auto bodyRange = clang::SourceRange(FTL.getRParenLoc(), FD->getLocEnd());
            //HandlePreprocessedEntities(F, bodyRange, MacroLocation::FunctionBody);
        }
    }

    F->ReturnType = GetQualifiedType(FD->getReturnType(),
        WalkType(FD->getReturnType(), &RTL));

    String Mangled = GetDeclMangledName(FD);
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

    for(auto it = FD->param_begin(); it != FD->param_end(); ++it)
    {
        ParmVarDecl* VD = (*it);
        
        auto P = new Parameter();
        P->Name = VD->getNameAsString();

        TypeLoc PTL;
        if (auto TSI = VD->getTypeSourceInfo())
            PTL = VD->getTypeSourceInfo()->getTypeLoc();

        auto paramRange = VD->getSourceRange();
        paramRange.setBegin(ParamStartLoc);

        HandlePreprocessedEntities(P, paramRange, MacroLocation::FunctionParameters);

        P->QualifiedType = GetQualifiedType(VD->getType(), WalkType(VD->getType(), &PTL));
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

    bool IsMicrosoftABI = C->getASTContext().getTargetInfo().getCXXABI().isMicrosoft();
    bool CheckCodeGenInfo = !FD->isDependentContext() && !FD->isInvalidDecl();
    CheckCodeGenInfo &= CanCheckCodeGenInfo(C->getSema(), FD->getReturnType().getTypePtr(),
        IsMicrosoftABI);
    for (auto I = FD->param_begin(), E = FD->param_end(); I != E; ++I)
        CheckCodeGenInfo &= CanCheckCodeGenInfo(C->getSema(), (*I)->getType().getTypePtr(),
        IsMicrosoftABI);

    if (CheckCodeGenInfo)
    {
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

    if (auto FTSI = FD->getTemplateSpecializationInfo())
        F->SpecializationInfo = WalkFunctionTemplateSpec(FTSI, F);
}

Function* Parser::WalkFunction(clang::FunctionDecl* FD, bool IsDependent,
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

    if(PLoc.isInvalid())
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

Variable* Parser::WalkVariable(clang::VarDecl *VD)
{
    using namespace clang;

    auto NS = GetNamespace(VD);
    assert(NS && "Expected a valid namespace");

    auto USR = GetDeclUSR(VD);
    if (auto Var = NS->FindVariable(USR))
       return Var;

    auto Var = new Variable();
    HandleDeclaration(VD, Var);

    Var->Name = VD->getName();
    Var->_Namespace = NS;
    Var->Access = ConvertToAccess(VD->getAccess());

    auto TL = VD->getTypeSourceInfo()->getTypeLoc();
    Var->QualifiedType = GetQualifiedType(VD->getType(), WalkType(VD->getType(), &TL));

    auto Mangled = GetDeclMangledName(VD);
    Var->Mangled = Mangled;

    NS->Variables.push_back(Var);

    return Var;
}

//-----------------------------------//

Friend* Parser::WalkFriend(clang::FriendDecl *FD)
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

    //auto TL = FD->getFriendType()->getTypeLoc();
    //F->QualifiedType = GetQualifiedType(VD->getType(), WalkType(FD->getFriendType(), &TL));

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
        auto MD = cast<clang::MacroExpansion>(PPEntity);
        Entity = new MacroExpansion();

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
    Entity->_Namespace = GetTranslationUnit(PPEntity->getSourceRange().getBegin());

    if (Decl->Kind == CppSharp::CppParser::AST::DeclarationKind::TranslationUnit)
    {
        Entity->_Namespace->PreprocessedEntities.push_back(Entity);
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
        return new AST::Expression(GetStringFromStatement(Expr), StatementClass::CXXOperatorCallExpr,
            WalkDeclaration(cast<CXXOperatorCallExpr>(Expr)->getCalleeDecl()));
    case Stmt::CXXConstructExprClass:
    case Stmt::CXXTemporaryObjectExprClass:
    {
        auto ConstructorExpr = cast<clang::CXXConstructExpr>(Expr);
        auto ConstructorExpression = new AST::CXXConstructExpr(GetStringFromStatement(Expr),
            WalkDeclaration(ConstructorExpr->getConstructor()));
        if (ConstructorExpr->getNumArgs() == 1)
        {
            auto Arg = ConstructorExpr->getArg(0);
            auto TemporaryExpr = dyn_cast<MaterializeTemporaryExpr>(Arg);
            if (TemporaryExpr)
            {
                auto SubTemporaryExpr = TemporaryExpr->GetTemporaryExpr();
                auto Cast = dyn_cast<CastExpr>(SubTemporaryExpr);
                if (!Cast || Cast->getSubExprAsWritten()->getStmtClass() != Stmt::IntegerLiteralClass)
                    return WalkExpression(SubTemporaryExpr);
            }
        }
        else
        {
            for (clang::Expr* arg : ConstructorExpr->arguments())
            {
                ConstructorExpression->Arguments.push_back(WalkExpression(arg));
            }
        }
        return ConstructorExpression;
    }
    case Stmt::MaterializeTemporaryExprClass:
        return WalkExpression(cast<MaterializeTemporaryExpr>(Expr)->GetTemporaryExpr());
    default:
        break;
    }
    llvm::APSInt integer;
    if (Expr->getStmtClass() != Stmt::CharacterLiteralClass &&
        Expr->getStmtClass() != Stmt::CXXBoolLiteralExprClass &&
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

void Parser::HandleOriginalText(clang::Decl* D, Declaration* Decl)
{
    auto &SM = C->getSourceManager();
    auto &LangOpts = C->getLangOpts();

    auto Range = clang::CharSourceRange::getTokenRange(D->getSourceRange());

    bool Invalid;
    auto DeclText = clang::Lexer::getSourceText(Range, SM, LangOpts, &Invalid);
    
    if (!Invalid)
        Decl->DebugText = DeclText;
}

void Parser::HandleDeclaration(clang::Decl* D, Declaration* Decl)
{
    if (Decl->OriginalPtr != nullptr)
        return;

    Decl->OriginalPtr = (void*) D;
    Decl->USR = GetDeclUSR(D);
    Decl->Location = SourceLocation(D->getLocation().getRawEncoding());
    auto& SM = C->getSourceManager();
    auto DecomposedLocStart = SM.getDecomposedLoc(D->getLocation());
    Decl->LineNumberStart = SM.getLineNumber(DecomposedLocStart.first, DecomposedLocStart.second);
    auto DecomposedLocEnd = SM.getDecomposedLoc(D->getLocEnd());
    Decl->LineNumberEnd = SM.getLineNumber(DecomposedLocEnd.first, DecomposedLocEnd.second);

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
    return WalkDeclaration(D, /*IgnoreSystemDecls=*/true,
        /*CanBeDefinition=*/true);
}

Declaration* Parser::WalkDeclaration(clang::Decl* D,
                                           bool IgnoreSystemDecls,
                                           bool CanBeDefinition)
{
    using namespace clang;


    // Ignore declarations that do not come from user-provided
    // header files.
    if (IgnoreSystemDecls && !IsValidDeclaration(D->getLocation()))
        return nullptr;

    for(auto it = D->attr_begin(); it != D->attr_end(); ++it)
    {
        Attr* Attr = (*it);

        if(Attr->getKind() != clang::attr::Annotate)
            continue;

        AnnotateAttr* Annotation = cast<AnnotateAttr>(Attr);
        assert(Annotation != nullptr);

        StringRef AnnotationText = Annotation->getAnnotation();
    }

    Declaration* Decl = nullptr;

    auto Kind = D->getKind();
    switch(D->getKind())
    {
    case Decl::Record:
    {
        RecordDecl* RD = cast<RecordDecl>(D);

        auto Record = WalkRecord(RD);

        // We store a definition order index into the declarations.
        // This is needed because declarations are added to their contexts as
        // soon as they are referenced and we need to know the original order
        // of the declarations.

        if (CanBeDefinition && Record->DefinitionOrder == 0)
        {
            Record->DefinitionOrder = Index++;
            //Debug("%d: %s\n", Index++, GetTagDeclName(RD).c_str());
        }

        Decl = Record;
        break;
    }
    case Decl::CXXRecord:
    {
        CXXRecordDecl* RD = cast<CXXRecordDecl>(D);

        auto Class = WalkRecordCXX(RD);

        // We store a definition order index into the declarations.
        // This is needed because declarations are added to their contexts as
        // soon as they are referenced and we need to know the original order
        // of the declarations.

        if (CanBeDefinition && Class->DefinitionOrder == 0)
        {
            Class->DefinitionOrder = Index++;
            //Debug("%d: %s\n", Index++, GetTagDeclName(RD).c_str());
        }

        Decl = Class;
        break;
    }
    case Decl::ClassTemplate:
    {
        ClassTemplateDecl* TD = cast<ClassTemplateDecl>(D);
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
        FunctionTemplateDecl* TD = cast<FunctionTemplateDecl>(D);
        auto FT = WalkFunctionTemplate(TD);

        Decl = FT;
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
        Decl = WalkEnumItem(ED);
        break;
    }
    case Decl::Function:
    {
        FunctionDecl* FD = cast<FunctionDecl>(D);
        if (!FD->isFirstDecl())
            break;

        // Check for and ignore built-in functions.
        if (FD->getBuiltinID() != 0)
            break;

        Decl = WalkFunction(FD);
        break;
    }
    case Decl::LinkageSpec:
    {
        LinkageSpecDecl* LS = cast<LinkageSpecDecl>(D);
        
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
        Typedef->QualifiedType = GetQualifiedType(TD->getUnderlyingType(),
            WalkType(TD->getUnderlyingType(), &TTL));

        Decl = Typedef;
        break;
    }
    case Decl::Namespace:
    {
        NamespaceDecl* ND = cast<NamespaceDecl>(D);

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
    // Ignore these declarations since they must have been declared in
    // a class already.
    case Decl::CXXDestructor:
    case Decl::CXXConversion:
    case Decl::CXXMethod:
        break;
    case Decl::Empty:
    case Decl::AccessSpec:
    case Decl::Using:
    case Decl::UsingDirective:
    case Decl::UsingShadow:
    case Decl::UnresolvedUsingTypename:
    case Decl::UnresolvedUsingValue:
    case Decl::IndirectField:
    case Decl::StaticAssert:
    case Decl::TypeAliasTemplate:
        break;
    default:
    {
        Debug("Unhandled declaration kind: %s\n", D->getDeclKindName());

        auto &SM = C->getSourceManager();
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
    auto &Diags = DiagClient.Diagnostics;

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

ParserResult* Parser::ParseHeader(const std::string& File, ParserResult* res)
{
    assert(Opts->ASTContext && "Expected a valid ASTContext");

    res->ASTContext = Lib;

    if (File.empty())
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

    auto FileEntry = C->getPreprocessor().getHeaderSearchInfo().LookupFile(File,
        clang::SourceLocation(), /*isAngled*/true,
        nullptr, Dir, Includers, nullptr, nullptr, nullptr);
    if (!FileEntry)
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    // Create a virtual file that includes the header. This gets rid of some
    // Clang warnings about parsing an header file as the main file.

    std::string str;
    str += "#include \"" + File + "\"" + "\n";
    str += "\0";

    auto buffer = llvm::MemoryBuffer::getMemBuffer(str);
    auto &SM = C->getSourceManager();
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

    auto FileName = FileEntry->getName();
    auto Unit = Lib->FindOrCreateModule(FileName);

    auto TU = AST->getTranslationUnitDecl();
    HandleDeclaration(TU, Unit);

    if (Unit->OriginalPtr == nullptr)
        Unit->OriginalPtr = (void*) FileEntry;

    // Initialize enough Clang codegen machinery so we can get at ABI details.
    llvm::LLVMContext Ctx;
    std::unique_ptr<llvm::Module> M(new llvm::Module("", Ctx));

    M->setTargetTriple(AST->getTargetInfo().getTriple().getTriple());
    M->setDataLayout(AST->getTargetInfo().getDataLayoutString());

    std::unique_ptr<clang::CodeGen::CodeGenModule> CGM(
        new clang::CodeGen::CodeGenModule(C->getASTContext(), C->getHeaderSearchOpts(),
        C->getPreprocessorOpts(), C->getCodeGenOpts(), *M, C->getDiagnostics()));

    std::unique_ptr<clang::CodeGen::CodeGenTypes> CGT(
        new clang::CodeGen::CodeGenTypes(*CGM.get()));

    CodeGenInfo = (clang::TargetCodeGenInfo*) &CGM->getTargetCodeGenInfo();
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
    for (const auto &Dependency : ELFDumper.getNeededLibraries())
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
    }
    else
    {
        if (auto COFFObjectFile = llvm::dyn_cast<llvm::object::COFFObjectFile>(ObjectFile))
        {
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
        }
        else
        {
            return ParserResultKind::Error;
        }
    }

    return ParserResultKind::Success;
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

    C.reset(new clang::CompilerInstance());
    C->createFileManager();

    auto &FM = C->getFileManager();
    llvm::StringRef FileEntry;

    for (unsigned I = 0, E = Opts->LibraryDirs.size(); I != E; ++I)
    {
        auto& LibDir = Opts->LibraryDirs[I];
        llvm::SmallString<256> Path(LibDir);
        llvm::sys::path::append(Path, File);

        if (!(FileEntry = Path.str()).empty())
            break;
    }

    if (FileEntry.empty())
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    auto Buffer = FM.getBufferForFile(FileEntry);
    if (Buffer.getError())
    {
        res->Kind = ParserResultKind::Error;
        return res;
    }
    std::unique_ptr<llvm::MemoryBuffer> FileBuf(std::move(Buffer.get()));
    auto BinaryOrErr = llvm::object::createBinary(FileBuf->getMemBufferRef(),
        &llvm::getGlobalContext());
    if (BinaryOrErr.getError())
    {
        res->Kind = ParserResultKind::Error;
        return res;
    }
    std::unique_ptr<llvm::object::Binary> Bin(std::move(BinaryOrErr.get()));
    if (auto Archive = llvm::dyn_cast<llvm::object::Archive>(Bin.get())) {
        res->Kind = ParseArchive(File, Archive, res->Library);
        if (res->Kind == ParserResultKind::Success)
            return res;
    }
    if (auto ObjectFile = llvm::dyn_cast<llvm::object::ObjectFile>(Bin.get()))
    {
        res->Kind = ParseSharedLib(File, ObjectFile, res->Library);
        if (res->Kind == ParserResultKind::Success)
            return res;
    }
    res->Kind = ParserResultKind::Error;
    return res;
}

ParserResult* ClangParser::ParseHeader(ParserOptions* Opts)
{
    if (!Opts)
        return nullptr;

    auto res = new ParserResult();
    res->CodeParser = new Parser(Opts);
    return res->CodeParser->ParseHeader(Opts->FileName, res);
}

ParserResult* ClangParser::ParseLibrary(ParserOptions* Opts)
{
    if (!Opts)
        return nullptr;

    auto res = new ParserResult();
    res->CodeParser = new Parser(Opts);
    return res->CodeParser->ParseLibrary(Opts->FileName, res);
}

ParserTargetInfo* ClangParser::GetTargetInfo(ParserOptions* Opts)
{
    if (!Opts)
        return nullptr;

    Parser parser(Opts);
    return parser.GetTargetInfo();
}

ParserTargetInfo* Parser::GetTargetInfo()
{
    assert(Opts->ASTContext && "Expected a valid ASTContext");

    auto res = new ParserResult();
    res->ASTContext = Lib;

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
    M->setDataLayout(AST->getTargetInfo().getDataLayoutString());

    std::unique_ptr<clang::CodeGen::CodeGenModule> CGM(
        new clang::CodeGen::CodeGenModule(C->getASTContext(), C->getHeaderSearchOpts(),
        C->getPreprocessorOpts(), C->getCodeGenOpts(), *M, C->getDiagnostics()));

    std::unique_ptr<clang::CodeGen::CodeGenTypes> CGT(
        new clang::CodeGen::CodeGenTypes(*CGM.get()));

    CodeGenInfo = (clang::TargetCodeGenInfo*) &CGM->getTargetCodeGenInfo();
    CodeGenTypes = CGT.get();

    auto parserTargetInfo = new ParserTargetInfo();

    parserTargetInfo->ABI = AST->getTargetInfo().getABI();

    parserTargetInfo->Char16Type = ConvertIntType(AST->getTargetInfo().getChar16Type());
    parserTargetInfo->Char32Type = ConvertIntType(AST->getTargetInfo().getChar32Type());
    parserTargetInfo->Int64Type = ConvertIntType(AST->getTargetInfo().getInt64Type());
    parserTargetInfo->IntMaxType = ConvertIntType(AST->getTargetInfo().getIntMaxType());
    parserTargetInfo->IntPtrType = ConvertIntType(AST->getTargetInfo().getIntPtrType());
    parserTargetInfo->SizeType = ConvertIntType(AST->getTargetInfo().getSizeType());
    parserTargetInfo->UIntMaxType = ConvertIntType(AST->getTargetInfo().getUIntMaxType());
    parserTargetInfo->WCharType = ConvertIntType(AST->getTargetInfo().getWCharType());
    parserTargetInfo->WIntType = ConvertIntType(AST->getTargetInfo().getWIntType());

    parserTargetInfo->BoolAlign = AST->getTargetInfo().getBoolAlign();
    parserTargetInfo->BoolWidth = AST->getTargetInfo().getBoolWidth();
    parserTargetInfo->CharAlign = AST->getTargetInfo().getCharAlign();
    parserTargetInfo->CharWidth = AST->getTargetInfo().getCharWidth();
    parserTargetInfo->Char16Align = AST->getTargetInfo().getChar16Align();
    parserTargetInfo->Char16Width = AST->getTargetInfo().getChar16Width();
    parserTargetInfo->Char32Align = AST->getTargetInfo().getChar32Align();
    parserTargetInfo->Char32Width = AST->getTargetInfo().getChar32Width();
    parserTargetInfo->HalfAlign = AST->getTargetInfo().getHalfAlign();
    parserTargetInfo->HalfWidth = AST->getTargetInfo().getHalfWidth();
    parserTargetInfo->FloatAlign = AST->getTargetInfo().getFloatAlign();
    parserTargetInfo->FloatWidth = AST->getTargetInfo().getFloatWidth();
    parserTargetInfo->DoubleAlign = AST->getTargetInfo().getDoubleAlign();
    parserTargetInfo->DoubleWidth = AST->getTargetInfo().getDoubleWidth();
    parserTargetInfo->ShortAlign = AST->getTargetInfo().getShortAlign();
    parserTargetInfo->ShortWidth = AST->getTargetInfo().getShortWidth();
    parserTargetInfo->IntAlign = AST->getTargetInfo().getIntAlign();
    parserTargetInfo->IntWidth = AST->getTargetInfo().getIntWidth();
    parserTargetInfo->IntMaxTWidth = AST->getTargetInfo().getIntMaxTWidth();
    parserTargetInfo->LongAlign = AST->getTargetInfo().getLongAlign();
    parserTargetInfo->LongWidth = AST->getTargetInfo().getLongWidth();
    parserTargetInfo->LongDoubleAlign = AST->getTargetInfo().getLongDoubleAlign();
    parserTargetInfo->LongDoubleWidth = AST->getTargetInfo().getLongDoubleWidth();
    parserTargetInfo->LongLongAlign = AST->getTargetInfo().getLongLongAlign();
    parserTargetInfo->LongLongWidth = AST->getTargetInfo().getLongLongWidth();
    parserTargetInfo->PointerAlign = AST->getTargetInfo().getPointerAlign(0);
    parserTargetInfo->PointerWidth = AST->getTargetInfo().getPointerWidth(0);
    parserTargetInfo->WCharAlign = AST->getTargetInfo().getWCharAlign();
    parserTargetInfo->WCharWidth = AST->getTargetInfo().getWCharWidth();

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