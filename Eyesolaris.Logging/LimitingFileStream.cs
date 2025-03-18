using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eyesolaris.Logging
{
    public class LimitingFileStream : Stream
    {
        public static float DefaultMaxLogSizeInMegabytes { get; private set; } = 500;

        public LimitingFileStream(DirectoryInfo logPath, float maxLogSizeMegabytes, double freeSpaceThreshold)
        {
            int equalPathSymbolsCount = 0;
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                DirectoryInfo root = drive.RootDirectory;
                if (logPath.FullName.StartsWith(root.FullName) && root.FullName.Length > equalPathSymbolsCount)
                {
                    equalPathSymbolsCount = drive.RootDirectory.FullName.Length;
                    Drive = drive;
                }
            }
            if (Drive == null)
            {
                throw new InvalidOperationException("Диск, содержащий папку с логами, не найден");
            }
            Directory.CreateDirectory(logPath.FullName);
            LogPath = logPath;
            MaxLogFileSizeMegabytes = maxLogSizeMegabytes;
            FreeSpaceThreshold = freeSpaceThreshold;
            CurrentFileStream = RecreateLogFile();
            Task.Factory.StartNew(CheckLogSize, _cts.Token, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            _sw.Start();
            _canWrite = _DetermineCanWrite();
        }

        /// <summary>
        /// Method returns value to suppress compiler warnings in constructor
        /// </summary>
        /// <returns></returns>
        private FileStream RecreateLogFile()
        {
            CurrentFileStream?.Dispose();
            string fileName = Logger.GetDateTimeForFileName() + ".txt";
            string fullPath = Path.Combine(LogPath.FullName, fileName);
            CurrentFileStream = new FileStream(fullPath, new FileStreamOptions() { Mode = FileMode.Append, Access = FileAccess.Write, Share = FileShare.Read, Options = FileOptions.WriteThrough });
            return CurrentFileStream;
        }

        public DirectoryInfo LogPath { get; }
        public FileStream CurrentFileStream { get; private set; }
        public float MaxLogFileSizeMegabytes { get; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => CurrentFileStream.Length;

        public override long Position
        {
            get => CurrentFileStream.Position;
            set => throw new NotSupportedException();
        }

        private float CurrentFileSizeInMegas
        {
            get
            {
                return (float)((double)CurrentFileStream.Length / 1024 / 1024);
            }
        }

        public override void Flush()
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                CurrentFileStream.Flush();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return CurrentFileStream.Read(buffer, offset, count);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override int Read(Span<byte> buffer)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return CurrentFileStream.Read(buffer);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return await CurrentFileStream.ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return await CurrentFileStream.ReadAsync(buffer, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override bool CanTimeout => CurrentFileStream.CanTimeout;

        public override void Close()
        {
            _cts.Cancel();
            CurrentFileStream.Close();
        }

        public override void CopyTo(Stream destination, int bufferSize)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                CurrentFileStream.CopyTo(destination, bufferSize);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return CurrentFileStream.CopyToAsync(destination, bufferSize, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override ValueTask DisposeAsync()
        {
            _cts.Cancel();
            return CurrentFileStream.DisposeAsync();
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => CurrentFileStream.FlushAsync(cancellationToken);

        public override int ReadByte()
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return CurrentFileStream.ReadByte();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override int ReadTimeout
        {
            get => CurrentFileStream.ReadTimeout;
            set
            {
                _semaphore.Wait();
                try
                {
                    CurrentFileStream.ReadTimeout = value;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public override int WriteTimeout
        {
            get => CurrentFileStream.WriteTimeout;
            set
            {
                _semaphore.Wait();
                try
                {
                    CurrentFileStream.WriteTimeout = value;
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return CurrentFileStream.BeginRead(buffer, offset, count, callback, state);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (_CanWrite)
            {
                _semaphore.Wait(_cts.Token);
                try
                {
                    return CurrentFileStream.BeginWrite(buffer, offset, count, callback, state);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            throw new InvalidOperationException("Can't write to file now");
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return CurrentFileStream.EndRead(asyncResult);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                CurrentFileStream.EndWrite(asyncResult);
            }
            finally
            {
                _semaphore.Release();
            }
        }

#pragma warning disable CS0672 // Член переопределяет устаревший член
        public override object InitializeLifetimeService()
#pragma warning restore CS0672 // Член переопределяет устаревший член
        {
            _semaphore.Wait(_cts.Token);
            try
            {
#pragma warning disable SYSLIB0010 // Тип или член устарел
                return CurrentFileStream.InitializeLifetimeService();
#pragma warning restore SYSLIB0010 // Тип или член устарел
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                return CurrentFileStream.Seek(offset, origin);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override void SetLength(long value)
        {
            _semaphore.Wait(_cts.Token);
            try
            {
                CurrentFileStream.SetLength(value);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_CanWrite)
            {
                _semaphore.Wait(_cts.Token);
                try
                {
                    CurrentFileStream.Write(buffer, offset, count);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_CanWrite)
            {
                _semaphore.Wait(_cts.Token);
                try
                {
                    CurrentFileStream.Write(buffer);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public override void WriteByte(byte value)
        {
            if (_CanWrite)
            {
                _semaphore.Wait(_cts.Token);
                try
                {
                    CurrentFileStream.WriteByte(value);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_CanWrite)
            {
                _semaphore.Wait(_cts.Token);
                try
                {
                    return CurrentFileStream.WriteAsync(buffer, offset, count, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_CanWrite)
            {
                _semaphore.Wait(_cts.Token);
                try
                {
                    return CurrentFileStream.WriteAsync(buffer, cancellationToken);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            return ValueTask.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            _cts?.Cancel();
            if (disposing)
            {
                CurrentFileStream.Dispose();
            }
        }

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        private async void CheckLogSize(object? data)
        {
            ArgumentNullException.ThrowIfNull(data);
            CancellationToken token = (CancellationToken)data;
            while (true)
            {
                float currentSizeInMegas;
                try
                {
                    currentSizeInMegas = CurrentFileSizeInMegas;
                }
                catch (Exception)
                {
                    await Task.Delay(10000);
                    continue;
                }
                if (currentSizeInMegas >= MaxLogFileSizeMegabytes)
                {
                    _semaphore.Wait();
                    try
                    {
                        RecreateLogFile();
                    }
                    catch (Exception e)
                    {
                        Logger.Global.LogError($"Ошибка при воссоздании файла лога");
                        continue;
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                if (token.IsCancellationRequested)
                {
                    return;
                }
                if (!_CanWrite)
                {
                    if (!_stopped)
                    {
                        _semaphore.Wait();
                        _stopped = true;
                        try
                        {
                            CurrentFileStream.Write(Encoding.ASCII.GetBytes(Environment.NewLine + $"{DateTimeOffset.Now} Stopped logging because of free space lower then threshold {FreeSpaceThreshold.ToString(CultureInfo.InvariantCulture)}" + Environment.NewLine));
                            CurrentFileStream.Flush(flushToDisk: true);
                        }
                        catch (Exception e)
                        {
                            continue;
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }
                }
                else
                {
                    if (_stopped)
                    {
                        _semaphore.Wait();
                        _stopped = false;
                        _semaphore.Release();
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(10));
                //Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }

        public double FreeSpaceThreshold { get; }

        private readonly Stopwatch _sw = new Stopwatch();

        private bool _stopped = false;

        private bool _canWrite = true;
        private bool _CanWrite
        {
            get
            {
                lock (this)
                {
                    if (_sw.ElapsedMilliseconds > 3000)
                    {
                        _sw.Restart();
                        if (_DetermineCanWrite())
                        {
                            _canWrite = true;
                        }
                        else
                        {
                            _canWrite = false;
                        }
                    }
                    return _canWrite;
                }
            }
        }

        private bool _DetermineCanWrite() => ((double)Drive.AvailableFreeSpace / Drive.TotalSize) > FreeSpaceThreshold;

        private DriveInfo Drive { get; set; }
    }
}
