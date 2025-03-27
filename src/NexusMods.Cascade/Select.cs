using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade;

public class Select<TIn, TOut>(Observable<TIn, TIn> source, Func<TIn, TOut> selector) : Observable<TOut, TOut>
{
    public override TOut Value => selector(source.Value);
    protected override ITransactionalDispose SubscribeCore(Observer<TOut, TOut> observer)
    {
        throw new System.NotImplementedException();
    }
}
