using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NexusMods.Cascade.Collections;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Flows;

public class AggregationFlow<TKey, TInput, TState, TResult> : Flow<KeyedValue<TKey, TResult>>
    where TResult : notnull
    where TKey : notnull
    where TInput : notnull
{

    public delegate void StepDelgate(ref TState state, TInput intput, int delta, out bool delete);

    public required Func<TState> StateFactory { get; init; }

    public required StepDelgate StepFn { get; init; }

    public required Func<TState, TResult> ResultFn { get; init; }

    public override Node CreateNode(Topology topology)
        => new Aggregation<TKey, TInput, TState, TResult>(topology, this, 1);

    public class Aggregation<TKey, TInput, TState, TResult>(Topology topology, AggregationFlow<TKey, TInput, TState, TResult> flow, int upstreamSlots) :
    Node<KeyedValue<TKey, TResult>>(topology, flow, upstreamSlots)
    where TResult : notnull
    where TKey : notnull
    where TInput : notnull
    {

        private HashSet<TKey> _updatedKeys = [];

        private Dictionary<TKey, TState> _state = new();

        private Dictionary<TKey, TResult> _previousValues = new();


        public override void Accept<TIn>(int idx, DiffSet<TIn> diffSet)
        {
            var casted = (DiffSet<KeyedValue<TKey, TInput>>)(object)diffSet;

            foreach (var (value, delta) in casted)
            {
                _updatedKeys.Add(value.Key);
                ref var currentState = ref CollectionsMarshal.GetValueRefOrAddDefault(_state, value.Key, out var exists);
                if (!exists)
                    currentState = flow.StateFactory();

                flow.StepFn(ref currentState!, value.Value, delta, out var delete);
                if (delete)
                {
                    _state.Remove(value.Key);
                }
            }
        }

        public override void EndEpoch()
        {
            foreach (var key in _updatedKeys)
            {
                if (_state.TryGetValue(key, out var state))
                {
                    var result = flow.ResultFn(state);
                    if (_previousValues.TryGetValue(key, out var previousValue))
                    {
                        if (!result.Equals(previousValue))
                        {
                            OutputSet.Add(new KeyedValue<TKey, TResult>(key, previousValue), -1);
                            OutputSet.Add(new KeyedValue<TKey, TResult>(key, result), 1);
                            _previousValues[key] = result;
                        }
                    }
                    else
                    {
                        OutputSet.Add(new KeyedValue<TKey, TResult>(key, result), 1);
                        _previousValues[key] = result;
                    }
                }
                else
                {
                    if (_previousValues.TryGetValue(key, out var previousValue))
                    {
                        OutputSet.Add(new KeyedValue<TKey, TResult>(key, previousValue), -1);
                        _previousValues.Remove(key);
                    }
                }
            }

            _updatedKeys.Clear();
        }

        public override void Created()
        {
            var upstreamCasted = (Node<KeyedValue<TKey, TInput>>)Upstream[0];
            upstreamCasted.ResetOutput();
            upstreamCasted.Prime();

            Accept(0, upstreamCasted.OutputSet);
            EndEpoch();
            upstreamCasted.ResetOutput();
        }

        public override void Prime()
        {
            foreach (var (key, state) in _previousValues)
            {
                OutputSet.Update(new KeyedValue<TKey, TResult>(key, state), 1);
            }
        }
    }

}

