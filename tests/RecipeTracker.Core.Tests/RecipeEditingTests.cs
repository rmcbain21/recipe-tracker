using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Quantities;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class RecipeEditingTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 6, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static EditedLine AddLine(RecipeEditor editor, EditedSection section, string quantity, string unit, IngredientChoice ingredient,
        string preparation = "", bool optional = false)
    {
        var line = editor.AddLine(section);
        line.QuantityText = quantity;
        line.Unit = unit;
        line.Ingredient = ingredient;
        line.Preparation = preparation;
        line.Optional = optional;
        return line;
    }

    private static RecipeEditor Focaccia(AppCore app)
    {
        var editor = app.Recipes.NewRecipe();
        editor.Title = "Focaccia";
        editor.Description = "Dimpled and oily.";
        editor.YieldAmount = 1;
        editor.YieldLabel = "9×13 pan";
        editor.PrepMinutes = 30;
        editor.CookMinutes = 25;
        editor.TotalMinutes = 1110;
        AddLine(editor, editor.Sections[0], "1½", "tsp", IngredientChoice.New("instant yeast"));
        var dough = editor.AddSection("For the dough");
        AddLine(editor, dough, "4", "cups", IngredientChoice.New("all-purpose flour"), "sifted");
        AddLine(editor, dough, "", "", IngredientChoice.New("Flaky salt"), optional: true);
        editor.AddStep("Mix everything.");
        editor.AddStep("Bake at 450°F.");
        return editor;
    }

    private static string[] LineTexts(RecipeView view) =>
        view.Ingredients.SelectMany(group => group.Lines).Select(line => line.Text).ToArray();

    [Fact]
    public async Task A_recipe_typed_by_hand_is_shown_on_its_recipe_page_after_reopening()
    {
        var saved = await CreateApp().Recipes.SaveAsync(Focaccia(CreateApp()));

        var view = await CreateApp().Recipes.GetRecipeAsync(saved.Id);

        Assert.NotNull(view);
        var recipe = view.Recipe;
        Assert.Equal("Focaccia", recipe.Title);
        Assert.Equal("Dimpled and oily.", recipe.Description);
        Assert.Equal("1 9×13 pan", recipe.Yield!.ToString());
        Assert.Equal(new StatedTimes(30, 25, 1110), recipe.StatedTimes);
        Assert.Equal(_clock.UtcNow, recipe.CreatedAt);

        Assert.Collection(view.Ingredients,
            group =>
            {
                Assert.Null(group.Heading);
                Assert.Equal(["1½ tsp instant yeast"], group.Lines.Select(l => l.Text));
            },
            group =>
            {
                Assert.Equal("For the dough", group.Heading);
                Assert.Equal(["4 cups all-purpose flour, sifted", "Flaky salt (optional)"], group.Lines.Select(l => l.Text));
            });
        Assert.Equal(["Mix everything.", "Bake at 450°F."], recipe.Steps.Select(s => s.Text));
    }

    [Fact]
    public async Task Quantities_are_stored_as_written()
    {
        var app = CreateApp();
        var editor = app.Recipes.NewRecipe();
        editor.Title = "Chili";
        AddLine(editor, editor.Sections[0], "2 to 3", "cans", IngredientChoice.New("kidney beans"));
        AddLine(editor, editor.Sections[0], "1 1/2", "lb", IngredientChoice.New("ground beef"));

        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal([new Quantity(2, 3), new Quantity(1.5m)], recipe.Lines.Select(l => l.Quantity));
        Assert.Equal(["2–3 cans kidney beans", "1½ lb ground beef"], LineTexts((await app.Recipes.GetRecipeAsync(recipe.Id))!));
    }

    [Fact]
    public async Task New_ingredients_are_created_once_and_shared_between_recipes()
    {
        var app = CreateApp();
        await app.Recipes.SaveAsync(Focaccia(app));

        var editor = app.Recipes.NewRecipe();
        editor.Title = "Pizza";
        AddLine(editor, editor.Sections[0], "3", "cups", IngredientChoice.New("All-Purpose  Flour"));
        AddLine(editor, editor.Sections[0], "1", "cup", IngredientChoice.New("tomatoes"));
        AddLine(editor, editor.Sections[0], "2", "", IngredientChoice.New("tomato"));
        var pizza = await app.Recipes.SaveAsync(editor);

        var names = (await app.Ingredients.ListIngredientsAsync()).Select(i => i.Name);
        Assert.Equal(["all-purpose flour", "Flaky salt", "instant yeast", "tomatoes"], names);

        var flour = await app.Ingredients.FindByNameAsync("all-purpose flour");
        Assert.Equal(flour!.Id, pizza.Lines[0].IngredientId);
        Assert.Equal(pizza.Lines[1].IngredientId, pizza.Lines[2].IngredientId);
    }

    [Fact]
    public async Task An_existing_ingredient_can_be_picked()
    {
        var app = CreateApp();
        await app.Recipes.SaveAsync(Focaccia(app));
        var yeast = Assert.Single(await app.Ingredients.SearchAsync("yeast"));

        var editor = app.Recipes.NewRecipe();
        editor.Title = "Bread";
        AddLine(editor, editor.Sections[0], "1", "tsp", IngredientChoice.Existing(yeast));
        var bread = await app.Recipes.SaveAsync(editor);

        Assert.Equal(yeast.Id, bread.Lines[0].IngredientId);
        Assert.Equal(3, (await app.Ingredients.ListIngredientsAsync()).Count);
    }

    [Fact]
    public async Task Editing_changes_the_saved_recipe_and_keeps_its_id_and_created_time()
    {
        var app = CreateApp();
        var saved = await app.Recipes.SaveAsync(Focaccia(app));
        _clock.UtcNow += TimeSpan.FromHours(1);

        var editor = (await app.Recipes.EditRecipeAsync(saved.Id))!;
        Assert.Equal("Focaccia", editor.Title);
        Assert.Equal("1½", editor.Sections[0].Lines[0].QuantityText);
        Assert.Equal("instant yeast", editor.Sections[0].Lines[0].Ingredient!.Name);
        Assert.Equal("For the dough", editor.Sections[1].Heading);

        editor.Title = "Overnight focaccia";
        editor.Sections[1].Lines[0].QuantityText = "5";
        editor.Steps[1].Text = "Bake at 475°F.";
        var edited = await app.Recipes.SaveAsync(editor);

        Assert.Equal(saved.Id, edited.Id);
        Assert.Equal(saved.CreatedAt, edited.CreatedAt);
        Assert.Equal(_clock.UtcNow, edited.UpdatedAt);
        var view = (await app.Recipes.GetRecipeAsync(saved.Id))!;
        Assert.Equal("Overnight focaccia", view.Recipe.Title);
        Assert.Contains("5 cups all-purpose flour, sifted", LineTexts(view));
        Assert.Equal("Bake at 475°F.", view.Recipe.Steps[1].Text);
        Assert.Single(await app.Recipes.ListRecipesAsync());
    }

    [Fact]
    public async Task An_unsaved_edit_leaves_the_recipe_as_it_was()
    {
        var app = CreateApp();
        var saved = await app.Recipes.SaveAsync(Focaccia(app));

        var editor = (await app.Recipes.EditRecipeAsync(saved.Id))!;
        editor.Title = "Changed";
        editor.RemoveStep(editor.Steps[0]);

        var view = (await app.Recipes.GetRecipeAsync(saved.Id))!;
        Assert.Equal("Focaccia", view.Recipe.Title);
        Assert.Equal(2, view.Recipe.Steps.Count);
    }

    [Fact]
    public async Task Lines_and_steps_can_be_reordered_and_removed()
    {
        var app = CreateApp();
        var editor = Focaccia(app);
        var dough = editor.Sections[1];

        editor.MoveLine(dough.Lines[1], -1);                // salt above flour
        editor.MoveLine(editor.Sections[0].Lines[0], +1);   // yeast down into the dough section
        editor.RemoveLine(dough.Lines.Single(l => l.Ingredient!.Name == "all-purpose flour"));
        editor.MoveStep(editor.Steps[1], -1);
        var recipe = await app.Recipes.SaveAsync(editor);

        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;
        var group = Assert.Single(view.Ingredients);
        Assert.Equal("For the dough", group.Heading);
        Assert.Equal(["1½ tsp instant yeast", "Flaky salt (optional)"], group.Lines.Select(l => l.Text));
        Assert.Equal(["Bake at 450°F.", "Mix everything."], recipe.Steps.Select(s => s.Text));
    }

    [Fact]
    public void Moving_past_the_first_or_last_place_does_nothing()
    {
        var editor = Focaccia(CreateApp());
        var yeast = editor.Sections[0].Lines[0];
        var salt = editor.Sections[1].Lines[1];

        editor.MoveLine(yeast, -1);
        editor.MoveLine(salt, +1);
        editor.MoveStep(editor.Steps[0], -1);

        Assert.Same(yeast, editor.Sections[0].Lines[0]);
        Assert.Same(salt, editor.Sections[1].Lines[^1]);
        Assert.Equal("Mix everything.", editor.Steps[0].Text);
    }

    [Fact]
    public async Task Sections_can_be_reordered_and_removing_one_keeps_its_lines()
    {
        var app = CreateApp();
        var editor = Focaccia(app);
        var topping = editor.AddSection("For the topping");
        AddLine(editor, topping, "¼", "cup", IngredientChoice.New("olive oil"));

        editor.MoveSection(topping, -1);
        var afterMove = editor.Sections.Select(s => s.Heading ?? "(none)").ToArray();
        editor.RemoveSection(editor.Sections.Single(s => s.Heading == "For the dough"));
        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal(["(none)", "For the topping", "For the dough"], afterMove);
        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;
        var groups = view.Ingredients;
        Assert.Equal(["(none)", "For the topping"], groups.Select(g => g.Heading ?? "(none)"));
        Assert.Equal(["¼ cup olive oil", "4 cups all-purpose flour, sifted", "Flaky salt (optional)"], groups[1].Lines.Select(l => l.Text));
        Assert.Single(recipe.Sections);
    }

    [Fact]
    public void The_lines_before_the_first_heading_are_not_a_section_to_move_or_remove()
    {
        var editor = Focaccia(CreateApp());

        Assert.Throws<ArgumentException>(() => editor.RemoveSection(editor.Sections[0]));
        Assert.Throws<ArgumentException>(() => editor.MoveSection(editor.Sections[0], 1));
    }

    [Fact]
    public async Task Blank_lines_and_steps_are_dropped()
    {
        var app = CreateApp();
        var editor = Focaccia(app);
        editor.AddLine(editor.Sections[0]);
        editor.AddStep("   ");

        var recipe = await app.Recipes.SaveAsync(editor);

        Assert.Equal(3, recipe.Lines.Count);
        Assert.Equal(2, recipe.Steps.Count);
    }

    [Theory]
    [InlineData("a handful")]
    [InlineData("2 cups")]
    public async Task A_quantity_that_cannot_be_read_is_not_saved(string quantity)
    {
        var app = CreateApp();
        var editor = Focaccia(app);
        editor.Sections[0].Lines[0].QuantityText = quantity;

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));

        Assert.StartsWith($"\"{quantity}\" isn't a quantity.", ex.Message);
        Assert.Empty(await app.Recipes.ListRecipesAsync());
        Assert.Empty(await app.Ingredients.ListIngredientsAsync());
    }

    [Fact]
    public async Task Every_line_needs_an_ingredient()
    {
        var app = CreateApp();
        var editor = Focaccia(app);
        editor.AddLine(editor.Sections[0]).QuantityText = "2";

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));

        Assert.Equal("Pick an Ingredient for every Ingredient Line.", ex.Message);
    }

    [Fact]
    public async Task Every_section_needs_a_heading()
    {
        var app = CreateApp();
        var editor = Focaccia(app);
        editor.AddSection("  ");

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));

        Assert.Equal("Give every Ingredient Section a heading.", ex.Message);
    }

    [Fact]
    public async Task An_edited_recipe_still_needs_a_title()
    {
        var app = CreateApp();
        var saved = await app.Recipes.SaveAsync(Focaccia(app));
        var editor = (await app.Recipes.EditRecipeAsync(saved.Id))!;
        editor.Title = " ";

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));

        Assert.Equal("Give the Recipe a title.", ex.Message);
        Assert.Equal("Focaccia", (await app.Recipes.GetRecipeAsync(saved.Id))!.Recipe.Title);
    }

    [Fact]
    public async Task Yield_and_times_are_optional()
    {
        var app = CreateApp();
        var recipe = await app.Recipes.AddRecipeAsync("Toast");

        Assert.Null(recipe.Yield);
        Assert.True(recipe.StatedTimes.IsEmpty);
        Assert.Empty(recipe.Lines);
    }

    [Fact]
    public async Task A_yield_needs_more_than_zero()
    {
        var app = CreateApp();
        var editor = Focaccia(app);
        editor.YieldAmount = 0;

        var ex = await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.SaveAsync(editor));

        Assert.Equal("The Yield needs to be more than zero.", ex.Message);
    }

    [Theory]
    [InlineData(45, "45 min")]
    [InlineData(60, "1 hr")]
    [InlineData(75, "1 hr 15 min")]
    public void Times_are_shown_in_hours_and_minutes(int minutes, string expected)
    {
        Assert.Equal(expected, StatedTimes.Format(minutes));
    }

    [Fact]
    public async Task Deleting_a_recipe_leaves_a_tombstone()
    {
        var app = CreateApp();
        var saved = await app.Recipes.SaveAsync(Focaccia(app));
        _clock.UtcNow += TimeSpan.FromMinutes(5);

        await app.Recipes.DeleteRecipeAsync(saved.Id);

        Assert.Empty(await CreateApp().Recipes.ListRecipesAsync());
        Assert.Null(await app.Recipes.GetRecipeAsync(saved.Id));
        Assert.Null(await app.Recipes.EditRecipeAsync(saved.Id));
        var tombstone = await _store.LoadAsync<Recipe>("recipes", saved.Id);
        Assert.True(tombstone!.Deleted);
        Assert.Equal(_clock.UtcNow, tombstone.UpdatedAt);
    }

    [Fact]
    public async Task There_is_no_recipe_page_for_an_unknown_id()
    {
        Assert.Null(await CreateApp().Recipes.GetRecipeAsync(Guid.NewGuid().ToString()));
    }
}
