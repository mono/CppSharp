/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "Parser.h"
#include "Interop.h"

#include <llvm/Support/Path.h>
#include <llvm/Object/Archive.h>
#include <llvm/Object/ObjectFile.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/IR/Module.h>
#include <llvm/IR/DataLayout.h>
#include <clang/Basic/Version.h>
#include <clang/Config/config.h>
#include <clang/AST/ASTContext.h>
#include <clang/AST/Comment.h>
#include <clang/AST/DeclTemplate.h>
#include <clang/Lex/DirectoryLookup.h>
#include <clang/Lex/HeaderSearch.h>
#include <clang/Lex/PreprocessingRecord.h>
#include <clang/Sema/Sema.h>
#include <clang/Sema/SemaConsumer.h>
#include <clang/Frontend/Utils.h>
#include <clang/Driver/Util.h>
#include <CodeGen/CodeGenModule.h>
#include <CodeGen/CodeGenTypes.h>
#include <CodeGen/TargetInfo.h>
#include <CodeGen/CGCall.h>

//-----------------------------------//

Parser::Parser(ParserOptions^ Opts)
    : Opts(Opts)
    , Lib(Opts->ASTContext)
    , Index(0)
{
}

//-----------------------------------//

static std::string GetClangResourceDir(const std::string& Dir)
{
    using namespace llvm;
    using namespace clang;

    // Compute the path to the resource directory.
    StringRef ClangResourceDir(CLANG_RESOURCE_DIR);
    
    SmallString<128> P(Dir);
    
    if (ClangResourceDir != "")
        llvm::sys::path::append(P, ClangResourceDir);
    else
        llvm::sys::path::append(P, "lib", "clang", CLANG_VERSION_STRING);
    
    return P.str();
}

static std::string GetClangBuiltinIncludeDir()
{
    using namespace llvm;
    
    SmallString<128> P( GetClangResourceDir(".") );
    llvm::sys::path::append(P, "include");
    
    return P.str();
}

//-----------------------------------//

static std::string GetCXXABIString(clang::TargetCXXABI::Kind Kind)
{
    switch(Kind)
    {
    case clang::TargetCXXABI::GenericItanium:
        return "itanium";
    case clang::TargetCXXABI::Microsoft:
        return "microsoft";
    }

    llvm_unreachable("Unknown C++ ABI kind");
}

#ifdef _MSC_VER
std::vector<std::string> GetWindowsSystemIncludeDirs();
#endif

void Parser::SetupHeader()
{
    using namespace clang;
    using namespace clix;

    std::vector<const char*> args;
    args.push_back("-cc1");

    // Enable C++ language mode
    args.push_back("-xc++");
    args.push_back("-std=gnu++11");
    //args.push_back("-Wno-undefined-inline");
    args.push_back("-fno-rtti");

    // Enable the Microsoft parsing extensions
    if (Opts->MicrosoftMode)
    {
        args.push_back("-fms-extensions");
        args.push_back("-fms-compatibility");
        args.push_back("-fdelayed-template-parsing");
    }

    C.reset(new CompilerInstance());
    C->createDiagnostics();

    CompilerInvocation* Inv = new CompilerInvocation();
    CompilerInvocation::CreateFromArgs(*Inv, args.data(), args.data() + args.size(),
      C->getDiagnostics());
    C->setInvocation(Inv);

    TargetOptions& TO = Inv->getTargetOpts();
    TargetABI = (Opts->Abi == CppAbi::Microsoft) ? TargetCXXABI::Microsoft
        : TargetCXXABI::GenericItanium;
    TO.CXXABI = GetCXXABIString(TargetABI);

    TO.Triple = llvm::sys::getDefaultTargetTriple();
    if (!System::String::IsNullOrWhiteSpace(Opts->TargetTriple))
        TO.Triple = marshalString<E_UTF8>(Opts->TargetTriple);

    TargetInfo* TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), &TO);
    if (!TI)
    {
        // We might have no target info due to an invalid user-provided triple.
        // Try again with the default triple.
        TO.Triple = llvm::sys::getDefaultTargetTriple();
        TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), &TO);
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

    for each(System::String^% include in Opts->IncludeDirs)
    {
        String s = marshalString<E_UTF8>(include);
        HSOpts.AddPath(s, frontend::Angled, false, false);
    }

    for each(System::String^% include in Opts->SystemIncludeDirs)
    {
        String s = marshalString<E_UTF8>(include);
        HSOpts.AddPath(s, frontend::System, false, false);
    }

    for each(System::String^% define in Opts->Defines)
        PPOpts.addMacroDef(marshalString<E_UTF8>(define));

    // Initialize the default platform headers.
    HSOpts.ResourceDir = GetClangResourceDir(".");
    HSOpts.AddPath(GetClangBuiltinIncludeDir(), clang::frontend::System,
        /*IsFramework=*/false, /*IgnoreSysRoot=*/false);

#ifdef _MSC_VER
    if (!Opts->NoBuiltinIncludes)
    {
        std::vector<std::string> SystemDirs = GetWindowsSystemIncludeDirs();
        for(unsigned i = 0; i < SystemDirs.size(); i++)
            HSOpts.AddPath(SystemDirs[i], frontend::System, /*IsFramework=*/false,
                /*IgnoreSysRoot=*/false);
    }

    if (Opts->MicrosoftMode)
    {
        LangOpts.MSCVersion = Opts->ToolSetToUse;
        if (!LangOpts.MSCVersion) LangOpts.MSCVersion = 1700;
    }
#endif

    // Enable preprocessing record.
    PPOpts.DetailedRecord = true;

    C->createPreprocessor();

    Preprocessor& PP = C->getPreprocessor();
    PP.getBuiltinInfo().InitializeBuiltins(PP.getIdentifierTable(),
        PP.getLangOpts());

    C->createASTContext();
}

//-----------------------------------//

std::string Parser::GetDeclMangledName(clang::Decl* D, clang::TargetCXXABI ABI,
                                       bool IsDependent)
{
    using namespace clang;

    if(!D || !isa<NamedDecl>(D))
        return "";

    bool CanMangle = isa<FunctionDecl>(D) || isa<VarDecl>(D)
        || isa<CXXConstructorDecl>(D) || isa<CXXDestructorDecl>(D);

    if (!CanMangle) return "";

    NamedDecl* ND = cast<NamedDecl>(D);
    llvm::OwningPtr<MangleContext> MC;
    
    switch(ABI.getKind())
    {
    default:
        llvm_unreachable("Unknown mangling ABI");
        break;
    case TargetCXXABI::GenericItanium:
       MC.reset(ItaniumMangleContext::create(*AST, AST->getDiagnostics()));
       break;
    case TargetCXXABI::Microsoft:
       MC.reset(MicrosoftMangleContext::create(*AST, AST->getDiagnostics()));
       break;
    }

    std::string Mangled;
    llvm::raw_string_ostream Out(Mangled);

    if (const ValueDecl *VD = dyn_cast<ValueDecl>(ND))
        IsDependent = VD->getType()->isDependentType();

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

static CppSharp::AST::TypeQualifiers GetTypeQualifiers(clang::QualType Type)
{
    CppSharp::AST::TypeQualifiers quals;
    quals.IsConst = Type.isLocalConstQualified();
    quals.IsRestrict = Type.isLocalRestrictQualified();
    quals.IsVolatile = Type.isVolatileQualified();
    return quals;
}

static CppSharp::AST::QualifiedType
GetQualifiedType(clang::QualType qual, CppSharp::AST::Type^ type)
{
    CppSharp::AST::QualifiedType qualType;
    qualType.Type = type;
    qualType.Qualifiers = GetTypeQualifiers(qual);
    return qualType;
}

//-----------------------------------//

static CppSharp::AST::AccessSpecifier ConvertToAccess(clang::AccessSpecifier AS)
{
    switch(AS)
    {
    case clang::AS_private:
        return CppSharp::AST::AccessSpecifier::Private;
    case clang::AS_protected:
        return CppSharp::AST::AccessSpecifier::Protected;
    case clang::AS_public:
        return CppSharp::AST::AccessSpecifier::Public;
    }

    return CppSharp::AST::AccessSpecifier::Public;
}

CppSharp::AST::VTableComponent
Parser::WalkVTableComponent(const clang::VTableComponent& Component)
{
    using namespace clang;
    CppSharp::AST::VTableComponent VTC;

    switch(Component.getKind())
    {
    case VTableComponent::CK_VCallOffset:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::VBaseOffset;
        VTC.Offset = Component.getVCallOffset().getQuantity();
        break;
    }
    case VTableComponent::CK_VBaseOffset:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::VBaseOffset;
        VTC.Offset = Component.getVBaseOffset().getQuantity();
        break;
    }
    case VTableComponent::CK_OffsetToTop:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::OffsetToTop;
        VTC.Offset = Component.getOffsetToTop().getQuantity();
        break;
    }
    case VTableComponent::CK_RTTI:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::RTTI;
        auto RD = const_cast<CXXRecordDecl*>(Component.getRTTIDecl());
        VTC.Declaration = WalkRecordCXX(RD);
        break;
    }
    case VTableComponent::CK_FunctionPointer:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::FunctionPointer;
        auto MD = const_cast<CXXMethodDecl*>(Component.getFunctionDecl());
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    case VTableComponent::CK_CompleteDtorPointer:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::CompleteDtorPointer;
        auto MD = const_cast<CXXDestructorDecl*>(Component.getDestructorDecl());
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    case VTableComponent::CK_DeletingDtorPointer:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::DeletingDtorPointer;
        auto MD = const_cast<CXXDestructorDecl*>(Component.getDestructorDecl());
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    case VTableComponent::CK_UnusedFunctionPointer:
    {
        VTC.Kind = CppSharp::AST::VTableComponentKind::UnusedFunctionPointer;
        auto MD = const_cast<CXXMethodDecl*>(Component.getUnusedFunctionDecl());
        VTC.Declaration = WalkMethodCXX(MD);
        break;
    }
    default:
        llvm_unreachable("Unknown vtable component kind");
    }

    return VTC;
}

