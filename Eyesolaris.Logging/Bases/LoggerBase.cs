using Microsoft.Extensions.Logging;

namespace Eyesolaris.Logging.Bases
{
    public abstract class LoggerBase : Logger
    {
        protected LoggerBase()
        {
            AppDomain.CurrentDomain.ProcessExit += (o, args) => Flush();
        }

        protected override sealed void LogImpl(
            LogLevel logLevel,
            ReadOnlySpan<char> message,
            EventId eventId = default,
            bool isException = false)
        {
            LogGenericImpl(
                logLevel,
                DateTimeOffset.Now,
                GetScopes(),
                eventId,
                message,
                isException);
        }

        protected override sealed void LogImpl<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LogGenericImpl(
                logLevel,
                DateTimeOffset.Now,
                GetScopes(),
                eventId,
                formatter(state, exception),
                exception is not null);
        }

        protected abstract void LogGenericImpl(LogLevel logLevel, DateTimeOffset now, IEnumerable<Scope> scopeChain, EventId eventId, ReadOnlySpan<char> message, bool isException);

        protected override sealed IDisposable? BeginScopeImpl<TState>(TState state)
        {
            return base.BeginScopeImpl(state);
        }
    }
}
