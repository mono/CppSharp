using System;
using System.Runtime.InteropServices;
using Mono.VisualC.Interop;

namespace Qt.Core {
        public class QCoreApplication : QObject {
                #region Sync with qcoreapplication.h
                // C++ interface
                protected interface IQCoreApplication : ICppClassOverridable<QCoreApplication>, Base<QObject.IQObject> {
                        // ...
                        void QCoreApplication (CppInstancePtr @this, [MangleAs ("System.Int32&")] IntPtr argc,
                                               [MangleAs (typeof (string[]))] IntPtr argv);
                        // ...
                        [Static] int exec ();
                        // ...
                        [Virtual] bool notify (CppInstancePtr @this, IntPtr qObject, IntPtr qEvent);
                        [Virtual] bool compressEvent (CppInstancePtr @this, IntPtr qEvent, IntPtr qObject, IntPtr qPostEventList);
                }
                // C++ fields
                private struct _QCoreApplication {
                }
                #endregion

                private static IQCoreApplication impl = Qt.Libs.QtCore.GetClass<IQCoreApplication,_QCoreApplication,QCoreApplication> ("QCoreApplication");
                protected IntPtr argc;
                protected IntPtr argv;


                public QCoreApplication () : base (IntPtr.Zero)
                {
                        this.native = impl.Alloc (this);
                        InitArgcAndArgv ();
                        impl.QCoreApplication (native, argc, argv);
                }

                public QCoreApplication (IntPtr native) : base (native)
                {
                }

                // TODO: Should this be virtual in C#? was static in C++, but alas,
                //  I made it an instance, since that seems to fit .net semantics a bit better...
                //  but if I don't make it virtual, then what to do about Exec () in QApplication?
                public virtual int Exec ()
                {
                        return impl.exec ();
                }

                public override int NativeSize {
                        get { return impl.NativeSize + base.NativeSize; }
                }

                public override void Dispose ()
                {
                        impl.Destruct (native);
                        FreeArgcAndArgv ();
                        native.Dispose ();
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

