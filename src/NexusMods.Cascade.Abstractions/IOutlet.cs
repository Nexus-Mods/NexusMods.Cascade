using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IOutlet : IStage
{

}

public interface IOutlet<T> : IStage
where T : notnull
{
    void Add<TInput>(TInput input)
        where TInput : IEnumerable<KeyValuePair<T, int>>;
}
