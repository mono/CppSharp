using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public static class PropertyExtensions
    {
        public static bool IsInRefTypeAndBackedByValueClassField(this Property p)
        {
            if (p.Field == null || ((Class) p.Namespace).IsRefType)
                return false;

            Type type;
            p.Field.Type.IsPointerTo(out type);
            type = type ?? p.Field.Type;

            Class decl;
            return type.TryGetClass(out decl) && decl.IsValueType;
        }
    }
}