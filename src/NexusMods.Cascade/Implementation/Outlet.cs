using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Clarp;
using Clarp.Abstractions;
using Clarp.Concurrency;
using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Abstractions.Diffs;
using NexusMods.Cascade.TransactionalConnections;

namespace NexusMods.Cascade.Implementation;

public class Outlet<T>(IFlow<T> upstream) : IFlow<T>
{
    public ISource<T> ConstructIn(ITopology topology)
    {
        var source = topology.Intern(upstream);
        var outlet = new OutletSource(topology, source.Current);
        var disposable = source.Connect(outlet);
        outlet.UpstreamDisposable.Value = disposable;
        return outlet;
    }

    private class OutletSource : ASource<T>, ISink<T>, IOutlet<T>, IObservableOutlet<T>
    {
        private static readonly PropertyChangedEventArgs _valueChanged = new(nameof(Value));


        private readonly Ref<T> _current;
        internal readonly Ref<IDisposable> UpstreamDisposable = new();
        public override T Current => _current.Value;

        /// <summary>
        /// All the observers that are subscribed to this outlet.
        /// </summary>
        private TxArray<IObserver<T>> _observers = [];

        private readonly ITopology _topology;

        public OutletSource(ITopology topology, T initialValue)
        {
            _topology = topology;
            _current = new Ref<T>(initialValue);
        }

        public void OnNext(in T value)
        {
            var old = _current.Value;
            _current.Value = value;
            if (old!.Equals(value))
                return;

            Forward(value);

            if (PropertyChanged is not null || _observers.Count != 0)
            {
                _topology.EnqueueEffect(static o => o.PropertyChanged?.Invoke(o, _valueChanged), this);
                foreach (var observer in _observers)
                    observer.OnNext(value);
            }
        }

        public void OnCompleted()
        {
            CompleteSinks();
        }

        public T Value => _current.Value;
        public event PropertyChangedEventHandler? PropertyChanged;
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return Runtime.DoSync(static s =>
            {
                var (self, observer) = s;
                self._observers.Add(observer);
                self._topology.EnqueueEffect(static state =>
                {
                    var (obs, val) = state;
                    obs.OnNext(val);
                }, (observer, self.Current));
                return new Disposer(self, observer);
            }, (this, observer));
        }

        private class Disposer(OutletSource outlet, IObserver<T> observer) : IDisposable
        {
            public void Dispose()
            {
                Runtime.DoSync(() =>
                {
                    outlet._observers.Remove(observer);
                });
            }
        }
    }
}
