using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class CaptureTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 6, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private const string Pasted = """
        Scallion pancakes
        Serves 4
        Ingredients
        2 cups all-purpose flour
        3/4 cup boiling water
        4 scallions, thinly sliced
        Salt to taste
        Instructions
        1. Stir the water into the flour and knead into a dough.
        2. Roll out, scatter with scallions and salt, roll up and fry.
        """;

    private async Task<Ingredient> StoreIngredientAsync(string name, params string[] aliases)
    {
        var ingredient = new Ingredient { Id = Guid.NewGuid().ToString(), UpdatedAt = _clock.UtcNow, Name = name, Aliases = aliases };
        await _store.SaveAsync("ingredients", ingredient);
        return ingredient;
    }

    private static IReadOnlyList<EditedLine> Lines(RecipeEditor editor) => editor.Sections.SelectMany(s => s.Lines).ToList();

    [Fact]
    public async Task Pasted_text_fills_the_review_screen()
    {
        var editor = await CreateApp().Recipes.CaptureAsync(Pasted);

        Assert.Null(editor.RecipeId);
        Assert.Equal("Scallion pancakes", editor.Title);
        Assert.Equal((4m, "servings"), (editor.YieldAmount!.Value, editor.YieldLabel));
        var lines = Lines(editor);
        Assert.Equal(["2", "¾", "4", ""], lines.Select(l => l.QuantityText));
        Assert.Equal(["cups", "cup", "", ""], lines.Select(l => l.Unit));
        Assert.Equal("4 scallions, thinly sliced", lines[2].OriginalText);
        Assert.Equal("thinly sliced", lines[2].Preparation);
        Assert.Equal(["Stir the water into the flour and knead into a dough.", "Roll out, scatter with scallions and salt, roll up and fry."],
            editor.Steps.Select(s => s.Text));
    }

    [Fact]
    public async Task Each_line_suggests_a_matched_alias_or_new_ingredient_unconfirmed()
    {
        var flour = await StoreIngredientAsync("all-purpose flour");
        var greenOnion = await StoreIngredientAsync("green onion", "scallion");

        var lines = Lines(await CreateApp().Recipes.CaptureAsync(Pasted));

        Assert.Equal([IngredientMatch.Matched, IngredientMatch.New, IngredientMatch.Alias, IngredientMatch.New], lines.Select(l => l.Match));
        Assert.Equal(flour.Id, lines[0].Ingredient!.IngredientId);
        Assert.Equal(greenOnion.Id, lines[2].Ingredient!.IngredientId);
        Assert.Equal("boiling water", lines[1].Ingredient!.Name);
        Assert.True(lines[1].Ingredient!.IsNew);
        Assert.All(lines, line => Assert.False(line.IsConfirmed));
    }

    [Fact]
    public async Task Finishing_needs_every_match_confirmed()
    {
        var app = CreateApp();
        var editor = await app.Recipes.CaptureAsync(Pasted);

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));

        Assert.Equal("Confirm the Ingredient for every Ingredient Line, or save it as a Draft.", ex.Message);
        Assert.Empty(await app.Ingredients.ListIngredientsAsync());
    }

    [Fact]
    public async Task Confirming_and_picking_both_confirm_a_line()
    {
        var app = CreateApp();
        var water = await StoreIngredientAsync("water");
        var editor = await app.Recipes.CaptureAsync(Pasted);
        var lines = Lines(editor);

        lines[0].Confirm();
        lines[1].Ingredient = IngredientChoice.Existing(water);
        lines[2].Confirm();
        lines[3].Confirm();
        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.False(recipe.IsDraft);
        Assert.Equal(water.Id, recipe.Lines[1].IngredientId);
        Assert.Equal(["all-purpose flour", "Salt", "scallions", "water"], (await app.Ingredients.ListIngredientsAsync()).Select(i => i.Name));
        Assert.All(recipe.Lines, line => Assert.False(line.MatchUnconfirmed));
    }

    [Fact]
    public async Task Confirm_all_accepts_every_suggestion()
    {
        var app = CreateApp();
        var editor = await app.Recipes.CaptureAsync(Pasted);

        editor.ConfirmAllMatches();
        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.False(editor.HasUnconfirmedMatches);
        Assert.Equal(4, recipe.Lines.Count);
        Assert.All(recipe.Lines, line => Assert.NotNull(line.IngredientId));
    }

    [Fact]
    public async Task A_draft_keeps_suggestions_without_creating_ingredients()
    {
        var app = CreateApp();
        var greenOnion = await StoreIngredientAsync("green onion", "scallion");
        var editor = await app.Recipes.CaptureAsync(Pasted);
        Lines(editor)[0].Confirm();

        var draft = await app.Recipes.SaveDraftAsync(editor);

        Assert.True(draft.IsDraft);
        Assert.Equal(["all-purpose flour", "green onion"], (await app.Ingredients.ListIngredientsAsync()).Select(i => i.Name));
        Assert.False(draft.Lines[0].MatchUnconfirmed);
        Assert.Equal((null, true, "boiling water"), (draft.Lines[1].IngredientId, draft.Lines[1].MatchUnconfirmed, draft.Lines[1].SuggestedName));
        Assert.Equal((greenOnion.Id, true), (draft.Lines[2].IngredientId, draft.Lines[2].MatchUnconfirmed));
    }

    [Fact]
    public async Task Reopening_a_draft_shows_its_suggestions_still_unconfirmed()
    {
        var app = CreateApp();
        await StoreIngredientAsync("green onion", "scallion");
        var draft = await app.Recipes.SaveDraftAsync(await app.Recipes.CaptureAsync(Pasted));

        var editor = (await CreateApp().Recipes.EditRecipeAsync(draft.Id))!;

        Assert.True(editor.IsDraft);
        var lines = Lines(editor);
        Assert.Equal([IngredientMatch.New, IngredientMatch.New, IngredientMatch.Alias, IngredientMatch.New], lines.Select(l => l.Match));
        Assert.Equal("green onion", lines[2].Ingredient!.Name);
        Assert.Equal("boiling water", lines[1].Ingredient!.Name);
        Assert.True(editor.HasUnconfirmedMatches);
    }

    [Fact]
    public async Task A_draft_is_finished_once_its_matches_are_confirmed()
    {
        var app = CreateApp();
        var draft = await app.Recipes.SaveDraftAsync(await app.Recipes.CaptureAsync(Pasted));
        _clock.UtcNow += TimeSpan.FromDays(1);
        var editor = (await app.Recipes.EditRecipeAsync(draft.Id))!;

        editor.ConfirmAllMatches();
        var finished = await app.Recipes.SaveAsync(editor);

        Assert.False(finished.IsDraft);
        Assert.Equal(draft.Id, finished.Id);
        Assert.Equal(draft.CreatedAt, finished.CreatedAt);
        Assert.Equal(4, (await app.Ingredients.ListIngredientsAsync()).Count);
    }

    [Fact]
    public async Task A_draft_may_have_lines_with_no_ingredient_and_no_title()
    {
        var app = CreateApp();
        var editor = app.Recipes.NewRecipe();
        editor.AddLine(editor.Sections[0]).QuantityText = "2";

        var draft = await app.Recipes.SaveDraftAsync(editor);

        Assert.Equal("Untitled Recipe", draft.Title);
        var line = Assert.Single(draft.Lines);
        Assert.Null(line.IngredientId);
        Assert.False(line.MatchUnconfirmed);
    }

    [Fact]
    public async Task Finishing_needs_a_line_or_a_step()
    {
        var app = CreateApp();
        var editor = app.Recipes.NewRecipe();
        editor.Title = "Toast";

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));

        Assert.Equal("Add at least one Ingredient Line or Step, or save it as a Draft.", ex.Message);
    }

    [Fact]
    public async Task Drafts_list_first()
    {
        var app = CreateApp();
        var finished = app.Recipes.NewRecipe();
        finished.Title = "Apple pie";
        finished.AddStep("Bake it.");
        await app.Recipes.SaveAsync(finished);
        await app.Recipes.AddRecipeAsync("Zucchini bread");
        await app.Recipes.AddRecipeAsync("Banana bread");

        var recipes = await app.Recipes.ListRecipesAsync();

        Assert.Equal(["Banana bread", "Zucchini bread", "Apple pie"], recipes.Select(r => r.Title));
        Assert.Equal([true, true, false], recipes.Select(r => r.IsDraft));
    }

    [Fact]
    public async Task A_shared_url_becomes_the_website_source()
    {
        var app = CreateApp();

        var editor = await app.Recipes.CaptureAsync(Pasted, url: "https://example.com/scallion-pancakes");
        editor.ConfirmAllMatches();
        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal(Source.Website("https://example.com/scallion-pancakes"), recipe.Source);
        Assert.Equal(recipe.Source, (await app.Recipes.GetRecipeAsync(recipe.Id))!.Recipe.Source);
    }

    [Fact]
    public async Task A_url_on_its_own_line_in_shared_text_becomes_the_source_and_leaves_the_text()
    {
        var editor = await CreateApp().Recipes.CaptureAsync(Pasted + "\r\nhttps://example.com/pancakes\r\n");

        Assert.Equal("https://example.com/pancakes", editor.Source!.Url);
        Assert.Equal(2, editor.Steps.Count);
        Assert.DoesNotContain(editor.Steps, step => step.Text.Contains("example.com"));
    }

    [Fact]
    public async Task A_shared_title_is_used_when_the_text_has_none()
    {
        var editor = await CreateApp().Recipes.CaptureAsync("2 cups flour\n1 cup water", title: "Flatbread | A Food Site");

        Assert.Equal("Flatbread | A Food Site", editor.Title);
    }

    [Fact]
    public async Task A_finished_recipe_page_shows_a_pinch_without_a_number()
    {
        var app = CreateApp();
        var editor = await app.Recipes.CaptureAsync("Soup\nIngredients\npinch of salt\nMethod\nStir.");
        editor.ConfirmAllMatches();
        var recipe = await app.Recipes.SaveAsync(editor);

        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;

        Assert.Equal("pinch of salt", view.Ingredients[0].Lines[0].Text);
    }
}
