using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Mono.VisualC.Interop.ABI {
	public class ItaniumTypeInfo : CppTypeInfo {

		private bool hasVirtualDtor;

		public ItaniumTypeInfo (ItaniumAbi abi, IEnumerable<MethodInfo> virtualMethods, Type nativeLayout)
			: base (abi, virtualMethods, nativeLayout)
		{
			for (int i = 0; i < VirtualMethods.Count; i++) {
				if (Abi.GetMethodType (VirtualMethods [i]) != MethodType.NativeDtor)
					continue;

				hasVirtualDtor = true;
				VirtualMethods.RemoveAt (i);
				break;
			}
		}

		public bool HasVirtualDestructor {
			get { return hasVirtualDtor; }
		}

		public override void AddBase (CppTypeInfo baseType)
		{
			hasVirtualDtor |= ((ItaniumTypeInfo)baseType).HasVirtualDestructor;
			base.AddBase (baseType);
		}

		// Itanium puts all its virtual dtors at the bottom of the vtable (2 slots each).
		//  Since we will never be in a position of calling a dtor virtually, we can just pad the vtable out.
		public override int VTableBottomPadding {
			get {
				if (!hasVirtualDtor)
					return 0;

				return 2 + CountBases (b => ((ItaniumTypeInfo)b).HasVirtualDestructor) * 2;
			}
		}

	}
}

