namespace RecipeTracker.Core.Ingredients;

/// <summary>How a captured line's Ingredient was suggested.</summary>
public enum IngredientMatch
{
    /// <summary>An existing Ingredient with that name.</summary>
    Matched,

    /// <summary>An existing Ingredient that has the name as an alias.</summary>
    Alias,

    /// <summary>No Ingredient has that name, so a new one is suggested.</summary>
    New,
}
