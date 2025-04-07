using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using NexusMods.Cascade.Abstractions.Diffs;

namespace NexusMods.Cascade.Abstractions;

public interface IObservableDiffOutlet<T> : INotifyPropertyChanged, INotifyCollectionChanged, IReadOnlySet<T>
{
}
