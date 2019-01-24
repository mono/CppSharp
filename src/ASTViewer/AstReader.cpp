#include "AstReader.h"
#include <sstream>
#include "CommandLineSplitter.h"
#include <iostream>
#include "ClangUtilities/StringLiteralExtractor.h"
#include "ClangUtilities/TemplateUtilities.h"


#pragma warning (push)
#pragma warning (disable:4100 4127 4800 4512 4245 4291 4510 4610 4324 4267 4244 4996)
#include <llvm/Support/Path.h>
#include <clang/Tooling/Tooling.h>
#include <clang/AST/RecursiveASTVisitor.h>
#include <clang/Frontend/FrontendActions.h>
#include <clang/AST/Decl.h>
#include <clang/Lex/Lexer.h>
#include <clang/Basic/TargetInfo.h>
#include <clang/Frontend/CompilerInstance.h>
#include <clang/AST/Mangle.h>
#include <clang/Analysis/CFG.h>
#pragma warning (pop)

using namespace clang;

namespace props
{
    std::string const Name = "Name";
    std::string const Mangling = "Mangling";
    std::string const Referenced = "Referenced name";
    std::string const Resolved = "Resolved name";
    std::string const Value = "Value";
    std::string const InterpretedValue = "Interpreted value";
    std::string const IsTemplateDecl = "Is template declaration";
    std::string const IsGenerated = "Generated";
    std::string const Type = "Type";
}


CFG::BuildOptions getCFGBuildOptions()
{
    CFG::BuildOptions cfgBuildOptions; // TODO: Initialize it correctly
    cfgBuildOptions.AddImplicitDtors = true;
    cfgBuildOptions.AddTemporaryDtors = true;
    cfgBuildOptions.AddCXXDefaultInitExprInCtors = true;
    cfgBuildOptions.AddInitializers = true;
    return cfgBuildOptions;
}

std::string getCFG(clang::FunctionDecl const *FD)
{
    try
    {
        auto& astContext = FD->getASTContext();
        auto cfgBuildOptions = getCFGBuildOptions();
        auto cfg = CFG::buildCFG(FD, FD->getBody(), &astContext, cfgBuildOptions);
        if (!cfg)
            return "";
        std::string dumpBuf;
        llvm::raw_string_ostream dumpBufOS(dumpBuf);

        cfg->print(dumpBufOS, astContext.getLangOpts(), false);
        auto dumped = dumpBufOS.str();
        return dumped;
    }
    catch (std::exception &e)
    {
        return std::string("<Error: ") + e.what() + ">";
    }
}



GenericAstNode::GenericAstNode() :
myParent(nullptr), hasDetails(false)
{

}

int GenericAstNode::findChildIndex(GenericAstNode *node)
{
    auto it = std::find_if(myChidren.begin(), myChidren.end(), [node](std::unique_ptr<GenericAstNode> const & n){return n.get() == node; });
    return it == myChidren.end() ?
        -1 :
        it - myChidren.begin();
}

void GenericAstNode::attach(std::unique_ptr<GenericAstNode> child)
{
    child->myParent = this;
    myChidren.push_back(std::move(child));
}

//struct SourceRangeVisitor : boost::static_visitor<SourceRange>
//{
//    template<class T>
//    SourceRange operator()(T const *t) const
//    {
//        if (t == nullptr)
//            return SourceRange();
//        return t->getSourceRange();
//    }
//};

SourceRange GenericAstNode::getRange()
{
    //return boost::apply_visitor(SourceRangeVisitor(), myAstNode);
    return SourceRange();
}

bool GenericAstNode::getRangeInMainFile(std::pair<int, int> &result, clang::SourceManager const &manager, clang::ASTContext &context)
{
    auto range = getRange();
    if (range.isInvalid())
    {
        return false;
    }
    auto start = manager.getDecomposedSpellingLoc(range.getBegin());
    auto end = manager.getDecomposedSpellingLoc(clang::Lexer::getLocForEndOfToken(range.getEnd(), 0, manager, context.getLangOpts()));
    if (start.first != end.first || start.first != manager.getMainFileID())
    {
        //Not in the same file, or not in the main file (probably #included)
        return false;
    }
    result = std::make_pair(start.second, end.second);
    return true;
}


//struct NodeColorVisitor : boost::static_visitor<int>
//{
//    int operator()(Decl const *) const
//    {
//        return 0;
//    }
//    int operator()(Stmt const *) const
//    {
//        return 1;
//    }
//};

