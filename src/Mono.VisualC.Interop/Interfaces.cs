//
// Mono.VisualC.Interop.Interfaces.cs
//
// Author:
//   Alexander Corrado (alexander.corrado@gmail.com)
//   Andreia Gaita (shana@spoiledcat.net)
//
// Copyright (C) 2010 Alexander Corrado
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Mono.VisualC.Interop.ABI;

namespace Mono.VisualC.Interop {

	// The contract for ICppObject requires implementations to have the following constructors:
	//  + A public constructor that takes CppInstancePtr (native constructor)
	//  + A public constructor that takes CppTypeInfo, and as its sole operation,
	//     calls AddBase on that passed CppTypeInfo, passing its own typeinfo (subclass constructor)
	//  NOTE: It is important that the subclass constructor have no side effects.
	//  All constructors for wrappers of native subclasses must call the subclass constructor for the
	//   wrappers of their base class(es).
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
	public interface ICppClassOverridable<TManaged> : ICppClassInstantiatable
		/* where TManaged : ICppObject */
	{
		CppInstancePtr Alloc (TManaged managed);
	}
}
