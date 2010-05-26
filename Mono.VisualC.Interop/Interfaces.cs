//
// Mono.VisualC.Interop.Interfaces.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;

namespace Mono.VisualC.Interop
{

	public interface ICppInstance : IDisposable
        {
			IntPtr Native { get; }
        }

        public interface ICppInstantiatable
        {
                CppInstancePtr Alloc();
                void Destruct(CppInstancePtr instance);
        }

        public interface ICppOverridable<T> where T : ICppInstance
        {
                CppInstancePtr Alloc(T managed);
                void Destruct(CppInstancePtr instance);
        }
}