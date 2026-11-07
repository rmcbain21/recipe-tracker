using System.Text.Json.Serialization;

namespace RecipeTracker.Core.Recipes;

[JsonConverter(typeof(JsonStringEnumConverter<SourceKind>))]
public enum SourceKind
{
    Website,
    Book,
    Video,
    Other,
}

/// <summary>
/// Where a Recipe came from. Only a Website's URL is set for now (from a share); picking a Book,
/// and editing the Source, arrive with issue 04.
/// </summary>
public sealed record Source
{
    public required SourceKind Kind { get; init; }

    /// <summary>The page or video address, for a Website or a Video.</summary>
    public string Url { get; init; } = "";

    /// <summary>The site or channel name.</summary>
    public string Name { get; init; } = "";

    /// <summary>The Book, for a Book Source.</summary>
    public string? BookId { get; init; }

    public string Page { get; init; } = "";

    /// <summary>Free text, for an Other Source such as "Grandma".</summary>
    public string Text { get; init; } = "";

    public static Source Website(string url) => new() { Kind = SourceKind.Website, Url = url.Trim() };
}
