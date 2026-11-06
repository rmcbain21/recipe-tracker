using RecipeTracker.Core.Ports;
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
        Recipes = new RecipeCatalogue(store, clock);
    }

    public RecipeCatalogue Recipes { get; }
}
