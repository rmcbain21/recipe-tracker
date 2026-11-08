namespace RecipeTracker.Core.Organizing;

/// <summary>
/// A label the Cook puts on Recipes to find them, such as "crockpot" or "weeknight". Shared by every
/// Recipe that has it, so renaming it renames it everywhere.
/// </summary>
public sealed record Tag : StoredRecord
{
    /// <summary>Never blank, and unique among Tags ignoring case.</summary>
    public required string Name { get; init; }

    /// <summary>The Tag Group it's in, or null for an ungrouped Tag.</summary>
    public string? GroupId { get; init; }

    /// <summary>On a tombstone left by a Merge, the Tag this one was merged into.</summary>
    public string? MergedInto { get; init; }
}

/// <summary>A named set of Tags, such as Meal or Equipment, that the filters show as one row.</summary>
public sealed record TagGroup : StoredRecord
{
    /// <summary>Never blank, and unique among Tag Groups ignoring case.</summary>
    public required string Name { get; init; }

    /// <summary>Position in the Cook's chosen order, starting at 0.</summary>
    public required int SortOrder { get; init; }
}

/// <summary>A Tag with how many Recipes (Drafts included) have it.</summary>
public sealed record TagUse(Tag Tag, int RecipeCount);

/// <summary>A Tag Group (null for the ungrouped Tags) and its Tags by name.</summary>
public sealed record GroupedTags<T>(TagGroup? Group, IReadOnlyList<T> Tags);
