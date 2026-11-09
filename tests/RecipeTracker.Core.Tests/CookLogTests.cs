using RecipeTracker.Core.Cooking;
using RecipeTracker.Core.Quantities;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class CookLogTests
{
    private const string Pasted = """
        Brownies

        Ingredients
        1 cup sugar

        Method
        1. Bake 25 minutes.
        """;

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 4, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<Recipe> RecipeAsync(AppCore app)
    {
        var editor = await app.Recipes.CaptureAsync(Pasted);
        editor.ConfirmAllMatches();
        return await app.Recipes.SaveAsync(editor);
    }

    private async Task CookAsync(AppCore app, string recipeId, int day)
    {
        _clock.UtcNow = new DateTimeOffset(2026, 11, day, 18, 0, 0, TimeSpan.Zero);
        await app.Cooking.StartAsync(recipeId, Scale.One);
        await app.Cooking.FinishAsync(recipeId);
    }

    [Fact]
    public async Task Done_records_today_and_ends_the_session()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.By(2));

        var entry = await app.Cooking.FinishAsync(recipe.Id);

        Assert.Equal(recipe.Id, entry.RecipeId);
        Assert.Equal(new DateOnly(2026, 11, 4), entry.Date);
        Assert.Null(await app.Cooking.GetSessionAsync(recipe.Id));
        Assert.Equal([entry], await CreateApp().Cooking.ListCookLogAsync(recipe.Id));
    }

    [Fact]
    public async Task The_recipe_shows_how_often_and_when_it_was_last_cooked()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        var other = await RecipeAsync(app);

        Assert.Equal("Never cooked", (await app.Cooking.GetCookSummaryAsync(recipe.Id)).ToString());

        await CookAsync(app, recipe.Id, 1);
        Assert.Equal("Cooked once · last on 1 Nov 2026", (await app.Cooking.GetCookSummaryAsync(recipe.Id)).ToString());

        await CookAsync(app, recipe.Id, 4);
        await CookAsync(app, recipe.Id, 2);
        await CookAsync(app, other.Id, 9);

        var summary = await app.Cooking.GetCookSummaryAsync(recipe.Id);
        Assert.Equal(new CookSummary(3, new DateOnly(2026, 11, 4)), summary);
        Assert.Equal("Cooked 3 times · last on 4 Nov 2026", summary.ToString());
        Assert.Equal(
            [new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 1)],
            (await app.Cooking.ListCookLogAsync(recipe.Id)).Select(e => e.Date));
    }

    [Fact]
    public async Task A_mistaken_entry_can_be_deleted_and_the_deletion_syncs()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await CookAsync(app, recipe.Id, 1);
        await CookAsync(app, recipe.Id, 1);
        var mistake = (await app.Cooking.ListCookLogAsync(recipe.Id))[0];
        _clock.UtcNow += TimeSpan.FromMinutes(1);

        await app.Cooking.DeleteCookLogEntryAsync(mistake.Id);

        Assert.Equal(1, (await app.Cooking.GetCookSummaryAsync(recipe.Id)).TimesCooked);
        var tombstone = Assert.Single(await _store.ListAsync<CookLogEntry>("cookLog"), e => e.Id == mistake.Id);
        Assert.True(tombstone.Deleted);
        Assert.Equal(_clock.UtcNow, tombstone.UpdatedAt);
    }

    [Fact]
    public async Task Deleting_a_recipe_tombstones_its_cook_log()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        var other = await RecipeAsync(app);
        await CookAsync(app, recipe.Id, 1);
        await CookAsync(app, recipe.Id, 2);
        await CookAsync(app, other.Id, 3);

        await app.Recipes.DeleteRecipeAsync(recipe.Id);

        var log = await _store.ListAsync<CookLogEntry>("cookLog");
        Assert.All(log.Where(e => e.RecipeId == recipe.Id), e => Assert.True(e.Deleted));
        Assert.False(Assert.Single(log, e => e.RecipeId == other.Id).Deleted);
        Assert.Empty(await app.Cooking.ListCookLogAsync(recipe.Id));
    }

    [Fact]
    public async Task Drafts_and_deleted_recipes_cannot_be_logged()
    {
        var app = CreateApp();
        var draft = await app.Recipes.AddRecipeAsync("Maybe later");
        var recipe = await RecipeAsync(app);
        await app.Recipes.DeleteRecipeAsync(recipe.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Cooking.FinishAsync(draft.Id));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Cooking.FinishAsync(recipe.Id));
        Assert.Empty(await _store.ListAsync<CookLogEntry>("cookLog"));
    }
}
