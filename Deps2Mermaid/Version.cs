using System.Runtime.InteropServices;

namespace Deps;

public readonly record struct Version(VersionPart Major, VersionPart Minor, VersionPart Patch, string? Tag) : IComparable<Version>
{
    public const int UNKNOWN = -1;
    public const int ABSENT = -2;
    public const int STAR = -3;
    public int CompareTo(Version other)
    {
        var result = Major.ComparisonValue.CompareTo(other.Major.ComparisonValue);
        if (result != 0) return result;
        result = Minor.ComparisonValue.CompareTo(other.Minor.ComparisonValue);
        if (result != 0) return result;
        result = Patch.ComparisonValue.CompareTo(other.Patch.ComparisonValue);
        if (result != 0) return result;
        if (Tag is null && other.Tag is not null) return 1;
        if (Tag is not null && other.Tag is null) return -1;
        return string.CompareOrdinal(Tag, other.Tag);
    }

    private string[] Parts
    {
        get
        {
            var vals = new[] {Major, Minor, Patch};
            return vals.TakeWhile(x => x.IsPresent)
                .Select(x => x.ToString())
                .Concat(vals.Any(x => x == VersionPart.Star) ? ["*"] : [])
                .ToArray();
        }
    }

    public override string ToString()
    {
        var regularPart = string.Join(".", Parts);
        return this switch
        {
            {Tag: null} => regularPart,
            _ => $"{regularPart}-{Tag}"
        };
    }


    public static Version? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var parts = value.Split('-', 2);
        var numbers = parts[0].Split('.').Select(VersionPart.Parse).Concat([VersionPart.Absent, VersionPart.Absent]).Take(3).ToArray();
        if (numbers.Any(n => n == VersionPart.Unknown))
            return null;
        return new(numbers[0], numbers[1], numbers[2], parts.Length == 1 ? null : parts[1]);
    }

    public static Version Parse(string value)
        => TryParse(value)!.Value;

}