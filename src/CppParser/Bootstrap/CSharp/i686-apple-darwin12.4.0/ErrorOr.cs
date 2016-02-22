//----------------------------------------------------------------------------
// This is autogenerated code by CppSharp.
// Do not edit this file or all your changes will be lost after re-generation.
//----------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace CppSharp
{
    namespace llvm
    {
        /// <summary>
        /// <para>Represents either an error or a value T.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Represents either an error or a value T.</para>
        /// <para>///</para>
        /// <para>/// ErrorOr&lt;T&gt; is a pointer-like class that represents the
        /// result of an</para>
        /// <para>/// operation. The result is either an error, or a value of type
        /// T. This is</para>
        /// <para>/// designed to emulate the usage of returning a pointer where
        /// nullptr indicates</para>
        /// <para>/// failure. However instead of just knowing that the operation
        /// failed, we also</para>
        /// <para>/// have an error_code and optional user data that describes why
        /// it failed.</para>
        /// <para>///</para>
        /// <para>/// It is used like the following.</para>
        /// <para>/// \code</para>
        /// <para>///   ErrorOr&lt;Buffer&gt; getBuffer();</para>
        /// <para>///</para>
        /// <para>///   auto buffer = getBuffer();</para>
        /// <para>///   if (error_code ec = buffer.getError())</para>
        /// <para>///     return ec;</para>
        /// <para>///   buffer-&gt;write(&quot;adena&quot;);</para>
        /// <para>/// \endcode</para>
        /// <para>///</para>
        /// <para>///</para>
        /// <para>/// Implicit conversion to bool returns true if there is a usable
        /// value. The</para>
        /// <para>/// unary * and -&gt; operators provide pointer like access to
        /// the value. Accessing</para>
        /// <para>/// the value when there is an error has undefined
        /// behavior.</para>
        /// <para>///</para>
        /// <para>/// When T is a reference type the behaivor is slightly
        /// different. The reference</para>
        /// <para>/// is held in a
        /// std::reference_wrapper&lt;std::remove_reference&lt;T&gt;::type&gt;,
        /// and</para>
        /// <para>/// there is special handling to make operator -&gt; work as if T
        /// was not a</para>
        /// <para>/// reference.</para>
        /// <para>///</para>
        /// <para>/// T cannot be a rvalue reference.</para>
        /// </remarks>
        public unsafe partial class ErrorOr
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [FieldOffset(229420686)]
                internal llvm.ErrorOr._.Internal _0;

                [FieldOffset(0)]
                public bool HasError;
            }

            internal unsafe partial struct _
            {
                [StructLayout(LayoutKind.Explicit, Size = 0)]
                public partial struct Internal
                {
                    [FieldOffset(99511470)]
                    internal llvm.AlignedCharArrayUnion.Internal ErrorStorage;
                }
            }
        }

        /// <summary>
        /// <para>Stores a reference that can be changed.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// \brief Stores a reference that can be changed.</para>
        /// </remarks>
        public unsafe partial class ReferenceStorage
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [FieldOffset(0)]
                public global::System.IntPtr Storage;
            }
        }
    }
}
