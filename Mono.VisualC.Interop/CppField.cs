//
// Mono.VisualC.Interop.CppField.cs: Represents a field in a native C++ object
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using System.Runtime.InteropServices;

namespace Mono.VisualC.Interop {
        public class CppField<T> {
                private int fieldOffset;

                public CppField (int fieldOffset)
                {
                        this.fieldOffset = fieldOffset;
                }

                public T this [CppInstancePtr ip] {
                        get {
                                Type retType = typeof (T);
                                object retVal;
<<<<<<< HEAD
                                     if (retType.Equals(typeof(Byte)))
                                                retVal = Marshal.ReadByte(ip.Native, fieldOffset);
                                else if (retType.Equals(typeof(Int16)))
                                                retVal = Marshal.ReadInt16(ip.Native, fieldOffset);
                                else if (retType.Equals(typeof(Int32)))
                                                retVal = Marshal.ReadInt32(ip.Native, fieldOffset);
                                else throw new NotImplementedException("Cannot read C++ fields of type " + retType.Name);

=======
                                     if (retType.Equals (typeof (Byte)))
                                                retVal = Marshal.ReadByte (ip.Native, fieldOffset);
                                else if (retType.Equals (typeof (Int16)))
                                                retVal = Marshal.ReadInt16 (ip.Native, fieldOffset);
                                else if (retType.Equals (typeof (Int32)))
                                                retVal = Marshal.ReadInt32 (ip.Native, fieldOffset);
                                else throw new NotImplementedException ("Cannot read C++ fields of type " + retType.Name);
                                
>>>>>>> Refactored and completed managed VTable implementation. Prepared for
                                return (T)retVal;
                        }
                        set {
                                Type setType = typeof (T);
                                object setVal = value;
                                     if (setType.Equals (typeof (Byte)))
                                                Marshal.WriteByte (ip.Native, fieldOffset, (byte)setVal);
                                else if (setType.Equals (typeof (Int16)))
                                                Marshal.WriteInt16 (ip.Native, fieldOffset, (Int16)setVal);
                                else if (setType.Equals (typeof (Int32)))
                                                Marshal.WriteInt32 (ip.Native, fieldOffset, (Int32)setVal);
                                else throw new NotImplementedException("Cannot write C++ fields of type " + setType.Name);
                        }
                }
        }
}
