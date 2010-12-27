using System;
namespace Mono.VisualC.Interop {
        public enum MethodType {
                NoOp,
                Native,
                NativeCtor,
                NativeDtor,
                ManagedAlloc
        }
}