CppSharp::AST::VTableLayout^
Parser::WalkVTableLayout(const clang::VTableLayout& VTLayout)
{
    auto Layout = gcnew CppSharp::AST::VTableLayout();

    for (auto I = VTLayout.vtable_component_begin(),
              E = VTLayout.vtable_component_end(); I != E; ++I)
    {
        auto VTComponent = WalkVTableComponent(*I);
        Layout->Components->Add(VTComponent);
    }

    return Layout;
}

void Parser::WalkVTable(clang::CXXRecordDecl* RD, CppSharp::AST::Class^ C)
{
    using namespace clang;
    using namespace clix;

    assert(RD->isDynamicClass() && "Only dynamic classes have virtual tables");

    switch(TargetABI)
    {
    case TargetCXXABI::Microsoft:
    {
        C->Layout->ABI = CppSharp::AST::CppAbi::Microsoft;
        MicrosoftVTableContext VTContext(*AST);

        auto VFPtrs = VTContext.getVFPtrOffsets(RD);
        for (auto I = VFPtrs.begin(), E = VFPtrs.end(); I != E; ++I)
        {
            auto& VFPtrInfo = *I;

            CppSharp::AST::VFTableInfo Info;
            Info.VBTableIndex = VFPtrInfo.VBTableIndex;
            Info.VFPtrOffset = VFPtrInfo.VFPtrOffset.getQuantity();
            Info.VFPtrFullOffset = VFPtrInfo.VFPtrFullOffset.getQuantity();

            auto& VTLayout = VTContext.getVFTableLayout(RD, VFPtrInfo.VFPtrFullOffset);
            Info.Layout = WalkVTableLayout(VTLayout);

            C->Layout->VFTables->Add(Info);
        }
        break;
    }
    case TargetCXXABI::GenericItanium:
    {
        C->Layout->ABI = CppSharp::AST::CppAbi::Itanium;
        ItaniumVTableContext VTContext(*AST);

        auto& VTLayout = VTContext.getVTableLayout(RD);
        C->Layout->Layout = WalkVTableLayout(VTLayout);
        break;
    }
    default:
        llvm_unreachable("Unsupported C++ ABI kind");
    }
}

void Parser::WalkRecordCXX(clang::CXXRecordDecl* Record,
                          CppSharp::AST::Class^ RC)
{
    using namespace clang;
    using namespace clix;

    auto headStartLoc = GetDeclStartLocation(C.get(), Record);
    auto headEndLoc = Record->getLocation(); // identifier location
    auto bodyEndLoc = Record->getLocEnd();

    auto headRange = clang::SourceRange(headStartLoc, headEndLoc);
    auto bodyRange = clang::SourceRange(headEndLoc, bodyEndLoc);

    HandlePreprocessedEntities(RC, headRange, CppSharp::AST::MacroLocation::ClassHead);
    HandlePreprocessedEntities(RC, bodyRange, CppSharp::AST::MacroLocation::ClassBody);

    auto &Sema = C->getSema();
    Sema.ForceDeclarationOfImplicitMembers(Record);

    RC->IsPOD = Record->isPOD();
    RC->IsUnion = Record->isUnion();
    RC->IsAbstract = Record->isAbstract();
    RC->IsDependent = Record->isDependentType();
    RC->IsDynamic = Record->isDynamicClass();
    RC->IsPolymorphic = Record->isPolymorphic();
    RC->HasNonTrivialDefaultConstructor = Record->hasNonTrivialDefaultConstructor();
    RC->HasNonTrivialCopyConstructor = Record->hasNonTrivialCopyConstructor();

    bool hasLayout = !Record->isDependentType() && !Record->isInvalidDecl();

    // Get the record layout information.
    const ASTRecordLayout* Layout = 0;
    if (hasLayout)
    {
        Layout = &C->getASTContext().getASTRecordLayout(Record);
        RC->Layout->Alignment = (int)Layout-> getAlignment().getQuantity();
        RC->Layout->Size = (int)Layout->getSize().getQuantity();
        RC->Layout->DataSize = (int)Layout->getDataSize().getQuantity();
        RC->Layout->HasOwnVFPtr = Layout->hasOwnVFPtr();
        RC->Layout->VBPtrOffset = Layout->getVBPtrOffset().getQuantity();
    }

    CppSharp::AST::AccessSpecifierDecl^ AccessDecl = nullptr;

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

            AccessDecl = gcnew CppSharp::AST::AccessSpecifierDecl();
            HandleDeclaration(AS, AccessDecl);

            AccessDecl->Access = ConvertToAccess(AS->getAccess());
            AccessDecl->Namespace = RC;

            RC->Specifiers->Add(AccessDecl);
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

    // Iterate through the record bases.
    for(auto it = Record->bases_begin(); it != Record->bases_end(); ++it)
    {
        clang::CXXBaseSpecifier &BS = *it;

        CppSharp::AST::BaseClassSpecifier^ Base = gcnew CppSharp::AST::BaseClassSpecifier();
        Base->Access = ConvertToAccess(BS.getAccessSpecifier());
        Base->IsVirtual = BS.isVirtual();
        Base->Type = WalkType(BS.getType(), &BS.getTypeSourceInfo()->getTypeLoc());

        RC->Bases->Add(Base);
    }

    // Process the vtables
    if (hasLayout && Record->isDynamicClass())
        WalkVTable(Record, RC);
}

