using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class IngredientUseTests
{
    private const string Pasted = """
        Brownies

        Ingredients
        1 cup sugar
        2 tbsp butter
        2 eggs
        Pinch of salt

        Method
        1. Melt the butter with half the sugar.
        2. Beat in the eggs and the rest of the sugar.
        3. Bake 25 minutes.
        """;

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static string[] Uses(RecipeEditor editor, int step, bool ticked = true) =>
        editor.Steps[step].Uses.Where(u => u.Ticked == ticked).Select(u => editor.LineFor(u.LineId)!.Label).ToArray();

    private static async Task<RecipeEditor> CaptureAsync(AppCore app)
    {
        var editor = await app.Recipes.CaptureAsync(Pasted);
        editor.ConfirmAllMatches();
        return editor;
    }

    private static async Task<string[]> ShownAsync(AppCore app, Recipe recipe, int step)
    {
        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;
        return view.UsesFor(view.Recipe.Steps[step]).Select(u => u.Text).ToArray();
    }

    [Fact]
    public async Task Capture_suggests_each_steps_uses_ticked()
    {
        var editor = await CaptureAsync(CreateApp());

        Assert.Equal(["1 cup sugar", "2 tbsp butter"], Uses(editor, 0));
        Assert.Equal(["1 cup sugar", "2 eggs"], Uses(editor, 1));
        Assert.Empty(editor.Steps[2].Uses);
    }

    [Fact]
    public async Task Suggestions_use_ingredient_aliases()
    {
        var app = CreateApp();
        await _store.SaveAsync("ingredients", new Ingredient { Id = "g", UpdatedAt = _clock.UtcNow, Name = "green onion", Aliases = ["scallion"] });

        var editor = await app.Recipes.CaptureAsync("Ingredients\n2 green onions\n\nMethod\n1. Scatter over the scallions.");

        Assert.Equal(["2 green onion"], Uses(editor, 0));
    }

    [Fact]
    public async Task Finishing_saves_ticked_uses_with_their_amounts()
    {
        var app = CreateApp();
        var editor = await CaptureAsync(app);
        var sugar = editor.Steps[0].Uses.First(u => editor.LineFor(u.LineId)!.Label == "1 cup sugar");
        sugar.QuantityText = "½";
        sugar.Unit = "cup";
        editor.Steps[1].Uses.First(u => editor.LineFor(u.LineId)!.Label == "2 eggs").Ticked = false;

        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal(["sugar, ½ cup (of 1 cup)", "butter, 2 tbsp"], await ShownAsync(app, recipe, 0));
        Assert.Equal(["sugar, 1 cup"], await ShownAsync(app, recipe, 1));
        Assert.Single(recipe.Steps[1].Uses); // the unticked one isn't kept
    }

    [Fact]
    public async Task A_use_amount_must_be_a_quantity()
    {
        var app = CreateApp();
        var editor = await CaptureAsync(app);
        editor.Steps[0].Uses[0].QuantityText = "lots";

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));
    }

    [Fact]
    public async Task Add_an_ingredient_the_step_text_does_not_name()
    {
        var app = CreateApp();
        var editor = await CaptureAsync(app);
        var step = editor.Steps[1];
        var salt = editor.LinesNotUsedBy(step).Single(line => line.Label == "Pinch salt" || line.Label.EndsWith("salt"));

        editor.AddUse(step, salt);
        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal("salt, pinch", (await ShownAsync(app, recipe, 1)).Last());
    }

    [Fact]
    public async Task Removing_a_line_removes_its_uses()
    {
        var app = CreateApp();
        var editor = await CaptureAsync(app);
        var sugarLine = editor.Sections[0].Lines.First(line => line.Label == "1 cup sugar");

        editor.RemoveLine(sugarLine);
        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal(["butter, 2 tbsp"], await ShownAsync(app, recipe, 0));
        Assert.DoesNotContain(recipe.Steps.SelectMany(s => s.Uses), u => u.LineId == sugarLine.Id);
    }

    [Fact]
    public async Task A_draft_remembers_unticked_suggestions_and_finishing_drops_them()
    {
        var app = CreateApp();
        var editor = await CaptureAsync(app);
        editor.Steps[1].Uses.First(u => editor.LineFor(u.LineId)!.Label == "2 eggs").Ticked = false;
        var draft = await app.Recipes.SaveDraftAsync(editor);

        var reopened = (await app.Recipes.EditRecipeAsync(draft.Id))!;
        reopened.RefreshUses();
        Assert.Equal(["2 eggs"], Uses(reopened, 1, ticked: false));
        Assert.Equal(["1 cup sugar"], Uses(reopened, 1));

        var finished = await app.Recipes.SaveAsync(reopened);
        Assert.All(finished.Steps.SelectMany(s => s.Uses), use => Assert.False(use.Unticked));
        Assert.Equal(["sugar, 1 cup"], await ShownAsync(app, finished, 1));
    }

    [Fact]
    public async Task Editing_a_finished_recipe_does_not_put_back_a_use_the_cook_took_off()
    {
        var app = CreateApp();
        var editor = await CaptureAsync(app);
        editor.Steps[1].Uses.First(u => editor.LineFor(u.LineId)!.Label == "2 eggs").Ticked = false;
        var recipe = await app.Recipes.SaveAsync(editor);

        var edit = (await app.Recipes.EditRecipeAsync(recipe.Id))!;
        edit.Title = "Fudgy brownies";
        edit.RefreshUses();
        Assert.Equal(["2 eggs"], Uses(edit, 1, ticked: false)); // offered, not ticked
        var saved = await app.Recipes.SaveAsync(edit);

        Assert.Equal(["sugar, 1 cup"], await ShownAsync(app, saved, 1));
    }

    [Fact]
    public async Task A_changed_or_new_step_gets_ticked_suggestions_when_editing()
    {
        var app = CreateApp();
        var recipe = await app.Recipes.SaveAsync(await CaptureAsync(app));

        var edit = (await app.Recipes.EditRecipeAsync(recipe.Id))!;
        edit.Steps[2].Text = "Bake 25 minutes, then sprinkle with salt.";
        edit.AddStep("Dust with sugar.");
        var saved = await app.Recipes.SaveAsync(edit);

        Assert.Equal(["salt"], (await ShownAsync(app, saved, 2)).Select(t => t.Split(',')[0]).Select(n => n.Replace("pinch of ", "")));
        Assert.Equal(["sugar, 1 cup"], await ShownAsync(app, saved, 3));
    }

    [Fact]
    public async Task A_recipe_saved_before_ingredient_uses_shows_them_suggested_from_the_step_text()
    {
        var app = CreateApp();
        var recipe = await app.Recipes.SaveAsync(await CaptureAsync(app));
        await _store.SaveAsync("recipes", recipe with { Steps = recipe.Steps.Select(s => s with { Uses = [] }).ToList() });

        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;

        Assert.True(view.UsesAreSuggested);
        Assert.Equal(["sugar, 1 cup", "butter, 2 tbsp"], view.UsesFor(view.Recipe.Steps[0]).Select(u => u.Text));
        Assert.Equal(["sugar, 2 cup", "butter, 4 tbsp"], view.AtScale(Quantities.Scale.By(2)).UsesFor(view.Recipe.Steps[0]).Select(u => u.Text));
    }

    [Fact]
    public async Task Once_any_step_has_saved_uses_nothing_is_suggested_on_the_page()
    {
        var app = CreateApp();
        var editor = await CaptureAsync(app);
        foreach (var use in editor.Steps.Skip(1).SelectMany(s => s.Uses))
        {
            use.Ticked = false;
        }

        var recipe = await app.Recipes.SaveAsync(editor);
        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;

        Assert.False(view.UsesAreSuggested);
        Assert.Empty(view.UsesFor(view.Recipe.Steps[1]));
    }

    [Fact]
    public async Task A_use_of_a_line_with_no_amount_shows_just_its_name()
    {
        var app = CreateApp();
        var editor = app.Recipes.NewRecipe();
        editor.Title = "Seasoned water";
        var salt = editor.AddLine(editor.Sections[0]);
        salt.Ingredient = IngredientChoice.New("salt");
        editor.AddStep("Season with salt.");

        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal(["salt"], await ShownAsync(app, recipe, 0));
    }
}
