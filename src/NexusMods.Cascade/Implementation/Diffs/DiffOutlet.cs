using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.Cascade.Implementation.Diffs;

public class DiffOutlet<T>(IDiffFlow<T> upstream) : IDiffFlow<T>
{
    public ISource<DiffSet<T>> ConstructIn(ITopology topology)
    {
        var source = (IDiffSource<T>)topology.Intern(upstream);
        var outlet = new DiffOutletImpl(topology, source);
        var disposable = source.Connect(outlet);
        outlet.UpstreamDisposable.Value = disposable;
        return outlet;
    }

    internal class DiffOutletImpl : ASource<DiffSet<T>>, IDiffSink<T>, IDiffOutlet<T>, IObservableDiffOutlet<T>
    {
        private static readonly PropertyChangedEventArgs _countChanged = new(nameof(Count));
        private readonly ResultSet<T> _resultSet;
        internal readonly Ref<IDisposable> UpstreamDisposable = new();
        private readonly IDiffSource<T>? _upstream;
        private readonly ITopology _topology;
        private int _count = 0;

        public DiffOutletImpl(ITopology topology, IDiffSource<T> source)
        {
            _topology = topology;
            _upstream = source;
            _resultSet = new(source.Current);
        }


        public DiffOutletImpl(ITopology topology, ReadOnlySpan<Diff<T>> initialValue)
        {
            _topology = topology;
            _resultSet = new(new DiffSet<T>(initialValue));
        }

        public override DiffSet<T> Current => _resultSet.AsDiffSet();
        public void OnNext(in DiffSet<T> diff)
        {
            var old = _resultSet.AsSpan();
            _resultSet.Merge(diff);
            Forward(diff);

            var netChanges = new List<Diff<T>>();

            int countDelta = 0;


            foreach (var (value, _) in diff.AsSpan())
            {
                var oldContains = old.BinarySearch(new Diff<T>(value, 0), Diff<T>.ValueOnlyComparerInstance) >= 0;
                var newContains = _resultSet.Contains(value);

                // Delta caused a removal of the result.
                if (oldContains && !newContains)
                {
                    netChanges.Add(new Diff<T>(value, -1));
                    countDelta--;
                }

                if (!oldContains && newContains)
                {
                    netChanges.Add(new Diff<T>(value, 1));
                    countDelta++;
                }
            }

            if (netChanges.Count > 0)
            {
                _topology.EnqueueEffect(static s =>
                {
                    s.Item1.NotifyObservers(s.Item2, s.Item3);
                }, (this, countDelta, netChanges));
            }

        }

        private void NotifyObservers(int countDelta, List<Diff<T>> netChanges)
        {
            if (countDelta != 0)
            {
                // Update the count
                _count += countDelta;
                PropertyChanged?.Invoke(this, _countChanged);
            }

            if (CollectionChanged != null)
            {
                foreach (var (value, delta) in netChanges)
                {
                    var op = delta > 0 ? NotifyCollectionChangedAction.Add : NotifyCollectionChangedAction.Remove;
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(op, value));
                }
            }
        }

        public void OnCompleted()
        {
            _resultSet.Clear();
            UpstreamDisposable.Value = null!;
            CompleteSinks();
        }

        public DiffSet<T> Value => _resultSet.AsDiffSet();

        #region IObservableDiffOutlet

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public IEnumerator<T> GetEnumerator()
        {
            return _resultSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _count;
        public bool Contains(T item)
        {
            return _resultSet.Contains(item);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
            => _resultSet.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other)
            => _resultSet.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other)
            => _resultSet.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other)
            => _resultSet.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other)
            => _resultSet.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other)
            => _resultSet.SetEquals(other);

        #endregion

        public IDiffOutlet<T> Get<TKey>(Func<T, TKey> keySelector, TKey key)
        {
            throw new NotImplementedException();
        }
    }

}

