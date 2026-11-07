namespace RecipeTracker.Core.Ingredients;

/// <summary>
/// A food the Cook buys or keeps, shared by every Recipe that uses it (ADR 0003).
/// Density, Store Section and Staple arrive with the Shopping List.
/// </summary>
public sealed record Ingredient : StoredRecord
{
    /// <summary>What the Cook calls it, such as "all-purpose flour". Never blank.</summary>
    public required string Name { get; init; }

    /// <summary>Other names that mean the same Ingredient, such as "scallion" for green onion.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>On a tombstone left by a Merge, the Ingredient this one was merged into.</summary>
    public string? MergedInto { get; init; }
}
