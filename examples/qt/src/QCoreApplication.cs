using System;
using System.Runtime.InteropServices;
using Mono.Cxxi;

namespace Qt.Gui {
	public partial class QCoreApplication {
		
		public partial interface IQCoreApplication {
			[Constructor] CppInstancePtr QCoreApplication (CppInstancePtr @this, [MangleAs ("int&")] IntPtr argc, [MangleAs ("char**")] IntPtr argv);
		}

		protected IntPtr argc, argv;
	
		public QCoreApplication () : base (impl.TypeInfo)
		{
		       	InitArgcAndArgv ();
		        Native = impl.QCoreApplication (impl.Alloc (this), argc, argv);
		}
	
		partial void AfterDestruct ()
		{
			FreeArgcAndArgv ();
		}
	
		protected void InitArgcAndArgv ()
		{
			var args = Environment.GetCommandLineArgs ();
			var argCount = args.Length;
	
			argc = Marshal.AllocHGlobal (sizeof(int));
			Marshal.WriteInt32 (argc, argCount);
	
			argv = Marshal.AllocHGlobal (Marshal.SizeOf (typeof(IntPtr)) * argCount);
			for (var i = 0; i < argCount; i++) {
				IntPtr arg = Marshal.StringToHGlobalAnsi (args [i]);
				Marshal.WriteIntPtr (argv, i * Marshal.SizeOf (typeof(IntPtr)), arg);
			}
		}
	
		protected void FreeArgcAndArgv ()
		{
			Marshal.FreeHGlobal (argc);
			for (var i = 0; i < Environment.GetCommandLineArgs ().Length; i++)
				Marshal.FreeHGlobal (Marshal.ReadIntPtr (argv, i * Marshal.SizeOf (typeof(IntPtr))));
			Marshal.FreeHGlobal (argv);
		}
		
	}
}

