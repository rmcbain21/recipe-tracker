using RecipeTracker.Core.Books;
using RecipeTracker.Core.Cooking;
using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Organizing;
using RecipeTracker.Core.Parsing;
using RecipeTracker.Core.Ports;
using RecipeTracker.Core.Preferences;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core;

/// <summary>
/// The application-level API the UI uses. Each area of the app hangs off it.
/// It depends only on the ports, so tests run it against in-memory fakes.
/// </summary>
public sealed class AppCore
{
    public AppCore(IRecordStore store, IClock clock)
    {
        Recipes = new RecipeCatalogue(store, clock, new RuleBasedRecipeParser());
        Ingredients = new IngredientCatalogue(store);
        Books = new BookCatalogue(store, clock);
        Tags = new TagCatalogue(store, clock);
        Finder = new RecipeFinder(store, clock, Tags);
        Cooking = new CookingCatalogue(store, clock);
        Settings = new UserSettings(store, clock);
    }

    public TagCatalogue Tags { get; }

    /// <summary>The home list: search, filters, sort and Surprise me.</summary>
    public RecipeFinder Finder { get; }

    public CookingCatalogue Cooking { get; }

    public UserSettings Settings { get; }

    public BookCatalogue Books { get; }

    public RecipeCatalogue Recipes { get; }

    public IngredientCatalogue Ingredients { get; }
}
