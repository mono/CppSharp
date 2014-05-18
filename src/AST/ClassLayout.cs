using System.Collections.Generic;
using System.Diagnostics;

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
        public Method Method
        {
            get
            {
                Debug.Assert(Kind == VTableComponentKind.FunctionPointer);
                return Declaration as Method;
            }
        }

        public bool Ignore
        {
            get
            {
                return Method != null &&
                       !Method.IsDeclared &&
                       ((Class) Method.Namespace).GetPropertyByConstituentMethod(Method) == null;
            }
        }
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

    // Represents ABI-specific layout details for a class.
    public class ClassLayout
    {
        public CppAbi ABI { get; set; }

        /// Virtual function tables in Microsoft mode.
        public List<VFTableInfo> VFTables { get; set; }

        /// Virtual table layout in Itanium mode.
        public VTableLayout Layout { get; set; }

        public ClassLayout()
        {
            VFTables = new List<VFTableInfo>();
        }

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
    }
}
