#include "TemplateUtilities.h"

#pragma warning (push)
#pragma warning (disable:4100 4127 4800 4512 4245 4291 4510 4610 4324 4267 4244 4996)
#include <clang/Tooling/Tooling.h>
#include <clang/AST/RecursiveASTVisitor.h>
#include <clang/Frontend/FrontendActions.h>
#include <clang/AST/Decl.h>
#include <clang/Lex/Lexer.h>
#include <clang/Basic/TargetInfo.h>
#include <clang/Frontend/CompilerInstance.h>
#include <clang/AST/Mangle.h>
#pragma warning (pop)

using namespace clang;
namespace clang_utilities {


void printDeclType(raw_ostream &out, PrintingPolicy const &policy, QualType T, StringRef DeclName, bool Pack) {
    // Normally, a PackExpansionType is written as T[3]... (for instance, as a
    // template argument), but if it is the type of a declaration, the ellipsis
    // is placed before the name being declared.
    if (auto *PET = T->getAs<PackExpansionType>()) {
        Pack = true;
        T = PET->getPattern();
    }
    T.print(out, policy, (Pack ? "..." : "") + DeclName);
}

void printTemplateParameters(raw_ostream &out, PrintingPolicy const &policy, TemplateParameterList const *params)
{
    if (params == nullptr)
    {
        return;
    }

    out << "<";
    for (unsigned i = 0, e = params->size(); i != e; ++i) {
        if (i != 0)
            out << ", ";

        auto param = params->getParam(i);
        printTemplateParameter(out, policy, param);
    }
    out << ">";

}

// Adapted from tools\clang\lib\AST\DeclPrinter.cpp DeclPrinter::PrintTemplateParameters
void printTemplateParameter(raw_ostream &out, PrintingPolicy const &policy, NamedDecl const *param)
{
    if (const TemplateTypeParmDecl *TTP =
        dyn_cast<TemplateTypeParmDecl>(param)) {

        if (TTP->wasDeclaredWithTypename())
            out << "typename ";
        else
            out << "class ";

        if (TTP->isParameterPack())
            out << "...";

        out << *TTP;

        if (TTP->hasDefaultArgument()) {
            out << " = ";
            out << TTP->getDefaultArgument().getAsString(policy);
        };
    }
    else if (const NonTypeTemplateParmDecl *NTTP =
        dyn_cast<NonTypeTemplateParmDecl>(param)) {
        StringRef Name;
        if (IdentifierInfo *II = NTTP->getIdentifier())
            Name = II->getName();
        printDeclType(out, policy, NTTP->getType(), Name, NTTP->isParameterPack());

        if (NTTP->hasDefaultArgument()) {
            out << " = ";
            NTTP->getDefaultArgument()->printPretty(out, nullptr, policy, 0);
        }
    }
    else if (const TemplateTemplateParmDecl *TTPD =
        dyn_cast<TemplateTemplateParmDecl>(param)) {
        out << "template";
        printTemplateParameters(out, policy, TTPD->getTemplateParameters());
        out << " class " << TTPD->getNameAsString();
        //VisitTemplateDecl(TTPD);
        // FIXME: print the default argument, if present.
    }
}


static void printIntegral(const TemplateArgument &TemplArg,
    raw_ostream &out, const PrintingPolicy& policy) {
    const ::clang::Type *T = TemplArg.getIntegralType().getTypePtr();
    const llvm::APSInt &Val = TemplArg.getAsIntegral();

    if (const EnumType *ET = T->getAs<EnumType>()) {
        for (const EnumConstantDecl* ECD : ET->getDecl()->enumerators()) {
            // In Sema::CheckTemplateArugment, enum template arguments value are
            // extended to the size of the integer underlying the enum type.  This
            // may create a size difference between the enum value and template
            // argument value, requiring isSameValue here instead of operator==.
            if (llvm::APSInt::isSameValue(ECD->getInitVal(), Val)) {
                ECD->printQualifiedName(out, policy);
                return;
            }
        }
    }

    if (T->isBooleanType()) {
        out << (Val.getBoolValue() ? "true" : "false");
    }
    else if (T->isCharType()) {
        const char Ch = Val.getZExtValue();
        out << ((Ch == '\'') ? "'\\" : "'");
        out.write_escaped(StringRef(&Ch, 1), /*UseHexEscapes=*/ true);
        out << "'";
    }
    else {
        out << Val;
    }
}

void printTemplateName(raw_ostream &OS, const PrintingPolicy &policy, TemplateName const &name, bool qualifyNames = false)
{
    if (auto Template = name.getAsTemplateDecl())
        OS << (qualifyNames ? Template->getQualifiedNameAsString() : Template->getNameAsString());
    else if (auto QTN = name.getAsQualifiedTemplateName()) {
        OS << (qualifyNames ? QTN->getDecl()->getQualifiedNameAsString() : QTN->getDecl()->getNameAsString());
    }
    else if (auto DTN = name.getAsDependentTemplateName()) {
        if (qualifyNames && DTN->getQualifier())
            DTN->getQualifier()->print(OS, policy);
        OS << "template ";

        if (DTN->isIdentifier())
            OS << DTN->getIdentifier()->getName();
        else
            OS << "operator " << getOperatorSpelling(DTN->getOperator());
    }
    else if (auto subst = name.getAsSubstTemplateTemplateParm()) {
        subst->getReplacement().print(OS, policy, !qualifyNames);
    }
    else if (auto SubstPack = name.getAsSubstTemplateTemplateParmPack())
        OS << *SubstPack->getParameterPack();
    else {
        auto OTS = name.getAsOverloadedTemplate();
        (*OTS->begin())->printName(OS);
    }
}



// Adapted from tools\clang\lib\AST\TemplateBase.cpp TemplateArgument::print
void printTemplateArgument(raw_ostream &out, const PrintingPolicy &policy, TemplateArgument const &arg, bool qualifyNames)
{
    switch (arg.getKind()) {
    case TemplateArgument::Null:
        out << "(no value)";
        break;

    case TemplateArgument::Type: {
        PrintingPolicy SubPolicy(policy);
        SubPolicy.SuppressStrongLifetime = true;
        arg.getAsType().print(out, SubPolicy);
        break;
    }

    case TemplateArgument::Declaration: {
        NamedDecl *ND = cast<NamedDecl>(arg.getAsDecl());
        out << '&';
        if (ND->getDeclName()) {
            // FIXME: distinguish between pointer and reference args?
            ND->printQualifiedName(out);
        }
        else {
            out << "(anonymous)";
        }
        break;
    }

    case TemplateArgument::NullPtr:
        out << "nullptr";
        break;

    case TemplateArgument::Template:
        // Orig: arg.getAsTemplate().print(out, policy);
    {
        auto templateName = arg.getAsTemplate();
        printTemplateName(out, policy, templateName, qualifyNames);
        break;
    }

    case TemplateArgument::TemplateExpansion:
        arg.getAsTemplateOrTemplatePattern().print(out, policy);
        out << "...";
        break;

    case TemplateArgument::Integral: {
        printIntegral(arg, out, policy);
        break;
    }

    case TemplateArgument::Expression:
        arg.getAsExpr()->printPretty(out, nullptr, policy);
        break;

    case TemplateArgument::Pack:
        out << "<";
        bool First = true;
        for (const auto &P : arg.pack_elements()) {
            if (First)
                First = false;
            else
                out << ", ";

            P.print(policy, out);
        }
        out << ">";
        break;
    }
}

void printTemplateArguments(raw_ostream &out, const PrintingPolicy &policy, TemplateArgumentList const *args, bool qualifyNames)
{
    if (args == nullptr)
    {
        return;
    }
    out << "<";
    for (unsigned i = 0, e = args->size(); i != e; ++i) {
        if (i != 0)
            out << ", ";

        auto arg = args->get(i);
        printTemplateArgument(out, policy, arg, qualifyNames);
    }
    out << ">";

}

std::string getTypeName(QualType qualType, bool qualifyNames)
{
    auto langOptions = clang::LangOptions{};
    auto printPolicy = PrintingPolicy{ langOptions };
    printPolicy.SuppressSpecifiers = false;
    printPolicy.ConstantArraySizeAsWritten = false;
    return qualType.getAsString(printPolicy);

}

std::string getFunctionPrototype(FunctionDecl *f, bool qualifyNames)
{
    std::string prototypeBuf;
    llvm::raw_string_ostream os(prototypeBuf);
    PrintingPolicy policy(f->getASTContext().getLangOpts());
    policy.TerseOutput = false;
    os << getTypeName(f->getReturnType(), qualifyNames) << ' ' << f->getNameAsString();
    if (f->getTemplatedKind() == FunctionDecl::TK_FunctionTemplateSpecialization ||
        //            f->getTemplatedKind() == FunctionDecl::TK_MemberSpecialization ||
        f->getTemplatedKind() == FunctionDecl::TK_DependentFunctionTemplateSpecialization)
    {
        printTemplateArguments(os, policy, f->getTemplateSpecializationArgs(), qualifyNames);
    }
    if (f->getTemplatedKind() == FunctionDecl::TK_FunctionTemplate)
    {
        printTemplateParameters(os, policy, f->getDescribedFunctionTemplate()->getTemplateParameters());
    }
    os << '(';
    bool first = true;
    for (auto param : f->parameters())
    {
        if (!first)
        {
            os << ", ";
        }
        first = false;

        os << getTypeName(param->getType(), qualifyNames) << ' ' << param->getNameAsString();
    }
    os << ')';
    return os.str();
}


} // namespace clang_utilities
