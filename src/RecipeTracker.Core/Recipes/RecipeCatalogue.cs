using RecipeTracker.Core.Ports;

namespace RecipeTracker.Core.Recipes;

/// <summary>The Cook's Recipes.</summary>
public sealed class RecipeCatalogue
{
    internal const string Table = "recipes";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public RecipeCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<Recipe> AddRecipeAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new RuleViolationException("Give the Recipe a title.");
        }

        var recipe = new Recipe { Id = Guid.NewGuid().ToString(), UpdatedAt = _clock.UtcNow, Title = title.Trim() };

        await _store.SaveAsync(Table, recipe);
        return recipe;
    }

    /// <summary>Every Recipe that isn't deleted, by title.</summary>
    public async Task<IReadOnlyList<Recipe>> ListRecipesAsync()
    {
        var recipes = await _store.ListAsync<Recipe>(Table);
        return recipes
            .Where(recipe => !recipe.Deleted)
            .OrderBy(recipe => recipe.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
