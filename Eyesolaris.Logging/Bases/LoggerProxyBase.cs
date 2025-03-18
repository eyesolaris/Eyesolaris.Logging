using Microsoft.Extensions.Logging;

namespace Eyesolaris.Logging.Bases
{
    public abstract class LoggerProxyBase : Logger
    {
        public override bool AutoFlush
        {
            get => base.AutoFlush;

            set
            {
                IEyeLogger source = GetLogger();
                if (!source.AutoFlush)
                {
                    throw new InvalidOperationException("AutoFlush can't be set to true" +
                        " as AutoFlush of the source logger is not set");
                }
                base.AutoFlush = value;
            }
        }

        protected abstract IEyeLogger GetLogger();

        public override string Name => "Logger Proxy";

        public override void Lock() => GetLogger().Lock();

        public override void Unlock() => GetLogger().Unlock();

        protected override void LogImpl(LogLevel logLevel, ReadOnlySpan<char> message, EventId eventId, bool isException)
        {
            GetLogger().Log(logLevel, message, eventId, isException);
        }

        protected override void LogImpl<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            GetLogger().Log(logLevel, eventId, state, exception, formatter);
        }

        protected override void WriteImpl(ReadOnlySpan<char> text)
        {
            GetLogger().Write(text);
        }

        protected override void WriteLineImpl(ReadOnlySpan<char> text)
        {
            GetLogger().WriteLine(text);
        }

        protected override void FlushImpl()
        {
            GetLogger().Flush();
        }
    }
}
