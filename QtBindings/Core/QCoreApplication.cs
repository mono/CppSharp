using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

namespace Qt.Core {
        public class QCoreApplication : QObject {
                #region Sync with qcoreapplication.h
                // C++ interface
                public interface IQCoreApplication : ICppClassOverridable<QCoreApplication> {
                        // ...
                        [Constructor] void QCoreApplication (CppInstancePtr @this, [MangleAs ("int&")] IntPtr argc,
                                                             [MangleAs ("char**")] IntPtr argv);
                        // ...
                        [Static] int exec ();
                        // ...
                        [Virtual] bool notify (CppInstancePtr @this, IntPtr qObject, IntPtr qEvent);
                        [Virtual] bool compressEvent (CppInstancePtr @this, IntPtr qEvent, IntPtr qObject, IntPtr qPostEventList);

			[Virtual, Destructor] void Destruct (CppInstancePtr @this);
                }
                // C++ fields
                private struct _QCoreApplication {
                }
                #endregion

                private static IQCoreApplication impl = Qt.Libs.QtCore.GetClass<IQCoreApplication,_QCoreApplication,QCoreApplication> ("QCoreApplication");
                protected IntPtr argc;
                protected IntPtr argv;


                public QCoreApplication () : base (impl.TypeInfo)
                {
                        Native = impl.Alloc (this);
                        InitArgcAndArgv ();
                        impl.QCoreApplication (Native, argc, argv);
                }

                public QCoreApplication (IntPtr native) : base (impl.TypeInfo)
                {
			Native = native;
                }

		internal QCoreApplication (CppTypeInfo subClass) : base (impl.TypeInfo)
		{
			subClass.AddBase (impl.TypeInfo);
		}

                public virtual int Exec ()
                {
                        return impl.exec ();
                }

                public override void Dispose ()
                {
                        impl.Destruct (Native);
                        FreeArgcAndArgv ();
                        Native.Dispose ();
                }

                protected void InitArgcAndArgv ()
                {
                        // for some reason, this includes arg0, but the args passed to Main (string[]) do not!
                        string[] args = Environment.GetCommandLineArgs ();

                        int argCount = args.Length;
                        argc = Marshal.AllocHGlobal (sizeof (int));
                        Marshal.WriteInt32 (argc, argCount);

                        argv = Marshal.AllocHGlobal (Marshal.SizeOf (typeof (IntPtr)) * argCount);
                        for (var i = 0; i < argCount; i++) {
                                IntPtr arg = Marshal.StringToHGlobalAnsi (args [i]);
                                Marshal.WriteIntPtr (argv, i * Marshal.SizeOf (typeof (IntPtr)), arg);
                        }
                }

                protected void FreeArgcAndArgv ()
                {
                        Marshal.FreeHGlobal (argc);
                        for (var i = 0; i < Environment.GetCommandLineArgs ().Length; i++)
                                Marshal.FreeHGlobal (Marshal.ReadIntPtr (argv, i * Marshal.SizeOf (typeof (IntPtr))));
                        Marshal.FreeHGlobal (argv);
                }
        }
}

