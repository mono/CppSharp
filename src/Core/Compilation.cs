namespace CppSharp
{
    public enum CompilationTarget
    {
        SharedLibrary,
        StaticLibrary,
        Application
    }

    public class CompilationOptions
    {
        /// <summary>
        /// Target platform for code compilation.
        /// </summary>
        public TargetPlatform? Platform;

        /// <summary>
        /// Specifies the VS version.
        /// </summary>
        /// <remarks>When null, latest is used.</remarks>
        public VisualStudioVersion VsVersion;

        // If code compilation is enabled, then sets the compilation target.
        public CompilationTarget Target;

        // If true, will compile the generated as a shared library / DLL.
        public bool CompileSharedLibrary => Target == CompilationTarget.SharedLibrary;

        // If true, will force the generation of debug metadata for the native
        // and managed code.
        public bool DebugMode;
    }
}
