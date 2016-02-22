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
        /// <para>PointerIntPair - This class implements a pair of a pointer and
        /// small integer. It is designed to represent this in the space required by
        /// one pointer by bitmangling the integer into the low part of the pointer.
        /// This can only be done for small integers: typically up to 3 bits, but it
        /// depends on the number of bits available according to PointerLikeTypeTraits
        /// for the type.</para>
        /// </summary>
        /// <remarks>
        /// <para>/// PointerIntPair - This class implements a pair of a pointer
        /// and small</para>
        /// <para>/// integer.  It is designed to represent this in the space
        /// required by one</para>
        /// <para>/// pointer by bitmangling the integer into the low part of the
        /// pointer.  This</para>
        /// <para>/// can only be done for small integers: typically up to 3 bits,
        /// but it depends</para>
        /// <para>/// on the number of bits available according to
        /// PointerLikeTypeTraits for the</para>
        /// <para>/// type.</para>
        /// <para>///</para>
        /// <para>/// Note that PointerIntPair always puts the IntVal part in the
        /// highest bits</para>
        /// <para>/// possible.  For example, PointerIntPair&lt;void*, 1, bool&gt;
        /// will put the bit for</para>
        /// <para>/// the bool into bit #2, not bit #0, which allows the low two
        /// bits to be used</para>
        /// <para>/// for something else.  For example, this allows:</para>
        /// <para>///   PointerIntPair&lt;PointerIntPair&lt;void*, 1, bool&gt;, 1,
        /// bool&gt;</para>
        /// <para>/// ... and the two bools will land in different bits.</para>
        /// <para>///</para>
        /// </remarks>
        public unsafe partial class PointerIntPair
        {
            [StructLayout(LayoutKind.Explicit, Size = 0)]
            public partial struct Internal
            {
                [FieldOffset(0)]
                public int Value;
            }
        }
    }
}
