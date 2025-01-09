using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace NexusMods.Cascade.Implementation;

/// <summary>
/// A scoped lock that is disposed when the lock is released.
/// </summary>
internal readonly struct ScopedLock
{
    private readonly SemaphoreSlim _lock;

    public ScopedLock()
    {
        _lock = new SemaphoreSlim(1, 1);
    }

    [MustDisposeResource]
    public DisposableLockHandle Lock()
    {
        _lock.Wait();
        return new DisposableLockHandle(_lock);
    }

    [MustDisposeResource]
    public async ValueTask<DisposableLockHandle> LockAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        return new DisposableLockHandle(_lock);
    }

    internal struct DisposableLockHandle : IDisposable
    {
        private readonly SemaphoreSlim _semephoreSlim;

        internal DisposableLockHandle(SemaphoreSlim semaphoreSlim)
        {
            _semephoreSlim = semaphoreSlim;
        }

        public void Dispose()
        {
            _semephoreSlim.Release();
        }
    }
}
