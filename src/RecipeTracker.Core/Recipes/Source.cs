using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RecipeTracker.Core.Recipes;

[JsonConverter(typeof(JsonStringEnumConverter<SourceKind>))]
public enum SourceKind
{
    Website,
    Book,
    Video,
    Other,
}

/// <summary>Where a Recipe came from. Only the fields for its kind are set.</summary>
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

    /// <summary>A Website Source with only its URL, as a share gives it; saving fills in the site name.</summary>
    public static Source Website(string url) => new() { Kind = SourceKind.Website, Url = url.Trim() };

    /// <summary>"seriouseats.com" for "https://www.seriouseats.com/…", or null if the text isn't a web address.</summary>
    public static string? HostName(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) || uri.Host.Length == 0)
        {
            return null;
        }

        return uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
    }
}

/// <summary>
/// A Recipe's Source being edited. Every kind's fields are kept while the Cook switches between
/// kinds; saving keeps only the chosen kind's.
/// </summary>
public sealed class EditedSource
{
    /// <summary>The kind picked, or null for no Source.</summary>
    public SourceKind? Kind { get; set; }

    public string Url { get; set; } = "";

    /// <summary>The site name (Website) or channel (Video). A blank site name is filled in from the URL.</summary>
    public string Name { get; set; } = "";

    public string? BookId { get; set; }

    public string Page { get; set; } = "";

    public string Text { get; set; } = "";

    internal static EditedSource From(Source? source) => source is null ? new() : new()
    {
        Kind = source.Kind,
        Url = source.Url,
        Name = source.Name,
        BookId = source.BookId,
        Page = source.Page,
        Text = source.Text,
    };

    /// <summary>The Source to save, or null when the chosen kind's fields are all blank.</summary>
    /// <param name="bookExists">Whether a Book id is one of the Cook's Books.</param>
    /// <exception cref="RuleViolationException">The URL isn't a web address, or a Book Source has no Book.</exception>
    internal Source? ToSource(Func<string, bool> bookExists)
    {
        switch (Kind)
        {
            case SourceKind.Website or SourceKind.Video:
                var url = TidyUrl(Url);
                var name = Name.Trim();
                if (Kind == SourceKind.Website && name.Length == 0 && url.Length > 0)
                {
                    name = Source.HostName(url)!;
                }

                return url.Length == 0 && name.Length == 0 ? null : new Source { Kind = Kind.Value, Url = url, Name = name };

            case SourceKind.Book:
                if (BookId is null)
                {
                    throw new RuleViolationException("Pick the Book this Recipe is from, or choose another kind of Source.");
                }

                if (!bookExists(BookId))
                {
                    throw new RuleViolationException("That Book has been deleted. Pick another one.");
                }

                return new Source { Kind = SourceKind.Book, BookId = BookId, Page = Page.Trim() };

            case SourceKind.Other:
                var text = Text.Trim();
                return text.Length == 0 ? null : new Source { Kind = SourceKind.Other, Text = text };

            default:
                return null;
        }
    }

    // "seriouseats.com/x" becomes "https://seriouseats.com/x"; anything that still isn't an http(s) address is refused.
    private static string TidyUrl(string url)
    {
        url = url.Trim();
        if (url.Length == 0)
        {
            return "";
        }

        if (!url.Contains("://", StringComparison.Ordinal))
        {
            url = "https://" + url;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !uri.Host.Contains('.'))
        {
            throw new RuleViolationException($"\"{url}\" isn't a web address. Try one like \"https://seriouseats.com/…\".");
        }

        return url;
    }
}

/// <summary>
/// A Source as the recipe page shows it, after "From": the Book title, site, channel or free text
/// (<see cref="Name"/>), a link when there is one, and a Book's page.
/// </summary>
public sealed partial record ShownSource(SourceKind Kind, string Name, string? Url, string Page)
{
    /// <summary>", p. 212" for a page, ", pp. 12–13" for a range, the text as typed otherwise, or blank.</summary>
    public string PageText => Page switch
    {
        "" => "",
        _ when Page.All(char.IsAsciiDigit) => $", p. {Page}",
        _ when PageRange().Match(Page) is { Success: true } range => $", pp. {range.Groups[1].Value}–{range.Groups[2].Value}",
        _ => $", {Page}",
    };

    [GeneratedRegex(@"^(\d+)\s*[-–]\s*(\d+)$")]
    private static partial Regex PageRange();
}
