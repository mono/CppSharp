using System;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Passes;

namespace CppSharp.Generators.C
{
    public class NAPITypeCheck : TranslationUnitPass
    {
        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            return unit.IsSystemHeader || base.VisitTranslationUnit(unit);
        }

        public override bool VisitFieldDecl(Field field)
        {
            return true;
        }

        private Declaration decl;

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.IsGenerated)
                return false;

            decl = function;
            return base.VisitFunctionDecl(function);
        }

        public override bool VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.LongDouble:
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.Float128:
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.Decimal:
                    Diagnostics.Warning($"Unsupported decl: {decl.QualifiedName}");
                    return false;
                default:
                    return true;
            }
        }

        public override bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (pointer.IsConstCharString())
                return true;

            var pointee = pointer.GetFinalPointee().Desugar();
            if (!pointee.TryGetClass(out _))
            {
                Diagnostics.Warning($"Unsupported decl: {decl.QualifiedName}");
                return false;
            }

            return true;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            return true;
        }

        public override bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return true;
        }
    }
}