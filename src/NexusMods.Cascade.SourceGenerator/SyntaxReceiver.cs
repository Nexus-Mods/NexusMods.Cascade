using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NexusMods.Cascade.SourceGenerator;

internal class SyntaxReceiver : ISyntaxReceiver
{
    public ImmutableList<RecordDeclarationSyntax> CandidateClasses { get; private set; } =
        ImmutableList<RecordDeclarationSyntax>.Empty;

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not RecordDeclarationSyntax)
            return;

        var recordDeclarationSyntax = (RecordDeclarationSyntax)syntaxNode;
        var implementedInterfaces = recordDeclarationSyntax.BaseList?.Types;

        if (implementedInterfaces is null)
            return;

        if (implementedInterfaces.Any<BaseTypeSyntax>(x => x.ToString() != Consts.IRowDefinitionName))
            return;

        CandidateClasses = CandidateClasses.Add(recordDeclarationSyntax);
    }
}
