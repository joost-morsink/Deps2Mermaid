using System.Text.RegularExpressions;
using System.Xml.XPath;
using Microsoft.Win32.SafeHandles;

namespace Deps;

public class Program
{
    public static void Main(string[] args)
    {
        Arguments = CommandLineArguments.Parse(args);
        if (Arguments.Help)
        {
            CommandLineArguments.ShowHelp();
            return;
        }
        
        using var projectAssetsFile = File.Open(Path.Combine(Arguments.Path, "obj/project.assets.json"), FileMode.Open,
            FileAccess.Read);
        var projectAssets = new ProjectAssetsJsonReader(projectAssetsFile);
        var dependencies = projectAssets.Dependencies.ToArray();
        var nodes = ComponentNode.GetNodes(dependencies).ToDictionary(c => c.Name);
        var forwardLinks = dependencies.ToLookup(d => d.Component.Name);
        var backwardLinks = dependencies.ToLookup(d => d.Reference.Name);

        if (Arguments.ProjectRoot)
            dependencies = Zoom(projectAssets.ProjectComponent().Name, nodes, forwardLinks, backwardLinks).ToArray();
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
        foreach (var dep in dependencies.Where(
                     d => filter.IsMatch(d.Component.Name) || filter.IsMatch(d.Reference.Name)))
        {
            Console.Write("  ");
            WriteReference(nodes[dep.Component.Name]);
            WriteLink(dep);
            WriteReference(nodes[dep.Reference.Name]);
            Console.WriteLine();
        }

        void WriteReference(ComponentNode r)
        {
            if (done.Add(r.Name))
                Console.Write($"{r.Name}[\"{r.Name}\r\n    {string.Join(", ", r.VersionRanges)}\"]");
            else
                Console.Write(r.Name);
        }

        void WriteLink(Dependency d)
        {
            if (nodes[d.Component.Name].VersionRanges.HasSingle() && nodes[d.Reference.Name].VersionRanges.HasSingle())
                Console.WriteLine(" --> ");
            else
            {
                var left = nodes[d.Component.Name].VersionRanges.HasSingle() ? "" : d.Component.Version.ToString();
                var right = nodes[d.Reference.Name].VersionRanges.HasSingle()
                    ? ""
                    : d.Reference.VersionRange.ToString();
                Console.WriteLine($" -- \"{left} -> {right}\"--> ");
            }
        }
    }

    public static CommandLineArguments Arguments { get; private set; }
}