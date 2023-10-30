using System.Text.Json.Serialization;

namespace Deps;

public class Coordinate
{
    [JsonPropertyName("x")] public double X { get; set; } = 0;
    [JsonPropertyName("y")] public double Y { get; set; } = 0;
}