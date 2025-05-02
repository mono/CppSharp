using System.Collections.Generic;

namespace CppSharp.Generators.Registrable
{
    public class InfoMap<T> : Dictionary<InfoEntry, T>
    {
        public InfoMap() : base()
        {
        }

        public T1 Get<T1>(InfoEntry infoEntry) where T1 : T
        {
            return (T1)this[infoEntry];
        }
    }
}
