using System.Reflection;

namespace Deps;

public record CommandLineArguments(string Path, bool Help, string Filter, string StrongFilter, string Exclude,
    string WeakExclude, string? Zoom,
    int Forward, int Backward, bool ProjectRoot, bool Verbose, bool Live)
{
    void Test()
    {
    }

    public static void ShowHelp()
    {
        var ver = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        Console.WriteLine(@$"
Deps2Mermaid v{ver} - Convert project.assets.json to mermaid.js graph
Usage: deps (-p(ath)) ([path]) (-help|-?) (-f(ilter) [filter]) (-z(oom) [zoom]) (-projectroot|-pr)
    
        -p(ath) [path]               Path to the project directory. Defaults to current directory.
        -help|-?                     Show this help.
        -f(ilter) [filter]           Regular expression to filter the edges (either from or to must match). 
                                        Defaults to "".*"".
        -(e)x(clude) [filter]        Regular expression to filter the edges (both from and to must not match). 
                                        Defaults to ""(?!)"".
        -s(trong)f(filter)           Regular expression to filter the edges (both from and to must match). 
                                        Defaults to "".*"".
        -w(eake)x(clude) [filter]    Regular expression to filter the edges (either from or to must not match). 
                                        Defaults to ""(?!)"".
        -z(oom) [zoom]               Regular expression to zoom the graph.
        -f(or)w(ard) ([n])           Show only forward edges (optional max depth=n). 
        -b(ack)w(ard) ([n])          Show only backward edges (optional max depth=n). 
        -projectroot|-pr             Zoom to the project root.
        -verbose|-v                  Show verbose output.
        -l(ive)                      Open the graph in the browser.
");
    }

    public static CommandLineArguments Parse(string[] args)
    {
        var (positional, named) = DetermineParameters(args);
        if (positional.Count == 0 && named.Count == 0)
            named = new Dictionary<string, string>
            {
                ["?"] = ""
            };

        var path = positional.GetAtOrDefault(0) ??
                   named.GetValueOrDefault("path") ?? named.GetValueOrDefault("p") ?? ".";
        var help = named.ContainsKey("help") || named.ContainsKey("?");
        var filter = named.GetValueOrDefault("filter") ?? named.GetValueOrDefault("f") ?? ".*";
        var strongFilter = named.GetValueOrDefault("strongfilter") ?? named.GetValueOrDefault("sf") ?? ".*";
        var exclude = named.GetValueOrDefault("exclude") ?? named.GetValueOrDefault("x") ?? "(?!)";
        var weakExclude = named.GetValueOrDefault("weakexclude") ?? named.GetValueOrDefault("wx") ?? "(?!)";
        var zoom = named.GetValueOrDefault("zoom") ?? named.GetValueOrDefault("z");
        var projectroot = named.ContainsKey("projectroot") || named.ContainsKey("pr");
        var verbose = named.ContainsKey("verbose") || named.ContainsKey("v");
        var live = named.ContainsKey("live") || named.ContainsKey("l");
        var forward = (named.TryGetValue("forward", out var x) || named.TryGetValue("fw", out x))
            ? int.TryParse(x, out var fwd)
                ? fwd
                : int.MaxValue
            : 0;
        var backward = (named.TryGetValue("backward", out x) || named.TryGetValue("bw", out x))
            ? int.TryParse(x, out var bwd)
                ? bwd
                : int.MaxValue
            : 0;
        if (forward == 0 && backward == 0)
        {
            forward = int.MaxValue;
            backward = int.MaxValue;
        }

        return new(path, help, filter, strongFilter, exclude, weakExclude, zoom, forward, backward, projectroot,
            verbose, live);
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