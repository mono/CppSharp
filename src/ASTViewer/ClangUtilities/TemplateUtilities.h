#pragma once

#pragma warning (push)
#pragma warning (disable:4100 4127 4800 4512 4245 4291 4510 4610 4324 4267 4244 4996)
#include <llvm/Support/raw_ostream.h>
#include <clang/AST/PrettyPrinter.h>
#include <clang/AST/Decl.h>
#include <clang/AST/DeclTemplate.h>
#pragma warning (pop)

namespace clang_utilities {

// Adapted from tools\clang\lib\AST\DeclPrinter.cpp DeclPrinter::PrintTemplateParameters
void printTemplateParameters(llvm::raw_ostream &out, clang::PrintingPolicy const &policy, const clang::TemplateParameterList *params);
void printTemplateParameter(llvm::raw_ostream &out, clang::PrintingPolicy const &policy, clang::NamedDecl const *param);


void printTemplateArguments(llvm::raw_ostream &out, const clang::PrintingPolicy &policy, clang::TemplateArgumentList const *args, bool qualifyNames);
// Adapted from tools\clang\lib\AST\TemplateBase.cpp TemplateArgument::print
void printTemplateArgument(llvm::raw_ostream &out, const clang::PrintingPolicy &policy, clang::TemplateArgument const &arg, bool qualifyNames);

std::string getFunctionPrototype(clang::FunctionDecl *f, bool qualifyNames);
std::string getTypeName(clang::QualType qualType, bool qualifyNames);


} // namespace clang_utilities

