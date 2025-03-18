namespace Eyesolaris.Logging
{
    /// <summary>
    /// Method <see cref="IDisposable.Dispose"/> decrements a counter
    /// </summary>
    public interface IRefCountingLogger : IEyeLogger, ICloneable
    {
        int RefCounter { get; }
        /// <summary>
        /// Increments the reference counter
        /// </summary>
        /// <returns>Ref count after increment</returns>
        int AddRef();
        /// <summary>
        /// Decrements the reference counter and disposes self
        /// if reference counter is zero
        /// </summary>
        /// <returns>Ref count after decrement</returns>
        int Release();
        new IRefCountingLogger Clone();

        object ICloneable.Clone() => Clone();
    }
}
