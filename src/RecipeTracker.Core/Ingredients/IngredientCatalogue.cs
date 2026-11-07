using RecipeTracker.Core.Ports;

namespace RecipeTracker.Core.Ingredients;

/// <summary>The Cook's shared Ingredients, found by name or alias.</summary>
public sealed class IngredientCatalogue
{
    internal const string Table = "ingredients";

    private readonly IRecordStore _store;

    public IngredientCatalogue(IRecordStore store)
    {
        _store = store;
    }

    /// <summary>Every Ingredient that isn't deleted, by name.</summary>
    public async Task<IReadOnlyList<Ingredient>> ListIngredientsAsync()
    {
        var ingredients = await _store.ListAsync<Ingredient>(Table);
        return ingredients
            .Where(ingredient => !ingredient.Deleted)
            .OrderBy(ingredient => ingredient.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>The Ingredient with this name or alias (ignoring case and a plural), or null if there isn't one.</summary>
    public async Task<Ingredient?> FindByNameAsync(string name) =>
        FindByName(await ListIngredientsAsync(), name);

    /// <summary>
    /// Ingredients whose name or an alias contains the text, for the Ingredient picker: exact
    /// matches first, then names that start with the text, then the rest, each by name.
    /// </summary>
    public async Task<IReadOnlyList<Ingredient>> SearchAsync(string text)
    {
        var ingredients = await ListIngredientsAsync();
        if (string.IsNullOrWhiteSpace(text))
        {
            return ingredients;
        }

        return ingredients
            .Select(ingredient => (ingredient, rank: Rank(ingredient, text)))
            .Where(match => match.rank is not null)
            .OrderBy(match => match.rank)
            .Select(match => match.ingredient)
            .ToList();
    }

    internal static Ingredient? FindByName(IEnumerable<Ingredient> ingredients, string name) =>
        ingredients.FirstOrDefault(ingredient => Names(ingredient).Any(n => IngredientNames.Same(n, name)));

    private static IEnumerable<string> Names(Ingredient ingredient) => ingredient.Aliases.Prepend(ingredient.Name);

    private static int? Rank(Ingredient ingredient, string text)
    {
        var tidy = IngredientNames.Tidy(text);
        if (Names(ingredient).Any(n => IngredientNames.Same(n, tidy)))
        {
            return 0;
        }

        if (Names(ingredient).Any(n => n.StartsWith(tidy, StringComparison.CurrentCultureIgnoreCase)))
        {
            return 1;
        }

        return Names(ingredient).Any(n => IngredientNames.Contains(n, tidy)) ? 2 : null;
    }
}
