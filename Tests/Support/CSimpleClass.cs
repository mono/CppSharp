// CSimpleClass.cs created with MonoDevelop
// User: alex at 17:41 03/14/2009
//

using System;
using System.Runtime.InteropServices;

using Mono.VisualC.Interop;

namespace CPPPOC {
	public class CSimpleClass : ICppObject {

                #region C++ Header
                // This interface is analogous to the C++ class public header -- it defines the
                //  C++ class's interface. The order of methods must be the same as in the C++ header.
		private interface __ICSimpleClass : ICppClassOverridable<CSimpleClass> {
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
		private struct __CSimpleClass {
			public int value;
		}
                #endregion

		private static __ICSimpleClass _impl;

		public static void Bind(CppLibrary lib) {
			_impl = lib.GetClass<__ICSimpleClass,__CSimpleClass,CSimpleClass>("CSimpleClass");
		}

		private CppInstancePtr _native;

		public CSimpleClass(int value) {
			_native = _impl.Alloc(this);
                        _impl.CSimpleClass(_native, value);
		}

		public CSimpleClass(IntPtr native) {
			_native = native;
		}

		public virtual IntPtr Native {
			get {
				return (IntPtr)_native;
			}
		}

		public virtual int value {
			get {
				return _impl.value[_native];
			}
			set {
				_impl.value[_native] = value;
			}
		}

		public void M0() {
			_impl.M0(_native);
		}

		public void M1(int x) {
	                _impl.M1(_native, x);
	        }

		public void M2(int x, int y) {
			_impl.M2(_native, x, y);
		}

		[OverrideNative]
		public virtual void V0(int x, int y) {
			Console.WriteLine("Managed V0({0}, {1})", x, y);
			_impl.V0(_native, x, y);
		}

		[OverrideNative]
		public virtual void V1(int x) {
			Console.WriteLine("Managed V1({0})", x);
			_impl.V1(_native, x);
		}

		[OverrideNative]
		public virtual void V2() {
			Console.WriteLine("Managed V2()");
			_impl.V2(_native);
		}

		public void Dispose() {
			_impl.Destruct(_native);
			_native.Dispose();
		}
	}

}
