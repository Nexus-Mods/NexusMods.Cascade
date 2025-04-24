using Microsoft.CodeAnalysis;

namespace NexusMods.Cascade.SourceGenerator;

public class MemberDefinition
{
    public MemberDefinition(IParameterSymbol parameter, int i)
    {
        Name = parameter.Name;
        Type = parameter.Type;
        Index = i;
    }

    public int Index { get; set; }

    public ITypeSymbol Type { get; set; }

    public string Name { get; set; }
}
