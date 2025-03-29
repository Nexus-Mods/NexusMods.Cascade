using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Collections;

// Adjust this based on your project's tUnit configuration

namespace NexusMods.Cascade.Tests.Collections;

public class ChangeSetBuilderTests
{
    [Test]
    public async Task AddingSingleChange_ReturnsSameItem()
    {
        // Arrange
        using var builder = new ChangeSetBuilder<int>();
        builder.Add(5, 1);

        // Act
        var resultSpan = builder.ToSpan();
        var result = resultSpan.ToArray();
        var expected = new[] { new Change<int>(5, 1) };

        // Assert
        await Assert.That(result)
            .IsEquivalentTo(expected)
            .Because("a single added change with value 5 and delta 1 should be returned unchanged");
    }

    [Test]
    public async Task AddingDistinctChanges_SortsResults()
    {
        // Arrange
        using var builder = new ChangeSetBuilder<int>();
        builder.Add(10, 1);
        builder.Add(5, 1);
        builder.Add(7, 1);

        // Act
        var resultSpan = builder.ToSpan();
        var result = resultSpan.ToArray();
        var expected = new[]
        {
            new Change<int>(5, 1),
            new Change<int>(7, 1),
            new Change<int>(10, 1)
        };

        // Assert
        await Assert.That(result)
            .IsEquivalentTo(expected)
            .Because("the changes should be sorted by value in ascending order after finalization");
    }

    [Test]
    public async Task AddingDuplicateChanges_CollapsesDeltas()
    {
        // Arrange
        using var builder = new ChangeSetBuilder<int>();
        builder.Add(5, 2);
        builder.Add(5, -1);
        builder.Add(5, -1);

        // Act
        var resultSpan = builder.ToSpan();
        var result = resultSpan.ToArray();
        var expected = Array.Empty<Change<int>>(); // The deltas cancel out and the item is omitted

        // Assert
        await Assert.That(result)
            .IsEquivalentTo(expected)
            .Because("changes for the same value that collapse to a zero delta should be omitted");
    }

    [Test]
    public async Task AddingMixedChanges_CollapsesAndSortsCorrectly()
    {
        // Arrange
        using var builder = new ChangeSetBuilder<int>();
        // For value 3: 2 + (-1) = 1.
        builder.Add(3, 2);
        builder.Add(3, -1);
        // For value 4: 3 + (-1) = 2.
        builder.Add(4, 3);
        builder.Add(4, -1);
        // For value 5: 1 + 2 = 3.
        builder.Add(5, 1);
        builder.Add(5, 2);

        // Act
        var resultSpan = builder.ToSpan();
        var result = resultSpan.ToArray();
        var expected = new[]
        {
            new Change<int>(3, 1),
            new Change<int>(4, 2),
            new Change<int>(5, 3)
        };

        // Assert
        await Assert.That(result)
            .IsEquivalentTo(expected)
            .Because(
                "each value's changes should be collapsed by summing their deltas and the final list sorted by value");
    }

    [Test]
    public async Task ExceedingInitialCapacity_ResizesAndStoresAllItems()
    {
        // Arrange
        using var builder = new ChangeSetBuilder<int>();
        var numberOfItems = 50; // More than the default capacity of 32
        for (var i = 0; i < numberOfItems; i++)
            // Adding items in reverse order to force sorting.
            builder.Add(numberOfItems - i, 1);

        // Act
        var resultSpan = builder.ToSpan();
        var result = resultSpan.ToArray();
        var expected = new Change<int>[numberOfItems];
        for (var i = 0; i < numberOfItems; i++) expected[i] = new Change<int>(i + 1, 1);

        // Assert
        await Assert.That(result)
            .IsEquivalentTo(expected)
            .Because(
                "the builder should correctly resize and store all items in sorted order when exceeding the initial capacity");
    }
}
