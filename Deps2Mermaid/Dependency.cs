namespace Deps;

public readonly record struct Dependency(Component Component, Reference Reference)
{
    public override string ToString()
        => $"{Component} -> {Reference}";
}