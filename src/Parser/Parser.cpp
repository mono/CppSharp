/************************************************************************
*
* Cxxi
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "Parser.h"
#include "Interop.h"

#include <llvm/Support/Path.h>
#include <clang/Basic/Version.h>
#include <clang/Config/config.h>
#include <clang/AST/ASTContext.h>
#include <clang/AST/DeclTemplate.h>
#include <clang/Lex/DirectoryLookup.h>
#include <clang/Lex/HeaderSearch.h>
#include <clang/Lex/PreprocessingRecord.h>
#include <clang/Frontend/Utils.h>
#include <clang/Driver/Util.h>

#include <string>
#include <iostream>
#include <sstream>

//-----------------------------------//

Parser::Parser(ParserOptions^ Opts) : Lib(Opts->Library), Index(0)
{
    Setup(Opts);
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
bool GetVisualStudioEnv(const char* env,
                        std::vector<std::string>& pathes,
                        int probeFrom = 0, int probeTo = 0);
#endif

void Parser::Setup(ParserOptions^ Opts)
{
    using namespace clang;
    using namespace clix;

    const char* args[] =
    {
        // Enable C++ language mode
        "-xc++", "-std=c++11", "-fno-rtti",
        // Enable the Microsoft parsing extensions
        "-fms-extensions", "-fms-compatibility", "-fdelayed-template-parsing",
        // Enable the Microsoft ABI
        //"-Xclang", "-cxx-abi", "-Xclang", "microsoft"
    };

    C.reset(new CompilerInstance());
    C->createDiagnostics();

    CompilerInvocation* Inv = new CompilerInvocation();
    CompilerInvocation::CreateFromArgs(*Inv, args, args + ARRAY_SIZE(args),
      C->getDiagnostics());
    C->setInvocation(Inv);

    TargetOptions& TO = Inv->getTargetOpts();
    TO.Triple = llvm::sys::getDefaultTargetTriple();

    TargetInfo* TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), &TO);
    TI->setCXXABI(TargetCXXABI::Microsoft);
    C->setTarget(TI);

    C->createFileManager();
    C->createSourceManager(C->getFileManager());

    if (Opts->Verbose)
        C->getHeaderSearchOpts().Verbose = true;

    for each(System::String^% include in Opts->IncludeDirs)
    {
        String s = marshalString<E_UTF8>(include);
        C->getHeaderSearchOpts().AddPath(s, frontend::Angled, false, false);
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


#if defined(WithClangWindowsSystemIncludeDirsPatch)
    std::vector<std::string> SystemDirs = clang::driver::GetWindowsSystemIncludeDirs();
    clang::HeaderSearchOptions& HSOpts = C->getHeaderSearchOpts();

    for(size_t i = 0; i < SystemDirs.size(); ++i)
    {
        HSOpts.AddPath(SystemDirs[i], frontend::System, false, false, true);
    }
#endif

#ifdef _MSC_VER
    std::vector<std::string> SystemDirs;
    if(GetVisualStudioEnv("INCLUDE", SystemDirs, Opts->ToolSetToUse, Opts->ToolSetToUse))
    {
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

    CanMangle = false;

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
    else if (const BlockDecl *BD = dyn_cast<BlockDecl>(ND))
        MC->mangleBlock(BD, Out);
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

Cxxi::TypeQualifiers GetTypeQualifiers(clang::QualType Type)
{
    Cxxi::TypeQualifiers quals;
    quals.IsConst = Type.isLocalConstQualified();
    quals.IsRestrict = Type.isLocalRestrictQualified();
    quals.IsVolatile = Type.isVolatileQualified();
    return quals;
}

Cxxi::QualifiedType GetQualifiedType(clang::QualType qual, Cxxi::Type^ type)
{
    Cxxi::QualifiedType qualType;
    qualType.Type = type;
    qualType.Qualifiers = GetTypeQualifiers(qual);
    return qualType;
}

//-----------------------------------//

static Cxxi::AccessSpecifier ConvertToAccess(clang::AccessSpecifier AS)
{
    switch(AS)
    {
    case clang::AS_private:
        return Cxxi::AccessSpecifier::Private;
    case clang::AS_protected:
        return Cxxi::AccessSpecifier::Protected;
    case clang::AS_public:
        return Cxxi::AccessSpecifier::Public;
    }

    return Cxxi::AccessSpecifier::Public;
}

static bool HasClassDependentFields(clang::CXXRecordDecl* Record)
{
    using namespace clang;

    for(auto it = Record->field_begin(); it != Record->field_end(); ++it)
    {
        clang::FieldDecl* FD = (*it);

        switch (FD->getType()->getTypeClass()) {
        #define TYPE(Class, Base)
        #define ABSTRACT_TYPE(Class, Base)
        #define NON_CANONICAL_TYPE(Class, Base)
        #define DEPENDENT_TYPE(Class, Base) case Type::Class:
        #include "clang/AST/TypeNodes.def"
            return true;
        }
    }
    return false;
}

Cxxi::Class^ Parser::WalkRecordCXX(clang::CXXRecordDecl* Record, bool IsDependent)
{
    using namespace clang;
    using namespace clix;

    if (Record->hasFlexibleArrayMember())
    {
        assert(0);
        return nullptr;
    }

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

    RC->IsPOD = Record->isPOD();
    RC->IsUnion = Record->isUnion();
    RC->IsAbstract = Record->isAbstract();

    // Iterate through the record ctors.
    for(auto it = Record->ctor_begin(); it != Record->ctor_end(); ++it)
    {
        CXXMethodDecl* Ctor = (*it);
        Cxxi::Method^ Method = WalkMethodCXX(Ctor);
        RC->Methods->Add(Method);
    }

    // Iterate through the record methods.
    for(auto it = Record->method_begin(); it != Record->method_end(); ++it)
    {
        CXXMethodDecl* M = (*it);
        
        if( isa<CXXConstructorDecl>(M) || isa<CXXDestructorDecl>(M) )
            continue;
        
        Cxxi::Method^ Method = WalkMethodCXX(M);
        RC->Methods->Add(Method);
    }

    if (!IsDependent)
        IsDependent = HasClassDependentFields(Record);

    // Get the record layout information.
    const ASTRecordLayout* Layout = 0;
    if (!IsDependent)
        Layout = &C->getASTContext().getASTRecordLayout(Record);

    // Iterate through the record fields.
    for(auto it = Record->field_begin(); it != Record->field_end(); ++it)
    {
        FieldDecl* FD = (*it);
        
        Cxxi::Field^ Field = WalkFieldCXX(FD, RC);
        
        if (Layout)
            Field->Offset = Layout->getFieldOffset(FD->getFieldIndex());

        RC->Fields->Add(Field);
    }

    // Iterate through the record bases.
    for(auto it = Record->bases_begin(); it != Record->bases_end(); ++it)
    {
        clang::CXXBaseSpecifier &BS = *it;

        Cxxi::BaseClassSpecifier^ Base = gcnew Cxxi::BaseClassSpecifier();
        Base->Access = ConvertToAccess(BS.getAccessSpecifier());
        Base->IsVirtual = BS.isVirtual();
        Base->Type = WalkType(BS.getType(), &BS.getTypeSourceInfo()->getTypeLoc());

        RC->Bases->Add(Base);
    }

    //Debug("Size: %I64d\n", Layout.getSize().getQuantity());

    return RC;
}

//-----------------------------------//

Cxxi::ClassTemplate^ Parser::WalkClassTemplate(clang::ClassTemplateDecl* TD)
{
    using namespace clang;
    using namespace clix;

    auto NS = GetNamespace(TD);

    auto Class = WalkRecordCXX(TD->getTemplatedDecl(), /*IsDependent*/true);
    Cxxi::ClassTemplate^ CT = gcnew Cxxi::ClassTemplate(Class);

    return CT;
}

