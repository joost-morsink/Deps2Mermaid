namespace Deps;

public readonly record struct Component(string Name, Version Version)
{
    public static Component Parse(string value)
    {
        var values = value.Split('/');
        return new(values[0], Deps.Version.Parse(values[1]));
    }

    public override string ToString()
        => $"{Name}/{Version}";

    public Reference ToReference()
        => new(Name, VersionRange.Exact(Version));
}