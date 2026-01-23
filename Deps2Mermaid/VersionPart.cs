namespace Deps;

public readonly record struct VersionPart(int Value): IComparable<VersionPart>, IComparable
{
    public static VersionPart Unknown => UNKNOWN;
    public static VersionPart Star => STAR;
    public static VersionPart Absent => ABSENT;

    public bool IsPresent => Value >= 0;
    public int ComparisonValue => IsPresent ? Value : 0;
    
    public const int UNKNOWN = -1;
    public const int ABSENT = -2;
    public const int STAR = -3;
    
    public static int Parse(string value)
        => value switch
        {
            "*" => STAR,
            "" => ABSENT,
            null => ABSENT,
            _ => int.TryParse(value, out var v) ? v : UNKNOWN
        };

    public override string ToString()
        => Value switch
        {
            STAR => "*",
            ABSENT => "",
            UNKNOWN => "?",
            _ => Value.ToString()
        };
    public static implicit operator int(VersionPart part)
        => part.Value;
    public static implicit operator VersionPart(int value)
        => new (value);

    public int CompareTo(VersionPart other)
        => Value.CompareTo(other.Value);
    

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        return obj is VersionPart other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(VersionPart)}");
    }
}