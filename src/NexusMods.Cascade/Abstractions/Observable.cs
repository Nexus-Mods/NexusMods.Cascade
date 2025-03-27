using Clarp.Concurrency;

namespace NexusMods.Cascade.Abstractions;

public abstract class Observable<TValue, TDelta>
{
    public abstract TValue Value { get; }
    public ITransactionalDispose Subscribe(Observer<TValue, TDelta> observer)
    {
        try
        {
            var subscription = SubscribeCore(observer);
            return subscription;
        }
        catch
        {
            observer.Dispose();
            throw;
        }
    }

    protected abstract ITransactionalDispose SubscribeCore(Observer<TValue, TDelta> observer);
}

public abstract class Observer<TValue, TDelta> : ITransactionalDispose
{
    private readonly Ref<Observable<TValue, TDelta>> _upstream = new();

    protected TValue UpstreamValue => _upstream.Value.Value;

    public abstract TValue Value { get; }

    public void OnNext(in TDelta delta)
    {
        OnNextCore(delta);
    }

    protected abstract void OnNextCore(in TDelta delta);

    public void Dispose()
    {
        throw new System.NotImplementedException();
    }
}