int GenericAstNode::getColor()
{
    //return boost::apply_visitor(NodeColorVisitor(), myAstNode);
    return 0;
}


void GenericAstNode::setProperty(std::string const &propertyName, std::string const &value)
{
    myProperties[propertyName] = value;
}

GenericAstNode::Properties const &GenericAstNode::getProperties() const
{
    return myProperties;
}




class AstDumpVisitor : public RecursiveASTVisitor<AstDumpVisitor>
{
public:
    using PARENT = clang::RecursiveASTVisitor<AstDumpVisitor>;
    AstDumpVisitor(clang::ASTContext &context, GenericAstNode *rootNode) :
        myRootNode(rootNode),
        myAstContext(context)
    {
        myStack.push_back(myRootNode);
    }

    bool shouldVisitTemplateInstantiations() const
    {
        return true;
    }

    bool shouldVisitImplicitCode() const 
    { 
        return true; 
    }

    std::string getMangling(clang::NamedDecl const *ND)
    {
        if (auto funcContext = dyn_cast<FunctionDecl>(ND->getDeclContext()))
        {
            if (funcContext->getTemplatedKind() == FunctionDecl::TK_FunctionTemplate)
            {
                return "<Cannot mangle template name>";
            }
        }

        std::vector<TagDecl const *> containers;
        auto currentElement = dyn_cast<TagDecl>(ND->getDeclContext());
        while (currentElement)
        {
            containers.push_back(currentElement);
            currentElement = dyn_cast<TagDecl>(currentElement->getDeclContext());
        }
        for (auto tag : containers)
        {
            if (auto partialSpe = dyn_cast<ClassTemplatePartialSpecializationDecl>(tag))
            {
                return "<Inside partial specialization " + tag->getNameAsString() + ": " + ND->getNameAsString() + ">";
            }
            else if (auto recContext = dyn_cast<CXXRecordDecl>(tag))
            {
                if (recContext->getDescribedClassTemplate() != nullptr)
                {
                    return "<Inside a template" + tag->getNameAsString() + ": " + ND->getNameAsString() + ">";
                }
            }
        }

        auto mangleContext = std::unique_ptr<clang::MangleContext>{ND->getASTContext().createMangleContext()};
        std::string FrontendBuf;
        llvm::raw_string_ostream FrontendBufOS(FrontendBuf);

        if (auto ctor = dyn_cast<CXXConstructorDecl>(ND))
        {
            mangleContext->mangleCXXCtor(ctor, CXXCtorType::Ctor_Complete, FrontendBufOS);
        }
        else if (auto dtor = dyn_cast<CXXDestructorDecl>(ND))
        {
            mangleContext->mangleCXXDtor(dtor, CXXDtorType::Dtor_Complete, FrontendBufOS);
        }
        else if (mangleContext->shouldMangleDeclName(ND) && !isa<ParmVarDecl>(ND))
        {
            mangleContext->mangleName(ND, FrontendBufOS);
        }
        else
        {
            return ND->getNameAsString();
        }
        return FrontendBufOS.str();
    }


