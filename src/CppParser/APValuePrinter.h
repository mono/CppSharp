#pragma once

#include <llvm/Support/raw_ostream.h>
#include <llvm/ADT/SmallString.h>
#include <llvm/ADT/ArrayRef.h>
#include <clang/AST/APValue.h>
#include <clang/AST/Type.h>
#include <clang/AST/ASTContext.h>

namespace CppSharp { namespace CppParser {
    struct APValuePrinter
    {
        clang::ASTContext &ctx;
        clang::raw_ostream &out;

        bool Print(clang::APValue &value, clang::QualType type)
        {
            using namespace clang;

            switch (value.getKind())
            {
            case APValue::Int:
                if (type->isBooleanType())
                    out << (value.getInt().getBoolValue() ? "true" : "false");
                else
                    out << value.getInt();
                return true;
            case APValue::Float:
            {
                SmallString<50> str;
                value.getFloat().toString(str);
                out << str.str().str();
                return true;
            }
            case APValue::Vector:
            {
                out << '{';
                const auto elementType = type->castAs<clang::VectorType>()->getElementType();
                if (!Print(value.getVectorElt(0), elementType))
                    return false;
                for (unsigned i = 1; i != value.getVectorLength(); ++i)
                {
                    out << ", ";
                    Print(value.getVectorElt(i), elementType);
                }
                out << '}';
                return true;
            }
            case APValue::Array:
            {
                const auto *arrayType = ctx.getAsArrayType(type);
                const auto elementType = arrayType->getElementType();
                out << '{';
                if (unsigned n = value.getArrayInitializedElts())
                {
                    if (!Print(value.getArrayInitializedElt(0), elementType))
                        return false;
                    for (unsigned i = 1; i != n; ++i)
                    {
                        out << ", ";
                        Print(value.getArrayInitializedElt(i), elementType);
                    }
                }
                out << '}';
                return true;
            }
            case APValue::LValue:
            {
                auto base = value.getLValueBase();

                if (const ValueDecl *VD = base.dyn_cast<const ValueDecl *>())
                {
                    out << *VD;
                    return true;
                }
                else if (TypeInfoLValue TI = base.dyn_cast<TypeInfoLValue>())
                    return false;
                else if (DynamicAllocLValue DA = base.dyn_cast<DynamicAllocLValue>())
                    return false;
                else
                {
                    base.get<const clang::Expr *>()->printPretty(out, nullptr, ctx.getPrintingPolicy());
                    return true;
                }
            }
            }

            return false;
        }
    };
} } 