using System.Text.RegularExpressions;
using System.Xml.XPath;
using Microsoft.Win32.SafeHandles;

namespace Deps;

public class Program
{
    public static IEnumerable<string> PathToProjectAssetsFiles(string path)
    {
        if (path.EndsWith('*'))
            return Directory.GetFiles(path[..^1], "project.assets.json", SearchOption.AllDirectories);
        return new [] { Path.Combine(path, "obj/project.assets.json") }.Where(File.Exists);
    }
    public static void Main(string[] args)
    {
        Arguments = CommandLineArguments.Parse(args);
        
        if (Arguments.Help)
        {
            CommandLineArguments.ShowHelp();
            return;
        }
        Verbose($"{args.Length} arguments given.");
        Verbose($"{string.Join(Environment.NewLine, args.Select(x => $"  {x}"))}");

        var allPaths = Arguments.Path.Split(',').SelectMany(PathToProjectAssetsFiles).ToArray();
        Verbose($"Using {allPaths.Length} project.assets.json files.");
        Verbose($"{string.Join(Environment.NewLine, allPaths.Select(x => $"  {x}"))}");
        var allProjectAssets = allPaths.Select(ProjectAssetsJsonReader.FromPath).ToArray();
        var dependencies = (from projectAssets in  allProjectAssets
                            from dep in projectAssets.Dependencies
                            select dep).Distinct().ToArray();
        Verbose($"Found {dependencies.Length} dependencies.");
        var nodes = ComponentNode.GetNodes(dependencies).ToDictionary(c => c.Name);
        Verbose($"Found {nodes.Count} nodes.");
        var forwardLinks = dependencies.ToLookup(d => d.Component.Name);
        var backwardLinks = dependencies.ToLookup(d => d.Reference.Name);

        if (Arguments.ProjectRoot)
            dependencies = Zoom(string.Join("|", allProjectAssets.Select(x => $"^{x.ProjectComponent().Name}$")), 
                nodes, forwardLinks, backwardLinks).ToArray();
        else if (Arguments.Zoom is not null)
            dependencies = Zoom(Arguments.Zoom, nodes, forwardLinks, backwardLinks).ToArray();

        Console.WriteLine("graph LR");
        WriteToOutput(dependencies, nodes);
    }

    private static IEnumerable<Dependency> Zoom(string filter, Dictionary<string, ComponentNode> nodes,
        ILookup<string, Dependency> forwardLinks, ILookup<string, Dependency> backwardLinks)
    {
        var regex = new Regex(filter);
        var result = new HashSet<Dependency>();
        var todo = new Stack<string>();

        Forward();
        Backward();
        return result;

        void Forward()
        {
            foreach (var key in nodes.Keys.Where(x => regex.IsMatch(x)))
                todo.Push(key);
            Verbose($"Applying zoom filter {filter} results in {todo.Count} nodes.");
            while (todo.Count > 0)
            {
                var name = todo.Pop();
                var node = nodes[name];
                foreach (var next in forwardLinks[name])
                    if (result.Add(next))
                        todo.Push(next.Reference.Name);
            }
        }

        void Backward()
        {
            foreach (var key in nodes.Keys.Where(x => regex.IsMatch(x)))
                todo.Push(key);
            while (todo.Count > 0)
            {
                var name = todo.Pop();
                var node = nodes[name];
                foreach (var next in backwardLinks[name])
                    if (result.Add(next))
                        todo.Push(next.Component.Name);
            }
        }
    }

    private static void WriteToOutput(IEnumerable<Dependency> dependencies, Dictionary<string, ComponentNode> nodes)
    {
        var done = new HashSet<string>();
        var filter = new Regex(Arguments.Filter);
        var strongFilter = new Regex(Arguments.StrongFilter);
        foreach (var dep in dependencies)
        {
            if (!((filter.IsMatch(dep.Component.Name) || filter.IsMatch(dep.Reference.Name))
                  && strongFilter.IsMatch(dep.Component.Name) && strongFilter.IsMatch(dep.Reference.Name)))
            {
                Verbose($"Skipping {dep}...");
                continue;
            }
            Console.Write("  ");
            WriteReference(nodes[dep.Component.Name]);
            WriteLink(dep);
            WriteReference(nodes[dep.Reference.Name]);
            Console.WriteLine();
        }

        void WriteReference(ComponentNode r)
        {
            if (done.Add(r.Name))
                Console.Write($"{r.Name}[\"{r.Name}\r\n    {string.Join(", ", r.VersionRanges.OrderBy(x => x.Min))}\"]");
            else
                Console.Write(r.Name);
        }

        void WriteLink(Dependency d)
        {
            if (nodes[d.Component.Name].VersionRanges.HasSingle() && nodes[d.Reference.Name].VersionRanges.HasSingle())
                Console.Write(" --> ");
            else
            {
                var left = nodes[d.Component.Name].VersionRanges.HasSingle() ? "" : d.Component.Version.ToString();
                var right = nodes[d.Reference.Name].VersionRanges.HasSingle()
                    ? ""
                    : d.Reference.VersionRange.ToString();
                Console.Write($" -- \"{left} -> {right}\"--> ");
            }
        }
    }

    public static CommandLineArguments Arguments { get; private set; }

    public static void Verbose(FormattableString text)
    {
        if (Arguments.Verbose)
            Console.Error.WriteLine(text);
    }
}