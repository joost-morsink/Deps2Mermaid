using System.Collections.Immutable;

namespace Deps;

public readonly record struct ComponentNode(string Name, ImmutableList<VersionRange> VersionRanges)
{
    public static IEnumerable<ComponentNode> GetNodes(IEnumerable<Dependency> dependencies)
        => from d in dependencies
            from x in new[] {d.Component.ToReference(), d.Reference}
            group x.VersionRange by x.Name
            into g
            select new ComponentNode(g.Key, g.Distinct().ToImmutableList());
}