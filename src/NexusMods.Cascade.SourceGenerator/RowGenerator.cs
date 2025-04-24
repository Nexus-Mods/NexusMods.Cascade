using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace NexusMods.Cascade.SourceGenerator;

[Generator]
public class RowGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        List<RowDefinitionAnalyzer> rowAnalyzers = [];
        foreach (var candidate in ((SyntaxReceiver)context.SyntaxReceiver!).CandidateClasses)
        {
            var rowAnalyzer = new RowDefinitionAnalyzer(candidate, context);
            if (!rowAnalyzer.Analyze())
                break;

            rowAnalyzers.Add(rowAnalyzer);
        }


        foreach (var rowAnalyzer in rowAnalyzers)
        {
            var writer = new StringWriter();
            Templates.RenderModel(rowAnalyzer, writer);
            var full = rowAnalyzer.Namespace.ToDisplayString().Replace(".", "_") + "_" + rowAnalyzer.Name;
            context.AddSource($"{full}.Generated.cs", writer.ToString());
        }
    }
}