CppSharp::AST::Class^ Parser::WalkRecordCXX(clang::CXXRecordDecl* Record)
{
    using namespace clang;
    using namespace clix;

    if (Record->isInjectedClassName())
        return nullptr;

    auto NS = GetNamespace(Record);
    assert(NS && "Expected a valid namespace");

    bool isCompleteDefinition = Record->isCompleteDefinition();

    CppSharp::AST::Class^ RC = nullptr;

    auto Name = marshalString<E_UTF8>(GetTagDeclName(Record));
    auto HasEmptyName = Record->getDeclName().isEmpty();

    if (HasEmptyName)
    {
        if (auto AR = NS->FindAnonymous((uint64_t)Record))
            RC = safe_cast<CppSharp::AST::Class^>(AR);
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
        NS->Anonymous[(uint64_t)Record] = RC;

    if (!isCompleteDefinition)
        return RC;

    WalkRecordCXX(Record, RC);

    return RC;
}

//-----------------------------------//

static CppSharp::AST::TemplateSpecializationKind
WalkTemplateSpecializationKind(clang::TemplateSpecializationKind Kind)
{
    using namespace clang;
    switch(Kind)
    {
    case TSK_Undeclared:
        return CppSharp::AST::TemplateSpecializationKind::Undeclared;
    case TSK_ImplicitInstantiation:
        return CppSharp::AST::TemplateSpecializationKind::ImplicitInstantiation;
    case TSK_ExplicitSpecialization:
        return CppSharp::AST::TemplateSpecializationKind::ExplicitSpecialization;
    case TSK_ExplicitInstantiationDeclaration:
        return CppSharp::AST::TemplateSpecializationKind::ExplicitInstantiationDeclaration;
    case TSK_ExplicitInstantiationDefinition:
        return CppSharp::AST::TemplateSpecializationKind::ExplicitInstantiationDefinition;
    }
}

CppSharp::AST::ClassTemplateSpecialization^
Parser::WalkClassTemplateSpecialization(clang::ClassTemplateSpecializationDecl* CTS)
{
    auto CT = WalkClassTemplate(CTS->getSpecializedTemplate());
    auto Spec = CT->FindSpecialization(System::IntPtr(CTS));
    if (Spec != nullptr)
        return Spec;

    auto TS = gcnew CppSharp::AST::ClassTemplateSpecialization();
    HandleDeclaration(CTS, TS);

    TS->Name = clix::marshalString<clix::E_UTF8>(CTS->getName());

    auto NS = GetNamespace(CTS);
    assert(NS && "Expected a valid namespace");
    TS->Namespace = NS;

    TS->TemplatedDecl = CT;
    TS->SpecializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
    CT->Specializations->Add(TS);

    // TODO: Parse the template argument list

    if (CTS->isCompleteDefinition())
        WalkRecordCXX(CTS, TS);

    return TS;
}

//-----------------------------------//

CppSharp::AST::ClassTemplatePartialSpecialization^
Parser::WalkClassTemplatePartialSpecialization(clang::ClassTemplatePartialSpecializationDecl* CTS)
{
    auto CT = WalkClassTemplate(CTS->getSpecializedTemplate());
    auto Spec = CT->FindPartialSpecialization(System::IntPtr(CTS));
    if (Spec != nullptr)
        return Spec;

    auto TS = gcnew CppSharp::AST::ClassTemplatePartialSpecialization();
    HandleDeclaration(CTS, TS);

    TS->Name = clix::marshalString<clix::E_UTF8>(CTS->getName());

    auto NS = GetNamespace(CTS);
    assert(NS && "Expected a valid namespace");
    TS->Namespace = NS;

    TS->TemplatedDecl = CT;
    TS->SpecializationKind = WalkTemplateSpecializationKind(CTS->getSpecializationKind());
    CT->Specializations->Add(TS);

    // TODO: Parse the template argument list

    if (CTS->isCompleteDefinition())
        WalkRecordCXX(CTS, TS);

    return TS;
}

//-----------------------------------//

CppSharp::AST::ClassTemplate^ Parser::WalkClassTemplate(clang::ClassTemplateDecl* TD)
{
    using namespace clang;
    using namespace clix;

    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto CT = NS->FindClassTemplate(System::IntPtr(TD));
    if (CT != nullptr)
        return CT;

    CT = gcnew CppSharp::AST::ClassTemplate();
    HandleDeclaration(TD, CT);

    CT->Namespace = NS;
    NS->Templates->Add(CT);
    
    CT->TemplatedDecl = WalkRecordCXX(TD->getTemplatedDecl());

    auto TPL = TD->getTemplateParameters();
    for(auto it = TPL->begin(); it != TPL->end(); ++it)
    {
        auto ND = *it;

        auto TP = CppSharp::AST::TemplateParameter();
        TP.Name = clix::marshalString<clix::E_UTF8>(ND->getNameAsString());

        CT->Parameters->Add(TP);
    }

    return CT;
}

//-----------------------------------//

CppSharp::AST::FunctionTemplate^ Parser::WalkFunctionTemplate(clang::FunctionTemplateDecl* TD)
{
    using namespace clang;
    using namespace clix;

    auto NS = GetNamespace(TD);
    assert(NS && "Expected a valid namespace");

    auto FT = NS->FindFunctionTemplate(System::IntPtr(TD));
    if (FT != nullptr)
        return FT;

    auto Function = WalkFunction(TD->getTemplatedDecl(), /*IsDependent=*/true,
        /*AddToNamespace=*/false);

    FT = gcnew CppSharp::AST::FunctionTemplate(Function);
    HandleDeclaration(TD, FT);

    FT->Namespace = NS;
    FT->TemplatedDecl = Function;

    auto TPL = TD->getTemplateParameters();
    for(auto it = TPL->begin(); it != TPL->end(); ++it)
    {
        auto ND = *it;

        auto TP = CppSharp::AST::TemplateParameter();
        TP.Name = clix::marshalString<clix::E_UTF8>(ND->getNameAsString());

        FT->Parameters->Add(TP);
    }

    NS->Templates->Add(FT);

    return FT;
}

//-----------------------------------//

static CppSharp::AST::CXXMethodKind GetMethodKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    switch(Name.getNameKind())
    {
    case DeclarationName::Identifier:
    case DeclarationName::ObjCZeroArgSelector:
    case DeclarationName::ObjCOneArgSelector:
    case DeclarationName::ObjCMultiArgSelector:
        return CppSharp::AST::CXXMethodKind::Normal;
    case DeclarationName::CXXConstructorName:
        return CppSharp::AST::CXXMethodKind::Constructor;
    case DeclarationName::CXXDestructorName:
        return CppSharp::AST::CXXMethodKind::Destructor;
    case DeclarationName::CXXConversionFunctionName:
        return CppSharp::AST::CXXMethodKind::Conversion;
    case DeclarationName::CXXOperatorName:
    case DeclarationName::CXXLiteralOperatorName:
        return CppSharp::AST::CXXMethodKind::Operator;
    case DeclarationName::CXXUsingDirective:
        return CppSharp::AST::CXXMethodKind::UsingDirective;
    }
    return CppSharp::AST::CXXMethodKind::Normal;
}

static CppSharp::AST::CXXOperatorKind GetOperatorKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    if (Name.getNameKind() != DeclarationName::CXXOperatorName)
        return CppSharp::AST::CXXOperatorKind::None;

    switch(Name.getCXXOverloadedOperator())
    {
    #define OVERLOADED_OPERATOR(Name,Spelling,Token,Unary,Binary,MemberOnly) \
    case OO_##Name: return CppSharp::AST::CXXOperatorKind::Name;
    #include "clang/Basic/OperatorKinds.def"
    }

    return CppSharp::AST::CXXOperatorKind::None;
}

CppSharp::AST::Method^ Parser::WalkMethodCXX(clang::CXXMethodDecl* MD)
{
    using namespace clang;

    // We could be in a redeclaration, so process the primary context.
    if (MD->getPrimaryContext() != MD)
        return WalkMethodCXX(cast<CXXMethodDecl>(MD->getPrimaryContext()));

    auto RD = MD->getParent();
    auto Decl = WalkDeclaration(RD, /*IgnoreSystemDecls=*/false);

    auto Class = safe_cast<CppSharp::AST::Class^>(Decl);

    // Check for an already existing method that came from the same declaration.
    for each (CppSharp::AST::Method^ Method in Class->Methods)
    {
        if (Method->OriginalPtr.ToPointer() == MD)
            return Method;
    }

    DeclarationName Name = MD->getDeclName();

    CppSharp::AST::Method^ Method = gcnew CppSharp::AST::Method();
    HandleDeclaration(MD, Method);

    Method->Access = ConvertToAccess(MD->getAccess());
    Method->IsStatic = MD->isStatic();
    Method->IsVirtual = MD->isVirtual();
    Method->IsConst = MD->isConst();
    Method->IsOverride = MD->size_overridden_methods() > 0;

    WalkFunction(MD, Method);

    Method->Kind = GetMethodKindFromDecl(Name);

    if (const CXXConstructorDecl* CD = dyn_cast<CXXConstructorDecl>(MD))
    {
        Method->IsDefaultConstructor = CD->isDefaultConstructor();
        Method->IsCopyConstructor = CD->isCopyConstructor();
        Method->IsMoveConstructor = CD->isMoveConstructor();
    }
    else if (const CXXDestructorDecl* DD = dyn_cast<CXXDestructorDecl>(MD))
    {
    }
    else if (const CXXConversionDecl* CD = dyn_cast<CXXConversionDecl>(MD))
    {
        auto TL = MD->getTypeSourceInfo()->getTypeLoc().castAs<FunctionTypeLoc>();
        auto RTL = TL.getResultLoc();
        auto ConvTy = WalkType(CD->getConversionType(), &RTL);
        Method->ConversionType = GetQualifiedType(CD->getConversionType(), ConvTy);
    }

    Class->Methods->Add(Method);

    return Method;
}

