//
// Mono.VisualC.Interop.Interfaces.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;

namespace Mono.VisualC.Interop {
	public interface ICppObject : IDisposable {
                	IntPtr Native { get; }
        }

        public interface ICppClassInstantiatable {
                CppInstancePtr Alloc ();
                void Destruct (CppInstancePtr instance);
        }

        public interface ICppClassOverridable<T> where T : ICppObject {
                CppInstancePtr Alloc (T managed);
                void Destruct (CppInstancePtr instance);
        }
}