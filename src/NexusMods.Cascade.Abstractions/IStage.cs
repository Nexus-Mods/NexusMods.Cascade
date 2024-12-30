using System;
using System.Collections.Generic;

namespace NexusMods.Cascade.Abstractions;

public interface IStage
{

    public IInput[] Inputs { get; }

    public IOutput[] Outputs { get; }

    public IOutput[] UpstreamInputs { get; }
}

public interface IInput
{
    /// <summary>
    /// The name of the input
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The type of the input
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// The index of the input
    /// </summary>
    public int Index { get; }
}

public interface IInput<T> : IInput
    where T : notnull
{

}

public interface IOutput
{
    public string Name { get; }

    public Type Type { get; }

    public int Index { get; }

    /// <summary>
    /// Gets the associated stage
    /// </summary>
    public IStage Stage { get; }

    public IOutputSet OutputSet { get; set; }
}

public interface IOutput<T> : IOutput
    where T : notnull
{
    public IOutputSet<T> OutputSet { get; }
}
