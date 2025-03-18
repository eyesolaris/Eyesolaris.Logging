using Microsoft.Extensions.Logging;

namespace Eyesolaris.Logging.Bases
{
    public abstract class TextWriterLoggerBase : SyncMonitorLoggerBase
    {
        protected void LogInTextWriter(TextWriter textWriter, LogLevel logLevel, DateTimeOffset now, IEnumerable<Scope> scopeChain, EventId eventId, ReadOnlySpan<char> message, bool isException)
        {
            WriteDateTime(textWriter, now);
            WriteScope(textWriter, scopeChain);
            WriteLevelHeader(textWriter, logLevel);
            WriteExceptionHeader(textWriter, isException);
            WriteEventId(textWriter, eventId);
            WriteMessage(textWriter, message);
        }

        protected virtual void WriteDateTime(TextWriter textWriter, DateTimeOffset now)
        {
            textWriter.Write(now.ToString(null, GetThreadOrCurrentCulture()));
            textWriter.Write(' ');
        }

        protected virtual void WriteScope(TextWriter textWriter, IEnumerable<Scope> scopeChain)
        {
            if (!HasScope)
            {
                return;
            }
            textWriter.Write("Scope: ");
            IEnumerator<Scope> enumerator = scopeChain.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return;
            }
            bool notLast;
            do
            {
                Scope currentScope = enumerator.Current;
                notLast = enumerator.MoveNext();
                WriteScopeElement(textWriter, currentScope, notLast);
            } while (!notLast);
        }

        protected virtual void WriteScopeElement(TextWriter textWriter, Scope scopeElement, bool notLast)
        {
            textWriter.Write(scopeElement.ToString());
            textWriter.Write(", ");
        }

        protected virtual void WriteLevelHeader(TextWriter textWriter, LogLevel logLevel)
        {
            textWriter.Write(GetDefaultLevelHeader(logLevel));
        }

        protected virtual void WriteExceptionHeader(TextWriter textWriter, bool isException)
        {
            if (isException)
            {
                textWriter.Write("EXCEPTION OCCURED. ");
            }
        }

        protected virtual void WriteEventId(TextWriter textWriter, EventId eventId)
        {
            if (eventId != default)
            {
                textWriter.Write($"Event {eventId.Id}");
                if (eventId.Name is not null)
                    textWriter.Write($" ({eventId.Name}). ");
            }
        }

        protected virtual void WriteMessage(TextWriter textWriter, ReadOnlySpan<char> message)
        {
            textWriter.WriteLine(message);
        }
    }
}
