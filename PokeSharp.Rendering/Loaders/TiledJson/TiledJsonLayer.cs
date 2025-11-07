using System.Text.Json.Serialization;

namespace PokeSharp.Rendering.Loaders.TiledJson;

/// <summary>
///     Represents a layer in a Tiled JSON map.
/// </summary>
public class TiledJsonLayer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "tilelayer";

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    [JsonPropertyName("opacity")]
    public float Opacity { get; set; } = 1.0f;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary>
    ///     Tile data as flat array (for tilelayer).
    /// </summary>
    [JsonPropertyName("data")]
    public int[]? Data { get; set; }

    /// <summary>
    ///     Objects in this layer (for objectgroup).
    /// </summary>
    [JsonPropertyName("objects")]
    public List<TiledJsonObject>? Objects { get; set; }

    [JsonPropertyName("properties")]
    public List<TiledJsonProperty>? Properties { get; set; }
}

