using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CppSharp.AST
{
    /// <summary>
    /// Virtual table component kind.
    /// </summary>
    public enum VTableComponentKind
    {
        VCallOffset,
        VBaseOffset,
        OffsetToTop,
        RTTI,
        FunctionPointer,
        CompleteDtorPointer,
        DeletingDtorPointer,
        UnusedFunctionPointer,
    }

    /// <summary>
    /// Represents a C++ virtual table component.
    /// </summary>
    [DebuggerDisplay("{Kind}, {Offset}, {Declaration}")]
    public struct VTableComponent
    {
        public VTableComponentKind Kind;
        public ulong Offset;
        public Declaration Declaration;

        /// Method declaration (if Kind == FunctionPointer).
        public Method Method => Declaration as Method;
    }

    /// <summary>
    /// Represents a C++ virtual table layout.
    /// </summary>
    public class VTableLayout
    {
        public List<VTableComponent> Components { get; set; }

        public VTableLayout()
        {
            Components = new List<VTableComponent>();
        }
    }

    /// <summary>
    /// Contains information about virtual function pointers.
    /// </summary>
    public struct VFTableInfo
    {
        /// If nonzero, holds the vbtable index of the virtual base with the vfptr.
        public ulong VBTableIndex;

        /// This is the offset of the vfptr from the start of the last vbase,
        /// or the complete type if there are no virtual bases.
        public long VFPtrOffset;

        /// This is the full offset of the vfptr from the start of the complete type.
        public long VFPtrFullOffset;

        /// Layout of the table at this pointer.
        public VTableLayout Layout;
    }

    public enum RecordArgABI
    {
        /// <summary>
        /// <para>Pass it using the normal C aggregate rules for the ABI,</para>
        /// <para>potentially introducing extra copies and passing some</para>
        /// <para>or all of it in registers.</para>
        /// </summary>
        Default = 0,
        /// <summary>
        /// <para>Pass it on the stack using its defined layout.</para>
        /// <para>The argument must be evaluated directly into the correct</para>
        /// <para>stack position in the arguments area, and the call machinery</para>
        /// <para>must not move it or introduce extra copies.</para>
        /// </summary>
        DirectInMemory = 1,
        /// <summary>Pass it as a pointer to temporary memory.</summary>
        Indirect = 2
    };

    // Represents ABI-specific layout details for a class.
    public class ClassLayout
    {
        public CppAbi ABI { get; set; }

        /// Provides native argument ABI information.
        public RecordArgABI ArgABI { get; set; }

        /// Virtual function tables in Microsoft mode.
        public List<VFTableInfo> VFTables { get; set; }

        /// Virtual table layout in Itanium mode.
        public VTableLayout Layout { get; set; }

        public ClassLayout()
        {
            VFTables = new List<VFTableInfo>();
            Fields = new List<LayoutField>();
            Bases = new List<LayoutBase>();
        }

        public List<LayoutField> Fields { get; private set; }
        public List<LayoutBase> Bases { get; private set; }

        public ClassLayout(ClassLayout classLayout)
            : this()
        {
            ABI = classLayout.ABI;
            HasOwnVFPtr = classLayout.HasOwnVFPtr;
            VBPtrOffset = classLayout.VBPtrOffset;
            PrimaryBase = classLayout.PrimaryBase;
            HasVirtualBases = classLayout.HasVirtualBases;
            Alignment = classLayout.Alignment;
            Size = classLayout.Size;
            DataSize = classLayout.DataSize;
            VFTables.AddRange(classLayout.VFTables);
            if (classLayout.Layout != null)
            {
                Layout = new VTableLayout();
                Layout.Components.AddRange(classLayout.Layout.Components);
            }
        }

        /// <summary>
        /// Does this class provide its own virtual-function table
        /// pointer, rather than inheriting one from a primary base
        /// class? If so, it is at offset zero.
        /// </summary>
        public bool HasOwnVFPtr;

        /// <summary>
        /// Get the offset for virtual base table pointer.
        /// This is only meaningful with the Microsoft ABI.
        /// </summary>
        public long VBPtrOffset;

        /// <summary>
        /// Primary base for this record.
        /// </summary>
        public Class PrimaryBase;

        public bool HasVirtualBases;

        public int Alignment;
        public int Size;
        public int DataSize;

        public IList<LayoutField> VTablePointers
        {
            get
            {
                if (vTablePointers == null)
                {
                    vTablePointers = ABI == CppAbi.Microsoft ?
                        new List<LayoutField>(Fields.Where(f => f.IsVTablePtr)) :
                        new List<LayoutField> { Fields.First(f => f.IsVTablePtr) };
                }
                return vTablePointers;
            }
        }

        /// <summary>
        /// Indicates whether this class layout has a subclass at a non-zero offset.
        /// </summary>
        public bool HasSubclassAtNonZeroOffset { get; set; }

        private List<LayoutField> vTablePointers;
    }
}
