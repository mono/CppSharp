namespace CppSharp
{
    /// <summary>
    /// Encodes a location in the source.
    /// The SourceManager can decode this to get at the full include stack,
    /// line and column information.
    /// </summary>
    public struct SourceLocation
    {
        private const uint MacroIDBit = 1U << 31;

        public SourceLocation(uint id)
        {
            ID = id;
        }

        public readonly uint ID;

        public bool IsFileID
        {
            get { return (ID & MacroIDBit) == 0; }
        }

        public bool IsMacroID
        {
            get { return (ID & MacroIDBit) != 0; }
        }

        /// <summary>
        /// Return true if this is a valid SourceLocation object.
        /// </summary>
        public bool IsValid
        {
            get { return ID != 0; }
        }

        /// <summary>
        /// Return true if this is an invalid SourceLocation object.
        /// Invalid SourceLocations are often used when events have no corresponding
        /// location in the source (e.g. a diagnostic is required for a command line
        /// option).
        /// </summary>
        public bool IsInvalid
        {
            get { return ID == 0; }
        }

        /// <summary>
        /// Offset into the source manager's global input view.
        /// </summary>
        public uint Offset
        {
            get { return ID & ~MacroIDBit; }
        }

        public static bool operator ==(SourceLocation a, SourceLocation b)
        {
            return a.ID == b.ID;
        }

        public static bool operator !=(SourceLocation a, SourceLocation b)
        {
            return !(a == b);
        }

        public bool Equals(SourceLocation other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SourceLocation && Equals((SourceLocation)obj);
        }

        public override int GetHashCode()
        {
            return (int)ID;
        }

        public override string ToString()
        {
            if (IsInvalid)
                return "<invalid>";

            return IsMacroID ? "Macro ID: " + ID : "File ID: " + Offset;
        }
    }
}
