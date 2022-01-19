using System;
using System.Collections.Generic;
using System.Text;

namespace CSharp
{
    public partial class TestFinalizer
    {
        public static Action<bool, IntPtr> DisposeCallback { get; set; }

        partial void DisposePartial(bool disposing)
        {
            DisposeCallback?.Invoke(disposing, __Instance);
        }
    }
}
