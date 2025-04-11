using System.IO;

namespace NexusMods.Cascade.Abstractions;

public record DebugInfo
{
    public static readonly DebugInfo Empty = new DebugInfo();

    public string Expression { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int LineNumber { get; init; } = 0;

    public static DebugInfo Create(string expression, string filePath, int lineNumber)
    {
        // If we aren't happy with using memory for these strings we can disable this construction
        // in release builds.
        return new DebugInfo
        {
            Expression = expression,
            FilePath = filePath,
            LineNumber = lineNumber
        };
    }


    public override string ToString()
    {
        if (string.IsNullOrEmpty(Expression) || string.IsNullOrEmpty(FilePath) || LineNumber == 0)
        {
            return "";
        }

        return $"{Expression} ({Path.GetFileName(FilePath)}:{LineNumber})";
    }
}
