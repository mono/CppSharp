using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Mono.VisualC.Interop.ABI {
	public class MsvcTypeInfo : CppTypeInfo {
		public MsvcTypeInfo (MsvcAbi abi, IEnumerable<MethodInfo> virtualMethods, Type nativeLayout)
			: base (abi, virtualMethods, nativeLayout)
		{
		}

		// AFIK, MSVC places only its first base's virtual methods in the derived class's
		//  primary vtable. Subsequent base classes (each?) get another vtable pointer
		public override void AddBase (CppTypeInfo baseType)
		{
			if (TypeComplete)
				return;

			if (BaseClasses.Count == 0)
				base.AddBase (baseType, false);
			else
				base.AddBase (baseType, true);
		}

	}
}

