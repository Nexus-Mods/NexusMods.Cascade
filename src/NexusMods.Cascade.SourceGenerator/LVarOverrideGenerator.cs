using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace NexusMods.Cascade.SourceGenerator;
[Generator]
public class LVarOverridesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {

        // 1) Find all method declarations with attribute syntax.
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0,
                transform: static (ctx, ct) =>
                {
                    var methodDecl = (MethodDeclarationSyntax)ctx.Node;
                    var sym = ctx.SemanticModel.GetDeclaredSymbol(methodDecl, ct);
                    if (sym is null || !sym.IsStatic)
                        return null;

                    // look for [GenerateLVarOverrides]
                    foreach (var attr in sym.GetAttributes())
                    {
                        var name = attr.AttributeClass?.ToDisplayString();
                        if (name == "NexusMods.Cascade.GenerateLVarOverridesAttribute"
                            || name!.EndsWith("GenerateLVarOverridesAttribute"))
                            return sym;
                    }

                    return null;
                })
            .Where(sym => sym is not null)
            .Collect();

        // 2) For each collected method, emit a partial file with all the overloads.
        context.RegisterSourceOutput(candidates, (spc, methods) =>
        {
            foreach (var methodSym in methods!)
            {
                var src = GenerateOverrides((IMethodSymbol)methodSym!);
                // name it uniquely per method
                var hint = $"{methodSym!.ContainingType.Name}_{methodSym.Name}_LVarOverrides.g.cs";
                spc.AddSource(hint, SourceText.From(src, Encoding.UTF8));
            }
        });
    }

    private static string GenerateOverrides(IMethodSymbol method)
    {
        // gather info
        var ns = method.ContainingNamespace.IsGlobalNamespace
            ? null
            : method.ContainingNamespace.ToDisplayString();
        var typeDecl = BuildContainingTypeChain(method.ContainingType);
        var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodName = method.Name;

        // build generic parameter list if any
        var genericParams = method.TypeParameters.Length > 0
            ? $"<{string.Join(", ", method.TypeParameters.Select(t => t.Name))}>"
            : "";
        var genericConstraints = string.Join(" ", method.TypeParameters
            .Where(t => t.ConstraintTypes.Length > 0 || t.HasReferenceTypeConstraint || t.HasValueTypeConstraint)
            .Select(t =>
            {
                var constraints = new List<string>();
                if (t.HasReferenceTypeConstraint) constraints.Add("class");
                if (t.HasValueTypeConstraint) constraints.Add("struct");
                constraints.AddRange(t.ConstraintTypes.Select(c => c.ToDisplayString()));
                return $"where {t.Name} : {string.Join(", ", constraints)}";
            }));

        // locate all LVar<T> parameters
        var allParams = method.Parameters;
        var lvarParams = allParams
            .Select((p, i) => (Param: p, Index: i))
            .Where(pair =>
            {
                if (pair.Param.Type is INamedTypeSymbol nts
                    && nts.Name == "LVar"
                    && nts.TypeArguments.Length == 1)
                    return true;
                return false;
            })
            .ToArray();

        // if none, nothing to do
        if (lvarParams.Length == 0)
            return "";

        // prepare collector
        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        if (ns != null)
        {
            sb.AppendLine($"namespace {ns}");
            sb.AppendLine("{");
        }

        // open each nesting
        foreach (var t in typeDecl)
        {
            sb.AppendLine($"    partial class {t}");
            sb.AppendLine("    {");
        }
        // generate one overload per non-empty subset of lvarParams
        var subsets = GetNonEmptySubsets(Enumerable.Range(0, lvarParams.Length).ToArray());
        var idxT = 0;
        foreach (var subset in subsets)
        {
            idxT += 1;
            // method signature
            sb.Append("        public static ");
            sb.Append(returnType).Append(' ').Append(methodName).Append(idxT);
            if (!string.IsNullOrEmpty(genericParams))
                sb.Append(genericParams);
            sb.Append('(');

            // build signature parameters
            var parts = new List<string>();
            var exprParts = new List<string>();
            foreach (var p in allParams)
            {
                var found = lvarParams
                    .Select((lp, idx) => (lp.Param, ParamIdx: lp.Index, LVarIdx: idx))
                    .FirstOrDefault(lp => SymbolEqualityComparer.Default.Equals(lp.Param, p));
                if (found.Param != null && subset.Contains(found.LVarIdx))
                {
                    // out LVar<T> param
                    var tstr = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    parts.Add($"out {tstr} {p.Name}");
                    exprParts.Add($"[global::System.Runtime.CompilerServices.CallerArgumentExpression(\"{p.Name}\")] string? {p.Name}__Expr = \"\"");
                }
                else
                {
                    // keep original
                    var tstr = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    parts.Add($"{tstr} {p.Name}");
                }
            }

            sb.Append(string.Join(", ", parts.Concat(exprParts)));
            sb.AppendLine(")");

            if (!string.IsNullOrEmpty(genericConstraints))
                sb.AppendLine($"            {genericConstraints}");

            sb.AppendLine("        {");

            // initialize out LVar<T>
            foreach (var idx in subset)
            {
                var p = lvarParams[idx].Param;
                var tstr = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.AppendLine($"            {p.Name} = {tstr}.Create({p.Name}__Expr);");
                //sb.AppendLine($"            {p.Name} = {tstr}.Create(\"\");");
            }

            // call original
            sb.Append("            ");
            if (!method.ReturnsVoid)
                sb.Append("return ");
            sb.Append(methodName).Append('(')
                .Append(string.Join(", ", allParams.Select(p => p.Name)))
                .AppendLine(");");

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        // close types and namespace
        for (int i = typeDecl.Count - 1; i >= 0; i--)
        {
            sb.AppendLine("    }");
        }
        if (ns != null)
            sb.AppendLine("}");

        return sb.ToString();
    }

    // helper: unwrap nested types into a list
    private static List<string> BuildContainingTypeChain(INamedTypeSymbol t)
    {
        var stack = new Stack<string>();
        var curr = t;
        while (curr != null)
        {
            stack.Push(curr.Name);
            curr = curr.ContainingType;
        }
        return stack.ToList();
    }

    // helper: all non-empty subsets of [0..n)
    private static IEnumerable<int[]> GetNonEmptySubsets(int[] indices)
    {
        int n = indices.Length;
        for (int mask = 1; mask < (1 << n); mask++)
        {
            var tmp = new List<int>();
            for (int j = 0; j < n; j++)
                if ((mask & (1 << j)) != 0)
                    tmp.Add(j);
            yield return tmp.ToArray();
        }
    }
}
