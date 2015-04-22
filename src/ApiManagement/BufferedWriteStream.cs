using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace ApiManagement
{
    /// <summary>
    /// A Stream that wraps another stream and enables reading by buffering the content as it is writen.
    /// The content is buffered in memory.
    /// </summary>
    public class BufferingWriteStream : Stream
    {
        private readonly Stream _inner;

        private Stream _buffer = new MemoryStream(); 

        private bool _disposed;

        public BufferingWriteStream([NotNull] Stream inner)
        {
            _inner = inner;
        }

        public Stream Buffer
        {
            get { return _buffer; }
        }

        public override bool CanRead
        {
            get { return _inner.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _inner.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _inner.CanWrite; }
        }

        public override long Length
        {
            get { return _inner.Length; }
        }

        public override long Position
        {
            get { return _inner.Position; }
            set { _inner.Position = value; }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _inner.Seek(offset, origin);
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }
#if DNX451
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            ThrowIfDisposed();
            var tcs = new TaskCompletionSource<int>(state);
            BeginRead(buffer, offset, count, callback, tcs);
            return tcs.Task;
        }

        private async void BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, TaskCompletionSource<int> tcs)
        {
            try
            {
                var read = await ReadAsync(buffer, offset, count);
                tcs.TrySetResult(read);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            if (callback != null)
            {
                try
                {
                    callback(tcs.Task);
                }
                catch (Exception)
                {
                    // Suppress exceptions on background threads.
                }
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            var task = (Task<int>)asyncResult;
            return task.GetAwaiter().GetResult();
        }
#endif
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _inner.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffer.Write(buffer, offset, count);
            _inner.Write(buffer, offset, count);
        }
#if DNX451
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            ThrowIfDisposed();
            var tcs = new TaskCompletionSource<object>(state);
            BeginWrite(buffer, offset, count, callback, tcs);
            return tcs.Task;
        }
        
        private async void BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, TaskCompletionSource<object> tcs)
        {
            try
            {
                await _buffer.WriteAsync(buffer, offset, count);
                await WriteAsync(buffer, offset, count);        
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            if (callback != null)
            {
                try
                {
                    callback(tcs.Task);
                }
                catch (Exception)
                {
                    // Suppress exceptions on background threads.
                }
            }
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {           
            var task = (Task)asyncResult;
            task.GetAwaiter().GetResult();
        }
#endif
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _buffer.WriteAsync(buffer, offset, count, cancellationToken);
            await _inner.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override void SetLength(long value)
        {
            _inner.SetLength(value);
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _buffer.Dispose();
                    _inner.Dispose();
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BufferingWriteStream));
            }
        }
    }
}