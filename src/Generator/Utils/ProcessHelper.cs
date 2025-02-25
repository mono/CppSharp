using System.Diagnostics;
using System.Text;

namespace CppSharp.Utils
{
    public static class ProcessHelper
    {
        public static string Run(string path, string args, out int error, out string errorMessage)
        {
            return RunFrom(null, path, args, out error, out errorMessage);
        }

        public static string RunFrom(string workingDir, string path, string args, out int error, out string errorMessage)
        {
            using var process = new Process();
            process.StartInfo.WorkingDirectory = workingDir;
            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            var reterror = new StringBuilder();
            var retout = new StringBuilder();
            process.OutputDataReceived += (sender, outargs) =>
            {
                if (string.IsNullOrEmpty(outargs.Data))
                    return;

                if (retout.Length > 0)
                    retout.AppendLine();
                retout.Append(outargs.Data);
            };
            process.ErrorDataReceived += (sender, errargs) =>
            {
                if (string.IsNullOrEmpty(errargs.Data))
                    return;

                if (reterror.Length > 0)
                    reterror.AppendLine();
                reterror.Append(errargs.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            process.CancelErrorRead();

            error = process.ExitCode;
            errorMessage = reterror.ToString();
            return retout.ToString();
        }
    }
}
