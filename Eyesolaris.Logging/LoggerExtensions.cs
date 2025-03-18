namespace Eyesolaris.Logging
{
    public static class LoggerExtensions
    {
        public static IRefCountingLogger CreateRefCountingWrapper(this IEyeLogger logger)
        {
            return new RefCountingLoggerProxy(logger);
        }
    }
}
