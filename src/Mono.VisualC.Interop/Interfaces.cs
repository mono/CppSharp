//
// Mono.VisualC.Interop.Interfaces.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;
using Mono.VisualC.Interop.ABI;

namespace Mono.VisualC.Interop {
	public interface ICppObject : IDisposable {
                CppInstancePtr Native { get; }
        }

        public interface ICppClass {
		CppTypeInfo TypeInfo { get; }
        }

        // This should go without saying, but the C++ class must have a constructor
        //  if it is to be instantiatable.
        public interface ICppClassInstantiatable : ICppClass {
                CppInstancePtr Alloc ();
        }

        // It is recommended that managed wrappers implement ICppObject, but
        //  I'm not making it required so that any arbitrary object can be exposed to
        //  C++ via CppInstancePtr.ForManagedObject.
        public interface ICppClassOverridable<T> : ICppClass /* where T : ICppObject */ {
                CppInstancePtr Alloc (T managed);
        }
}