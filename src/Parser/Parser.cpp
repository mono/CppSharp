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
#include <clang/Basic/Version.h>
#include <clang/Config/config.h>
#include <clang/AST/ASTContext.h>
#include <clang/AST/DeclTemplate.h>
#include <clang/Lex/DirectoryLookup.h>
#include <clang/Lex/HeaderSearch.h>
#include <clang/Lex/PreprocessingRecord.h>
#include <clang/Sema/Sema.h>
#include <clang/Sema/SemaConsumer.h>
#include <clang/Frontend/Utils.h>
#include <clang/Driver/Util.h>

#include <string>
#include <iostream>
#include <sstream>

//-----------------------------------//

Parser::Parser(ParserOptions^ Opts) : Lib(Opts->Library), Opts(Opts), Index(0)
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
    if (!System::String::IsNullOrWhiteSpace(Opts->TargetTriple))
        TO.Triple = marshalString<E_UTF8>(Opts->TargetTriple);
    else
        TO.Triple = llvm::sys::getDefaultTargetTriple();

    TargetABI = Opts->MicrosoftMode ? TargetCXXABI::Microsoft
        : TargetCXXABI::GenericItanium;

    TargetInfo* TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), &TO);
    TI->setCXXABI(TargetABI);
    C->setTarget(TI);

    C->createFileManager();
    C->createSourceManager(C->getFileManager());

    if (Opts->NoStandardIncludes)
    {
        auto HSOpts = C->getHeaderSearchOpts();
        HSOpts.UseStandardSystemIncludes = false;
        HSOpts.UseStandardCXXIncludes = false;
    }

    if (Opts->NoBuiltinIncludes)
    {
        auto HSOpts = C->getHeaderSearchOpts();
        HSOpts.UseBuiltinIncludes = false;
    }

    if (Opts->Verbose)
        C->getHeaderSearchOpts().Verbose = true;

    for each(System::String^% include in Opts->IncludeDirs)
    {
        String s = marshalString<E_UTF8>(include);
        C->getHeaderSearchOpts().AddPath(s, frontend::Angled, false, false);
    }

    for each(System::String^% include in Opts->SystemIncludeDirs)
    {
        String s = marshalString<E_UTF8>(include);
        C->getHeaderSearchOpts().AddPath(s, frontend::System, false, false);
    }

    for each(System::String^% def in Opts->Defines)
    {
        String s = marshalString<E_UTF8>(def);
        C->getPreprocessorOpts().addMacroDef(s);
    }

    // Initialize the default platform headers.
    std::string ResourceDir = GetClangResourceDir(".");
    C->getHeaderSearchOpts().ResourceDir = ResourceDir;
    C->getHeaderSearchOpts().AddPath(GetClangBuiltinIncludeDir(),
        clang::frontend::System, false, false);

#ifdef _MSC_VER
    if (!Opts->NoBuiltinIncludes)
        {
        std::vector<std::string> SystemDirs = GetWindowsSystemIncludeDirs();
        clang::HeaderSearchOptions& HSOpts = C->getHeaderSearchOpts();

        for(size_t i = 0; i < SystemDirs.size(); ++i)
        {
            HSOpts.AddPath(SystemDirs[i], frontend::System, false, false);
        }
    }
#endif

    C->createPreprocessor();
    C->createASTContext();

    if (C->hasPreprocessor())
    {
        Preprocessor& P = C->getPreprocessor();
        P.createPreprocessingRecord();
        P.getBuiltinInfo().InitializeBuiltins(P.getIdentifierTable(),
            P.getLangOpts());
    }
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
       MC.reset(createItaniumMangleContext(*AST, AST->getDiagnostics()));
       //AST->setCXXABI(CreateItaniumCXXABI(*AST));
       break;
    case TargetCXXABI::Microsoft:
       MC.reset(createMicrosoftMangleContext(*AST, AST->getDiagnostics()));
       //AST->setCXXABI(CreateMicrosoftCXXABI(*AST));
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

    auto prevDecl = GetPreviousDeclInContext(D);
    if(!prevDecl)
        return lineBeginLoc;

    auto prevDeclEndLoc = SM.getExpansionLoc(prevDecl->getLocEnd());
    auto prevDeclEndOffset = SM.getFileOffset(prevDeclEndLoc);

    if(SM.getFileID(prevDeclEndLoc) != SM.getFileID(startLoc))
        return lineBeginLoc;

    assert(prevDeclEndOffset <= startOffset);

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

CppSharp::TypeQualifiers GetTypeQualifiers(clang::QualType Type)
{
    CppSharp::TypeQualifiers quals;
    quals.IsConst = Type.isLocalConstQualified();
    quals.IsRestrict = Type.isLocalRestrictQualified();
    quals.IsVolatile = Type.isVolatileQualified();
    return quals;
}

