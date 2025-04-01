using System;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Implementation;
using NexusMods.Cascade.Implementation.Omega;

namespace NexusMods.Cascade;


public static class StageBuilder
{
    public static class Delegates<TIn, TOut, TState> where TOut : IComparable<TOut>
    {
        public delegate void OnNextNoState(in TIn value, int delta, ref ChangeSetWriter<TOut> writer);
        public delegate void OnNext(in TIn value, int delta, ref ChangeSetWriter<TOut> writer, in TState state);
    }


    public static IQuery<TOut> Create<TIn, TOut>(IQuery<TIn> upstream, Delegates<TIn, TOut, NoState>.OnNextNoState onNext)
        where TOut : IComparable<TOut>
        where TIn : IComparable<TIn>
    {
        return new DelegateStage<TIn, TOut>(upstream, onNext);
    }

    public static IQuery<TOut> Create<TIn, TOut, TState>(IQuery<TIn> upstream, Delegates<TIn, TOut, TState>.OnNext onNext, TState state)
        where TOut : IComparable<TOut>
        where TIn : IComparable<TIn>
    {
        return new DelegateStageOuterState<TIn,TOut,TState>(upstream, onNext, state);
    }

    private class DelegateStage<TIn, TOut>(IStageDefinition<TIn> upstream, Delegates<TIn, TOut, NoState>.OnNextNoState onNext)
        : AUnaryStageDefinition<TIn, TOut, NoState>(upstream)
        where TOut : IComparable<TOut>
        where TIn : IComparable<TIn>
    {
        protected override void AcceptChange(TIn input, int delta, ref ChangeSetWriter<TOut> writer, NoState state) => onNext(input, delta, ref writer);
    }

    private class DelegateStageOuterState<TIn, TOut, TState>(IStageDefinition<TIn> upstream, Delegates<TIn, TOut, TState>.OnNext onNext, TState state)
        : AUnaryStageDefinition<TIn, TOut, NoState>(upstream)
        where TOut : IComparable<TOut>
        where TIn : IComparable<TIn>
    {
        protected override void AcceptChange(TIn input, int delta, ref ChangeSetWriter<TOut> writer, NoState _) => onNext(input, delta, ref writer, state);
    }

}
