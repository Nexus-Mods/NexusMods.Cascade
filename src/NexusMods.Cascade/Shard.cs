namespace NexusMods.Cascade;

using System;
using System.Collections.Generic;
using System.Threading;

public class Shard : IDisposable
{
    private readonly Queue<Action> _queue = new Queue<Action>();
    private readonly Thread _workerThread;
    private readonly object _lock = new object();

    private bool _disposed = false;      // set true once Dispose() is called
    private bool _isRunning = false;     // true if any action (queued or inline) has the shard
    private bool _inlinePending = false; // true if ≥1 inline caller is waiting

    private int _runningThreadId = -1; // The managed thread id of the currently running thread
    public Shard()
    {
        _workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "ShardWorkerThread"
        };
        _workerThread.Start();
    }

    /// <summary>
    /// Enqueue a callback to run on the dedicated worker thread.
    /// Throws ObjectDisposedException if the shard has been disposed.
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Shard));
            _queue.Enqueue(action);
            // Wake up the worker if it is parked
            Monitor.PulseAll(_lock);
        }
    }


    /// <summary>
    /// Run the given action inline on the caller's thread, but only once no other action
    /// (queued or inline) is running. If necessary, block until the shard is free.
    /// Throws ObjectDisposedException if the shard has already been disposed.
    /// </summary>
    public TOut RunInline<TIn, TOut>(Func<TIn, TOut> action, TIn input)
     where TIn : allows ref struct
     where TOut : allows ref struct
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(Shard));

            // Announce that at least one inline caller is waiting
            _inlinePending = true;

            // Wait until _isRunning is false (no action currently holding the shard)
            while (_isRunning)
            {
                Monitor.Wait(_lock);
            }

            // Now we have exclusive access—claim it:
            _isRunning = true;
            // Clear the inline‐pending flag for *this* caller
            _inlinePending = false;
            _runningThreadId = Environment.CurrentManagedThreadId;
        }

        try
        {
            return action(input);
        }
        finally
        {
            lock (_lock)
            {
                _runningThreadId = -1;
                _isRunning = false;
                Monitor.PulseAll(_lock);
            }
        }
    }

    /// <summary>
    /// Dedicated worker loop.
    /// Repeatedly:
    ///   1) Acquire the lock
    ///   2) Refuse to start a new queued action if:
    ///        • the shard is disposed, or
    ///        • an inline caller is pending (must yield), or
    ///        • _isRunning == true (someone else has already claimed it)
    ///      Otherwise, if queue not empty, dequeue + set _isRunning = true + break out.
    ///   3) Release the lock, run the action, then in finally set _isRunning = false + PulseAll.
    ///   4) Loop
    /// </summary>
    private void WorkerLoop()
    {
        while (true)
        {
            Action? nextAction;

            lock (_lock)
            {
                while (true)
                {
                    if (_disposed)
                    {
                        // Someone called Dispose(): stop the thread now
                        return;
                    }

                    // If any inline caller is waiting, yield immediately
                    if (_inlinePending)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }

                    // **NEW CHECK**: If _isRunning is already true, that means either:
                    //   • an inline call grabbed the shard, or
                    //   • we’re in the middle of a queued action (though typically we clear it in finally).
                    // Either way: don’t dequeue or start a new action now.
                    if (_isRunning)
                    {
                        Monitor.Wait(_lock);
                        continue;
                    }

                    // If there is a queued action, grab it & set _isRunning = true
                    if (_queue.Count > 0)
                    {
                        nextAction = _queue.Dequeue();
                        _isRunning = true;
                        break;
                    }

                    // Nothing to do → park until either:
                    //  • Enqueue() calls PulseAll
                    //  • RunInline() calls PulseAll
                    //  • Dispose() calls PulseAll
                    Monitor.Wait(_lock);
                }
            }

            // At this point, we have “reserved” the shard for nextAction.
            // (We are outside the lock, running user code.)
            try
            {
                _runningThreadId = Environment.CurrentManagedThreadId;
                nextAction();
            }
            catch
            {
                // Swallow exceptions from the user’s callback.
                // (If you want them bubbled up, store & rethrow, etc.)
            }
            finally
            {
                lock (_lock)
                {
                    _runningThreadId = -1;
                    _isRunning = false;
                    Monitor.PulseAll(_lock);
                }
            }
        }
    }

    /// <summary>
    /// Call this to verify that the currently running thread has access to the shard
    /// </summary>
    public void VerifyAccess()
    {
        if (_runningThreadId != Environment.CurrentManagedThreadId)
            throw new InvalidOperationException("This method must be called inside a shard thread");
    }

    /// <summary>
    /// Dispose the shard.
    /// Signals the worker thread to exit, then joins it.
    /// Further Enqueue/RunInline will throw.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            Monitor.PulseAll(_lock);
        }

        _workerThread.Join();
    }
}
