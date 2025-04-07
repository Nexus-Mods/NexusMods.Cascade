using System;
using System.Collections.Generic;
using System.Linq;
using Clarp;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.Cascade.Implementation.Diffs;

public class IndexedDiffFlow<TKey, TValue>(IDiffFlow<TValue> upstream, Func<TValue, TKey> keySelector) : IIndexedDiffFlow<TKey, TValue> where TKey : notnull
{
    public IIndexedDiffOutlet<TKey, TValue> ConstructIn(ITopology topology)
    {
        var up = topology.Intern(upstream);
        var ret = new IndexedDiffOutlet(topology, keySelector);
        up.Connect(ret);
        return ret;
    }

    private class IndexedDiffOutlet(ITopology topology, Func<TValue, TKey> keySelector) : IIndexedDiffOutlet<TKey, TValue>, IDiffSink<TValue>
    {
        private TxDictionary<TKey, DiffOutlet<TValue>.DiffOutletImpl> _outlets = new();
        private readonly IndexedResultSet<TKey, TValue> _resultSet = new();
        public IDiffOutlet<TValue> this[TKey key]
        {
            get
            {
                return Runtime.DoSync(() =>
                {
                    if (_outlets.TryGetValue(key, out var outlet))
                        return outlet;

                    var diffSet = _resultSet[key];
                    outlet = new DiffOutlet<TValue>.DiffOutletImpl(topology, diffSet.ToArray().Select(s => s.ValueDiff).ToArray());
                    _outlets.Add(key, outlet);
                    return outlet;
                });
            }
        }

        public void OnNext(in DiffSet<TValue> srcDiff)
        {
            var keyedWriter = new KeyedDiffSetWriter<TKey, TValue>();
            var keys = new HashSet<TKey>();
            foreach (var diff in srcDiff.AsSpan())
            {
                var key = keySelector(diff.Value);
                keys.Add(key);
                keyedWriter.Add(key, diff);
            }

            keyedWriter.Build(out var keyedDiffSet);
            _resultSet.Merge(keyedDiffSet);

            foreach (var key in keys)
            {
                if (!_outlets.TryGetValue(key, out var outlet))
                {
                    var diffSet = _resultSet[key];
                    outlet = new DiffOutlet<TValue>.DiffOutletImpl(topology, diffSet.ToArray().Select(s => s.ValueDiff).ToArray());
                    _outlets.Add(key, outlet);
                }
                else
                {
                    var section = keyedDiffSet[key];
                    outlet.OnNext(new DiffSet<TValue>(section.AsSpan().ToArray().Select(s => s.ValueDiff).ToArray()));
                }
            }
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }
    }
}
