/************************************************************************
*
* Flush3D <http://www.flush3d.com> © (2008-201x) 
* Licensed under the LGPL 2.1 (GNU Lesser General Public License)
*
************************************************************************/

#include "Parser.h"

#include <llvm/Support/Path.h>
#include <clang/Basic/Version.h>
#include <clang/Config/config.h>
#include "clang/AST/ASTContext.h"
#include "clang/Lex/HeaderSearch.h"
#include <clang/Lex/PreprocessingRecord.h>
#include "clang/Frontend/HeaderSearchOptions.h"
#include <clang/Frontend/Utils.h>
#include <clang/Driver/Util.h>

#include "Interop.h"
#include <string>

//-----------------------------------//

Parser::Parser(ParserOptions^ Opts) : Lib(Opts->Library)
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

void Parser::Setup(ParserOptions^ Opts)
{
    using namespace clang;

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
    C->createDiagnostics(ARRAY_SIZE(args), args);

    CompilerInvocation* Inv = new CompilerInvocation();
    CompilerInvocation::CreateFromArgs(*Inv,
        args,  args + ARRAY_SIZE(args), C->getDiagnostics());
    C->setInvocation(Inv);

    TargetOptions& TO = Inv->getTargetOpts();
    TO.Triple = llvm::sys::getDefaultTargetTriple();

    TargetInfo* TI = TargetInfo::CreateTargetInfo(C->getDiagnostics(), TO);
    TI->setCXXABI(CXXABI_Microsoft);
    C->setTarget(TI);

    C->createFileManager();
    C->createSourceManager(C->getFileManager());

	if (Opts->Verbose)
		C->getHeaderSearchOpts().Verbose = true;

    // Initialize the default platform headers.
    std::string ResourceDir = GetClangResourceDir(".");
    C->getHeaderSearchOpts().ResourceDir = ResourceDir;
    C->getHeaderSearchOpts().AddPath(GetClangBuiltinIncludeDir(),
        clang::frontend::System, false, false, true);

#ifdef _WIN32
    std::vector<std::string> SystemDirs = clang::driver::GetWindowsSystemIncludeDirs();
    clang::HeaderSearchOptions& HSOpts = C->getHeaderSearchOpts();

    for(size_t i = 0; i < SystemDirs.size(); ++i) {
        HSOpts.AddPath(SystemDirs[i], frontend::System, false, false, true);
    }
#endif

    C->createPreprocessor();
    C->createASTContext();

    if (C->hasPreprocessor())
    {
        Preprocessor& P = C->getPreprocessor();
        P.createPreprocessingRecord(false /*RecordConditionals*/);
        P.getBuiltinInfo().InitializeBuiltins(P.getIdentifierTable(), P.getLangOpts());
    }
}

//-----------------------------------//

std::string Parser::GetDeclMangledName(clang::Decl* D, clang::TargetCXXABI ABI)
{
    using namespace clang;

    if(!D || !isa<NamedDecl>(D))
        return "";

    bool CanMangle = isa<FunctionDecl>(D) || isa<VarDecl>(D)
        || isa<CXXConstructorDecl>(D) || isa<CXXDestructorDecl>(D);

    if (!CanMangle) return "";

    NamedDecl* ND = cast<NamedDecl>(D);
    llvm::OwningPtr<MangleContext> MC;
    
    switch(ABI)
    {
    default:
        llvm_unreachable("Unknown mangling ABI");
        break;
    case CXXABI_Itanium:
       MC.reset(createItaniumMangleContext(*AST, AST->getDiagnostics()));
       //AST->setCXXABI(CreateItaniumCXXABI(*AST));
       break;
    case CXXABI_Microsoft:
       MC.reset(createMicrosoftMangleContext(*AST, AST->getDiagnostics()));
       //AST->setCXXABI(CreateMicrosoftCXXABI(*AST));
       break;
    }

    std::string Mangled;
    llvm::raw_string_ostream Out(Mangled);

    if (!MC->shouldMangleDeclName(ND))
    {
        IdentifierInfo *II = ND->getIdentifier();
        return II->getName();
    }

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

static std::string GetTagDeclName(const clang::TagDecl* D)
{
    using namespace clang;

    if (const IdentifierInfo *II = D->getIdentifier())
        return II->getName();
    else if (TypedefNameDecl *Typedef = D->getTypedefNameForAnonDecl())
    {
        assert(Typedef->getIdentifier() && "Typedef without identifier?");
        return Typedef->getIdentifier()->getName();
    }
    else
        return D->getNameAsString();

    assert(0 && "Expected to have a name");
    return std::string();
}

std::string Parser::GetTypeBindName(const clang::Type* Type)
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
    //pp.SuppressSpecifiers = true;

    std::string TypeName;
    QualType::getAsStringInternal(Type, Qualifiers(), TypeName, pp);

    return std::string();
}