    bool TraverseDecl(clang::Decl *decl)
    {
        if (decl == nullptr)
        {
            return PARENT::TraverseDecl(decl);
        }
        auto node = std::make_unique<GenericAstNode>();
        node->myAstNode = decl;
        node->name = decl->getDeclKindName() + std::string("Decl"); // Try to mimick clang default dump
        if (auto *FD = dyn_cast<FunctionDecl>(decl))
        {
#ifndef NDEBUG
            auto &mngr = FD->getASTContext().getSourceManager();
            auto fileName = mngr.getFilename(FD->getLocation()).str();
            bool invalid;
            auto startingLine = mngr.getExpansionLineNumber(FD->getLocation(), &invalid);

            std::string FrontendBuf;
            llvm::raw_string_ostream FrontendBufOS(FrontendBuf);
            clang::PrintingPolicy policyForDebug(FD->getASTContext().getLangOpts());
            FD->getNameForDiagnostic(FrontendBufOS, policyForDebug, true);
            auto debugName = FrontendBufOS.str();

            RecordDecl const*containingClass = nullptr;
            if (FD->isCXXClassMember())
            {
                auto methodDecl = cast<CXXMethodDecl>(FD);
                containingClass = cast<RecordDecl>(methodDecl->getDeclContext());
            }
#endif

            node->name += " " + clang_utilities::getFunctionPrototype(FD, false);
            if (FD->getTemplatedKind() != FunctionDecl::TK_FunctionTemplate)
            {
                node->setProperty(props::Mangling, getMangling(FD));
            }
            node->setProperty(props::Name, clang_utilities::getFunctionPrototype(FD, true));
            if (auto *MD = dyn_cast<CXXMethodDecl>(FD))
            {
                node->setProperty(props::IsGenerated, MD->isUserProvided() ? "False" : "True");

            }
            node->hasDetails = true;
            node->detailsTitle = "Control flow graph";
            node->detailsComputer = [FD]() {return getCFG(FD); };
        }
        else if (auto *PVD = dyn_cast<ParmVarDecl>(decl))
        {
            if (auto *PFD = dyn_cast_or_null<FunctionDecl>(decl->getParentFunctionOrMethod()))
            {
                if (PFD->getTemplatedKind() != FunctionDecl::TK_FunctionTemplate)
                {
                    node->setProperty(props::Mangling, getMangling(PFD));
                }
            }
            else
            {
                node->setProperty(props::Mangling, getMangling(PVD));
            }
            node->setProperty(props::Name, PVD->getNameAsString());
        }
        else if (auto *VD = dyn_cast<VarDecl>(decl))
        {
            //node->setProperty(props::Mangling, getMangling(VD));
            node->setProperty(props::Name, VD->getNameAsString());
            node->setProperty(props::Type, clang_utilities::getTypeName(VD->getType(), true));
        }
        else if (auto *ECD = dyn_cast<EnumConstantDecl>(decl))
        {
            node->setProperty(props::Name, ECD->getNameAsString());
            node->setProperty(props::Value, ECD->getInitVal().toString(10));
        }
        else if (auto *tag = dyn_cast<TagDecl>(decl))
        {
            std::string nameBuf;
            llvm::raw_string_ostream os(nameBuf);

            if (TypedefNameDecl *Typedef = tag->getTypedefNameForAnonDecl())
                os << Typedef->getIdentifier()->getName();
            else if (tag->getIdentifier())
                os << tag->getIdentifier()->getName();
            else
                os << "No name";

            if (auto templateInstance = dyn_cast<ClassTemplateSpecializationDecl>(tag))
            {
                clang::PrintingPolicy policy(templateInstance->getASTContext().getLangOpts());
                clang_utilities::printTemplateArguments(os, policy, &templateInstance->getTemplateArgs(), false);
            }
            node->name += " " + tag->getNameAsString();
            node->setProperty(props::Name, os.str());
        }
        else if (auto *ND = dyn_cast<NamedDecl>(decl))
        {


            node->name += " " + ND->getNameAsString();
            node->setProperty(props::Name, ND->getNameAsString());
        }

        auto nodePtr = node.get();
        myStack.back()->attach(std::move(node));
        myStack.push_back(nodePtr);
        auto res = PARENT::TraverseDecl(decl);
        myStack.pop_back();
        return res;
    }

    bool TraverseStmt(clang::Stmt *stmt)
    {
        if (stmt == nullptr)
        {
            return PARENT::TraverseStmt(stmt);
        }
        auto node = std::make_unique<GenericAstNode>();
        node->myAstNode = stmt;
        node->name = stmt->getStmtClassName();
        auto nodePtr = node.get();
        myStack.back()->attach(std::move(node));
        myStack.push_back(nodePtr);
        auto res = PARENT::TraverseStmt(stmt);
        myStack.pop_back();
        return res;
    }

    bool VisitStringLiteral(clang::StringLiteral *s)
    {
        myStack.back()->name += (" " + s->getBytes()).str();
        myStack.back()->setProperty(props::InterpretedValue, s->getBytes());
        auto parts = clang_utilities::splitStringLiteral(s, myAstContext.getSourceManager(), myAstContext.getLangOpts(), myAstContext.getTargetInfo());
        if (parts.size() == 1)
        {
            myStack.back()->setProperty(props::Value, parts[0]);

        }
        else
        {
            int i = 0;
            for (auto &part : parts)
            {
                ++i;
                myStack.back()->setProperty(props::Value + " " + std::to_string(i), part);

            }
        }
        return true;
    }

    bool VisitIntegerLiteral(clang::IntegerLiteral *i)
    {
        bool isSigned = i->getType()->isSignedIntegerType();
        myStack.back()->setProperty(props::Value, i->getValue().toString(10, isSigned));
        return true;
    }

