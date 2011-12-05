using System;
using System.Reflection.Emit;

namespace Mono.Cxxi.Abi {

	public class EmitInfo {
		public TypeBuilder type_builder;
		public FieldBuilder typeinfo_field, native_vtable_field;
		public ILGenerator ctor_il, current_il;
	}
}

