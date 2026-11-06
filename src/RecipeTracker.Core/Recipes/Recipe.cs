namespace RecipeTracker.Core.Recipes;

/// <summary>A dish the Cook has captured. Ingredients, Steps, Source and the rest arrive in later slices.</summary>
public sealed record Recipe : StoredRecord
{
    /// <summary>What the Cook calls it, such as "Weeknight chili". Never blank.</summary>
    public required string Title { get; init; }
}
