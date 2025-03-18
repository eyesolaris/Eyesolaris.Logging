using Eyesolaris.Logging.Bases;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyesolaris.Logging
{
    public class ConsoleLogger : TextWriterLoggerBase
    {
        public override string Name => "ConsoleLogger";

        protected override void FlushImpl()
        {
            Console.Out.Flush();
        }

        protected override void LogGenericImpl(LogLevel logLevel, DateTimeOffset now, IEnumerable<Scope> scopeChain, EventId eventId, ReadOnlySpan<char> message, bool isException)
        {
            TextWriter consoleWriter = isException ? Console.Error : Console.Out;
            LogInTextWriter(consoleWriter, logLevel, now, scopeChain, eventId, message, isException);
        }

        protected override void WriteImpl(ReadOnlySpan<char> text)
        {
            Console.Out.Write(text);
        }

        protected override void WriteLineImpl(ReadOnlySpan<char> text)
        {
            Console.Out.WriteLine(text);
        }

        protected override void WriteScope(TextWriter textWriter, IEnumerable<Scope> scopeChain)
            => _WriteInColsoleColored(
                base.WriteScope,
                textWriter,
                scopeChain,
                ConsoleColor.DarkCyan);

        protected override void WriteLevelHeader(TextWriter textWriter, LogLevel logLevel)
            => _WriteInColsoleColored(
                base.WriteLevelHeader,
                textWriter,
                logLevel,
                _LogLevelToConsoleColor(logLevel));

        protected override void WriteExceptionHeader(TextWriter textWriter, bool isException)
            => _WriteInColsoleColored(
                base.WriteExceptionHeader,
                textWriter,
                isException,
                ConsoleColor.DarkMagenta);

        protected override void WriteEventId(TextWriter textWriter, EventId eventId)
            => _WriteInColsoleColored(
                base.WriteEventId,
                textWriter,
                eventId,
                ConsoleColor.DarkGreen);

        private static void _WriteInColsoleColored<TParam>(Action<TextWriter, TParam> writeAction, TextWriter textWriter, TParam parameter, ConsoleColor color)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            writeAction(textWriter, parameter);
            Console.ForegroundColor = oldColor;
        }

        private static ConsoleColor _LogLevelToConsoleColor(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => ConsoleColor.DarkGray,
                LogLevel.Debug => ConsoleColor.Cyan,
                LogLevel.Information => ConsoleColor.Green,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