//-----------------------------------//

CppSharp::AST::Field^ Parser::WalkFieldCXX(clang::FieldDecl* FD, CppSharp::AST::Class^ Class)
{
    using namespace clang;
    using namespace clix;

    CppSharp::AST::Field^ F = gcnew CppSharp::AST::Field();
    HandleDeclaration(FD, F);

    F->Namespace = Class;
    F->Name = marshalString<E_UTF8>(FD->getName());
    auto TL = FD->getTypeSourceInfo()->getTypeLoc();
    F->QualifiedType = GetQualifiedType(FD->getType(), WalkType(FD->getType(), &TL));
    F->Access = ConvertToAccess(FD->getAccess());
    F->Class = Class;

    Class->Fields->Add(F);

    return F;
}

//-----------------------------------//

CppSharp::AST::TranslationUnit^ Parser::GetTranslationUnit(clang::SourceLocation Loc,
                                                      SourceLocationKind *Kind)
{
    using namespace clang;

    SourceManager& SM = C->getSourceManager();

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

    auto Unit = Lib->FindOrCreateModule(clix::marshalString<clix::E_UTF8>(File));
    if (LocKind != SourceLocationKind::Invalid)
        Unit->IsSystemHeader = SM.isInSystemHeader(Loc);

    return Unit;
}

//-----------------------------------//

CppSharp::AST::TranslationUnit^ Parser::GetTranslationUnit(const clang::Decl* D)
{
    clang::SourceLocation Loc = D->getLocation();

    SourceLocationKind Kind;
    CppSharp::AST::TranslationUnit^ Unit = GetTranslationUnit(Loc, &Kind);

    return Unit;
}

