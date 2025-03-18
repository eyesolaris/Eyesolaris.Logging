using Eyesolaris.Logging.Bases;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyesolaris.Logging
{
    internal class RefCounter
    {
        internal int Counter;
    }

    public class RefCountingLoggerProxy : LoggerProxyBase, IRefCountingLogger
    {
        public RefCountingLoggerProxy(IEyeLogger innerLogger)
        {
            _refCount = new() { Counter = 1 };
            _innerLogger = innerLogger;
        }

        private RefCountingLoggerProxy(RefCountingLoggerProxy other)
        {
            
            _refCount = other._refCount;
            _innerLogger = other._innerLogger;
        } 

        public IRefCountingLogger Clone()
        {
            int incremented = Interlocked.Increment(ref _refCount.Counter);
            if (incremented <= 1)
            {
                throw new ObjectDisposedException("Trying to clone a disposed object");
            }
            return new RefCountingLoggerProxy(this);
        }

        public int RefCounter => _refCount.Counter;

        public int AddRef() => Interlocked.Increment(ref _refCount.Counter);

        public int Release()
        {
            int currentValue = Interlocked.Decrement(ref _refCount.Counter);
            if (currentValue == 0)
            {
                _innerLogger.Dispose();
            }
            return currentValue;
        }

        protected override void DisposeManagedResources()
        {
            _innerLogger.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void Lock() => _innerLogger.Lock();

        public override void Unlock() => _innerLogger.Unlock();

        protected override void LogImpl(LogLevel logLevel, ReadOnlySpan<char> message, EventId eventId = default, bool isException = false)
            => _innerLogger.Log(logLevel, message, eventId, isException);

        protected override void LogImpl<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _innerLogger.Log(logLevel, eventId, state, exception, formatter);

        protected override void WriteImpl(ReadOnlySpan<char> text)
            => _innerLogger.Write(text);

        protected override void WriteLineImpl(ReadOnlySpan<char> text)
            => _innerLogger.WriteLine(text);

        protected override void FlushImpl()
            => _innerLogger.Flush();

        protected override IEyeLogger GetLogger()
        {
            return _innerLogger;
        }

        private readonly RefCounter _refCount;
        private readonly IEyeLogger _innerLogger;

        public override string Name => throw new NotImplementedException();
    }
}