CppSharp::QualifiedType GetQualifiedType(clang::QualType qual, CppSharp::Type^ type)
{
    CppSharp::QualifiedType qualType;
    qualType.Type = type;
    qualType.Qualifiers = GetTypeQualifiers(qual);
    return qualType;
}

//-----------------------------------//

static CppSharp::AccessSpecifier ConvertToAccess(clang::AccessSpecifier AS)
{
    switch(AS)
    {
    case clang::AS_private:
        return CppSharp::AccessSpecifier::Private;
    case clang::AS_protected:
        return CppSharp::AccessSpecifier::Protected;
    case clang::AS_public:
        return CppSharp::AccessSpecifier::Public;
    }

    return CppSharp::AccessSpecifier::Public;
}

CppSharp::Class^ Parser::WalkRecordCXX(clang::CXXRecordDecl* Record)
{
    using namespace clang;
    using namespace clix;

    if (Record->isInjectedClassName())
        return nullptr;

    auto NS = GetNamespace(Record);
    assert(NS && "Expected a valid namespace");

    bool isCompleteDefinition = Record->isCompleteDefinition();
    auto Name = marshalString<E_UTF8>(GetTagDeclName(Record));
    auto RC = NS->FindClass(Name, isCompleteDefinition, /*Create=*/false);

    if (RC)
        return RC;

    RC = NS->FindClass(Name, isCompleteDefinition, /*Create=*/true);

    if (!isCompleteDefinition)
        return RC;

    auto headStartLoc = GetDeclStartLocation(C.get(), Record);
    auto headEndLoc = Record->getLocation(); // identifier location
    auto bodyEndLoc = Record->getLocEnd();

    auto headRange = clang::SourceRange(headStartLoc, headEndLoc);
    auto bodyRange = clang::SourceRange(headEndLoc, bodyEndLoc);

    HandlePreprocessedEntities(RC, headRange, CppSharp::MacroLocation::ClassHead);
    HandlePreprocessedEntities(RC, bodyRange, CppSharp::MacroLocation::ClassBody);

    RC->IsPOD = Record->isPOD();
    RC->IsUnion = Record->isUnion();
    RC->IsAbstract = Record->isAbstract();
    RC->IsDependent = Record->isDependentType();

    auto &Sema = C->getSema();
    Sema.ForceDeclarationOfImplicitMembers(Record);

    // Get the record layout information.
    const ASTRecordLayout* Layout = 0;
    if (!Record->isDependentType())
    {
        Layout = &C->getASTContext().getASTRecordLayout(Record);
        RC->Layout->Alignment = (int)Layout-> getAlignment().getQuantity();
        RC->Layout->Size = (int)Layout->getSize().getQuantity();
        RC->Layout->DataSize = (int)Layout->getDataSize().getQuantity();
    }

    CppSharp::AccessSpecifierDecl^ AccessDecl = nullptr;

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
            RC->Methods->Add(Method);
            HandleComments(MD, Method);
            break;
        }
        case Decl::Field:
        {
            auto FD = cast<FieldDecl>(D);
            auto Field = WalkFieldCXX(FD, RC);

            if (Layout)
                Field->Offset = Layout->getFieldOffset(FD->getFieldIndex());

            RC->Fields->Add(Field);
            HandleComments(FD, Field);
            break;
        }
        case Decl::AccessSpec:
        {
            AccessSpecDecl* AS = cast<AccessSpecDecl>(D);

            AccessDecl = gcnew CppSharp::AccessSpecifierDecl();
            AccessDecl->Access = ConvertToAccess(AS->getAccess());
            AccessDecl->Namespace = RC;

            auto startLoc = GetDeclStartLocation(C.get(), AS);
            auto range = SourceRange(startLoc, AS->getColonLoc());
            HandlePreprocessedEntities(AccessDecl, range,
                CppSharp::MacroLocation::Unknown);

            RC->Specifiers->Add(AccessDecl);
            break;
        }
        case Decl::IndirectField: // FIXME: Handle indirect fields
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

        CppSharp::BaseClassSpecifier^ Base = gcnew CppSharp::BaseClassSpecifier();
        Base->Access = ConvertToAccess(BS.getAccessSpecifier());
        Base->IsVirtual = BS.isVirtual();
        Base->Type = WalkType(BS.getType(), &BS.getTypeSourceInfo()->getTypeLoc());

        RC->Bases->Add(Base);
    }

    return RC;
}

//-----------------------------------//

CppSharp::ClassTemplate^ Parser::WalkClassTemplate(clang::ClassTemplateDecl* TD)
{
    using namespace clang;
    using namespace clix;

    auto Class = WalkRecordCXX(TD->getTemplatedDecl());
    CppSharp::ClassTemplate^ CT = gcnew CppSharp::ClassTemplate(Class);

    return CT;
}

