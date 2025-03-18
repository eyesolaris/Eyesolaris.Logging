using Microsoft.Extensions.Logging;

namespace Eyesolaris.Logging
{
    public interface IEyeLogger : ILogger, IDisposable
    {
        LogLevel LogLevel { get; set; }
        bool AutoFlush { get; set; }
        bool HasScope { get; }
        string Name { get; }

        void Flush();

        void Log(
            LogLevel logLevel,
            ReadOnlySpan<char> message,
            EventId eventId = default,
            bool isException = false);

        void Write(ReadOnlySpan<char> text);
        void WriteLine(ReadOnlySpan<char> text);

        /// <summary>
        /// Must be reenterable
        /// </summary>
        void Lock();

        /// <summary>
        /// Must be reenterable
        /// </summary>
        void Unlock();
    }
}
