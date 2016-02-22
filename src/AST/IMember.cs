using System;

namespace CppSharp.AST
{
    public interface IMember<D> where D : Declaration, IMember<D>
    {
        String Name { get; }
        bool CanOverride(D decl);
    }
}
