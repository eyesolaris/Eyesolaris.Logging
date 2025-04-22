using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Eyesolaris.Logging
{
    internal static class LoggerDefaults
    {
        internal const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Information;
        internal const double DEFAULT_FREE_SPACE_LOG_THRESHOLD = 0.1;
        internal const bool DEFAULT_AUTO_FLUSH = true;

        internal const float DEFAULT_MAX_LOG_SIZE_IN_MB = 500;
    }

    public abstract partial class Logger : IEyeLogger
    {
        private LogLevel _logLevel = LoggerDefaults.DEFAULT_LOG_LEVEL;
        public LogLevel LogLevel
        {
            get => _logLevel;

            set
            {
                CheckIfDisposed();
                Lock();
                try
                {
                    _logLevel = value;
                    OnLogLevelChanged(value);
                }
                finally
                {
                    Unlock();
                }
            }
        }

        private bool _autoFlush = LoggerDefaults.DEFAULT_AUTO_FLUSH;

        public virtual bool AutoFlush
        {
            get => _autoFlush;

            set
            {
                CheckIfDisposed();
                Lock();
                try
                {
                    _autoFlush = value;
                    OnAutoFlushChanged(value);
                }
                finally
                {
                    Unlock();
                }
            }
        }

        public bool HasScope => _scopes.Count > 0;
        public abstract string Name { get; }

        public abstract void Lock();

        public abstract void Unlock();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            CheckIfDisposed();
            //lock (_lock)
            Lock();
            try
            {
                return BeginScopeImpl(state);
            }
            finally
            {
                Unlock();
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CheckIfDisposed();
            //lock (_lock)
            Lock();
            try
            {
                if (IsEnabled(logLevel) && logLevel < LogLevel.None)
                {
                    LogImpl(logLevel, eventId, state, exception, formatter);
                    if (AutoFlush)
                    {
                        FlushImpl();
                    }
                }
            }
            finally
            {
                Unlock();
            }
        }

        public void Log(
            LogLevel logLevel,
            ReadOnlySpan<char> message,
            EventId eventId = default,
            bool isException = false)
        {
            CheckIfDisposed();
            Lock();
            try
            {
                if (IsEnabled(logLevel) && logLevel < LogLevel.None)
                {
                    LogImpl(logLevel, message, eventId, isException);
                    if (AutoFlush)
                    {
                        FlushImpl();
                    }
                }
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Write any text into log
        /// </summary>
        /// <param name="text"></param>
        public void Write(ReadOnlySpan<char> text)
        {
            CheckIfDisposed();
            //lock (_lock)
            Lock();
            try
            {
                WriteImpl(text);
                if (AutoFlush)
                {
                    FlushImpl();
                }
            }
            finally
            {
                Unlock();
            }
        }

        public void WriteLine(ReadOnlySpan<char> text)
        {
            CheckIfDisposed();
            Lock();
            //lock (_lock)
            try
            {
                WriteLineImpl(text);
                if (AutoFlush)
                {
                    FlushImpl();
                }
            }
            finally
            {
                Unlock();
            }
        }

        public void Flush()
        {
            CheckIfDisposed();
            Lock();
            try
            {
                FlushImpl();
            }
            finally
            {
                Unlock();
            }
        }

        public static string GetDefaultLevelHeader(LogLevel level) => level switch
        {
            LogLevel.Trace => "TRACE: ",
            LogLevel.Debug => "DEBUG: ",
            LogLevel.Information => "INFO: ",
            LogLevel.Warning => "WARN: ",
            LogLevel.Error => "ERROR: ",
            LogLevel.Critical => "CRIT: ",
            _ => throw new InvalidOperationException("UNREACHABLE"),
        };

        [MemberNotNullWhen(false, nameof(_logPath))]
        public static bool IsDefaultPath { get; set; } = true;

        public static string LogFolderName { get; set; } = "UnknownApp";

        private static string? _logPath;

        public static string DefaultLogPath
        {
            get
            {
                if (IsDefaultPath)
                {
                    var sep = Path.DirectorySeparatorChar;
                    return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + sep + LogFolderName + sep + "logs";
                }
                else
                {
                    return _logPath;
                }
            }

            private set
            {
                _logPath = value;
                IsDefaultPath = false;
            }
        }

        public static CultureInfo GetThreadOrCurrentCulture()
        {
            return CultureInfo.DefaultThreadCurrentCulture ?? CultureInfo.CurrentCulture;
        }

        public static string GetDateTimeForFileName()
        {
            return DateTimeOffset.Now.LocalDateTime.ToString("yyyy.MM.dd._HH-mm-ss");
        }

        public static void SetLogPath(string path)
        {
            DefaultLogPath = path;
        }

        public static void SetLogger(IEyeLogger instance)
        {
            if (instance is LoggerProxy)
            {
                throw new InvalidOperationException($"Using a {nameof(LoggerProxy)} as a global logger is forbidden, as it will cause an infinite recursion");
            }
            IEyeLogger prev = Global;
            // Locking to prevent logger slicing while using GlobalLoggerProxy
            prev.Lock();
            Global = instance;
            prev.Unlock();
        }

        public static IEyeLogger Global { get; private set; } = new ConsoleLogger();

        public static TextWriterLogger CreateFileLogger(DirectoryInfo logDir, float maxFileSizeInMegas, double freeSpaceThreshold = LoggerDefaults.DEFAULT_FREE_SPACE_LOG_THRESHOLD)
        {
            LimitingFileStream stream = new(logDir, maxFileSizeInMegas, freeSpaceThreshold);
            TextWriter writer = new StreamWriter(stream, leaveOpen: false);
            return new TextWriterLogger(writer, takeOwnership: true);
        }

        public static TextWriterLogger CreateFileLogger(float maxFileSizeInMegas, double freeSpaceThreshold = LoggerDefaults.DEFAULT_FREE_SPACE_LOG_THRESHOLD)
        {
            return CreateFileLogger(new DirectoryInfo(DefaultLogPath), maxFileSizeInMegas, freeSpaceThreshold);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logDir"></param>
        /// <exception cref="IOException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="PathTooLongException"></exception>
        /// <returns></returns>
        public static TextWriterLogger CreateFileLogger(DirectoryInfo logDir, double freeSpaceThreshold = LoggerDefaults.DEFAULT_FREE_SPACE_LOG_THRESHOLD)
        {
            return CreateFileLogger(logDir, LoggerDefaults.DEFAULT_MAX_LOG_SIZE_IN_MB, freeSpaceThreshold);
        }

        public static TextWriterLogger CreateFileLogger(double freeSpaceThreshold = LoggerDefaults.DEFAULT_FREE_SPACE_LOG_THRESHOLD)
        {
            return CreateFileLogger(LoggerDefaults.DEFAULT_MAX_LOG_SIZE_IN_MB, freeSpaceThreshold);
        }

        public static IRefCountingLogger CreateRefCountingWrapper(IEyeLogger logger)
        {
            return new RefCountingLoggerProxy(logger);
        }

        protected abstract void LogImpl(
            LogLevel logLevel,
            ReadOnlySpan<char> message,
            EventId eventId = default,
            bool isException = false);

        protected abstract void LogImpl<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter);

        protected abstract void WriteImpl(ReadOnlySpan<char> text);
        protected abstract void WriteLineImpl(ReadOnlySpan<char> text);
        protected abstract void FlushImpl();

        protected virtual void OnLogLevelChanged(LogLevel newLogLevel)
        {
        }

        protected virtual void OnAutoFlushChanged(bool autoFlush)
        {
        }

        protected virtual IDisposable? BeginScopeImpl<TState>(TState state)
            where TState : notnull
        {
            CheckIfDisposed();
            Scope s = new Scope<TState>(this, state);
            AddScopeNoLock(s);
            return s;
        }

        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // освобождение управляемого состояния (управляемые объекты)
                    DisposeManagedResources();
                }

                // освобождение неуправляемых ресурсов (неуправляемые объекты)
                DisposeUnmanagedResources();
                // установка значения NULL для больших полей
                NullifyLargeFields();
                _disposed = true;
            }
        }

        protected virtual void DisposeManagedResources() { }
        /// <summary>
        /// When implementing, don't forget to call the base method
        /// </summary>
        protected virtual void DisposeUnmanagedResources() { }
        protected virtual void NullifyLargeFields() { }

        // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        ~Logger()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected void AddScopeNoLock(Scope scope)
        {
            _scopes.Add(scope.StateHash, scope);
        }

        protected void RemoveScopeWithLock(Scope scope)
        {
            //lock (_lock)
            Lock();
            try
            {
                _scopes.Remove(scope.StateHash);
            }
            finally
            {
                Unlock();
            }
        }

        protected IEnumerable<Scope> GetScopes()
        {
            return _scopes.Values.Cast<Scope>();
        }

        private bool _disposed;
        private readonly OrderedDictionary _scopes = new();

        protected void CheckIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(Logger), "Logger is already disposed");
            }
        }
    }
}
