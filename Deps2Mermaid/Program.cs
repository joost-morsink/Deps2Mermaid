using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Deps;

public class Program
{
    public static IEnumerable<string> PathToProjectAssetsFiles(string path)
    {
        if (path.EndsWith('*'))
            return Directory.GetFiles(path[..^1], "project.assets.json", SearchOption.AllDirectories);
        return new[] {Path.Combine(path, "obj/project.assets.json")}.Where(File.Exists);
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
        var dependencies = (from projectAssets in allProjectAssets
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

        switch (Arguments.OutputType)
        {
            case OutputType.Live:
                OpenInBrowser(dependencies, nodes);
                break;
            case OutputType.Markdown:
                WriteMarkdown(Console.Out, dependencies, nodes);
                break;
            case OutputType.Url:
                WriteUrl(Console.Out, dependencies, nodes);
                break;
            case OutputType.ImageUrl:
                WriteImage(Console.Out, dependencies, nodes);
                break;
            case OutputType.Mermaid:
                WriteToOutput(Console.Out, dependencies, nodes);
                break;
        }
    }

    private static void WriteImage(TextWriter @out, Dependency[] dependencies, Dictionary<string, ComponentNode> nodes)
    {
        var str = CreateMermaidStringPayload(dependencies, nodes);
        @out.WriteLine($"https://mermaid.ink/img/{str}");
    }

    private static void WriteUrl(TextWriter @out, Dependency[] dependencies, Dictionary<string, ComponentNode> nodes)
    {
        var str = CreateMermaidStringPayload(dependencies, nodes);
        @out.WriteLine($"https://mermaid.live/edit#pako:{str}");
    }

    private static void WriteMarkdown(TextWriter @out, Dependency[] dependencies, Dictionary<string, ComponentNode> nodes)
    {
        var str = CreateMermaidStringPayload(dependencies, nodes);
        @out.WriteLine($"[![](https://mermaid.ink/img/{str})](https://mermaid.live/edit#pako:{str})");
    }

    private static void OpenInBrowser(Dependency[] dependencies, Dictionary<string, ComponentNode> nodes)
    {
        var str = CreateMermaidStringPayload(dependencies, nodes);

        var url = $"https://mermaid.live/edit#pako:{str}";
        OpenUrlInBrowser(url);
    }

    private static string CreateMermaidStringPayload(Dependency[] dependencies, Dictionary<string, ComponentNode> nodes)
    {
        var payload = CreateMermaidPayload(dependencies, nodes);
        var str = Convert.ToBase64String(payload);
        
        Verbose($"Original Base64: {str}");
        
        str=str.Replace("/", "_").Replace("+", "-");
        return str;
    }

    private static void OpenUrlInBrowser(string url)
    {
        Verbose($"Opening {url}");
        var psi = new ProcessStartInfo
        {
            UseShellExecute = true,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            FileName = url
        };
        Process.Start(psi);
    }

    private static byte[] CreateMermaidPayload(Dependency[] dependencies, Dictionary<string, ComponentNode> nodes)
    {
        var settings = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        WriteToOutput(writer, dependencies, nodes);
        writer.Flush();
        var mermaid = new MermaidGraph
        {
            Code = sb.ToString()
        };
        var content = JsonSerializer.Serialize(mermaid, settings);
        using var ms = new MemoryStream();
        using (var zipper = new ZLibStream(ms, CompressionLevel.SmallestSize))
        {
            using var wri = new StreamWriter(zipper, leaveOpen: true);
            wri.Write(content);
            wri.Flush();
            zipper.Flush();
        }

        ms.Flush();

        var bytes = ms.ToArray();
        return bytes;
    }

    private static IEnumerable<Dependency> Zoom(string filter, Dictionary<string, ComponentNode> nodes,
        ILookup<string, Dependency> forwardLinks, ILookup<string, Dependency> backwardLinks)
    {
        var regex = new Regex(filter);
        var result = new HashSet<Dependency>();
        var todo = new Stack<(int, string)>();

        Forward(Arguments.Forward);
        Backward(Arguments.Backward);

        return result;

        void Forward(int depth)
        {
            foreach (var key in nodes.Keys.Where(x => regex.IsMatch(x)))
                todo.Push((depth, key));
            Verbose($"Applying zoom filter {filter} results in {todo.Count} nodes.");
            while (todo.Count > 0)
            {
                var (d, name) = todo.Pop();
                if (d <= 0)
                    continue;
                var node = nodes[name];
                foreach (var next in forwardLinks[name])
                    if (result.Add(next))
                        todo.Push((d - 1, next.Reference.Name));
            }
        }

        void Backward(int depth)
        {
            foreach (var key in nodes.Keys.Where(x => regex.IsMatch(x)))
                todo.Push((depth, key));
            while (todo.Count > 0)
            {
                var (d, name) = todo.Pop();
                if (d <= 0)
                    continue;
                var node = nodes[name];
                foreach (var next in backwardLinks[name])
                    if (result.Add(next))
                        todo.Push((d - 1, next.Component.Name));
            }
        }
    }

    private static void WriteToOutput(TextWriter writer, IEnumerable<Dependency> dependencies,
        Dictionary<string, ComponentNode> nodes)
    {
        var done = new HashSet<string>();
        var filter = new Regex(Arguments.Filter);
        var strongFilter = new Regex(Arguments.StrongFilter);
        var exclude = new Regex(Arguments.Exclude);
        var weakExclude = new Regex(Arguments.WeakExclude);

        writer.WriteLine("graph LR");

        foreach (var dep in dependencies)
        {
            if (!((filter.IsMatch(dep.Component.Name) || filter.IsMatch(dep.Reference.Name))
                  && strongFilter.IsMatch(dep.Component.Name) && strongFilter.IsMatch(dep.Reference.Name)
                    )
                || weakExclude.IsMatch(dep.Component.Name) && weakExclude.IsMatch(dep.Reference.Name)
                || exclude.IsMatch(dep.Component.Name) || exclude.IsMatch(dep.Reference.Name))
            {
                Verbose($"Skipping {dep}...");
                continue;
            }

            writer.Write("  ");
            WriteReference(nodes[dep.Component.Name]);
            WriteLink(dep);
            WriteReference(nodes[dep.Reference.Name]);
            writer.WriteLine();
        }

        void WriteReference(ComponentNode r)
        {
            if (done.Add(r.Name))
                writer.Write(
                    $"{r.Name}[\"{r.Name}\r\n    {string.Join(", ", r.VersionRanges.OrderBy(x => x.Min))}\"]");
            else
                writer.Write(r.Name);
        }

        void WriteLink(Dependency d)
        {
            if (nodes[d.Component.Name].VersionRanges.HasSingle() && nodes[d.Reference.Name].VersionRanges.HasSingle())
                writer.Write(" --> ");
            else
            {
                var left = nodes[d.Component.Name].VersionRanges.HasSingle() ? "" : d.Component.Version.ToString();
                var right = nodes[d.Reference.Name].VersionRanges.HasSingle()
                    ? ""
                    : d.Reference.VersionRange.ToString();
                writer.Write($" -- \"{left} -> {right}\"--> ");
            }
        }
    }

    public static CommandLineArguments Arguments { get; private set; } = null!;

    public static void Verbose(FormattableString text)
    {
        if (Arguments.Verbose)
            Console.Error.WriteLine(text);
    }
}