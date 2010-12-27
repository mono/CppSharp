using System;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop {
        public class CppObjectMarshaler : ICustomMarshaler {
                private static CppObjectMarshaler marshaler = null;
                private CppObjectMarshaler () {
                }

                public IntPtr MarshalManagedToNative (object managedObj)
                {
                        if (managedObj == null)
                                return IntPtr.Zero;

                        ICppObject cppObject = managedObj as ICppObject;
                        if (cppObject == null)
                                throw new ArgumentException ("Object to marshal must implement ICppObject");

                        return (IntPtr)cppObject.Native;
                }

                public object MarshalNativeToManaged (IntPtr pNativeData)
                {
                        throw new NotImplementedException ();
                }

                public void CleanUpManagedData (object ManagedObj)
                {
                }

                public void CleanUpNativeData (IntPtr pNativeData)
                {
                }

                public int GetNativeDataSize ()
                {
                        return -1;
                }

                public static ICustomMarshaler GetInstance(string cookie)
                {
                        if(marshaler == null)
                                marshaler = new CppObjectMarshaler ();
                        return marshaler;
                }

        }
}

