#pragma once

#pragma warning (push)
#pragma warning (disable:4100 4127 4800 4512 4245 4291 4510 4610 4324 4267 4244 4996)
#include "clang/Frontend/TextDiagnosticBuffer.h"
#include "clang/Tooling/CommonOptionsParser.h"
#include "clang/Tooling/Tooling.h"
#include "llvm/Support/CommandLine.h"
#include "clang/basic/SourceLocation.h"
#pragma warning(pop)
#include <string>
#include <variant>


class GenericAstNode
{
public:
    GenericAstNode();
    int findChildIndex(GenericAstNode *node); // Return -1 if not found
    void attach(std::unique_ptr<GenericAstNode> child);
    std::string name;
    std::vector<std::unique_ptr<GenericAstNode>> myChidren;
    bool getRangeInMainFile(std::pair<int, int> &result, clang::SourceManager const &manager, clang::ASTContext &context); // Return false if the range is not fully in the main file
    clang::SourceRange getRange();
    int getColor(); // Will return a color identifier How this is linked to the real color is up to the user
    using Properties = std::map<std::string, std::string>;
    void setProperty(std::string const &propertyName, std::string const &value);
    Properties const &getProperties() const;
    std::variant<clang::Decl *, clang::Stmt *> myAstNode;
    GenericAstNode *myParent;

    bool hasDetails;
    std::string detailsTitle;
    std::string details;
    std::function<std::string()> detailsComputer;

private:
    Properties myProperties;
};

class AstReader
{
public:
    AstReader();
    GenericAstNode *readAst(std::string const &sourceCode, std::string const &options);
    clang::SourceManager &getManager();
    clang::ASTContext &getContext();
    GenericAstNode *getRealRoot();
    std::vector<GenericAstNode *> getBestNodeMatchingPosition(int position); // Return the path from root to the node
    bool ready();
    void dirty(); // Ready will be false until the reader is run again
private:
    GenericAstNode *findPosInChildren(std::vector<std::unique_ptr<GenericAstNode>> const &candidates, int position);
    std::string args;
    std::string mySourceCode; // Needs to stay alive while we navigate the tree
    std::unique_ptr<clang::ASTUnit> myAst;
    std::unique_ptr<GenericAstNode> myArtificialRoot; // We need an artificial root on top of the real root, because the root is not displayed by Qt
    bool isReady;
};

