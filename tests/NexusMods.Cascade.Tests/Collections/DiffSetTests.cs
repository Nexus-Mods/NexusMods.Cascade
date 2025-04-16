// File: tests/NexusMods.Cascade.Tests/Collections/DiffSetTests.cs

using FluentAssertions;
using NexusMods.Cascade.Collections;

namespace NexusMods.Cascade.Tests.Collections;

public class DiffSetTests
{
    [Fact]
    public void Update_AddsAndRemovesItems_Correctly()
    {
        var diffSet = new DiffSet<string>();

        // Add an entry.
        diffSet.Update("A", 3);
        diffSet.Should().ContainKey("A");
        diffSet["A"].Should().Be(3);

        // Update same entry to cancel out.
        diffSet.Update("A", -3);
        diffSet.Should().NotContainKey("A");
    }

    [Fact]
    public void MergeIn_CombinesItems_Correctly()
    {
        var diffSet = new DiffSet<string>();
        var entries = new List<KeyValuePair<string, int>>
        {
            new("A", 2),
            new("B", 3)
        };

        diffSet.MergeIn(entries);
        diffSet.Should().Contain(new KeyValuePair<string, int>("A", 2));
        diffSet.Should().Contain(new KeyValuePair<string, int>("B", 3));

        // Merge again with overlapping key.
        var moreEntries = new List<KeyValuePair<string, int>>
        {
            new("A", 1),
            new("B", -3),
            new("C", 5)
        };

        diffSet.MergeIn(moreEntries);
        diffSet["A"].Should().Be(3); // 2 + 1.
        diffSet.Should().NotContainKey("B"); // 3 - 3 = 0.
        diffSet.Should().Contain(new KeyValuePair<string, int>("C", 5));
    }

    [Fact]
    public void MergeInInverted_CombinesItemsInverted_Correctly()
    {
        var diffSet = new DiffSet<string>();
        var entries = new List<KeyValuePair<string, int>>
        {
            new("A", 4),
            new("B", 2)
        };

        diffSet.MergeInInverted(entries);
        diffSet.Should().Contain(new KeyValuePair<string, int>("A", -4));
        diffSet.Should().Contain(new KeyValuePair<string, int>("B", -2));

        // Merge inverted again.
        var moreEntries = new List<KeyValuePair<string, int>>
        {
            new("A", 4),
            new("B", -2)
        };

        diffSet.MergeInInverted(moreEntries);
        diffSet.Should().Contain(new KeyValuePair<string, int>("A", -8)); // -4 - 4 = -8.
        diffSet.Should().NotContainKey("B"); // -2 - (-2) = 0.
    }

    [Fact]
    public void MergeIn_ArrayOverload_AddsItems_Correctly()
    {
        var diffSet = new DiffSet<int>();
        var items = new[] { 1, 2, 3, 2 };

        // Each item gets delta of 2.
        diffSet.MergeIn(items, 2);
        diffSet.Should().Contain(new KeyValuePair<int, int>(1, 2));
        diffSet.Should().Contain(new KeyValuePair<int, int>(2, 4)); // 2 occurs twice.
        diffSet.Should().Contain(new KeyValuePair<int, int>(3, 2));
    }

    [Fact]
    public void SetTo_ReplacesContents_Correctly()
    {
        var original = new DiffSet<string>();
        original.Update("X", 5);
        original.Update("Y", -2);

        var target = new DiffSet<string>();
        target.Update("A", 1);
        target.Update("B", 2);

        target.SetTo(original);
        target.Count.Should().Be(2);
        target.Should().Contain(new KeyValuePair<string, int>("X", 5));
        target.Should().Contain(new KeyValuePair<string, int>("Y", -2));
        target.Should().NotContainKey("A");
        target.Should().NotContainKey("B");
    }
}
