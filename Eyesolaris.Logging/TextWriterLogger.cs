using Eyesolaris.Logging.Bases;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;

namespace Eyesolaris.Logging
{
    public class TextWriterLogger : TextWriterLoggerBase
    {
        public override string Name => "TextWriterLogger";

        public TextWriterLogger(TextWriter textWriter, bool takeOwnership)
        {
            TextWriter = textWriter;
            _onwershipTaken = takeOwnership;
        }

        public TextWriter TextWriter { get; }

        protected override void FlushImpl()
        {
            TextWriter.Flush();
        }

        protected override sealed void LogGenericImpl(LogLevel logLevel, DateTimeOffset now, IEnumerable<Scope> scopeChain, EventId eventId, ReadOnlySpan<char> message, bool isException)
        {
            LogInTextWriter(TextWriter, logLevel, now, scopeChain, eventId, message, isException);
        }

        protected override void WriteImpl(ReadOnlySpan<char> text)
        {
            TextWriter.Write(text);
        }

        protected override void WriteLineImpl(ReadOnlySpan<char> text)
        {
            TextWriter.WriteLine(text);
        }

        protected override void DisposeManagedResources()
        {
            if (_onwershipTaken)
            {
                TextWriter.Dispose();
            }
        }

        private readonly bool _onwershipTaken;
    }
}
