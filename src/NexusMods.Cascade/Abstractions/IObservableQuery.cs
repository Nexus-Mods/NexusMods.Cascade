using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IObservableQuery<TActiveRow, TRowDefinition, TPrimaryKey>
    where TActiveRow : IActiveRow<TRowDefinition, TPrimaryKey>
    where TRowDefinition : IRowDefinition
    where TPrimaryKey : notnull
{

}