//-----------------------------------//

Cxxi::Class^ Parser::WalkRecordCXX(clang::CXXRecordDecl* Record)
{
    using namespace clang;
    using namespace clix;

    if (Record->isAnonymousStructOrUnion())
        return nullptr;

    auto RC = gcnew Cxxi::Class();
    RC->Name = marshalString<E_UTF8>(GetTagDeclName(Record));
    RC->IsPOD = Record->isPOD();

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

    // Iterate through the record fields.
    for(auto it = Record->field_begin(); it != Record->field_end(); ++it)
    {
        FieldDecl* FD = (*it);
        Cxxi::Field^ Field = WalkFieldCXX(FD);
        RC->Fields->Add(Field);
    }

    // Get the record layout information.
    const ASTRecordLayout& Layout = C->getASTContext().getASTRecordLayout(Record);
    //Debug("Size: %I64d\n", Layout.getSize().getQuantity());

    return RC;
}

//-----------------------------------//

Cxxi::Method^ Parser::WalkMethodCXX(clang::CXXMethodDecl* Method)
{
    using namespace clang;

    DeclarationName MethodName = Method->getDeclName();

    Debug("Method: %s\n", MethodName.getAsString().c_str());

    // Write the return type.
    QualType Return = Method->getResultType();

    for(auto it = Method->param_begin(); it != Method->param_end(); ++it)
    {
        ParmVarDecl* Parm = (*it);

        QualType ParmType = Parm->getType();
        std::string ParmTypeName = GetTypeBindName(ParmType.getTypePtr());
        
        Debug("\tParameter: %s %s\n",
            ParmTypeName.c_str(), Parm->getName().str().c_str());
    }

    std::string Mangled = GetDeclMangledName(Method, CXXABI_Microsoft);
    Debug("\tMangling: %s\n", Mangled.c_str());

    return nullptr;
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

Cxxi::Field^ Parser::WalkFieldCXX(clang::FieldDecl* FD)
{
    using namespace clang;
    using namespace clix;

    Cxxi::Field^ F = gcnew Cxxi::Field();
    F->Name = marshalString<E_UTF8>(FD->getName());
    F->Type = ConvertTypeToCLR(FD->getType());
    F->Access = ConvertToAccess(FD->getAccess());

    HandleComments(FD, F);

    return F;
}

//-----------------------------------//

static Cxxi::PrimitiveType ConvertBuiltinTypeToCLR(const clang::BuiltinType* Builtin)
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

Cxxi::Type^ Parser::ConvertTypeToCLR(clang::QualType QualType)
{
    using namespace clang;
    using namespace clix;

    if (QualType.isNull())
        return nullptr;

    clang::QualType Desugared = QualType.getDesugaredType(*AST);

    if (Desugared.isNull())
        return nullptr;

    const clang::Type* Type = QualType.getTypePtr();
    assert(Type && "Expected a valid type");

    switch(Type->getTypeClass())
    {
    case Type::Builtin:
    {
        auto Builtin = Type->getAs<clang::BuiltinType>();
        assert(Builtin && "Expected a builtin type");
    
        auto BT = gcnew Cxxi::BuiltinType();
        BT->Type = ConvertBuiltinTypeToCLR(Builtin);
        
        return BT;
    }
    case Type::Enum:
    {
        auto ET = Type->getAs<clang::EnumType>();
        const EnumDecl* ED = ET->getDecl();

        // Assume that the type has already been defined for now.
        String Name(GetTagDeclName(ED));
        auto E = Lib->FindEnum(marshalString<E_UTF8>(Name));

        auto TT = gcnew Cxxi::TagType();
        TT->Declaration = E;

        return TT;
    }
    case Type::Pointer:
    {
        auto Pointer = Type->getAs<clang::PointerType>();
        
        auto P = gcnew Cxxi::PointerType();
        P->Modifier = Cxxi::PointerType::TypeModifier::Pointer;
        P->Pointee = ConvertTypeToCLR(Pointer->getPointeeType());

        return P;
    }
    case Type::Typedef:
    {
        auto TT = Type->getAs<clang::TypedefType>();
        const TypedefNameDecl* TND = TT->getDecl();

        return ConvertTypeToCLR(TND->getUnderlyingType());
    }
    case Type::Elaborated:
    {
        auto ET = Type->getAs<clang::ElaboratedType>();
        return ConvertTypeToCLR(ET->getNamedType());
    }
    case Type::Record:
    {
        auto RT = Type->getAs<clang::RecordType>();
        const RecordDecl* RD = RT->getDecl();

        // Assume that the type has already been defined for now.
        String Name(GetTagDeclName(RD));
        auto D = Lib->FindClass(marshalString<E_UTF8>(Name));

        auto TT = gcnew Cxxi::TagType();
        TT->Declaration = D;

        return TT;
    }
    case Type::Paren:
    {
        auto PT = Type->getAs<clang::ParenType>();
        return ConvertTypeToCLR(PT->getInnerType());
    }
    case Type::ConstantArray:
    {
        auto AT = AST->getAsConstantArrayType(QualType);

        auto A = gcnew Cxxi::ArrayType();
        A->Type = ConvertTypeToCLR(AT->getElementType());
        A->SizeType = Cxxi::ArrayType::ArraySize::Constant;
        A->Size = AST->getConstantArrayElementCount(AT);

        return A;
    }
    case Type::FunctionProto:
    {
        auto FP = Type->getAs<clang::FunctionProtoType>();

        auto F = gcnew Cxxi::FunctionType();
        F->ReturnType = ConvertTypeToCLR(FP->getResultType());

        return F;
    }
    case Type::TypeOf:
    {
        auto TO = Type->getAs<clang::TypeOfType>();
        return ConvertTypeToCLR(TO->getUnderlyingType());
    }
    case Type::TypeOfExpr:
    {
        auto TO = Type->getAs<clang::TypeOfExprType>();
        return ConvertTypeToCLR(TO->getUnderlyingExpr()->getType());
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

    auto E = gcnew Cxxi::Enumeration();
    E->Name = marshalString<E_UTF8>(GetTagDeclName(ED));

    if (ED->isScoped())
        E->Modifiers |= Cxxi::Enumeration::EnumModifiers::Scoped;

    // Get the underlying integer backing the enum.
    QualType IntType = ED->getIntegerType();
    E->Type = safe_cast<Cxxi::BuiltinType^>(ConvertTypeToCLR(IntType));

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

Cxxi::Function^ Parser::WalkFunction(clang::FunctionDecl* FD)
{
    using namespace clang;
    using namespace clix;

    auto F = gcnew Cxxi::Function();
    F->Name = marshalString<E_UTF8>(FD->getNameAsString());
    F->IsVariadic = FD->isVariadic();
    F->IsInline = FD->isInlined();
    F->CallingConvention = Cxxi::CallingConvention::Default;
    F->ReturnType = ConvertTypeToCLR(FD->getResultType());

    for(auto it = FD->param_begin(); it != FD->param_end(); ++it)
    {
         ParmVarDecl* VD = (*it);
         
         auto P = gcnew Cxxi::Parameter();
         P->Name = marshalString<E_UTF8>(VD->getNameAsString());
         P->Type = ConvertTypeToCLR(VD->getType());
         P->HasDefaultValue = VD->hasDefaultArg();

         F->Parameters->Add(P);
    }

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

    // Igore built in declarations.
    if(PLoc.isInvalid() || !strcmp(PLoc.getFilename(), "<built-in>"))
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
        assert(PR && "Expected a valid preprocessing record");

        WalkMacros(PR);
    }

    TranslationUnitDecl* TU = AST->getTranslationUnitDecl();

    for(auto it = TU->decls_begin(); it != TU->decls_end(); ++it)
    {
        Decl* D = (*it);
        WalkDeclaration(D);
    }
}

//-----------------------------------//

Cxxi::Module^ Parser::GetModule(clang::SourceLocation Loc)
{
    using namespace clang;
    using namespace clix;

    SourceManager& SM = C->getSourceManager();
    StringRef File = SM.getFilename(Loc);

    return Lib->FindOrCreateModule(marshalString<E_UTF8>(File));
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
            StringRef Expression = Lexer::getSourceText(Range, SM, LangOpts, &Invalid);

            if (Invalid || Expression.empty())
                break;

            auto macro = gcnew Cxxi::MacroDefine();
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
    assert(!Invalid && "Should have a valid location");

    Decl->DebugText = marshalString<E_UTF8>(DeclText);
}

//-----------------------------------//

void Parser::WalkDeclaration(clang::Decl* D)
{
    using namespace clang;

    if (!IsValidDeclaration(D->getLocation()))
        return;

    if(NamedDecl* ND = dyn_cast<NamedDecl>(D))
    {
        const char* KindName = D->getDeclKindName();
            
        DeclarationName DN = ND->getDeclName();
        std::string DeclName = DN.getAsString();

        //Debug("(%s) %s\n", KindName, DeclName.c_str());

        std::string Mangled = GetDeclMangledName(ND, CXXABI_Microsoft);
    }

    for(auto it = D->attr_begin(); it != D->attr_end(); ++it)
    {
        Attr* Attr = (*it);

        if(Attr->getKind() != clang::attr::Annotate)
            continue;

        AnnotateAttr* Annotation = cast<AnnotateAttr>(Attr);
        assert(Annotation != nullptr);

        StringRef AnnotationText = Annotation->getAnnotation();
    }

    using namespace clix;

    switch(D->getKind())
    {
    case Decl::CXXRecord:
    {
        CXXRecordDecl* RD = cast<CXXRecordDecl>(D);
        if (!RD->isThisDeclarationADefinition())
            break;

        auto Class = WalkRecordCXX(RD);
        HandleComments(RD, Class);

        auto M = GetModule(RD->getLocation());
        M->Global->Classes->Add(Class);
        
        break;
    }
    case Decl::Enum:
    {
        EnumDecl* ED = cast<EnumDecl>(D);
        if (!ED->isThisDeclarationADefinition())
            break;

        auto E = WalkEnum(ED);
        HandleComments(ED, E);

        auto M = GetModule(ED->getLocation());
        M->Global->Enums->Add(E);
        
        break;
    }
    case Decl::Function:
    {
        FunctionDecl* FD = cast<FunctionDecl>(D);
        if (!FD->isFirstDeclaration())
            break;

        auto F = WalkFunction(FD);
        HandleComments(FD, F);

        auto M = GetModule(FD->getLocation());
        M->Global->Functions->Add(F);
        
        break;
    }
    case Decl::LinkageSpec:
    {
        LinkageSpecDecl* LS = cast<LinkageSpecDecl>(D);
        
        for (auto it = LS->decls_begin(); it != LS->decls_end(); ++it)
        {
            Decl* D = (*it);
            WalkDeclaration(D);
        }
        
        break;
    }
    case Decl::Typedef:
    {
        TypedefDecl* TD = cast<TypedefDecl>(D);
        
        const QualType& Type = TD->getUnderlyingType();
        std::string Name = GetTypeBindName(Type.getTypePtr());

        if (Type->isBuiltinType())
            break;

#if 0
        // If we have a type name for a previously unnamed type,
        // then use this name as the name of the original type.

        if(auto CType = ConvertTypeToCLR(Type))
            CType->Name = marshalString<E_UTF8>(Name);
#endif

        break;
    }
    case Decl::Namespace:
    {
        NamespaceDecl* ND = cast<NamespaceDecl>(D);

        for (auto it = ND->decls_begin(); it != ND->decls_end(); ++it)
        {
            Decl* D = (*it);
            WalkDeclaration(D);
        }
        
        break;
    }
    default:
    {
        Debug("Unhandled declaration kind: %s\n", D->getDeclKindName());
        //assert(0 && "Unhandled declaration kind");
        break;
    } };
}

//-----------------------------------//

struct ParseConsumer : public clang::ASTConsumer
{
    virtual ~ParseConsumer() { }
    virtual bool HandleTopLevelDecl(clang::DeclGroupRef) { return true; }
};

bool Parser::Parse(const std::string& File)
{
    if (File.empty())
        return false;

    C->setASTConsumer(new ParseConsumer());

    // Get the file  from the file system
    const clang::FileEntry* file = C->getFileManager().getFile(File.c_str());

    if (!file)
    {
        Debug("Filename '%s' was not found."
              "Check your compiler paths.\n", File.c_str());
        return false;
    }

    C->getSourceManager().createMainFileID(file);

    clang::DiagnosticConsumer* client = C->getDiagnostics().getClient();
    client->BeginSourceFile(C->getLangOpts(), &C->getPreprocessor());
    ParseAST(C->getPreprocessor(), &C->getASTConsumer(), C->getASTContext());
    client->EndSourceFile();

    if(client->getNumErrors() != 0)
    {
        // We had some errors while parsing the file.
        // Report this...
        return false;
    }

    AST = &C->getASTContext();
    WalkAST();

    return true;
 }