namespace Eyesolaris.Logging
{
    public sealed class GlobalLoggerProxy : LoggerProxy
    {
        public GlobalLoggerProxy()
            : base(() => Global)
        {
        }

        public override sealed string Name => "GlobalLoggerProxy";
    }
}
