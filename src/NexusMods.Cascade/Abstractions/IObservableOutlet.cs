using System;
using System.ComponentModel;

namespace NexusMods.Cascade.Abstractions.Diffs;

public interface IObservableOutlet<T> : IObservable<T>, INotifyPropertyChanged
{

}