CppSharp::AST::DeclarationContext^ Parser::GetNamespace(clang::Decl* D,
                                                   clang::DeclContext *Ctx)
{
    using namespace clang;

    // If the declaration is at global scope, just early exit.
    if (Ctx->isTranslationUnit())
        return GetTranslationUnit(D);

    CppSharp::AST::TranslationUnit^ Unit = GetTranslationUnit(cast<Decl>(Ctx));

    // Else we need to do a more expensive check to get all the namespaces,
    // and then perform a reverse iteration to get the namespaces in order.
    typedef SmallVector<DeclContext *, 8> ContextsTy;
    ContextsTy Contexts;

    for(; Ctx != nullptr; Ctx = Ctx->getParent())
        Contexts.push_back(Ctx);

    assert(Contexts.back()->isTranslationUnit());
    Contexts.pop_back();

    CppSharp::AST::DeclarationContext^ DC = Unit;

    for (auto I = Contexts.rbegin(), E = Contexts.rend(); I != E; ++I)
    {
        DeclContext* Ctx = *I;

        switch(Ctx->getDeclKind())
        {
        case Decl::Namespace:
        {
            const NamespaceDecl* ND = cast<NamespaceDecl>(Ctx);
            if (ND->isAnonymousNamespace())
                continue;
            auto Name = clix::marshalString<clix::E_UTF8>(ND->getName());
            DC = DC->FindCreateNamespace(Name);
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
        default:
        {
            StringRef Kind = Ctx->getDeclKindName();
            printf("Unhandled declaration context kind: %s\n", Kind);
            assert(0 && "Unhandled declaration context kind");
        } }
    }

    return DC;
}

CppSharp::AST::DeclarationContext^ Parser::GetNamespace(clang::Decl *D)
{
    return GetNamespace(D, D->getDeclContext());
}

static CppSharp::AST::PrimitiveType WalkBuiltinType(const clang::BuiltinType* Builtin)
{
    using namespace CppSharp::AST;

    assert(Builtin && "Expected a builtin type");

    switch(Builtin->getKind())
    {
    case clang::BuiltinType::Void: return PrimitiveType::Void;
    case clang::BuiltinType::Bool: return PrimitiveType::Bool;

    case clang::BuiltinType::SChar:
    case clang::BuiltinType::Char_S: return PrimitiveType::Int8;
    
    case clang::BuiltinType::UChar:
    case clang::BuiltinType::Char_U: return PrimitiveType::UInt8;

    case clang::BuiltinType::WChar_S:
    case clang::BuiltinType::WChar_U: return PrimitiveType::WideChar;

    case clang::BuiltinType::Short: return PrimitiveType::Int16;
    case clang::BuiltinType::UShort: return PrimitiveType::UInt16;

    case clang::BuiltinType::Int: return PrimitiveType::Int32;
    case clang::BuiltinType::UInt: return PrimitiveType::UInt32;

    case clang::BuiltinType::Long: return PrimitiveType::Int32;
    case clang::BuiltinType::ULong: return PrimitiveType::UInt32;
    
    case clang::BuiltinType::LongLong: return PrimitiveType::Int64;
    case clang::BuiltinType::ULongLong: return PrimitiveType::UInt64;

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

static CppSharp::AST::CallingConvention ConvertCallConv(clang::CallingConv CC)
{
    using namespace clang;

    switch(CC)
    {
    case CC_C:
        return CppSharp::AST::CallingConvention::C;
    case CC_X86StdCall:
        return CppSharp::AST::CallingConvention::StdCall;
    case CC_X86FastCall:
        return CppSharp::AST::CallingConvention::FastCall;
    case CC_X86ThisCall:
        return CppSharp::AST::CallingConvention::ThisCall;
    case CC_X86Pascal:
    case CC_AAPCS:
    case CC_AAPCS_VFP:
        return CppSharp::AST::CallingConvention::Unknown;
    }

    return CppSharp::AST::CallingConvention::Default;
}

CppSharp::AST::QualifiedType^ Parser::WalkQualifiedType(clang::TypeSourceInfo* TSI)
{
    auto TL = TSI->getTypeLoc();
    auto Ty = WalkType(TSI->getType(), &TL);
    return GetQualifiedType(TSI->getType(), Ty);
}

CppSharp::AST::Type^ Parser::WalkType(clang::QualType QualType, clang::TypeLoc* TL,
    bool DesugarType)
{
    using namespace clang;
    using namespace clix;

    if (QualType.isNull())
        return nullptr;

    const clang::Type* Type = QualType.getTypePtr();

    if (DesugarType)
    {
        clang::QualType Desugared = QualType.getDesugaredType(*AST);
        assert(!Desugared.isNull() && "Expected a valid desugared type");
        Type = Desugared.getTypePtr();
    }

    CppSharp::AST::Type^ Ty = nullptr;

    assert(Type && "Expected a valid type");
    switch(Type->getTypeClass())
    {
    case Type::Attributed:
    {
        auto Attributed = Type->getAs<clang::AttributedType>();
        assert(Attributed && "Expected an attributed type");

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto AT = gcnew CppSharp::AST::AttributedType();

        auto Modified = Attributed->getModifiedType();
        AT->Modified = GetQualifiedType(Modified, WalkType(Modified, &Next));

        auto Equivalent = Attributed->getEquivalentType();
        AT->Equivalent = GetQualifiedType(Equivalent, WalkType(Equivalent, &Next));

        Ty = AT;
        break;
    }
    case Type::Builtin:
    {
        auto Builtin = Type->getAs<clang::BuiltinType>();
        assert(Builtin && "Expected a builtin type");
    
        auto BT = gcnew CppSharp::AST::BuiltinType();
        BT->Type = WalkBuiltinType(Builtin);
        
        Ty = BT;
        break;
    }
    case Type::Enum:
    {
        auto ET = Type->getAs<clang::EnumType>();
        EnumDecl* ED = ET->getDecl();

        auto TT = gcnew CppSharp::AST::TagType();
        TT->Declaration = TT->Declaration = WalkDeclaration(ED, /*IgnoreSystemDecls=*/false);

        Ty = TT;
        break;
    }
    case Type::Pointer:
    {
        auto Pointer = Type->getAs<clang::PointerType>();
        
        auto P = gcnew CppSharp::AST::PointerType();
        P->Modifier = CppSharp::AST::PointerType::TypeModifier::Pointer;

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Pointee = Pointer->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        Ty = P;
        break;
    }
    case Type::Typedef:
    {
        auto TT = Type->getAs<clang::TypedefType>();
        TypedefNameDecl* TD = TT->getDecl();

        auto TTL = TD->getTypeSourceInfo()->getTypeLoc();
        auto TDD = safe_cast<CppSharp::AST::TypedefDecl^>(WalkDeclaration(TD,
            /*IgnoreSystemDecls=*/false));

        auto Type = gcnew CppSharp::AST::TypedefType();
        Type->Declaration = TDD;

        Ty = Type;
        break;
    }
    case Type::Decayed:
    {
        auto DT = Type->getAs<clang::DecayedType>();

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Type = gcnew CppSharp::AST::DecayedType();
        Type->Decayed = GetQualifiedType(DT->getDecayedType(), WalkType(DT->getDecayedType(), &Next));
        Type->Original = GetQualifiedType(DT->getOriginalType(), WalkType(DT->getOriginalType(), &Next));
        Type->Pointee = GetQualifiedType(DT->getPointeeType(), WalkType(DT->getPointeeType(), &Next));

        Ty = Type;
        break;
    }
    case Type::Elaborated:
    {
        auto ET = Type->getAs<clang::ElaboratedType>();

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        Ty = WalkType(ET->getNamedType(), &Next);
        break;
    }
    case Type::Record:
    {
        auto RT = Type->getAs<clang::RecordType>();
        RecordDecl* RD = RT->getDecl();

        auto TT = gcnew CppSharp::AST::TagType();
        TT->Declaration = WalkDeclaration(RD, /*IgnoreSystemDecls=*/false);

        Ty = TT;
        break;
    }
    case Type::Paren:
    {
        auto PT = Type->getAs<clang::ParenType>();

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        Ty = WalkType(PT->getInnerType(), &Next);
        break;
    }
    case Type::ConstantArray:
    {
        auto AT = AST->getAsConstantArrayType(QualType);

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto A = gcnew CppSharp::AST::ArrayType();
        A->Type = WalkType(AT->getElementType(), &Next);
        A->SizeType = CppSharp::AST::ArrayType::ArraySize::Constant;
        A->Size = AST->getConstantArrayElementCount(AT);

        Ty = A;
        break;
    }
    case Type::IncompleteArray:
    {
        auto AT = AST->getAsIncompleteArrayType(QualType);

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto A = gcnew CppSharp::AST::ArrayType();
        A->Type = WalkType(AT->getElementType(), &Next);
        A->SizeType = CppSharp::AST::ArrayType::ArraySize::Incomplete;

        Ty = A;
        break;
    }
    case Type::DependentSizedArray:
    {
        auto AT = AST->getAsDependentSizedArrayType(QualType);

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto A = gcnew CppSharp::AST::ArrayType();
        A->Type = WalkType(AT->getElementType(), &Next);
        A->SizeType = CppSharp::AST::ArrayType::ArraySize::Dependent;
        //A->Size = AT->getSizeExpr();

        Ty = A;
        break;
    }
    case Type::FunctionProto:
    {
        auto FP = Type->getAs<clang::FunctionProtoType>();

        FunctionProtoTypeLoc FTL;
        TypeLoc RL;
        if (TL && !TL->isNull())
        {
            FTL = TL->getAs<FunctionProtoTypeLoc>();
            RL = FTL.getResultLoc();
        }

        auto F = gcnew CppSharp::AST::FunctionType();
        F->ReturnType = GetQualifiedType(FP->getResultType(),
            WalkType(FP->getResultType(), &RL));
        F->CallingConvention = ConvertCallConv(FP->getCallConv());

        for (unsigned i = 0; i < FP->getNumArgs(); ++i)
        {
            auto FA = gcnew CppSharp::AST::Parameter();
            if (FTL)
            {
                auto PVD = FTL.getArg(i);

                HandleDeclaration(PVD, FA);

                auto PTL = PVD->getTypeSourceInfo()->getTypeLoc();

                FA->Name = marshalString<E_UTF8>(PVD->getNameAsString());
                FA->QualifiedType = GetQualifiedType(PVD->getType(), WalkType(PVD->getType(), &PTL));
            }
            else
            {
                auto Arg = FP->getArgType(i);
                FA->Name = "";
                FA->QualifiedType = GetQualifiedType(Arg, WalkType(Arg));
            }
            F->Parameters->Add(FA);
        }

        Ty = F;
        break;
    }
    case Type::TypeOf:
    {
        auto TO = Type->getAs<clang::TypeOfType>();

        Ty = WalkType(TO->getUnderlyingType());
        break;
    }
    case Type::TypeOfExpr:
    {
        auto TO = Type->getAs<clang::TypeOfExprType>();

        Ty = WalkType(TO->getUnderlyingExpr()->getType());
        break;
    }
    case Type::MemberPointer:
    {
        auto MP = Type->getAs<clang::MemberPointerType>();
        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto MPT = gcnew CppSharp::AST::MemberPointerType();
        MPT->Pointee = WalkType(MP->getPointeeType(), &Next);
        
        Ty = MPT;
        break;
    }
    case Type::TemplateSpecialization:
    {
        auto TS = Type->getAs<clang::TemplateSpecializationType>();
        auto TST = gcnew CppSharp::AST::TemplateSpecializationType();
        
        TemplateName Name = TS->getTemplateName();
        TST->Template = safe_cast<CppSharp::AST::Template^>(WalkDeclaration(
            Name.getAsTemplateDecl(), 0, /*IgnoreSystemDecls=*/false));
        if (TS->isSugared())
            TST->Desugared = WalkType(TS->desugar());

        auto TypeLocClass = TL->getTypeLocClass();

        if (TypeLocClass == TypeLoc::Qualified)
        {
            auto UTL = TL->getUnqualifiedLoc();
            TL = &UTL;
        }
        else if (TypeLocClass == TypeLoc::Elaborated)
        {
            auto ETL = TL->getAs<ElaboratedTypeLoc>();
            auto ITL = ETL.getNextTypeLoc();
            TL = &ITL;
        }

        assert(TL->getTypeLocClass() == TypeLoc::TemplateSpecialization);
        auto TSTL = TL->getAs<TemplateSpecializationTypeLoc>();

        for (unsigned I = 0, E = TS->getNumArgs(); I != E; ++I)
        {
            const TemplateArgument& TA = TS->getArg(I);
            auto Arg = CppSharp::AST::TemplateArgument();

            TemplateArgumentLoc ArgLoc;
            ArgLoc = TSTL.getArgLoc(I);

            switch(TA.getKind())
            {
            case TemplateArgument::Type:
            {
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::Type;
                TypeLoc ArgTL;
                ArgTL = ArgLoc.getTypeSourceInfo()->getTypeLoc();
                Arg.Type = GetQualifiedType(TA.getAsType(), WalkType(TA.getAsType(), &ArgTL));
                break;
            }
            case TemplateArgument::Declaration:
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::Declaration;
                Arg.Declaration = WalkDeclaration(TA.getAsDecl(), 0);
                break;
            case TemplateArgument::NullPtr:
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::NullPtr;
                break;
            case TemplateArgument::Integral:
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::Integral;
                //Arg.Type = WalkType(TA.getIntegralType(), 0);
                Arg.Integral = TA.getAsIntegral().getLimitedValue();
                break;
            case TemplateArgument::Template:
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::Template;
                break;
            case TemplateArgument::TemplateExpansion:
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::TemplateExpansion;
                break;
            case TemplateArgument::Expression:
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::Expression;
                break;
            case TemplateArgument::Pack:
                Arg.Kind = CppSharp::AST::TemplateArgument::ArgumentKind::Pack;
                break;
            }

            TST->Arguments->Add(Arg);
        }

        Ty = TST;
        break;
    }
    case Type::TemplateTypeParm:
    {
        auto TP = Type->getAs<TemplateTypeParmType>();

        auto TPT = gcnew CppSharp::AST::TemplateParameterType();

        if (auto Ident = TP->getIdentifier())
            TPT->Parameter.Name = marshalString<E_UTF8>(Ident->getName());

        Ty = TPT;
        break;
    }
    case Type::SubstTemplateTypeParm:
    {
        auto TP = Type->getAs<SubstTemplateTypeParmType>();
        auto TPT = gcnew CppSharp::AST::TemplateParameterSubstitutionType();

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto RepTy = TP->getReplacementType();
        TPT->Replacement = GetQualifiedType(RepTy, WalkType(RepTy, &Next));

        Ty = TPT;
        break;
    }
    case Type::InjectedClassName:
    {
        auto ICN = Type->getAs<InjectedClassNameType>();
        auto ICNT = gcnew CppSharp::AST::InjectedClassNameType();
        ICNT->Class = safe_cast<CppSharp::AST::Class^>(WalkDeclaration(
            ICN->getDecl(), 0, /*IgnoreSystemDecls=*/false));

        Ty = ICNT;
        break;
    }
    case Type::DependentName:
    {
        auto DN = Type->getAs<DependentNameType>();
        auto DNT = gcnew CppSharp::AST::DependentNameType();

        Ty = DNT;
        break;
    }
    case Type::LValueReference:
    {
        auto LR = Type->getAs<clang::LValueReferenceType>();

        auto P = gcnew CppSharp::AST::PointerType();
        P->Modifier = CppSharp::AST::PointerType::TypeModifier::LVReference;

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        Ty = P;
        break;
    }
    case Type::RValueReference:
    {
        auto LR = Type->getAs<clang::RValueReferenceType>();

        auto P = gcnew CppSharp::AST::PointerType();
        P->Modifier = CppSharp::AST::PointerType::TypeModifier::RVReference;

        TypeLoc Next;
        if (TL && !TL->isNull()) Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        Ty = P;
        break;
    }
    case Type::Vector:
    {
        // GCC-specific / __attribute__((vector_size(n))
        return nullptr;
    }
    case Type::PackExpansion:
    {
        // Ignored.
        return nullptr;
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

CppSharp::AST::Enumeration^ Parser::WalkEnum(clang::EnumDecl* ED)
{
    using namespace clang;
    using namespace clix;

    auto NS = GetNamespace(ED);
    assert(NS && "Expected a valid namespace");

    auto Name = marshalString<E_UTF8>(GetTagDeclName(ED));
    auto E = NS->FindEnum(Name, /*Create=*/false);

    if (E && !E->IsIncomplete)
        return E;

    if (!E)
        E = NS->FindEnum(Name, /*Create=*/true);

    if (ED->isScoped())
        E->Modifiers |= CppSharp::AST::Enumeration::EnumModifiers::Scoped;

    // Get the underlying integer backing the enum.
    QualType IntType = ED->getIntegerType();
    E->Type = WalkType(IntType, 0);
    E->BuiltinType = safe_cast<CppSharp::AST::BuiltinType^>(WalkType(IntType, 0,
        /*DesugarType=*/true));

    if (!ED->isThisDeclarationADefinition())
    {
        E->IsIncomplete = true;
        return E;
    }

    E->IsIncomplete = false;
    for(auto it = ED->enumerator_begin(); it != ED->enumerator_end(); ++it)
    {
        EnumConstantDecl* ECD = (*it);

        auto EnumItem = gcnew CppSharp::AST::Enumeration::Item();
        HandleDeclaration(ECD, EnumItem);

        EnumItem->Name = marshalString<E_UTF8>(ECD->getNameAsString());
        auto Value = ECD->getInitVal();
        EnumItem->Value = Value.isSigned() ? Value.getSExtValue()
            : Value.getZExtValue();

        std::string Text;
        if (GetDeclText(ECD->getSourceRange(), Text))
            EnumItem->Expression = marshalString<E_UTF8>(Text);

        E->AddItem(EnumItem);
    }

    return E;
}

//-----------------------------------//

static const clang::CodeGen::CGFunctionInfo& GetCodeGenFuntionInfo(
    clang::CodeGen::CodeGenTypes* CodeGenTypes, clang::FunctionDecl* FD)
{
    using namespace clang;
    if (auto CD = dyn_cast<clang::CXXConstructorDecl>(FD)) {
        return CodeGenTypes->arrangeCXXConstructorDeclaration(CD, Ctor_Base);
    } else if (auto DD = dyn_cast<clang::CXXDestructorDecl>(FD)) {
        return CodeGenTypes->arrangeCXXDestructor(DD, Dtor_Base);
    }

    return CodeGenTypes->arrangeFunctionDeclaration(FD);
}

static bool CanCheckCodeGenInfo(const clang::Type* Ty)
{
    bool CheckCodeGenInfo = true;

    if (auto RT = Ty->getAs<clang::RecordType>())
        CheckCodeGenInfo &= RT->getDecl()->getDefinition() != 0;

    if (auto RD = Ty->getAsCXXRecordDecl())
        CheckCodeGenInfo &= RD->hasDefinition();

    return CheckCodeGenInfo;
}


void Parser::WalkFunction(clang::FunctionDecl* FD, CppSharp::AST::Function^ F,
                          bool IsDependent)
{
    using namespace clang;
    using namespace clix;

    assert (FD->getBuiltinID() == 0);
    auto FT = FD->getType()->getAs<FunctionType>();

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    F->Name = marshalString<E_UTF8>(FD->getNameAsString());
    F->Namespace = NS;
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
            RTL = FTL.getResultLoc();

            auto &SM = C->getSourceManager();
            auto headStartLoc = GetDeclStartLocation(C.get(), FD);
            auto headEndLoc = SM.getExpansionLoc(FTL.getLParenLoc());
            auto headRange = clang::SourceRange(headStartLoc, headEndLoc);

            HandlePreprocessedEntities(F, headRange, CppSharp::AST::MacroLocation::FunctionHead);
            HandlePreprocessedEntities(F, FTL.getParensRange(), CppSharp::AST::MacroLocation::FunctionParameters);
            //auto bodyRange = clang::SourceRange(FTL.getRParenLoc(), FD->getLocEnd());
            //HandlePreprocessedEntities(F, bodyRange, CppSharp::AST::MacroLocation::FunctionBody);
        }
    }

    F->ReturnType = GetQualifiedType(FD->getResultType(),
        WalkType(FD->getResultType(), &RTL));

    String Mangled = GetDeclMangledName(FD, TargetABI, IsDependent);
    F->Mangled = marshalString<E_UTF8>(Mangled);

    SourceLocation ParamStartLoc = FD->getLocStart();
    SourceLocation ResultLoc;

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

            ParamStartLoc = FTInfo.getRParenLoc();
            ResultLoc = FTInfo.getResultLoc().getLocStart();
        }
    }

    clang::SourceRange Range(FD->getLocStart(), ParamStartLoc);
    if (ResultLoc.isValid())
        Range.setBegin(ResultLoc);

    std::string Sig;
    if (GetDeclText(Range, Sig))
        F->Signature = clix::marshalString<clix::E_UTF8>(Sig);

    for(auto it = FD->param_begin(); it != FD->param_end(); ++it)
    {
        ParmVarDecl* VD = (*it);
        
        auto P = gcnew CppSharp::AST::Parameter();
        P->Name = marshalString<E_UTF8>(VD->getNameAsString());

        TypeLoc PTL;
        if (auto TSI = VD->getTypeSourceInfo())
            PTL = VD->getTypeSourceInfo()->getTypeLoc();

        auto paramRange = VD->getSourceRange();
        paramRange.setBegin(ParamStartLoc);

        HandlePreprocessedEntities(P, paramRange, CppSharp::AST::MacroLocation::FunctionParameters);

        P->QualifiedType = GetQualifiedType(VD->getType(), WalkType(VD->getType(), &PTL));
        P->HasDefaultValue = VD->hasDefaultArg();
        P->Namespace = NS;
        HandleDeclaration(VD, P);

        F->Parameters->Add(P);

        ParamStartLoc = VD->getLocEnd();
    }

    bool CheckCodeGenInfo = !FD->isDependentContext() && !FD->isInvalidDecl();
    CheckCodeGenInfo &= CanCheckCodeGenInfo(FD->getResultType().getTypePtr());
    for (auto I = FD->param_begin(), E = FD->param_end(); I != E; ++I)
        CheckCodeGenInfo &= CanCheckCodeGenInfo((*I)->getType().getTypePtr());

    if (CheckCodeGenInfo)
    {
        auto& CGInfo = GetCodeGenFuntionInfo(CodeGenTypes, FD);
        F->IsReturnIndirect = CGInfo.getReturnInfo().isIndirect();

        auto Index = 0;
        for (auto I = CGInfo.arg_begin(), E = CGInfo.arg_end(); I != E; I++)
        {
            // Skip the first argument as it's the return type.
            if (I == CGInfo.arg_begin())
                continue;
            if (Index >= F->Parameters->Count)
                continue;
            F->Parameters[Index++]->IsIndirect = I->info.isIndirect();
        }
    }
}

CppSharp::AST::Function^ Parser::WalkFunction(clang::FunctionDecl* FD, bool IsDependent,
                                     bool AddToNamespace)
{
    using namespace clang;
    using namespace clix;

    assert (FD->getBuiltinID() == 0);

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    auto Name = marshalString<E_UTF8>(FD->getNameAsString());
    CppSharp::AST::Function^ F = NS->FindFunction(Name, /*Create=*/ false);

    if (F != nullptr)
        return F;

    F = gcnew CppSharp::AST::Function();
    HandleDeclaration(FD, F);

    WalkFunction(FD, F, IsDependent);

    if (AddToNamespace)
        NS->Functions->Add(F);

    return F;
}

//-----------------------------------//

SourceLocationKind Parser::GetLocationKind(const clang::SourceLocation& Loc)
{
    using namespace clang;

    SourceManager& SM = C->getSourceManager();
    PresumedLoc PLoc = SM.getPresumedLoc(Loc);

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
    using namespace clang;

    if (C->hasPreprocessor())
    {
        Preprocessor& P = C->getPreprocessor();
        PreprocessingRecord* PR = P.getPreprocessingRecord();

        if (PR)
        {
          assert(PR && "Expected a valid preprocessing record");
          WalkMacros(PR);
        }
    }

    TranslationUnitDecl* TU = AST->getTranslationUnitDecl();

    for(auto it = TU->decls_begin(); it != TU->decls_end(); ++it)
    {
        Decl* D = (*it);
        WalkDeclarationDef(D);
    }
}

//-----------------------------------//

void Parser::WalkMacros(clang::PreprocessingRecord* PR)
{
    using namespace clang;
    using namespace clix;

    Preprocessor& P = C->getPreprocessor();

    for(auto it = PR->begin(); it != PR->end(); ++it)
    {
        const PreprocessedEntity* PE = (*it);

        switch(PE->getKind())
        {
        case PreprocessedEntity::MacroDefinitionKind:
        {
            const MacroDefinition* MD = cast<MacroDefinition>(PE);
            
            if (!IsValidDeclaration(MD->getLocation()))
                break;

            const IdentifierInfo* II = MD->getName();
            assert(II && "Expected valid identifier info");

            MacroInfo* MI = P.getMacroInfo((IdentifierInfo*)II);

            if (!MI || MI->isBuiltinMacro() || MI->isFunctionLike())
                continue;

            SourceManager& SM = C->getSourceManager();
            const LangOptions &LangOpts = C->getLangOpts();

            auto Loc = MI->getDefinitionLoc();

            if (!IsValidDeclaration(Loc))
                break;

            SourceLocation BeginExpr =
                Lexer::getLocForEndOfToken(Loc, 0, SM, LangOpts);

            auto Range = CharSourceRange::getTokenRange(
                BeginExpr, MI->getDefinitionEndLoc());

            bool Invalid;
            StringRef Expression = Lexer::getSourceText(Range, SM, LangOpts,
                &Invalid);

            if (Invalid || Expression.empty())
                break;

            auto macro = gcnew CppSharp::AST::MacroDefinition();
            macro->Name = marshalString<E_UTF8>(II->getName())->Trim();
            macro->Expression = marshalString<E_UTF8>(Expression)->Trim();

            auto M = GetTranslationUnit(BeginExpr);
            if( M != nullptr )
                M->Macros->Add(macro);

            break;
        }
        default: break;
        }
    }
}

//-----------------------------------//

CppSharp::AST::Variable^ Parser::WalkVariable(clang::VarDecl *VD)
{
    using namespace clang;
    using namespace clix;

    auto Var = gcnew CppSharp::AST::Variable();
    HandleDeclaration(VD, Var);

    Var->Name = marshalString<E_UTF8>(VD->getName());
    Var->Access = ConvertToAccess(VD->getAccess());

    auto TL = VD->getTypeSourceInfo()->getTypeLoc();
    Var->QualifiedType = GetQualifiedType(VD->getType(), WalkType(VD->getType(), &TL));

    auto Mangled = GetDeclMangledName(VD, TargetABI, /*IsDependent=*/false);
    Var->Mangled = marshalString<E_UTF8>(Mangled);

    return Var;
}

//-----------------------------------//

bool Parser::GetDeclText(clang::SourceRange SR, std::string& Text)
{
    using namespace clang;
    SourceManager& SM = C->getSourceManager();
    const LangOptions &LangOpts = C->getLangOpts();

    auto Range = CharSourceRange::getTokenRange(SR);

    bool Invalid;
    Text = Lexer::getSourceText(Range, SM, LangOpts, &Invalid);

    return !Invalid && !Text.empty();
}

CppSharp::AST::PreprocessedEntity^ Parser::WalkPreprocessedEntity(
    CppSharp::AST::Declaration^ Decl, clang::PreprocessedEntity* PPEntity)
{
    using namespace clang;

    for (unsigned I = 0, E = Decl->PreprocessedEntities->Count;
        I != E; ++I)
    {
        auto Entity = Decl->PreprocessedEntities[I];
        if (Entity->OriginalPtr == System::IntPtr(PPEntity))
            return Entity;
    }

    CppSharp::AST::PreprocessedEntity^ Entity;

    switch(PPEntity->getKind())
    {
    case PreprocessedEntity::MacroExpansionKind:
    {
        const MacroExpansion* MD = cast<MacroExpansion>(PPEntity);
        Entity = gcnew CppSharp::AST::MacroExpansion();

        std::string Text;
        if (!GetDeclText(PPEntity->getSourceRange(), Text))
            return nullptr;

        static_cast<CppSharp::AST::MacroExpansion^>(Entity)->Text = 
            clix::marshalString<clix::E_UTF8>(Text);
        break;
    }
    case PreprocessedEntity::MacroDefinitionKind:
    {
        const MacroDefinition* MD = cast<MacroDefinition>(PPEntity);
        Entity = gcnew CppSharp::AST::MacroDefinition();
        break;
    }
    default:
        return nullptr;
    }

    Entity->OriginalPtr = System::IntPtr(PPEntity);
    Decl->PreprocessedEntities->Add(Entity);

    return Entity;
}

void Parser::HandlePreprocessedEntities(CppSharp::AST::Declaration^ Decl,
                                        clang::SourceRange sourceRange,
                                        CppSharp::AST::MacroLocation macroLocation)
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

    for (auto it = Range.first; it != Range.second; ++it)
    {
        PreprocessedEntity* PPEntity = (*it);

        auto Entity = WalkPreprocessedEntity(Decl, PPEntity);
        if (!Entity) continue;

        if (Entity->Location == CppSharp::AST::MacroLocation::Unknown)
            Entity->Location = macroLocation;
    }
}

void Parser::HandleOriginalText(clang::Decl* D, CppSharp::AST::Declaration^ Decl)
{
    auto &SM = C->getSourceManager();
    auto &LangOpts = C->getLangOpts();

    auto Range = clang::CharSourceRange::getTokenRange(D->getSourceRange());

    bool Invalid;
    auto DeclText = clang::Lexer::getSourceText(Range, SM, LangOpts, &Invalid);
    
    if (!Invalid)
        Decl->DebugText = clix::marshalString<clix::E_UTF8>(DeclText);
}

void Parser::HandleDeclaration(clang::Decl* D, CppSharp::AST::Declaration^ Decl)
{
    if (Decl->OriginalPtr.ToPointer() != nullptr)
        return;

    Decl->OriginalPtr = System::IntPtr(D);

    if (Decl->PreprocessedEntities->Count == 0)
    {
        auto startLoc = GetDeclStartLocation(C.get(), D);
        auto endLoc = D->getLocEnd();
        auto range = clang::SourceRange(startLoc, endLoc);

        HandlePreprocessedEntities(Decl, range);
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

CppSharp::AST::Declaration^ Parser::WalkDeclarationDef(clang::Decl* D)
{
    return WalkDeclaration(D, /*IgnoreSystemDecls=*/true,
        /*CanBeDefinition=*/true);
}

CppSharp::AST::Declaration^ Parser::WalkDeclaration(clang::Decl* D,
                                           bool IgnoreSystemDecls,
                                           bool CanBeDefinition)
{
    using namespace clang;
    using namespace clix;

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

    CppSharp::AST::Declaration^ Decl = nullptr;

    auto Kind = D->getKind();
    switch(D->getKind())
    {
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
        auto TD = cast<FunctionTemplateDecl>(D);
        auto FT = WalkFunctionTemplate(TD); 

        Decl = FT;
        break;
    }
    case Decl::Enum:
    {
        EnumDecl* ED = cast<EnumDecl>(D);
        Decl = WalkEnum(ED);
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
        TypedefDecl* TD = cast<TypedefDecl>(D);

        auto NS = GetNamespace(TD);
        auto Name = marshalString<E_UTF8>(GetDeclName(TD));
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

        auto NS = GetNamespace(VD);
        Decl->Namespace = NS;
        NS->Variables->Add(static_cast<CppSharp::AST::Variable^>(Decl));
        break;
    }
    // Ignore these declarations since they must have been declared in
    // a class already.
    case Decl::CXXConstructor:
    case Decl::CXXDestructor:
    case Decl::CXXConversion:
    case Decl::CXXMethod:
        break;
    case Decl::Empty:
    case Decl::AccessSpec:
    case Decl::Friend:
    case Decl::Using:
    case Decl::UsingDirective:
    case Decl::UsingShadow:
    case Decl::UnresolvedUsingTypename:
    case Decl::UnresolvedUsingValue:
    case Decl::IndirectField:
    case Decl::StaticAssert:
        break;
    default:
    {
        Debug("Unhandled declaration kind: %s\n", D->getDeclKindName());

        auto &SM = C->getSourceManager();
        auto Loc = D->getLocation();
        auto FileName = SM.getFilename(Loc);
        auto Offset = SM.getFileOffset(Loc);
        auto LineNo = SM.getLineNumber(SM.getFileID(Loc), Offset);
        Debug("  %s (line %u)\n", FileName, LineNo);

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

void Parser::HandleDiagnostics(ParserResult^ res)
{
    auto DiagClient = (DiagnosticConsumer&) C->getDiagnosticClient();

    // Convert the diagnostics to the managed types
    for each (auto& Diag in DiagClient.Diagnostics)
    {
        using namespace clix;

        auto& Source = C->getSourceManager();
        auto FileName = Source.getFilename(Source.getFileLoc(Diag.Location));

        auto PDiag = ParserDiagnostic();
        PDiag.FileName = marshalString<E_UTF8>(FileName.str());
        PDiag.Message = marshalString<E_UTF8>(Diag.Message.str());
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

        res->Diagnostics->Add(PDiag);
    }
}

ParserResult^ Parser::ParseHeader(const std::string& File)
{
    assert(Opts->ASTContext && "Expected a valid ASTContext");

    auto res = gcnew ParserResult();
    res->ASTContext = Lib;

    if (File.empty())
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    SetupHeader();

    auto SC = new clang::SemaConsumer();
    C->setASTConsumer(SC);

    C->createSema(clang::TU_Complete, 0);
    SC->InitializeSema(C->getSema());

    auto DiagClient = new DiagnosticConsumer();
    C->getDiagnostics().setClient(DiagClient);

    // Check that the file is reachable.
    const clang::DirectoryLookup *Dir;
    if (!C->getPreprocessor().getHeaderSearchInfo().LookupFile(File, /*isAngled*/true,
        nullptr, Dir, nullptr, nullptr, nullptr, nullptr))
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
    C->getSourceManager().createMainFileIDForMemBuffer(buffer);

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

    // Initialize enough Clang codegen machinery so we can get at ABI details.
    llvm::LLVMContext Ctx;
    llvm::OwningPtr<llvm::Module> M(new llvm::Module("", Ctx));

    M->setTargetTriple(AST->getTargetInfo().getTriple().getTriple());
    M->setDataLayout(AST->getTargetInfo().getTargetDescription());
    llvm::OwningPtr<llvm::DataLayout> TD(new llvm::DataLayout(AST->getTargetInfo()
        .getTargetDescription()));

    llvm::OwningPtr<clang::CodeGen::CodeGenModule> CGM(
        new clang::CodeGen::CodeGenModule(C->getASTContext(), C->getCodeGenOpts(),
        *M, *TD, C->getDiagnostics()));

    llvm::OwningPtr<clang::CodeGen::CodeGenTypes> CGT(
        new clang::CodeGen::CodeGenTypes(*CGM.get()));

    CodeGenInfo = (clang::TargetCodeGenInfo*) &CGM->getTargetCodeGenInfo();
    CodeGenTypes = CGT.get();

    WalkAST();

    res->Kind = ParserResultKind::Success;
    return res;
 }

ParserResultKind Parser::ParseArchive(llvm::StringRef File,
                                      llvm::MemoryBuffer *Buffer,
                                      CppSharp::AST::NativeLibrary^ NativeLib)
{
    llvm::error_code Code;
    llvm::object::Archive Archive(Buffer, Code);

    if (Code)
        return ParserResultKind::Error;

    auto LibName = clix::marshalString<clix::E_UTF8>(File);
    NativeLib->FileName = LibName;

    for(auto it = Archive.begin_symbols(); it != Archive.end_symbols(); ++it)
    {
        llvm::StringRef SymRef;

        if (it->getName(SymRef))
            continue;

        System::String^ SymName = clix::marshalString<clix::E_UTF8>(SymRef);
        NativeLib->Symbols->Add(SymName);
    }

    return ParserResultKind::Success;
}

ParserResultKind Parser::ParseSharedLib(llvm::StringRef File,
                                        llvm::MemoryBuffer *Buffer,
                                        CppSharp::AST::NativeLibrary^ NativeLib)
{
    auto Object = llvm::object::ObjectFile::createObjectFile(Buffer);

    if (!Object)
        return ParserResultKind::Error;

    auto LibName = clix::marshalString<clix::E_UTF8>(File);
    NativeLib->FileName = LibName;

    llvm::error_code ec;
    for(auto it = Object->begin_symbols(); it != Object->end_symbols(); it.increment(ec))
    {
        llvm::StringRef SymRef;

        if (it->getName(SymRef))
            continue;

        System::String^ SymName = clix::marshalString<clix::E_UTF8>(SymRef);
        NativeLib->Symbols->Add(SymName);
    }

    for(auto it = Object->begin_dynamic_symbols(); it != Object->end_dynamic_symbols();
        it.increment(ec))
    {
        llvm::StringRef SymRef;

        if (it->getName(SymRef))
            continue;

        System::String^ SymName = clix::marshalString<clix::E_UTF8>(SymRef);
        NativeLib->Symbols->Add(SymName);
    }

    return ParserResultKind::Success;
}

ParserResult^ Parser::ParseLibrary(const std::string& File)
{
    auto res = gcnew ParserResult();
    res->Library = gcnew CppSharp::AST::NativeLibrary();
    res->Library->FileName = clix::marshalString<clix::E_UTF8>(File);

    if (File.empty())
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    C.reset(new clang::CompilerInstance());
    C->createFileManager();

    auto &FM = C->getFileManager();
    const clang::FileEntry* FileEntry = 0;

    for each(System::String^ LibDir in Opts->LibraryDirs)
    {
        auto DirName = clix::marshalString<clix::E_UTF8>(LibDir);
        llvm::SmallString<256> Path(DirName);
        llvm::sys::path::append(Path, File);

        if (FileEntry = FM.getFile(Path.str()))
            break;
    }

    if (!FileEntry)
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    res->Kind = ParseArchive(File, FM.getBufferForFile(FileEntry),
        res->Library);

    if (res->Kind == ParserResultKind::Success)
        return res;

    res->Kind = ParseSharedLib(File, FM.getBufferForFile(FileEntry),
        res->Library);

    if (res->Kind == ParserResultKind::Success)
        return res;

    return res;
}