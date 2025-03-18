using Microsoft.Extensions.Logging;

namespace Eyesolaris.Logging
{
    public sealed class CombinedLogger : Logger
    {
        public CombinedLogger(IReadOnlyCollection<IEyeLogger> loggers, bool own)
        {
            ArgumentNullException.ThrowIfNull(loggers, nameof(loggers));
            if (loggers.Count == 0)
            {
                throw new ArgumentException("Empty logger collection is not allowed");
            }
            _internalLoggers = loggers.ToArray();
            _owner = own;
        }

        public CombinedLogger(params IEyeLogger[] loggers)
            : this(loggers, own: false)
        {
        }

        public override string Name => "CombinedLogger";

        public override void Lock()
        {
            CheckIfDisposed();
            Monitor.Enter(_lock);
            if (!_owner)
            {
                foreach (IEyeLogger logger in _internalLoggers)
                {
                    logger.Lock();
                }
            }
        }

        public override void Unlock()
        {
            CheckIfDisposed();
            if (!_owner)
            {
                lock (_lock)
                {
                    CheckIfDisposed();
                    foreach (IEyeLogger logger in _internalLoggers)
                    {
                        logger.Unlock();
                    }
                }
            }
            Monitor.Exit(_lock);
        }

        protected override void DisposeUnmanagedResources()
        {
            if (_owner)
            {
                foreach (IEyeLogger logger in _internalLoggers)
                {
                    try
                    {
                        logger.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
                _internalLoggers = Array.Empty<IEyeLogger>();
            }
        }

        protected override void LogImpl(LogLevel logLevel, ReadOnlySpan<char> message, EventId eventId = default, bool isException = false)
        {
            foreach (IEyeLogger logger in _internalLoggers)
            {
                logger.Log(logLevel, message, eventId, isException);
            }
        }

        protected override void LogImpl<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            foreach (IEyeLogger logger in _internalLoggers)
            {
                logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        protected override void WriteImpl(ReadOnlySpan<char> text)
        {
            foreach (IEyeLogger logger in _internalLoggers)
            {
                logger.Write(text);
            }
        }

        protected override void WriteLineImpl(ReadOnlySpan<char> text)
        {
            foreach (IEyeLogger logger in _internalLoggers)
            {
                logger.WriteLine(text);
            }
        }

        protected override void FlushImpl()
        {
            foreach (IEyeLogger logger in _internalLoggers)
            {
                logger.Flush();
            }
        }

        protected override sealed IDisposable? BeginScopeImpl<TState>(TState state)
        {
            IDisposable? currentScope = base.BeginScopeImpl(state);
            IDisposable?[] disposables = new IDisposable?[_internalLoggers.Length + 1];
            disposables[0] = currentScope;
            for (int i = 1; i < _internalLoggers.Length; i++)
            {
                IEyeLogger logger = _internalLoggers[i];
                disposables[i] = logger.BeginScope(state);
            }
            return new CombinedDisposable(disposables);
        }

        protected override void OnAutoFlushChanged(bool autoFlush)
        {
        }

        private IEyeLogger[] _internalLoggers;
        private readonly bool _owner;
        private readonly object _lock = new();

        private class CombinedDisposable : IDisposable
        {
            internal CombinedDisposable(IDisposable?[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (IDisposable? disposable in _disposables)
                {
                    try
                    {
                        disposable?.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            private IDisposable?[] _disposables;
        }
    }
}