//-----------------------------------//

CppSharp::FunctionTemplate^ Parser::WalkFunctionTemplate(clang::FunctionTemplateDecl* TD)
{
    using namespace clang;
    using namespace clix;

    auto Function = WalkFunction(TD->getTemplatedDecl(), /*IsDependent=*/true,
        /*AddToNamespace=*/false);
    CppSharp::FunctionTemplate^ FT = gcnew CppSharp::FunctionTemplate(Function);

    auto TPL = TD->getTemplateParameters();
    for(auto it = TPL->begin(); it != TPL->end(); ++it)
    {
        auto ND = *it;

        auto TP = CppSharp::TemplateParameter();
        TP.Name = clix::marshalString<clix::E_UTF8>(ND->getNameAsString());

        FT->Parameters->Add(TP);
    }

    return FT;
}

//-----------------------------------//

static CppSharp::CXXMethodKind GetMethodKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    switch(Name.getNameKind())
    {
    case DeclarationName::Identifier:
    case DeclarationName::ObjCZeroArgSelector:
    case DeclarationName::ObjCOneArgSelector:
    case DeclarationName::ObjCMultiArgSelector:
        return CppSharp::CXXMethodKind::Normal;
    case DeclarationName::CXXConstructorName:
        return CppSharp::CXXMethodKind::Constructor;
    case DeclarationName::CXXDestructorName:
        return CppSharp::CXXMethodKind::Destructor;
    case DeclarationName::CXXConversionFunctionName:
        return CppSharp::CXXMethodKind::Conversion;
    case DeclarationName::CXXOperatorName:
    case DeclarationName::CXXLiteralOperatorName:
        return CppSharp::CXXMethodKind::Operator;
    case DeclarationName::CXXUsingDirective:
        return CppSharp::CXXMethodKind::UsingDirective;
    }
    return CppSharp::CXXMethodKind::Normal;
}

static CppSharp::CXXOperatorKind GetOperatorKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    if (Name.getNameKind() != DeclarationName::CXXOperatorName)
        return CppSharp::CXXOperatorKind::None;

    switch(Name.getCXXOverloadedOperator())
    {
    #define OVERLOADED_OPERATOR(Name,Spelling,Token,Unary,Binary,MemberOnly) \
    case OO_##Name: return CppSharp::CXXOperatorKind::Name;
    #include "clang/Basic/OperatorKinds.def"
    }

    return CppSharp::CXXOperatorKind::None;
}

CppSharp::Method^ Parser::WalkMethodCXX(clang::CXXMethodDecl* MD)
{
    using namespace clang;

    DeclarationName Name = MD->getDeclName();

    CppSharp::Method^ Method = gcnew CppSharp::Method();
    Method->Access = ConvertToAccess(MD->getAccess());
    Method->Kind = GetMethodKindFromDecl(Name);
    Method->OperatorKind = GetOperatorKindFromDecl(Name);
    Method->IsStatic = MD->isStatic();

    WalkFunction(MD, Method);

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
    }

    return Method;
}

//-----------------------------------//

CppSharp::Field^ Parser::WalkFieldCXX(clang::FieldDecl* FD, CppSharp::Class^ Class)
{
    using namespace clang;
    using namespace clix;

    CppSharp::Field^ F = gcnew CppSharp::Field();
    F->Namespace = Class;

    F->Name = marshalString<E_UTF8>(FD->getName());
    auto TL = FD->getTypeSourceInfo()->getTypeLoc();
    F->QualifiedType = GetQualifiedType(FD->getType(), WalkType(FD->getType(), &TL));
    F->Access = ConvertToAccess(FD->getAccess());
    F->Class = Class;

    HandleComments(FD, F);

    return F;
}

//-----------------------------------//

