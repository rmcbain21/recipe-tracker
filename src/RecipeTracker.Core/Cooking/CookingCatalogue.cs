using RecipeTracker.Core.Ports;
using RecipeTracker.Core.Quantities;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Cooking;

/// <summary>
/// Cooking Mode's state. Cooking Sessions live in their own table, which sync leaves out, so they
/// stay on this device (spec: device-local, never synced).
/// </summary>
public sealed class CookingCatalogue
{
    /// <summary>Not a synced table: issue 02's sync must not list it.</summary>
    internal const string Table = "cookingSessions";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public CookingCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>
    /// The Recipe's Cooking Session on this device, for "Resume" or "Start over", or null if
    /// there isn't one (or its Recipe has been deleted).
    /// </summary>
    public async Task<CookingSession?> GetSessionAsync(string recipeId)
    {
        var session = await _store.LoadAsync<CookingSession>(Table, recipeId);
        if (session is not { Deleted: false })
        {
            return null;
        }

        var recipe = await _store.LoadAsync<Recipe>(RecipeCatalogue.Table, recipeId);
        return recipe is { Deleted: false } ? session : null;
    }

    /// <summary>Starts cooking the Recipe from Gather at the Scale given, replacing any session it had ("Start over").</summary>
    /// <exception cref="RuleViolationException">The Recipe is a Draft, or is gone.</exception>
    public async Task<CookingSession> StartAsync(string recipeId, Scale scale)
    {
        var recipe = await _store.LoadAsync<Recipe>(RecipeCatalogue.Table, recipeId);
        if (recipe is not { Deleted: false })
        {
            throw new RuleViolationException("That Recipe has been deleted.");
        }

        if (recipe.IsDraft)
        {
            throw new RuleViolationException("Finish reviewing this Draft before cooking from it.");
        }

        var now = _clock.UtcNow;
        return await SaveAsync(new CookingSession { Id = recipeId, UpdatedAt = now, StartedAt = now, ScaleFactor = scale.Factor });
    }

    /// <summary>Moves to the next screen: from Gather to Step 1, and on through the Steps. Stays on the last Step.</summary>
    public Task<CookingSession> NextAsync(string recipeId) => MoveAsync(recipeId, +1);

    /// <summary>Moves to the previous screen, back to Gather. Stays on Gather.</summary>
    public Task<CookingSession> BackAsync(string recipeId) => MoveAsync(recipeId, -1);

    /// <summary>Ticks or unticks an Ingredient Line on Gather.</summary>
    public async Task<CookingSession> TickAsync(string recipeId, string lineId, bool ticked)
    {
        var session = await RequireAsync(recipeId);
        var ticks = session.GatherTicks.Where(id => id != lineId).ToList();
        if (ticked)
        {
            ticks.Add(lineId);
        }

        return await SaveAsync(session with { GatherTicks = ticks });
    }

    /// <summary>Changes the Scale while cooking.</summary>
    public async Task<CookingSession> SetScaleAsync(string recipeId, Scale scale) =>
        await SaveAsync(await RequireAsync(recipeId) with { ScaleFactor = scale.Factor });

    /// <summary>Ends the Cooking Session, so that cooking again starts fresh.</summary>
    public async Task EndAsync(string recipeId)
    {
        var session = await _store.LoadAsync<CookingSession>(Table, recipeId);
        if (session is { Deleted: false })
        {
            await _store.SaveAsync(Table, session with { Deleted = true, UpdatedAt = _clock.UtcNow });
        }
    }

    private async Task<CookingSession> MoveAsync(string recipeId, int offset)
    {
        var session = await RequireAsync(recipeId);
        var recipe = await _store.LoadAsync<Recipe>(RecipeCatalogue.Table, recipeId);
        var last = recipe?.Steps.Count ?? 0;
        var position = Math.Clamp(session.Position + offset, 0, Math.Max(last, 0));
        return position == session.Position ? session : await SaveAsync(session with { Position = position });
    }

    private async Task<CookingSession> RequireAsync(string recipeId) =>
        await GetSessionAsync(recipeId) ?? throw new RuleViolationException("Start cooking this Recipe first.");

    private async Task<CookingSession> SaveAsync(CookingSession session)
    {
        session = session with { UpdatedAt = _clock.UtcNow };
        await _store.SaveAsync(Table, session);
        return session;
    }
}
