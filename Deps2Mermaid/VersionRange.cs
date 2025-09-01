namespace Deps;

public readonly record struct VersionRange(Version Min, Version? Max, bool MinInclusive, bool MaxInclusive)
{
    public static VersionRange Exact(Version version)
        => new(version, version, true, true);

    public static VersionRange Minimal(Version version)
        => new(version, null, true, false);

    public override string ToString()
        => this switch
        {
            {Max: null, MinInclusive: true} => $"[{Min},)",
            {Max: null, MinInclusive: false} => $"({Min},)",
            {MinInclusive: true, MaxInclusive: true} when Min == Max => $"{Min}",
            {MinInclusive: true, MaxInclusive: true} => $"[{Min},{Max}]",
            {MinInclusive: true, MaxInclusive: false} => $"[{Min},{Max})",
            {MinInclusive: false, MaxInclusive: true} => $"({Min},{Max}]",
            {MinInclusive: false, MaxInclusive: false} => $"({Min},{Max})"
        };

    public static VersionRange Parse(string value)
    {
        value = value.Trim();
        if (char.IsDigit(value[0]))
            return Exact(Version.Parse(value));

        var minInclusive = value[0] switch
        {
            '[' => true,
            '(' => false,
            _ => throw new ArgumentException("Invalid reference format.", nameof(value))
        };
        var maxInclusive = value[^1] switch
        {
            ']' => true,
            ')' => false,
            _ => throw new ArgumentException("Invalid reference format.", nameof(value))
        };
        var parts = value[1..^1].Split(',', 2);
        return new VersionRange(Version.Parse(parts[0]), parts.Length == 1 ? null : Version.TryParse(parts[1]),
            minInclusive, maxInclusive);
    }

    public static IComparer<VersionRange> CompareByMinimum { get; } = new CompareByMinimumImpl();

    private class CompareByMinimumImpl : IComparer<VersionRange>
    {
        public int Compare(VersionRange x, VersionRange y)
            => x.Min.CompareTo(y.Min);
    }
    public VersionRange MinimalToExact()
        => Max is null ? new VersionRange(Min, Min, true, true) : this;
}