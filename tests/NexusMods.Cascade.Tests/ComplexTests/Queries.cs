using NexusMods.Cascade.Abstractions;
using NexusMods.Cascade.Tests.ComplexTests.Rows;
using NexusMods.Paths;

namespace NexusMods.Cascade.Tests.ComplexTests;


public static class Queries
{
    public static IQuery<(string Type, int Count, Size Download)> ArchiveCountForType =
        from archive in Parsing.Archives
        group archive by archive.Type into g
        select (g.Key, g.Count, g.Sum(a => a.Size));


    public static IQuery<(Directive directive, string Mod)> DirectiveWithModName =
        from directive in Parsing.Directives
        let parts = directive.To.Parts
        where parts.First() == "mods" && parts.Count() > 2
        select (directive, parts.Skip(1).First().ToString());

    public static IQuery<(string Mod, int FileCount)> Mods =
        from directive in DirectiveWithModName
        group directive by directive.Mod
        into grouped
        select (grouped.Key, grouped.Count);


    public static IQuery<Directive> EnabledFiles =
        from directive in DirectiveWithModName
        join mod in Inlets.EnabledMods on directive.Mod equals mod
        select directive.directive;

}
