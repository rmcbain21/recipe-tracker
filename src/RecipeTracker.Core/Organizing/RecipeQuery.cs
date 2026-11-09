using System.Text.Json.Serialization;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Organizing;

[JsonConverter(typeof(JsonStringEnumConverter<RecipeSort>))]
public enum RecipeSort
{
    Title,
    RecentlyAdded,
    RecentlyCooked,
}

[JsonConverter(typeof(JsonStringEnumConverter<CookedFilter>))]
public enum CookedFilter
{
    Any,
    Cooked,
    NeverCooked,
}

/// <summary>
/// What the home list shows: search text, the Tags picked in the filter sheet, the field filters
/// and the sort. Tags combine as OR within a Tag Group and AND across groups; an ungrouped Tag and
/// every field filter is another AND.
/// </summary>
public sealed record RecipeQuery
{
    public static RecipeQuery Everything { get; } = new();

    /// <summary>Matched, ignoring case, against titles, Ingredient names and aliases, and Note text.</summary>
    public string Search { get; init; } = "";

    public IReadOnlyList<string> TagIds { get; init; } = [];

    /// <summary>Only Recipes with a Source of this kind.</summary>
    public SourceKind? SourceKind { get; init; }

    /// <summary>Only Recipes from this Book (and so of the Book kind).</summary>
    public string? BookId { get; init; }

    /// <summary>Only Recipes whose total Stated Time is at most this many minutes; Recipes with no stated time are left out.</summary>
    public int? MaxTotalMinutes { get; init; }

    public CookedFilter Cooked { get; init; } = CookedFilter.Any;

    public RecipeSort Sort { get; init; } = RecipeSort.Title;

    /// <summary>True when nothing narrows the list (the sort doesn't count).</summary>
    [JsonIgnore]
    public bool IsEverything =>
        Search.Trim().Length == 0 && TagIds.Count == 0 && SourceKind is null && BookId is null &&
        MaxTotalMinutes is null && Cooked == CookedFilter.Any;

    /// <summary>The number of filters set in the sheet (not the search), for the Filters button's badge.</summary>
    [JsonIgnore]
    public int FilterCount =>
        TagIds.Count + (SourceKind is null && BookId is null ? 0 : 1) + (MaxTotalMinutes is null ? 0 : 1) + (Cooked == CookedFilter.Any ? 0 : 1);

    public RecipeQuery WithTag(string tagId, bool on) => this with
    {
        TagIds = on ? [.. TagIds.Where(id => id != tagId), tagId] : TagIds.Where(id => id != tagId).ToList(),
    };

    /// <summary>Clears the filters and search, keeping the sort.</summary>
    public RecipeQuery Cleared() => Everything with { Sort = Sort };
}

/// <summary>Kept on this device only (not synced), so the home list opens as the Cook left it.</summary>
public sealed record SavedRecipeQuery : StoredRecord
{
    public RecipeQuery Query { get; init; } = RecipeQuery.Everything;
}

/// <summary>One Recipe in the home list.</summary>
public sealed record RecipeRow(
    string Id,
    string Title,
    bool IsDraft,
    string? SourceName,
    int? TotalMinutes,
    IReadOnlyList<string> TagNames,
    DateOnly? LastCooked)
{
    /// <summary>"45 min", "1 hr 15 min", or null with no stated time.</summary>
    public string? TotalTimeText => TotalMinutes is int minutes ? StatedTimes.Format(minutes) : null;
}

/// <summary>The home list for a query: Drafts first, then the rest in the chosen order.</summary>
public sealed record RecipeList(IReadOnlyList<RecipeRow> Rows, int TotalRecipes)
{
    /// <summary>Surprise me picks from these: the listed Recipes that aren't Drafts.</summary>
    public IReadOnlyList<RecipeRow> Cookable => Rows.Where(row => !row.IsDraft).ToList();

    public bool CanSurprise => Rows.Any(row => !row.IsDraft);

    /// <summary>A random listed Recipe that isn't a Draft, or null if there isn't one.</summary>
    public RecipeRow? Surprise(Random random)
    {
        var cookable = Cookable;
        return cookable.Count == 0 ? null : cookable[random.Next(cookable.Count)];
    }
}

/// <summary>A filter in use, shown as a removable chip above the list.</summary>
public sealed record ActiveFilter(string Label, RecipeQuery Without);

/// <summary>What the filter sheet offers: a row of Tags per Tag Group then the ungrouped Tags, and the Books in use.</summary>
public sealed record RecipeFilterOptions(IReadOnlyList<GroupedTags<Tag>> Tags, IReadOnlyList<Books.Book> Books);
