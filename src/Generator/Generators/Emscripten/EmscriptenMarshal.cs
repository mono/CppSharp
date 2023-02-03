using CppSharp.Generators.C;

namespace CppSharp.Generators.Emscripten
{
    public class EmscriptenMarshalNativeToManagedPrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
    {
        public EmscriptenMarshalNativeToManagedPrinter(MarshalContext marshalContext)
            : base(marshalContext)
        {
        }
    }

    public class EmscriptenMarshalManagedToNativePrinter : MarshalPrinter<MarshalContext, CppTypePrinter>
    {
        public EmscriptenMarshalManagedToNativePrinter(MarshalContext ctx)
            : base(ctx)
        {
            Context.MarshalToNative = this;
        }
    }
}