    bool VisitCharacterLiteral(clang::CharacterLiteral *c)
    {
        myStack.back()->setProperty(props::Value, std::string(1, c->getValue()));
        return true;
    }

    bool VisitFloatingLiteral(clang::FloatingLiteral *f)
    {
        myStack.back()->setProperty(props::Value, std::to_string(f->getValueAsApproximateDouble()));
        return true;
    }

    bool VisitCXXRecordDecl(clang::CXXRecordDecl *r)
    {
        myStack.back()->setProperty(props::IsTemplateDecl, std::to_string(r->getDescribedClassTemplate() != nullptr));
        return true;
    }


    void addReference(GenericAstNode *node, clang::NamedDecl *referenced, std::string const &label)
    {
        auto funcDecl = dyn_cast<FunctionDecl>(referenced);
        myStack.back()->setProperty(label, funcDecl == nullptr ?
            referenced->getNameAsString() :
            clang_utilities::getFunctionPrototype(funcDecl, false));
    }

    bool VisitDeclRefExpr(clang::DeclRefExpr *ref)
    {
        addReference(myStack.back(), ref->getDecl(), props::Referenced);
        addReference(myStack.back(), ref->getFoundDecl(), props::Resolved);

        return true;
    }

    bool TraverseType(clang::QualType type)
    {
        if (type.isNull())
        {
            return PARENT::TraverseType(type);
        }
        auto node = std::make_unique<GenericAstNode>();
        //node->myType = d;
        node->name = type->getTypeClassName();
        auto nodePtr = node.get();
        myStack.back()->attach(std::move(node));
        myStack.push_back(nodePtr);
        auto res = PARENT::TraverseType(type);
        myStack.pop_back();
        return res;
    }

private:
    std::vector<GenericAstNode*> myStack;
    GenericAstNode *myRootNode;
    ASTContext &myAstContext;
};


AstReader::AstReader() : isReady(false)
{
}

clang::SourceManager &AstReader::getManager()
{
    return myAst->getSourceManager();
}

clang::ASTContext &AstReader::getContext()
{
    return myAst->getASTContext();
}

GenericAstNode *AstReader::getRealRoot()
{
    return myArtificialRoot->myChidren.front().get();
}

GenericAstNode *AstReader::findPosInChildren(std::vector<std::unique_ptr<GenericAstNode>> const &candidates, int position)
{
    for (auto &candidate : candidates)
    {
        std::pair<int, int> location;
        if (!candidate->getRangeInMainFile(location, getManager(), getContext()))
        {
            continue;
        }
        if (location.first <= position && position <= location.second)
        {
            return candidate.get();
        }
    }
    return nullptr;
}

std::vector<GenericAstNode *> AstReader::getBestNodeMatchingPosition(int position)
{
    std::vector<GenericAstNode *> result;
    auto currentNode = getRealRoot();
    result.push_back(currentNode);
    currentNode = currentNode->myChidren[0].get();
    result.push_back(currentNode); // Translation unit does not have position
    while (true)
    {
        auto bestChild = findPosInChildren(currentNode->myChidren, position);
        if (bestChild == nullptr)
        {
            return result;
        }
        result.push_back(bestChild);
        currentNode = bestChild;
    }
}

GenericAstNode *AstReader::readAst(std::string const &sourceCode, std::string const &options)
{
    mySourceCode = sourceCode;
    myArtificialRoot = std::make_unique<GenericAstNode>();
    auto root = std::make_unique<GenericAstNode>();
    root->name = "AST";
    myArtificialRoot->attach(std::move(root));

    auto args = splitCommandLine(options);

    std::cout << "Launching Clang to create AST" << std::endl;
    //myAst = clang::tooling::buildASTFromCodeWithArgs(mySourceCode, args);
    myAst = nullptr;
    if (myAst != nullptr)
    {
        for (auto it = myAst->top_level_begin(); it != myAst->top_level_end(); ++it)
        {
            //(*it)->dumpColor();
        }
        std::cout << "Visiting AST and creating Qt Tree" << std::endl;
        auto visitor = AstDumpVisitor{ myAst->getASTContext(), getRealRoot() };
        visitor.TraverseDecl(myAst->getASTContext().getTranslationUnitDecl());
    }
    isReady = true;
    return myArtificialRoot.get();
}

bool AstReader::ready()
{
    return isReady;
}

void AstReader::dirty()
{
    isReady = false;
}

