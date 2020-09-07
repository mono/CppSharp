namespace CppSharp.Runtime
{
    // Struct added to help map a the wchar_t C++ type to C#
    // The data is stored as a 64 bit value so it could represent
    // an UTF-32 character but C# only represents UTF-16 characters
    // so beware of possible data loss when using it.
    public struct WideChar
    {
        public long Value { get; set; }

        public WideChar(char c)
        {
            Value = c;
        }

        public WideChar(long l)
        {
            Value = l;
        }

        public static implicit operator char(WideChar c)
        {
            return System.Convert.ToChar(c.Value);
        }

        public static implicit operator WideChar(long l)
        {
            return new WideChar { Value = l };
        }

        public static bool operator ==(WideChar wc, char c)
        {
            if (ReferenceEquals(null, c))
                return false;

            if (ReferenceEquals(null, wc))
                return false;

            return System.Convert.ToChar(wc.Value) == c;
        }

        public static bool operator !=(WideChar wc, char c)
        {
            return !(wc == c);
        }

        public static bool operator ==(WideChar wc, long l)
        {
            if (ReferenceEquals(null, l))
                return false;

            if (ReferenceEquals(null, wc))
                return false;

            return wc.Value == l;
        }

        public static bool operator !=(WideChar wc, long l)
        {
            return !(wc == l);
        }

        public bool Equals(WideChar obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return Value.Equals(obj.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == GetType() && Equals((WideChar)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Value.GetHashCode();
            }
        }
    }
}
