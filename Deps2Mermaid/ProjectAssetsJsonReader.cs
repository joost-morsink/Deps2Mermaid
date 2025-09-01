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
        if (Root["libraries"] is JsonObject libraries)
        {
            foreach (var x in libraries)
            {
                var to = Component.Parse(x.Key);
                if (x.Value is JsonObject o && o["type"] is JsonValue type && type.GetValue<string>() == "project")
                    yield return new Dependency(project, Reference.Parse(x.Key));
            }
        }

        if (Root["project"] is JsonObject proj
            && proj["frameworks"] is JsonObject frameworks)
        {
            foreach(var fr in frameworks)
            {
                if (fr.Value is JsonObject fobj && fobj["dependencies"] is JsonObject dependencies)
                {
                    foreach (var y in dependencies)
                        if (y.Value is JsonObject dep && dep["version"] is JsonValue version)
                            yield return new Dependency(project,
                                new Reference(y.Key, VersionRange.Parse(version.GetValue<string>()).MinimalToExact()));
                }
            }
        }
           
    }
}