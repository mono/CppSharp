// CSimpleClass.cs created with MonoDevelop
// User: alex at 17:41 03/14/2009
//

using System;
using System.Runtime.InteropServices;

using Mono.VisualC.Interop;

namespace Tests.Support {
	public class CSimpleClass : ICppObject {

                #region C++ Header
                // This interface is analogous to the C++ class public header -- it defines the
                //  C++ class's interface. The order of methods must be the same as in the C++ header.
		public interface ICSimpleClass : ICppClassOverridable<CSimpleClass> {
                        // constructor
                        void CSimpleClass(CppInstancePtr ths, int value);

			void M0(CppInstancePtr ths);
			[Virtual] void V0(CppInstancePtr ths, int x, int y);
			void M1(CppInstancePtr ths, int x);
			[Virtual] void V1(CppInstancePtr ths, int x);
			void M2(CppInstancePtr ths, int x, int y);

                        // a C++ field directly accessible to managed code
			CppField<int> value {get;}
		}

                // This struct defines the C++ class's memory footprint.
                //  Basically, it includes both the class's public and private fields.
                //  Again, the order must be the same as in the C++ header.
		public struct _CSimpleClass {
			public int value;
		}
                #endregion

		private CppInstancePtr native;
                private ICSimpleClass impl;

		public CSimpleClass(ICSimpleClass impl, int value) {
                        this.impl = impl;
			this.native = impl.Alloc(this);
                        impl.CSimpleClass(native, value);
		}

		public CSimpleClass(ICSimpleClass impl, IntPtr native) {
                        this.impl = impl;
			this.native = native;
		}

		public IntPtr Native {
			get { return (IntPtr)native; }
		}

                public ICSimpleClass Implementation {
                        get { return impl; }
                }

		public virtual int value {
			get {
				return impl.value[native];
			}
			set {
				impl.value[native] = value;
			}
		}

		public void M0() {
			impl.M0(native);
		}

		public void M1(int x) {
	                impl.M1(native, x);
	        }

		public void M2(int x, int y) {
			impl.M2(native, x, y);
		}

		[OverrideNative]
		public virtual void V0(int x, int y) {
			Console.WriteLine("Managed V0({0}, {1})", x, y);
			impl.V0(native, x, y);
		}

		[OverrideNative]
		public virtual void V1(int x) {
			Console.WriteLine("Managed V1({0})", x);
			impl.V1(native, x);
		}

		public void Dispose() {
			impl.Destruct(native);
			native.Dispose();
		}
	}

}
