using System;
using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Implementation;

public abstract class AJoinStageDefinition<TLeft, TRight, TResult, TState>(IStageDefinition<TLeft> left, IStageDefinition<TRight> right) : IQuery<TResult>
    where TResult : notnull
    where TLeft : notnull
    where TRight : notnull
    where TState : new()
{
    public IStage CreateInstance(IFlow flow)
    {
        var leftInstance = (IStage<TLeft>)flow.AddStage(left);
        var rightInstance = (IStage<TRight>)flow.AddStage(right);
        return new JoinStage(this, leftInstance, rightInstance, flow);
    }

    protected abstract void AcceptLeftChange(TLeft input, int delta, ref ChangeSetWriter<TResult> writer, in TState state);

    protected abstract void AcceptRightChange(TRight input, int delta, ref ChangeSetWriter<TResult> writer, in TState state);

    protected abstract void EmitCurrent(ref ChangeSetWriter<TResult> writer, in TState state);

    protected class JoinStage : AStage<TResult, AJoinStageDefinition<TLeft, TRight, TResult, TState>>
    {
        private readonly IStage<TLeft> _left;
        private readonly IStage<TRight> _right;
        private readonly TState _state;

        internal JoinStage(AJoinStageDefinition<TLeft, TRight, TResult, TState> definition, IStage<TLeft> left, IStage<TRight> right, IFlow flow) : base(definition, flow)
        {
            _left = left;
            _right = right;
            _left.ConnectOutput(this, 0);
            _right.ConnectOutput(this, 1);
            _state = new TState();

            Populate();
        }

        private void Populate()
        {
            var leftWriter = ChangeSetWriter<TLeft>.Create();
            var rightWriter = ChangeSetWriter<TRight>.Create();

            _left.WriteCurrentValues(ref leftWriter);
            _right.WriteCurrentValues(ref rightWriter);

            AcceptChange(0, leftWriter.ToChangeSet());
            AcceptChange(1, rightWriter.ToChangeSet());
        }

        /// <inheritdoc />
        public override ReadOnlySpan<IStage> Inputs => new([_left, _right]);

        /// <inheritdoc />
        public override void WriteCurrentValues(ref ChangeSetWriter<TResult> writer)
            => _definition.EmitCurrent(ref writer, _state);

        /// <inheritdoc />
        public override void AcceptChange<T>(int inputIndex, in ChangeSet<T> delta)
        {
            var writer = new ChangeSetWriter<TResult>();
            if (inputIndex == 0)
            {
                foreach (var (change, deltaValue) in delta.Changes)
                {
                    var casted = (TLeft)(object)change;
                    _definition.AcceptLeftChange(casted, deltaValue, ref writer, _state);
                }
            }
            else
            {
                foreach (var (change, deltaValue) in delta.Changes)
                {
                    var casted = (TRight)(object)change;
                    _definition.AcceptRightChange(casted, deltaValue, ref writer, _state);
                }
            }

            writer.ForwardAll(this);
        }
    }
}
