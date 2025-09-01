namespace Deps;

public readonly record struct Version(int Major, int Minor, int Patch, string? Tag) : IComparable<Version>
{
    public int CompareTo(Version other)
    {
        var result = Major.CompareTo(other.Major);
        if (result != 0) return result;
        result = Minor.CompareTo(other.Minor);
        if (result != 0) return result;
        result = Patch.CompareTo(other.Patch);
        if (result != 0) return result;
        if (Tag is null && other.Tag is not null) return 1;
        if (Tag is not null && other.Tag is null) return -1;
        return string.CompareOrdinal(Tag, other.Tag);
    }

    public override string ToString()
        => this switch
        {
            {Tag: null} => $"{Major}.{Minor}.{Patch}",
            _ => $"{Major}.{Minor}.{Patch}-{Tag}"
        };

    public static Version? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var parts = value.Split('-', 2);
        var numbers = parts[0].Split('.').Select(int.Parse).ToArray();
        return new(numbers[0], numbers[1], numbers[2], parts.Length == 1 ? null : parts[1]);
    }

    public static Version Parse(string value)
        => TryParse(value)!.Value;

}