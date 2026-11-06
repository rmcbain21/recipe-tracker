using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class RecipeTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 5, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<string[]> TitlesAsync(AppCore app) =>
        (await app.Recipes.ListRecipesAsync()).Select(recipe => recipe.Title).ToArray();

    [Fact]
    public async Task An_added_recipe_is_remembered_after_the_app_is_reopened()
    {
        var added = await CreateApp().Recipes.AddRecipeAsync("Weeknight chili");

        var recipe = Assert.Single(await CreateApp().Recipes.ListRecipesAsync());

        Assert.Equal(added.Id, recipe.Id);
        Assert.Equal("Weeknight chili", recipe.Title);
    }

    [Fact]
    public async Task A_new_recipe_gets_a_guid_id_the_clock_time_and_is_not_deleted()
    {
        var recipe = await CreateApp().Recipes.AddRecipeAsync("Focaccia");

        Assert.True(Guid.TryParse(recipe.Id, out _));
        Assert.Equal(_clock.UtcNow, recipe.UpdatedAt);
        Assert.False(recipe.Deleted);
    }

    [Fact]
    public async Task Each_recipe_gets_its_own_id()
    {
        var app = CreateApp();

        var first = await app.Recipes.AddRecipeAsync("Focaccia");
        var second = await app.Recipes.AddRecipeAsync("Focaccia");

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Recipes_are_listed_by_title_ignoring_case()
    {
        var app = CreateApp();
        await app.Recipes.AddRecipeAsync("pad thai");
        await app.Recipes.AddRecipeAsync("Banana bread");
        await app.Recipes.AddRecipeAsync("Weeknight chili");

        Assert.Equal(["Banana bread", "pad thai", "Weeknight chili"], await TitlesAsync(app));
    }

    [Fact]
    public async Task There_are_no_recipes_at_first()
    {
        Assert.Empty(await CreateApp().Recipes.ListRecipesAsync());
    }

    [Fact]
    public async Task The_title_is_trimmed()
    {
        var recipe = await CreateApp().Recipes.AddRecipeAsync("  Focaccia  ");

        Assert.Equal("Focaccia", recipe.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_recipe_needs_a_title(string title)
    {
        var app = CreateApp();

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.AddRecipeAsync(title));

        Assert.Equal("Give the Recipe a title.", ex.Message);
        Assert.Empty(await app.Recipes.ListRecipesAsync());
    }

    [Fact]
    public async Task Deleted_recipes_are_not_listed()
    {
        var app = CreateApp();
        var kept = await app.Recipes.AddRecipeAsync("Focaccia");
        var gone = await app.Recipes.AddRecipeAsync("Weeknight chili");

        // A tombstone, as one arrives by sync; deleting from the app comes in a later slice.
        await _store.SaveAsync("recipes", gone with { Deleted = true, UpdatedAt = _clock.UtcNow.AddMinutes(1) });

        Assert.Equal([kept.Title], await TitlesAsync(app));
    }
}
