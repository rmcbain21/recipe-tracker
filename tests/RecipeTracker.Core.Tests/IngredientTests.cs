using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class IngredientTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 6, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    // Aliases are edited in a later slice, so tests store an Ingredient with aliases directly.
    private async Task<Ingredient> StoreAsync(string name, params string[] aliases)
    {
        var ingredient = new Ingredient { Id = Guid.NewGuid().ToString(), UpdatedAt = _clock.UtcNow, Name = name, Aliases = aliases };
        await _store.SaveAsync("ingredients", ingredient);
        return ingredient;
    }

    private static async Task<string[]> SearchAsync(AppCore app, string text) =>
        (await app.Ingredients.SearchAsync(text)).Select(i => i.Name).ToArray();

    [Theory]
    [InlineData("green onion")]
    [InlineData("Green Onions")]
    [InlineData("  green   onion ")]
    [InlineData("scallion")]
    [InlineData("Scallions")]
    public async Task An_ingredient_is_found_by_name_or_alias_ignoring_case_spacing_and_plurals(string name)
    {
        var greenOnion = await StoreAsync("green onion", "scallion");

        var found = await CreateApp().Ingredients.FindByNameAsync(name);

        Assert.Equal(greenOnion.Id, found?.Id);
    }

    [Theory]
    [InlineData("tomatoes", "tomato")]
    [InlineData("olives", "olive")]
    [InlineData("eggs", "egg")]
    [InlineData("molasses", "molasses")]
    public async Task Plurals_match_singulars(string stored, string typed)
    {
        await StoreAsync(stored);

        Assert.NotNull(await CreateApp().Ingredients.FindByNameAsync(typed));
    }

    [Fact]
    public async Task Different_ingredients_do_not_match()
    {
        await StoreAsync("butter");

        Assert.Null(await CreateApp().Ingredients.FindByNameAsync("peanut butter"));
        Assert.Null(await CreateApp().Ingredients.FindByNameAsync("buttermilk"));
    }

    [Fact]
    public async Task Searching_puts_exact_matches_first_then_names_that_start_with_the_text()
    {
        await StoreAsync("peanut butter");
        await StoreAsync("buttermilk");
        await StoreAsync("butter", "unsalted butter");
        await StoreAsync("flour");

        Assert.Equal(["butter", "buttermilk", "peanut butter"], await SearchAsync(CreateApp(), "Butter"));
    }

    [Fact]
    public async Task Searching_finds_aliases()
    {
        await StoreAsync("green onion", "scallion");

        Assert.Equal(["green onion"], await SearchAsync(CreateApp(), "scal"));
    }

    [Fact]
    public async Task Searching_for_nothing_lists_every_ingredient()
    {
        await StoreAsync("salt");
        await StoreAsync("Butter");

        Assert.Equal(["Butter", "salt"], await SearchAsync(CreateApp(), " "));
    }

    [Fact]
    public async Task Deleted_ingredients_are_not_found_or_listed()
    {
        var gone = await StoreAsync("margarine");
        await _store.SaveAsync("ingredients", gone with { Deleted = true });
        var app = CreateApp();

        Assert.Null(await app.Ingredients.FindByNameAsync("margarine"));
        Assert.Empty(await app.Ingredients.SearchAsync("marg"));
        Assert.Empty(await app.Ingredients.ListIngredientsAsync());
    }

    [Fact]
    public async Task A_new_ingredient_that_matches_an_alias_uses_the_existing_one()
    {
        var greenOnion = await StoreAsync("green onion", "scallion");
        var app = CreateApp();
        var editor = app.Recipes.NewRecipe();
        editor.Title = "Fried rice";
        var line = editor.AddLine(editor.Sections[0]);
        line.Ingredient = IngredientChoice.New("Scallions");

        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal(greenOnion.Id, recipe.Lines[0].IngredientId);
        Assert.Single(await app.Ingredients.ListIngredientsAsync());
    }

    [Fact]
    public async Task A_line_whose_ingredient_was_deleted_still_shows_its_name()
    {
        var app = CreateApp();
        var editor = app.Recipes.NewRecipe();
        editor.Title = "Toast";
        editor.AddLine(editor.Sections[0]).Ingredient = IngredientChoice.New("margarine");
        var recipe = await app.Recipes.SaveAsync(editor);
        var margarine = (await app.Ingredients.FindByNameAsync("margarine"))!;
        await _store.SaveAsync("ingredients", margarine with { Deleted = true });

        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;

        Assert.Equal("margarine", view.Ingredients[0].Lines[0].IngredientName);
    }
}
