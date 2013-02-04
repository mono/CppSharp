using System;

namespace Cxxi
{
    public enum DiagnosticKind
    {
        Message,
        Warning,
        Error
    }

    public struct DiagnosticInfo
    {
        public string Message;
        public string File;
        public int Line;
        public int Column;
    }

    public interface IDiagnosticConsumer
    {
        void Emit(DiagnosticKind kind, DiagnosticInfo info);
    }

    public class TextDiagnosticPrinter : IDiagnosticConsumer
    {
        public void Emit(DiagnosticKind kind, DiagnosticInfo info)
        {
            Console.WriteLine(info.Message);
        }
    }
}