//-----------------------------------//

Cxxi::FunctionTemplate^ Parser::WalkFunctionTemplate(clang::FunctionTemplateDecl* TD)
{
    using namespace clang;
    using namespace clix;

    auto NS = GetNamespace(TD);

    auto Function = WalkFunction(TD->getTemplatedDecl(), /*IsDependent=*/true);
    Cxxi::FunctionTemplate^ FT = gcnew Cxxi::FunctionTemplate(Function);

    return FT;
}

//-----------------------------------//

static Cxxi::CXXMethodKind GetMethodKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    switch(Name.getNameKind())
    {
    case DeclarationName::Identifier:
    case DeclarationName::ObjCZeroArgSelector:
    case DeclarationName::ObjCOneArgSelector:
    case DeclarationName::ObjCMultiArgSelector:
        return Cxxi::CXXMethodKind::Normal;
    case DeclarationName::CXXConstructorName:
        return Cxxi::CXXMethodKind::Constructor;
    case DeclarationName::CXXDestructorName:
        return Cxxi::CXXMethodKind::Destructor;
    case DeclarationName::CXXConversionFunctionName:
        return Cxxi::CXXMethodKind::Conversion;
    case DeclarationName::CXXOperatorName:
    case DeclarationName::CXXLiteralOperatorName:
        return Cxxi::CXXMethodKind::Operator;
    case DeclarationName::CXXUsingDirective:
        return Cxxi::CXXMethodKind::UsingDirective;
    }
    return Cxxi::CXXMethodKind::Normal;
}

