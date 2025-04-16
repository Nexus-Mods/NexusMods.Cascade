using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexusMods.Cascade.SourceGenerator;

public class RowDefinitionAnalyzer
{
    private readonly INamedTypeSymbol _classSymbol;
    private readonly Compilation _compilation;
    private readonly INamedTypeSymbol _modelDefinitionTypeSymbol;

    public RowDefinitionAnalyzer(RecordDeclarationSyntax candidate, GeneratorExecutionContext context)
    {
        Syntax = candidate;
        Context = context;
        _compilation = context.Compilation;
        _classSymbol =
            (INamedTypeSymbol?)_compilation.GetSemanticModel(candidate.SyntaxTree).GetDeclaredSymbol(candidate)!;
        _modelDefinitionTypeSymbol = context.Compilation.GetTypeByMetadataName(Consts.IRowDefinitionFullName)!;
        Members = [];
        Name = "";
        Namespace = null!;
    }

    public MemberDefinition PrimaryKey { get; set; } = null!;
    public MemberDefinition[] Members { get; set; }

    public RecordDeclarationSyntax Syntax { get; set; }

    public GeneratorExecutionContext Context { get; set; }

    public string Name { get; set; }
    public INamespaceSymbol Namespace { get; set; }

    public bool Analyze()
    {
        if (!InheritsFromRowDefinition())
        {
            var actual = string.Join(",", _classSymbol.Interfaces);
            Context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.IncorrectBase, Syntax.GetLocation(),
                actual, Consts.IRowDefinitionFullName));
            return false;
        }

        Name = _classSymbol.Name;
        Namespace = _classSymbol.ContainingNamespace;
        AnalyzeMembers();

        return true;
    }

    private void AnalyzeMembers()
    {
        var primaryConstructor = _classSymbol.InstanceConstructors.FirstOrDefault(c => c.Parameters.Length > 0);
        if (primaryConstructor == null)
            return;

        Members = new MemberDefinition[primaryConstructor.Parameters.Length - 1];
        for (var i = 0; i < primaryConstructor.Parameters.Length; i++)
            if (i == 0)
                PrimaryKey = new MemberDefinition(primaryConstructor.Parameters[i], i);
            else
                Members[i - 1] = new MemberDefinition(primaryConstructor.Parameters[i], i - 1);
    }

    private bool InheritsFromRowDefinition()
    {
        foreach (var i in _classSymbol.Interfaces)
            if (SymbolEqualityComparer.Default.Equals(i, _modelDefinitionTypeSymbol))
                return true;
        return false;
    }
}