CppSharp::TranslationUnit^ Parser::GetTranslationUnit(clang::SourceLocation Loc,
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

CppSharp::TranslationUnit^ Parser::GetTranslationUnit(const clang::Decl* D)
{
    clang::SourceLocation Loc = D->getLocation();

    SourceLocationKind Kind;
    CppSharp::TranslationUnit^ Unit = GetTranslationUnit(Loc, &Kind);

    return Unit;
}

CppSharp::DeclarationContext^ Parser::GetNamespace(clang::Decl* D,
                                                   clang::DeclContext *Ctx)
{
    using namespace clang;

    // If the declaration is at global scope, just early exit.
    if (Ctx->isTranslationUnit())
        return GetTranslationUnit(D);

    CppSharp::TranslationUnit^ Unit = GetTranslationUnit(cast<Decl>(Ctx));

    // Else we need to do a more expensive check to get all the namespaces,
    // and then perform a reverse iteration to get the namespaces in order.
    typedef SmallVector<DeclContext *, 8> ContextsTy;
    ContextsTy Contexts;

    for(; Ctx != nullptr; Ctx = Ctx->getParent())
        Contexts.push_back(Ctx);

    assert(Contexts.back()->isTranslationUnit());
    Contexts.pop_back();

    CppSharp::DeclarationContext^ DC = Unit;

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
            // FIXME: Ignore ClassTemplateSpecialization namespaces...
            // We might be able to translate these to C# nested types.
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

CppSharp::DeclarationContext^ Parser::GetNamespace(clang::Decl *D)
{
    return GetNamespace(D, D->getDeclContext());
}

static CppSharp::PrimitiveType WalkBuiltinType(const clang::BuiltinType* Builtin)
{
    using namespace CppSharp;

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

CppSharp::Type^ Parser::WalkType(clang::QualType QualType, clang::TypeLoc* TL,
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

    assert(Type && "Expected a valid type");
    switch(Type->getTypeClass())
    {
    case Type::Builtin:
    {
        auto Builtin = Type->getAs<clang::BuiltinType>();
        assert(Builtin && "Expected a builtin type");
    
        auto BT = gcnew CppSharp::BuiltinType();
        BT->Type = WalkBuiltinType(Builtin);
        
        return BT;
    }
    case Type::Enum:
    {
        auto ET = Type->getAs<clang::EnumType>();
        EnumDecl* ED = ET->getDecl();

        //auto Name = marshalString<E_UTF8>(GetTagDeclName(ED));

        auto TT = gcnew CppSharp::TagType();
        TT->Declaration = WalkDeclaration(ED, 0, /*IgnoreSystemDecls=*/false);

        return TT;
    }
    case Type::Pointer:
    {
        auto Pointer = Type->getAs<clang::PointerType>();
        
        auto P = gcnew CppSharp::PointerType();
        P->Modifier = CppSharp::PointerType::TypeModifier::Pointer;

        auto Next = TL->getNextTypeLoc();

        auto Pointee = Pointer->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        return P;
    }
    case Type::Typedef:
    {
        auto TT = Type->getAs<clang::TypedefType>();
        TypedefNameDecl* TD = TT->getDecl();

        auto TTL = TD->getTypeSourceInfo()->getTypeLoc();
        auto TDD = safe_cast<CppSharp::TypedefDecl^>(WalkDeclaration(TD,
            /*IgnoreSystemDecls=*/false));

        auto Type = gcnew CppSharp::TypedefType();
        Type->Declaration = TDD;

        return Type;
    }
    case Type::Decayed:
    {
        auto DT = Type->getAs<clang::DecayedType>();
        auto Next = TL->getNextTypeLoc();

        auto Type = gcnew CppSharp::DecayedType();
        Type->Decayed = GetQualifiedType(DT->getDecayedType(), WalkType(DT->getDecayedType(), &Next));
        Type->Original = GetQualifiedType(DT->getOriginalType(), WalkType(DT->getOriginalType(), &Next));
        Type->Pointee = GetQualifiedType(DT->getPointeeType(), WalkType(DT->getPointeeType(), &Next));

        return Type;
    }
    case Type::Elaborated:
    {
        auto ET = Type->getAs<clang::ElaboratedType>();
        auto Next = TL->getNextTypeLoc();
        return WalkType(ET->getNamedType(), &Next);
    }
    case Type::Record:
    {
        auto RT = Type->getAs<clang::RecordType>();
        RecordDecl* RD = RT->getDecl();

        auto TT = gcnew CppSharp::TagType();
        TT->Declaration = WalkDeclaration(RD, /*IgnoreSystemDecls=*/false);

        return TT;
    }
    case Type::Paren:
    {
        auto PT = Type->getAs<clang::ParenType>();
        auto Next = TL->getNextTypeLoc();
        return WalkType(PT->getInnerType(), &Next);
    }
    case Type::ConstantArray:
    {
        auto AT = AST->getAsConstantArrayType(QualType);

        auto A = gcnew CppSharp::ArrayType();
        auto Next = TL->getNextTypeLoc();
        A->Type = WalkType(AT->getElementType(), &Next);
        A->SizeType = CppSharp::ArrayType::ArraySize::Constant;
        A->Size = AST->getConstantArrayElementCount(AT);

        return A;
    }
    case Type::IncompleteArray:
    {
        auto AT = AST->getAsIncompleteArrayType(QualType);

        auto A = gcnew CppSharp::ArrayType();
        auto Next = TL->getNextTypeLoc();
        A->Type = WalkType(AT->getElementType(), &Next);
        A->SizeType = CppSharp::ArrayType::ArraySize::Incomplete;

        return A;
    }
    case Type::DependentSizedArray:
    {
        auto AT = AST->getAsDependentSizedArrayType(QualType);

        auto A = gcnew CppSharp::ArrayType();
        auto Next = TL->getNextTypeLoc();
        A->Type = WalkType(AT->getElementType(), &Next);
        A->SizeType = CppSharp::ArrayType::ArraySize::Dependent;
        //A->Size = AT->getSizeExpr();

        return A;
    }
    case Type::FunctionProto:
    {
        auto FP = Type->getAs<clang::FunctionProtoType>();

        auto FTL = TL->getAs<FunctionProtoTypeLoc>();
        auto RL = FTL.getResultLoc();

        auto F = gcnew CppSharp::FunctionType();
        F->ReturnType = GetQualifiedType(FP->getResultType(),
            WalkType(FP->getResultType(), &RL));

        for (unsigned i = 0; i < FP->getNumArgs(); ++i)
        {
            auto FA = gcnew CppSharp::Parameter();

            auto PVD = FTL.getArg(i);
            auto PTL = PVD->getTypeSourceInfo()->getTypeLoc();

            FA->Name = marshalString<E_UTF8>(PVD->getNameAsString());
            FA->QualifiedType = GetQualifiedType(PVD->getType(), WalkType(PVD->getType(), &PTL));

            F->Parameters->Add(FA);
        }

        return F;
    }
    case Type::TypeOf:
    {
        auto TO = Type->getAs<clang::TypeOfType>();
        return WalkType(TO->getUnderlyingType());
    }
    case Type::TypeOfExpr:
    {
        auto TO = Type->getAs<clang::TypeOfExprType>();
        return WalkType(TO->getUnderlyingExpr()->getType());
    }
    case Type::MemberPointer:
    {
        auto MP = Type->getAs<clang::MemberPointerType>();
        auto Next = TL->getNextTypeLoc();

        auto MPT = gcnew CppSharp::MemberPointerType();
        MPT->Pointee = WalkType(MP->getPointeeType(), &Next);
        
        return MPT;
    }
    case Type::TemplateSpecialization:
    {
        auto TS = Type->getAs<clang::TemplateSpecializationType>();
        auto TST = gcnew CppSharp::TemplateSpecializationType();
        
        TemplateName Name = TS->getTemplateName();
        TST->Template = safe_cast<CppSharp::Template^>(WalkDeclaration(
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
            auto Arg = CppSharp::TemplateArgument();

            TemplateArgumentLoc ArgLoc;
            ArgLoc = TSTL.getArgLoc(I);

            switch(TA.getKind())
            {
            case TemplateArgument::Type:
            {
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::Type;
                TypeLoc ArgTL;
                ArgTL = ArgLoc.getTypeSourceInfo()->getTypeLoc();
                Arg.Type = GetQualifiedType(TA.getAsType(), WalkType(TA.getAsType(), &ArgTL));
                break;
            }
            case TemplateArgument::Declaration:
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::Declaration;
                Arg.Declaration = WalkDeclaration(TA.getAsDecl(), 0);
                break;
            case TemplateArgument::NullPtr:
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::NullPtr;
                break;
            case TemplateArgument::Integral:
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::Integral;
                //Arg.Type = WalkType(TA.getIntegralType(), 0);
                Arg.Integral = TA.getAsIntegral().getLimitedValue();
                break;
            case TemplateArgument::Template:
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::Template;
                break;
            case TemplateArgument::TemplateExpansion:
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::TemplateExpansion;
                break;
            case TemplateArgument::Expression:
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::Expression;
                break;
            case TemplateArgument::Pack:
                Arg.Kind = CppSharp::TemplateArgument::ArgumentKind::Pack;
                break;
            }

            TST->Arguments->Add(Arg);
        }

        return TST;
    }
    case Type::TemplateTypeParm:
    {
        auto TP = Type->getAs<TemplateTypeParmType>();

        auto TPT = gcnew CppSharp::TemplateParameterType();

        if (auto Ident = TP->getIdentifier())
            TPT->Parameter.Name = marshalString<E_UTF8>(Ident->getName());

        return TPT;
    }
    case Type::SubstTemplateTypeParm:
    {
        auto TP = Type->getAs<SubstTemplateTypeParmType>();
        auto Ty = TP->getReplacementType();
        auto TPT = gcnew CppSharp::TemplateParameterSubstitutionType();

        auto Next = TL->getNextTypeLoc();
        TPT->Replacement = GetQualifiedType(Ty, WalkType(Ty, &Next));

        return TPT;
    }
    case Type::InjectedClassName:
    {
        auto ICN = Type->getAs<InjectedClassNameType>();
        auto ICNT = gcnew CppSharp::InjectedClassNameType();
        ICNT->Class = safe_cast<CppSharp::Class^>(WalkDeclaration(
            ICN->getDecl(), 0, /*IgnoreSystemDecls=*/false));
        return ICNT;
    }
    case Type::DependentName:
    {
        auto DN = Type->getAs<DependentNameType>();
        auto DNT = gcnew CppSharp::DependentNameType();
        return DNT;
    }
    case Type::LValueReference:
    {
        auto LR = Type->getAs<clang::LValueReferenceType>();

        auto P = gcnew CppSharp::PointerType();
        P->Modifier = CppSharp::PointerType::TypeModifier::LVReference;

        TypeLoc Next;
        if (!TL->isNull())
            Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        return P;
    }
    case Type::RValueReference:
    {
        auto LR = Type->getAs<clang::RValueReferenceType>();

        auto P = gcnew CppSharp::PointerType();
        P->Modifier = CppSharp::PointerType::TypeModifier::RVReference;

        TypeLoc Next;
        if (!TL->isNull())
            Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        return P;
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
}

//-----------------------------------//

CppSharp::Enumeration^ Parser::WalkEnum(clang::EnumDecl* ED)
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
        E->Modifiers |= CppSharp::Enumeration::EnumModifiers::Scoped;

    // Get the underlying integer backing the enum.
    QualType IntType = ED->getIntegerType();
    E->Type = WalkType(IntType, 0);
    E->BuiltinType = safe_cast<CppSharp::BuiltinType^>(WalkType(IntType, 0,
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

        std::string BriefText;
        if (const RawComment* Comment = AST->getRawCommentForAnyRedecl(ECD))
            BriefText = Comment->getBriefText(*AST);

        auto EnumItem = gcnew CppSharp::Enumeration::Item();
        EnumItem->Name = marshalString<E_UTF8>(ECD->getNameAsString());
        EnumItem->Value = (int) ECD->getInitVal().getLimitedValue();
        EnumItem->Comment = marshalString<E_UTF8>(BriefText);
        //EnumItem->ExplicitValue = ECD->getExplicitValue();

        E->AddItem(EnumItem);
    }

    return E;
}

//-----------------------------------//

clang::CallingConv Parser::GetAbiCallConv(clang::CallingConv CC,
                                          bool IsInstMethod,
                                          bool IsVariadic)
{
  using namespace clang;

  // TODO: Itanium ABI

  if (CC == CC_Default) {
    if (IsInstMethod) {
      CC = AST->getDefaultCXXMethodCallConv(IsVariadic);
    } else {
      CC = CC_C;
    }
  }

  return CC;
}

static CppSharp::CallingConvention ConvertCallConv(clang::CallingConv CC)
{
    using namespace clang;

    switch(CC)
    {
    case CC_Default:
    case CC_C:
        return CppSharp::CallingConvention::C;
    case CC_X86StdCall:
        return CppSharp::CallingConvention::StdCall;
    case CC_X86FastCall:
        return CppSharp::CallingConvention::FastCall;
    case CC_X86ThisCall:
        return CppSharp::CallingConvention::ThisCall;
    case CC_X86Pascal:
    case CC_AAPCS:
    case CC_AAPCS_VFP:
        return CppSharp::CallingConvention::Unknown;
    }

    return CppSharp::CallingConvention::Default;
}

void Parser::WalkFunction(clang::FunctionDecl* FD, CppSharp::Function^ F,
                          bool IsDependent)
{
    using namespace clang;
    using namespace clix;

    assert (FD->getBuiltinID() == 0);

    auto FT = FD->getType()->getAs<FunctionType>();
    auto CC = FT->getCallConv();

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    F->Name = marshalString<E_UTF8>(FD->getNameAsString());
    F->Namespace = NS;
    F->IsVariadic = FD->isVariadic();
    F->IsInline = FD->isInlined();
    F->IsDependent = FD->isDependentContext();
    F->IsPure = FD->isPure();

    auto AbiCC = GetAbiCallConv(CC, FD->isCXXInstanceMember(), FD->isVariadic());
    F->CallingConvention = ConvertCallConv(AbiCC);

    TypeLoc RTL;
    if (auto TSI = FD->getTypeSourceInfo())
    {
        FunctionTypeLoc FTL = TSI->getTypeLoc().getAs<FunctionTypeLoc>();
        RTL = FTL.getResultLoc();

        auto &SM = C->getSourceManager();
        auto headStartLoc = GetDeclStartLocation(C.get(), FD);
        auto headEndLoc = SM.getExpansionLoc(FTL.getLParenLoc());
        auto headRange = clang::SourceRange(headStartLoc, headEndLoc);

        HandlePreprocessedEntities(F, headRange, CppSharp::MacroLocation::FunctionHead);
        HandlePreprocessedEntities(F, FTL.getParensRange(), CppSharp::MacroLocation::FunctionParameters);
        //auto bodyRange = clang::SourceRange(FTL.getRParenLoc(), FD->getLocEnd());
        //HandlePreprocessedEntities(F, bodyRange, CppSharp::MacroLocation::FunctionBody);
    }

    F->ReturnType = GetQualifiedType(FD->getResultType(),
        WalkType(FD->getResultType(), &RTL));

    String Mangled = GetDeclMangledName(FD, TargetABI, IsDependent);
    F->Mangled = marshalString<E_UTF8>(Mangled);

    for(auto it = FD->param_begin(); it != FD->param_end(); ++it)
    {
        ParmVarDecl* VD = (*it);
        
        auto P = gcnew CppSharp::Parameter();
        P->Name = marshalString<E_UTF8>(VD->getNameAsString());

        TypeLoc PTL;
        if (auto TSI = VD->getTypeSourceInfo())
            PTL = VD->getTypeSourceInfo()->getTypeLoc();
        P->QualifiedType = GetQualifiedType(VD->getType(), WalkType(VD->getType(), &PTL));
         
        P->HasDefaultValue = VD->hasDefaultArg();

        F->Parameters->Add(P);
    }
}

CppSharp::Function^ Parser::WalkFunction(clang::FunctionDecl* FD, bool IsDependent,
                                     bool AddToNamespace)
{
    using namespace clang;
    using namespace clix;

    assert (FD->getBuiltinID() == 0);

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    auto Name = marshalString<E_UTF8>(FD->getNameAsString());
    CppSharp::Function^ F = NS->FindFunction(Name, /*Create=*/ false);

    if (F != nullptr)
        return F;

    F = gcnew CppSharp::Function();
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

            auto macro = gcnew CppSharp::MacroDefinition();
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

CppSharp::Variable^ Parser::WalkVariable(clang::VarDecl *VD)
{
    using namespace clang;
    using namespace clix;

    auto Var = gcnew CppSharp::Variable();
    Var->Name = marshalString<E_UTF8>(VD->getName());
    Var->Access = ConvertToAccess(VD->getAccess());

    auto TL = VD->getTypeSourceInfo()->getTypeLoc();
    Var->QualifiedType = GetQualifiedType(VD->getType(), WalkType(VD->getType(), &TL));

    auto Mangled = GetDeclMangledName(VD, TargetABI, /*IsDependent=*/false);
    Var->Mangled = marshalString<E_UTF8>(Mangled);

    return Var;
}

//-----------------------------------//

void Parser::HandleComments(clang::Decl* D, CppSharp::Declaration^ Decl)
{
    using namespace clang;
    using namespace clix;

    // Get the declaration comment.
    std::string BriefText;
    if (const RawComment* Comment = AST->getRawCommentForAnyRedecl(D))
        BriefText = Comment->getBriefText(*AST);

    Decl->BriefComment = marshalString<E_UTF8>(BriefText);

    SourceManager& SM = C->getSourceManager();
    const LangOptions& LangOpts = C->getLangOpts();

    auto Range = CharSourceRange::getTokenRange(D->getSourceRange());

    bool Invalid;
    StringRef DeclText = Lexer::getSourceText(Range, SM, LangOpts, &Invalid);
    //assert(!Invalid && "Should have a valid location");
    
    if (!Invalid)
        Decl->DebugText = marshalString<E_UTF8>(DeclText);
}

//-----------------------------------//

bool Parser::GetPreprocessedEntityText(clang::PreprocessedEntity* PE, std::string& Text)
{
    using namespace clang;
    SourceManager& SM = C->getSourceManager();
    const LangOptions &LangOpts = C->getLangOpts();

    auto Range = CharSourceRange::getTokenRange(PE->getSourceRange());

    bool Invalid;
    Text = Lexer::getSourceText(Range, SM, LangOpts, &Invalid);

    return !Invalid && !Text.empty();
}

void Parser::HandlePreprocessedEntities(CppSharp::Declaration^ Decl,
                                        clang::SourceRange sourceRange,
                                        CppSharp::MacroLocation macroLocation)
{
    using namespace clang;
    auto PPRecord = C->getPreprocessor().getPreprocessingRecord();

    auto Range = PPRecord->getPreprocessedEntitiesInRange(sourceRange);

    for (auto it = Range.first; it != Range.second; ++it)
    {
        PreprocessedEntity* PPEntity = (*it);
        
        CppSharp::PreprocessedEntity^ Entity;
        switch(PPEntity->getKind())
        {
        case PreprocessedEntity::MacroExpansionKind:
        {
            const MacroExpansion* MD = cast<MacroExpansion>(PPEntity);
            Entity = gcnew CppSharp::MacroExpansion();

            std::string Text;
            if (!GetPreprocessedEntityText(PPEntity, Text))
                continue;

            static_cast<CppSharp::MacroExpansion^>(Entity)->Text = 
                clix::marshalString<clix::E_UTF8>(Text);
            break;
        }
        case PreprocessedEntity::MacroDefinitionKind:
        {
            const MacroDefinition* MD = cast<MacroDefinition>(PPEntity);
            Entity = gcnew CppSharp::MacroDefinition();
            break;
        }
        default:
            continue;
        }

        Entity->Location = macroLocation;

        Decl->PreprocessedEntities->Add(Entity);
    }
}

//-----------------------------------//

CppSharp::Declaration^ Parser::WalkDeclarationDef(clang::Decl* D)
{
    return WalkDeclaration(D, /*IgnoreSystemDecls=*/true,
        /*CanBeDefinition=*/true);
}

CppSharp::Declaration^ Parser::WalkDeclaration(clang::Decl* D,
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

    CppSharp::Declaration^ Decl = nullptr;

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

        auto NS = GetNamespace(TD);
        Template->Namespace = NS;
        NS->Templates->Add(Template);
        
        Decl = Template;
        break;
    }
    case Decl::ClassTemplateSpecialization:
    {
        auto TS = cast<ClassTemplateSpecializationDecl>(D);
        auto CT = gcnew CppSharp::ClassTemplateSpecialization();

        Decl = CT;
        break;
    }
    case Decl::ClassTemplatePartialSpecialization:
    {
        auto TS = cast<ClassTemplatePartialSpecializationDecl>(D);
        auto CT = gcnew CppSharp::ClassTemplatePartialSpecialization();

        Decl = CT;
        break;
    }
    case Decl::FunctionTemplate:
    {
        FunctionTemplateDecl* TD = cast<FunctionTemplateDecl>(D);
        auto Template = WalkFunctionTemplate(TD); 

        auto NS = GetNamespace(TD);
        Template->Namespace = NS;
        NS->Templates->Add(Template);

        Decl = Template;
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
        if (!FD->isFirstDeclaration())
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
        NS->Variables->Add(static_cast<CppSharp::Variable^>(Decl));
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
    case Decl::UsingShadow:
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

    if (Decl)
    {
        HandleComments(D, Decl);

        if (const ValueDecl *VD = dyn_cast_or_null<ValueDecl>(D))
            Decl->IsDependent = VD->getType()->isDependentType();
    }

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
        auto Diag = Diagnostic();
        Diag.Location = Info.getLocation();
        Diag.Level = Level;
        Info.FormatDiagnostic(Diag.Message);
        Diagnostics.push_back(Diag);
    }

    std::vector<Diagnostic> Diagnostics;
};

ParserResult^ Parser::ParseHeader(const std::string& File)
{
    auto res = gcnew ParserResult();
    res->Library = Lib;

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

    // Convert the diagnostics to the managed types
    for each (auto& Diag in DiagClient->Diagnostics)
    {
        using namespace clix;

        auto& Source = C->getSourceManager();
        auto FileName = Source.getFilename(Diag.Location);

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

    if(C->getDiagnosticClient().getNumErrors() != 0)
    {
        res->Kind = ParserResultKind::Error;
        return res;
    }

    AST = &C->getASTContext();
    WalkAST();

    res->Kind = ParserResultKind::Success;
    return res;
 }

ParserResultKind Parser::ParseArchive(llvm::StringRef File,
                                      llvm::MemoryBuffer *Buffer)
{
    llvm::error_code Code;
    llvm::object::Archive Archive(Buffer, Code);

    if (Code)
        return ParserResultKind::Error;

    auto LibName = clix::marshalString<clix::E_UTF8>(File);
    auto NativeLib = Lib->FindOrCreateLibrary(LibName);

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
                                        llvm::MemoryBuffer *Buffer)
{
    auto Object = llvm::object::ObjectFile::createObjectFile(Buffer);

    if (!Object)
        return ParserResultKind::Error;

    auto LibName = clix::marshalString<clix::E_UTF8>(File);
    auto NativeLib = Lib->FindOrCreateLibrary(LibName);

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
    res->Library = Lib;

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

        if (FileEntry = FM.getFile(P.str()))
            break;
    }

    if (!FileEntry)
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    res->Kind = ParseArchive(File, FM.getBufferForFile(FileEntry));
    if (res->Kind == ParserResultKind::Success)
        return res;

    res->Kind = ParseSharedLib(File, FM.getBufferForFile(FileEntry));
    if (res->Kind == ParserResultKind::Success)
        return res;

    return res;
}