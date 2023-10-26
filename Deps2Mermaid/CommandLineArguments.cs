using System.Reflection;

namespace Deps;

public readonly record struct CommandLineArguments(string Path, bool Help, string Filter, string StrongFilter, string? Zoom,
    bool ProjectRoot, bool Verbose)
{
    public static void ShowHelp()
    {
        var ver = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Console.WriteLine(@$"
Deps2Mermaid v{ver}- Convert project.assets.json to mermaid.js graph
Usage: deps (-p(ath)) ([path]) (-help|-?) (-f(ilter) [filter]) (-z(oom) [zoom]) (-projectroot|-pr)
    
        -p(ath) [path]       Path to the project directory. Defaults to current directory.
        -help|-?             Show this help.
        -f(ilter) [filter]   Regular expression to filter the edges (either from or to must match). Defaults to "".*"".
        -s(trong)f(filter)   Regular expression to filter the edges (both from and to must match). Defaults to "".*"".
        -z(oom) [zoom]       Regular expression to zoom the graph.
        -projectroot|-pr     Zoom to the project root.
        -verbose|-v          Show verbose output.
");
    }
    public static CommandLineArguments Parse(string[] args)
    {
        var (positional, named) = DetermineParameters(args);

        var path = positional.GetAtOrDefault(0) ??
                   named.GetValueOrDefault("path") ?? named.GetValueOrDefault("p") ?? ".";
        var help = named.ContainsKey("help") || named.ContainsKey("?");
        var filter = named.GetValueOrDefault("filter") ?? named.GetValueOrDefault("f") ?? ".*";
        var strongFilter = named.GetValueOrDefault("strongfilter") ?? named.GetValueOrDefault("sf") ?? ".*";
        var zoom = named.GetValueOrDefault("zoom") ?? named.GetValueOrDefault("z");
        var projectroot = named.ContainsKey("projectroot") || named.ContainsKey("pr");
        var verbose = named.ContainsKey("verbose") || named.ContainsKey("v");
        
        return new(path, help, filter, strongFilter, zoom, projectroot, verbose);
    }

    private static (IReadOnlyList<string> positional, IReadOnlyDictionary<string, string> named)
        DetermineParameters(string[] args)
    {
        var positional = new List<string>();
        var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? key = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith('-'))
            {
                if (key is not null)
                    named[key] = "";
                key = arg.TrimStart('-', '/');
            }
            else if (key is null)

                positional.Add(arg);
            else
            {
                named[key] = arg;
                key = null;
            }
        }

        if (key is not null)
            named[key] = "";

        return (positional, named);
    }
}