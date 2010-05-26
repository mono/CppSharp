//
// Mono.VisualC.Interop.Interfaces.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//
// Copyright (C) 2010 Alexander Corrado
//

using System;

<<<<<<< HEAD
namespace Mono.VisualC.Interop
{

	public interface ICppInstance : IDisposable
        {
			IntPtr Native { get; }
=======
namespace Mono.VisualC.Interop {
	
	public interface ICppObject : IDisposable {
                	IntPtr Native { get; }
>>>>>>> Refactored and completed managed VTable implementation. Prepared for
        }

        public interface ICppClassInstantiatable {
                CppInstancePtr Alloc ();
                void Destruct (CppInstancePtr instance);
        }
<<<<<<< HEAD

        public interface ICppOverridable<T> where T : ICppInstance
        {
                CppInstancePtr Alloc(T managed);
                void Destruct(CppInstancePtr instance);
=======
        
        public interface ICppClassOverridable<T> where T : ICppObject {
                CppInstancePtr Alloc (T managed);
                void Destruct (CppInstancePtr instance);
>>>>>>> Refactored and completed managed VTable implementation. Prepared for
        }
}