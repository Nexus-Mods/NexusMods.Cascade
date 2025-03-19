using NexusMods.Cascade.Abstractions;

namespace NexusMods.Cascade.Tests.SourceGeneratorTests;

public enum PointName : byte
{
    TopLeft,
    BottomRight,
    Center,
    TopRight,
    BottomLeft,
    Movable
}
public partial record struct Point(PointName Name, int X, int Y) : IRowDefinition;

public partial record struct Distance((PointName From, PointName To) Points, float H) : IRowDefinition;