static Cxxi::CXXOperatorKind GetOperatorKindFromDecl(clang::DeclarationName Name)
{
    using namespace clang;

    if (Name.getNameKind() != DeclarationName::CXXOperatorName)
        return Cxxi::CXXOperatorKind::None;

    switch(Name.getCXXOverloadedOperator())
    {
    #define OVERLOADED_OPERATOR(Name,Spelling,Token,Unary,Binary,MemberOnly) \
    case OO_##Name: return Cxxi::CXXOperatorKind::Name;
    #include "clang/Basic/OperatorKinds.def"
    }

    return Cxxi::CXXOperatorKind::None;
}

Cxxi::Method^ Parser::WalkMethodCXX(clang::CXXMethodDecl* MD)
{
    using namespace clang;

    DeclarationName Name = MD->getDeclName();

    Cxxi::Method^ Method = gcnew Cxxi::Method();
    Method->Access = ConvertToAccess(MD->getAccess());
    Method->Kind = GetMethodKindFromDecl(Name);
    Method->OperatorKind = GetOperatorKindFromDecl(Name);

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

Cxxi::Field^ Parser::WalkFieldCXX(clang::FieldDecl* FD, Cxxi::Class^ Class)
{
    using namespace clang;
    using namespace clix;

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    Cxxi::Field^ F = gcnew Cxxi::Field();
    F->Namespace = NS;

    F->Name = marshalString<E_UTF8>(FD->getName());
    auto TL = FD->getTypeSourceInfo()->getTypeLoc();
    F->QualifiedType = GetQualifiedType(FD->getType(), WalkType(FD->getType(), &TL));
    F->Access = ConvertToAccess(FD->getAccess());
    F->Class = Class;

    HandleComments(FD, F);

    return F;
}

//-----------------------------------//

Cxxi::Namespace^ Parser::GetNamespace(const clang::NamedDecl* ND)
{
    using namespace clang;
    using namespace clix;

    SourceLocation Loc = ND->getLocation();
    Cxxi::TranslationUnit^ M = GetModule(Loc);

    // If the declaration is at global scope, just early exit.
    const DeclContext *Ctx = ND->getDeclContext();
    if (Ctx->isTranslationUnit())
        return M;

    // Else we need to do a more expensive check to get all the namespaces,
    // and then perform a reverse iteration to get the namespaces in order.
    typedef SmallVector<const DeclContext *, 8> ContextsTy;
    ContextsTy Contexts;

    for(; Ctx != nullptr; Ctx = Ctx->getParent())
        Contexts.push_back(Ctx);

    assert(Contexts.back()->isTranslationUnit());
    Contexts.pop_back();

    Cxxi::Namespace^ NS = M;

    for (auto I = Contexts.rbegin(), E = Contexts.rend(); I != E; ++I)
    {
        const DeclContext* Ctx = *I;
        
        switch(Ctx->getDeclKind())
        {
        case Decl::Namespace:
        {
            const NamespaceDecl* ND = cast<NamespaceDecl>(Ctx);
            if (ND->isAnonymousNamespace())
                continue;
            auto Name = marshalString<E_UTF8>(ND->getName());
            NS = NS->FindCreateNamespace(Name, NS);
            break;
        }
        case Decl::LinkageSpec:
        {
            const LinkageSpecDecl* LD = cast<LinkageSpecDecl>(Ctx);
            continue;
        }
        case Decl::CXXRecord:
        {
            // FIXME: Ignore record namespaces...
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

    return NS;
}

static Cxxi::PrimitiveType WalkBuiltinType(const clang::BuiltinType* Builtin)
{
    using namespace Cxxi;

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

Cxxi::Type^ Parser::WalkType(clang::QualType QualType, clang::TypeLoc* TL,
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
    
        auto BT = gcnew Cxxi::BuiltinType();
        BT->Type = WalkBuiltinType(Builtin);
        
        return BT;
    }
    case Type::Enum:
    {
        auto ET = Type->getAs<clang::EnumType>();
        EnumDecl* ED = ET->getDecl();

        //auto Name = marshalString<E_UTF8>(GetTagDeclName(ED));

        auto TT = gcnew Cxxi::TagType();
        TT->Declaration = WalkDeclaration(ED, 0, /*IgnoreSystemDecls=*/false);

        return TT;
    }
    case Type::Pointer:
    {
        auto Pointer = Type->getAs<clang::PointerType>();
        
        auto P = gcnew Cxxi::PointerType();
        P->Modifier = Cxxi::PointerType::TypeModifier::Pointer;

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
        auto TDD = safe_cast<Cxxi::TypedefDecl^>(WalkDeclaration(TD, &TTL,
            /*IgnoreSystemDecls=*/false));

        auto Type = gcnew Cxxi::TypedefType();
        Type->Declaration = TDD;

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

        auto TT = gcnew Cxxi::TagType();
        TT->Declaration = WalkDeclaration(RD, 0, /*IgnoreSystemDecls=*/false);

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

        auto A = gcnew Cxxi::ArrayType();
        auto Next = TL->getNextTypeLoc();
        A->Type = WalkType(AT->getElementType(), &Next);
        A->SizeType = Cxxi::ArrayType::ArraySize::Constant;
        A->Size = AST->getConstantArrayElementCount(AT);

        return A;
    }
    case Type::FunctionProto:
    {
        auto FP = Type->getAs<clang::FunctionProtoType>();

        auto FTL = TL->getAs<FunctionProtoTypeLoc>();
        auto RL = FTL.getResultLoc();

        auto F = gcnew Cxxi::FunctionType();
        F->ReturnType = WalkType(FP->getResultType(), &RL);

        for (unsigned i = 0; i < FP->getNumArgs(); ++i)
        {
            auto FA = gcnew Cxxi::Parameter();

            auto PVD = FTL.getArg(i);
            auto PTL = PVD->getTypeSourceInfo()->getTypeLoc();

            FA->Name = marshalString<E_UTF8>(PVD->getNameAsString());
            FA->QualifiedType = GetQualifiedType(PVD->getType(), WalkType(PVD->getType(), &PTL));

            F->Arguments->Add(FA);
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

        auto MPT = gcnew Cxxi::MemberPointerType();
        MPT->Pointee = WalkType(MP->getPointeeType(), &Next);
        
        return MPT;
    }
    case Type::TemplateSpecialization:
    {
        auto TS = Type->getAs<clang::TemplateSpecializationType>();
        auto TST = gcnew Cxxi::TemplateSpecializationType();
        
        TemplateName Name = TS->getTemplateName();
        TST->Template = safe_cast<Cxxi::Template^>(WalkDeclaration(
            Name.getAsTemplateDecl(), 0, /*IgnoreSystemDecls=*/false));
        
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
            auto Arg = Cxxi::TemplateArgument();

            TemplateArgumentLoc ArgLoc;
            ArgLoc = TSTL.getArgLoc(I);

            switch(TA.getKind())
            {
            case TemplateArgument::Type:
            {
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::Type;
                TypeLoc ArgTL;
                ArgTL = ArgLoc.getTypeSourceInfo()->getTypeLoc();
                Arg.Type = GetQualifiedType(TA.getAsType(), WalkType(TA.getAsType(), &ArgTL));
                break;
            }
            case TemplateArgument::Declaration:
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::Declaration;
                Arg.Declaration = WalkDeclaration(TA.getAsDecl(), 0);
                break;
            case TemplateArgument::NullPtr:
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::NullPtr;
                break;
            case TemplateArgument::Integral:
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::Integral;
                //Arg.Type = WalkType(TA.getIntegralType(), 0);
                Arg.Integral = TA.getAsIntegral().getLimitedValue();
                break;
            case TemplateArgument::Template:
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::Template;
                break;
            case TemplateArgument::TemplateExpansion:
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::TemplateExpansion;
                break;
            case TemplateArgument::Expression:
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::Expression;
                break;
            case TemplateArgument::Pack:
                Arg.Kind = Cxxi::TemplateArgument::ArgumentKind::Pack;
                break;
            }

            TST->Arguments->Add(Arg);
        }

        return TST;
    }
    case Type::TemplateTypeParm:
    {
        auto TP = Type->getAs<TemplateTypeParmType>();
        auto TPT = gcnew Cxxi::TemplateParameterType();
        //TPT->Parameter = WalkDeclaration(TP->getDecl());

        return TPT;
    }
    case Type::InjectedClassName:
    {
        auto MYIN = Type->getAs<InjectedClassNameType>();
        return nullptr;
    }
    case Type::DependentName:
    {
        auto DN = Type->getAs<DependentNameType>();
        return nullptr;
    }
    case Type::LValueReference:
    {
        auto LR = Type->getAs<clang::LValueReferenceType>();

        auto P = gcnew Cxxi::PointerType();
        P->Modifier = Cxxi::PointerType::TypeModifier::LVReference;

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

        auto P = gcnew Cxxi::PointerType();
        P->Modifier = Cxxi::PointerType::TypeModifier::RVReference;

        TypeLoc Next;
        if (!TL->isNull())
            Next = TL->getNextTypeLoc();

        auto Pointee = LR->getPointeeType();
        P->QualifiedPointee = GetQualifiedType(Pointee, WalkType(Pointee, &Next));

        return P;
    }
    default:
    {   
        Debug("Unhandled type class '%s'\n", Type->getTypeClassName());
        return nullptr;
    } }
}

//-----------------------------------//

Cxxi::Enumeration^ Parser::WalkEnum(clang::EnumDecl* ED)
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
        E->Modifiers |= Cxxi::Enumeration::EnumModifiers::Scoped;

    // Get the underlying integer backing the enum.
    QualType IntType = ED->getIntegerType();
    E->Type = WalkType(IntType, 0);
    E->BuiltinType = safe_cast<Cxxi::BuiltinType^>(WalkType(IntType, 0,
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

        auto EnumItem = gcnew Cxxi::Enumeration::Item();
        EnumItem->Name = marshalString<E_UTF8>(ECD->getNameAsString());
        EnumItem->Value = (int) ECD->getInitVal().getLimitedValue();
        EnumItem->Comment = marshalString<E_UTF8>(BriefText);
        //EnumItem->ExplicitValue = ECD->getExplicitValue();

        E->AddItem(EnumItem);
    }

    return E;
}

//-----------------------------------//

static Cxxi::CallingConvention ConvertCallConv(clang::CallingConv CC)
{
    using namespace clang;

    switch(CC)
    {
    case CC_Default:
    case CC_C:
        return Cxxi::CallingConvention::C;
    case CC_X86StdCall:
        return Cxxi::CallingConvention::StdCall;
    case CC_X86FastCall:
        return Cxxi::CallingConvention::FastCall;
    case CC_X86ThisCall:
        return Cxxi::CallingConvention::ThisCall;
    case CC_X86Pascal:
    case CC_AAPCS:
    case CC_AAPCS_VFP:
        return Cxxi::CallingConvention::Unknown;
    }

    return Cxxi::CallingConvention::Default;
}

void Parser::WalkFunction(clang::FunctionDecl* FD, Cxxi::Function^ F,
                          bool IsDependent)
{
    using namespace clang;
    using namespace clix;

    auto FT = FD->getType()->getAs<FunctionType>();
    auto CC = FT->getCallConv();

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    F->Name = marshalString<E_UTF8>(FD->getNameAsString());
    F->Namespace = NS;
    F->IsVariadic = FD->isVariadic();
    F->IsInline = FD->isInlined();
    F->CallingConvention = ConvertCallConv(CC);

    TypeLoc RTL;
    if (auto TSI = FD->getTypeSourceInfo())
    {
       TypeLoc TL = TSI->getTypeLoc();
       RTL = TL.getAs<FunctionTypeLoc>().getResultLoc();
    }
    F->ReturnType = WalkType(FD->getResultType(), &RTL);

    String Mangled = GetDeclMangledName(FD, TargetCXXABI::Microsoft, IsDependent);
    F->Mangled = marshalString<E_UTF8>(Mangled);

    for(auto it = FD->param_begin(); it != FD->param_end(); ++it)
    {
        ParmVarDecl* VD = (*it);
        
        auto P = gcnew Cxxi::Parameter();
        P->Name = marshalString<E_UTF8>(VD->getNameAsString());

        TypeLoc PTL;
        if (auto TSI = VD->getTypeSourceInfo())
            PTL = VD->getTypeSourceInfo()->getTypeLoc();
        P->QualifiedType = GetQualifiedType(VD->getType(), WalkType(VD->getType(), &PTL));
         
        P->HasDefaultValue = VD->hasDefaultArg();

        F->Parameters->Add(P);
    }
}

Cxxi::Function^ Parser::WalkFunction(clang::FunctionDecl* FD, bool IsDependent)
{
    using namespace clang;
    using namespace clix;

    auto NS = GetNamespace(FD);
    assert(NS && "Expected a valid namespace");

    auto Name = marshalString<E_UTF8>(FD->getNameAsString());
    Cxxi::Function^ F = NS->FindFunction(Name, /*Create=*/ false);

    if (F != nullptr)
        return F;

    F = gcnew Cxxi::Function();
    WalkFunction(FD, F, IsDependent);
    NS->Functions->Add(F);

    return F;
}

//-----------------------------------//

static bool IsUserLocation(clang::SourceManager& SM, clang::SourceLocation Loc)
{
    auto Kind = SM.getFileCharacteristic(Loc);
    return Kind == clang::SrcMgr::C_User;
}

bool Parser::IsValidDeclaration(const clang::SourceLocation& Loc)
{
    using namespace clang;

    SourceManager& SM = C->getSourceManager();
    PresumedLoc PLoc = SM.getPresumedLoc(Loc);

    const char *FileName = PLoc.getFilename();

    // Igore built in declarations.
    if(PLoc.isInvalid() || !strcmp(FileName, "<built-in>"))
        return false;

    // Also ignore declarations that come from system headers.
    if (!IsUserLocation(SM, Loc))
        return false;

    return true;
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

Cxxi::TranslationUnit^ Parser::GetModule(clang::SourceLocation Loc)
{
    using namespace clang;
    using namespace clix;

    SourceManager& SM = C->getSourceManager();

    if (Loc.isMacroID())
        Loc = SM.getExpansionLoc(Loc);

    StringRef File = SM.getFilename(Loc);

    if (!File.data() || File.empty())
    {
        assert(0 && "Expected to find a valid file");
        return nullptr;
    }

    auto Unit = Lib->FindOrCreateModule(marshalString<E_UTF8>(File));
    Unit->IsSystemHeader = SM.isInSystemHeader(Loc);

    return Unit;

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

            if (!IsUserLocation(SM, Loc))
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

            auto macro = gcnew Cxxi::MacroDefinition();
            macro->Name = marshalString<E_UTF8>(II->getName())->Trim();
            macro->Expression = marshalString<E_UTF8>(Expression)->Trim();

            auto M = GetModule(BeginExpr);
            M->Macros->Add(macro);

            break;
        }
        default: break;
        }
    }
}

//-----------------------------------//

Cxxi::Variable^ Parser::WalkVariable(clang::VarDecl *VD)
{
    using namespace clang;
    using namespace clix;

    auto Var = gcnew Cxxi::Variable();
    Var->Name = marshalString<E_UTF8>(VD->getName());

    return Var;
}

//-----------------------------------//

void Parser::HandleComments(clang::Decl* D, Cxxi::Declaration^ Decl)
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

Cxxi::Declaration^ Parser::WalkDeclarationDef(clang::Decl* D)
{
    return WalkDeclaration(D, 0, /*IgnoreSystemDecls=*/true,
        /*CanBeDefinition=*/true);
}

Cxxi::Declaration^ Parser::WalkDeclaration(clang::Decl* D, clang::TypeLoc* TL,
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

    Cxxi::Declaration^ Decl;

    auto Kind = D->getKind();
    switch(D->getKind())
    {
    case Decl::CXXRecord:
    {
        CXXRecordDecl* RD = cast<CXXRecordDecl>(D);

        auto Class = WalkRecordCXX(RD);
        HandleComments(RD, Class);

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

        auto CT = gcnew Cxxi::ClassTemplateSpecialization();

        Decl = CT;

        break;
    }
    case Decl::ClassTemplatePartialSpecialization:
    {
        auto TS = cast<ClassTemplatePartialSpecializationDecl>(D);
        auto CT = gcnew Cxxi::ClassTemplatePartialSpecialization();
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

        auto E = WalkEnum(ED);
        HandleComments(ED, E);

        Decl = E;
        
        break;
    }
    case Decl::Function:
    {
        FunctionDecl* FD = cast<FunctionDecl>(D);
        if (!FD->isFirstDeclaration())
            break;

        auto F = WalkFunction(FD);
        HandleComments(FD, F);

        Decl = F;
        
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

        auto V = WalkVariable(VD);
        HandleComments(VD, V);

        Decl = V;

        break;
    }
    default:
    {
        Debug("Unhandled declaration kind: %s\n", D->getDeclKindName());
        break;
    } };

    return Decl;
}

//-----------------------------------//

struct ParseConsumer : public clang::ASTConsumer
{
    virtual ~ParseConsumer() { }
    virtual bool HandleTopLevelDecl(clang::DeclGroupRef) { return true; }
};

struct Diagnostic
{
    clang::SourceLocation Location;
    llvm::SmallString<100> Message;
};

struct DiagnosticConsumer : public clang::DiagnosticConsumer
{
    virtual ~DiagnosticConsumer() { }

    virtual void HandleDiagnostic(clang::DiagnosticsEngine::Level Level,
                                  const clang::Diagnostic& Info) override {
        auto Diag = Diagnostic();
        Diag.Location = Info.getLocation();
        Info.FormatDiagnostic(Diag.Message);
        Diagnostics.push_back(Diag);
    }

    virtual
    DiagnosticConsumer* clone(clang::DiagnosticsEngine& Diags) const override {
        return new DiagnosticConsumer();
    }

    std::vector<Diagnostic> Diagnostics;
};

ParserResult^ Parser::Parse(const std::string& File)
{
    auto res = gcnew ParserResult();
    res->Library = Lib;

    if (File.empty())
    {
        res->Kind = ParserResultKind::FileNotFound;
        return res;
    }

    C->setASTConsumer(new ParseConsumer());

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
    ParseAST(C->getPreprocessor(), &C->getASTConsumer(), C->getASTContext(),
        /*PrintStats=*/false, clang::TU_Complete, 0,
        /*SkipFunctionBodies=*/true);
    client->EndSourceFile();

    // Convert the diagnostics to the managed types
    for(auto& Diag : DiagClient->Diagnostics)
    {
        using namespace clix;

        auto& Source = C->getSourceManager();
        auto FileName = Source.getFilename(Diag.Location);

        auto PDiag = ParserDiagnostic();
        PDiag.FileName = marshalString<E_UTF8>(FileName.str());
        PDiag.Message = marshalString<E_UTF8>(Diag.Message.str());
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