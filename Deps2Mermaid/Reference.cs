namespace Deps;

public readonly record struct Reference(string Name, VersionRange VersionRange)
{
    public static Reference Parse(string value)
    {
        var values = value.Split('/');
        return new(values[0], Deps.VersionRange.Parse(values[1]));
    }

    public override string ToString()
        => $"{Name}/{VersionRange}";
}