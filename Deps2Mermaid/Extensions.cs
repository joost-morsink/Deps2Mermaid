namespace Deps;

public static class Extensions
{
    public static bool HasSingle<T>(this IEnumerable<T> source)
        => source.Take(2).Count() == 1;

    public static T? GetAtOrDefault<T>(this IReadOnlyList<T> list, int index)
        where T : notnull
        => list.Count > index ? list[index] : default;
}