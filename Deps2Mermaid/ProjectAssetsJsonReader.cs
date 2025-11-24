using System.Text.Json;
using System.Text.Json.Nodes;

namespace Deps;

public class ProjectAssetsJsonReader
{
    public static ProjectAssetsJsonReader FromPath(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
        return new ProjectAssetsJsonReader(stream);
    }

    public ProjectAssetsJsonReader(Stream stream)
    {
        Root = JsonSerializer.Deserialize<JsonObject>(stream) ??
               throw new ArgumentException("Stream does not contain Json Object.", nameof(stream));
    }

    public JsonObject Root { get; }

    public IEnumerable<Dependency> Dependencies
        => TargetDependencies().Concat(ProjectDependencies());

    private IEnumerable<Dependency> TargetDependencies()
    {
        // if (Root["targets"] is JsonObject targets
        //     && targets.FirstOrDefault().Value is JsonObject target)
        foreach(var target in (Root["targets"] as JsonObject)?.Select(x => x.Value).OfType<JsonObject>() ?? Enumerable.Empty<JsonObject>())
        {
            foreach (var x in target)
            {
                var from = Component.Parse(x.Key);
                if (x.Value is JsonObject o && o["dependencies"] is JsonObject dependencies)
                {
                    foreach (var y in dependencies)
                        if (y.Value is JsonValue value)
                            yield return new Dependency(from,
                                new Reference(y.Key, VersionRange.Parse(value.GetValue<string>())));
                }
            }
        }
    }

    public Component ProjectComponent()
    {
        if (Root["project"] is JsonObject project)
        {
            var version = project["version"]?.GetValue<string>();
            if (version is not null
                && project["restore"] is JsonObject restore
                && restore["projectName"] is JsonValue projectName)
            {
                return new Component(projectName.GetValue<string>(), Version.Parse(version));
            }
        }

        throw new InvalidCastException("Cannot find project.");
    }

    private IEnumerable<Dependency> ProjectDependencies()
    {
        var project = ProjectComponent();
        // Project dependencies should be filled based on the projectFileDependencyGroups property
        if (Root["projectFileDependencyGroups"] is JsonObject groups)
        {
            foreach (var group in groups)
            {
                if (group.Value is JsonArray arr)
                {
                    foreach (var item in arr)
                    {
                        if (item is JsonValue jv)
                        {
                            var s = jv.GetValue<string>();
                            if (string.IsNullOrWhiteSpace(s))
                                continue;

                            var parts = s.Trim().Split(' ',2, StringSplitOptions.RemoveEmptyEntries);
                            
                            if(parts.Length == 2)
                                yield return new Dependency(project, new Reference(parts[0], VersionRange.Parse(parts[1]).MinimalToExact()));
                            
                        }
                    }
                }
            }
        }
           
    }
}