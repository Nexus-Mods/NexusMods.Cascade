using System;
using NexusMods.Cascade.Structures;

namespace NexusMods.Cascade.Collections;

public interface IToDiffSpan<T>
{
    ReadOnlySpan<Diff<T>> ToDiffSpan();
}
