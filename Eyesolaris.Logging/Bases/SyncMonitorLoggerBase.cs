namespace Eyesolaris.Logging.Bases
{
    public abstract class SyncMonitorLoggerBase : LoggerBase
    {
        public object SyncRoot => _lock;

        public override sealed void Lock()
        {
            Monitor.Enter(_lock);
        }

        public override sealed void Unlock()
        {
            Monitor.Exit(_lock);
        }

        private readonly object _lock = new();
    }
}
