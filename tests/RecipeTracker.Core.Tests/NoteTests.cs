using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class NoteTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<Recipe> SaveRecipeAsync(AppCore app)
    {
        var editor = app.Recipes.NewRecipe();
        editor.Title = "Brownies";
        var line = editor.AddLine(editor.Sections[0]);
        line.QuantityText = "1";
        line.Unit = "cup";
        line.Ingredient = IngredientChoice.New("sugar");
        editor.AddStep("Mix the sugar and butter.");
        editor.AddStep("Bake 25 minutes.");
        return await app.Recipes.SaveAsync(editor);
    }

    private static async Task<RecipeView> ViewAsync(AppCore app, Recipe recipe) => (await app.Recipes.GetRecipeAsync(recipe.Id))!;

    [Fact]
    public async Task Notes_are_added_to_the_recipe_a_line_and_a_step()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);

        await app.Recipes.AddNoteAsync(recipe.Id, null, "  Kids loved it  ");
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Lines[0].Id, "Used half the sugar, still sweet enough");
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Steps[1].Id, "22 minutes in my oven");

        var view = await ViewAsync(CreateApp(), recipe);
        Assert.Equal(["Kids loved it"], view.RecipeNotes.Select(n => n.Text));
        Assert.Equal(["Used half the sugar, still sweet enough"], view.NotesFor(recipe.Lines[0].Id).Select(n => n.Text));
        Assert.Equal(["22 minutes in my oven"], view.NotesFor(recipe.Steps[1].Id).Select(n => n.Text));
        Assert.Empty(view.NotesFor(recipe.Steps[0].Id));
    }

    [Fact]
    public async Task A_note_does_not_change_the_recipe_itself()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);
        _clock.UtcNow += TimeSpan.FromMinutes(5);

        var note = await app.Recipes.AddNoteAsync(recipe.Id, recipe.Steps[0].Id, "Brown the butter first");

        var stored = (await ViewAsync(app, recipe)).Recipe;
        Assert.Equal(_clock.UtcNow, note.CreatedAt);
        Assert.Equal(_clock.UtcNow, stored.UpdatedAt); // so that the Note syncs
        Assert.Equal(recipe.Title, stored.Title);
        Assert.Equal(recipe.Lines, stored.Lines);
        Assert.Equal(recipe.Steps, stored.Steps);
        Assert.Equal(recipe.CreatedAt, stored.CreatedAt);
        Assert.False(stored.IsDraft);
    }

    [Fact]
    public async Task Notes_show_newest_first()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);
        foreach (var text in new[] { "First", "Second", "Third" })
        {
            await app.Recipes.AddNoteAsync(recipe.Id, null, text);
            _clock.UtcNow += TimeSpan.FromDays(1);
        }

        Assert.Equal(["Third", "Second", "First"], (await ViewAsync(app, recipe)).RecipeNotes.Select(n => n.Text));
    }

    [Fact]
    public async Task A_note_is_edited_keeping_its_date_and_place()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);
        var first = await app.Recipes.AddNoteAsync(recipe.Id, null, "Fist");
        await app.Recipes.AddNoteAsync(recipe.Id, null, "Second");
        _clock.UtcNow += TimeSpan.FromDays(1);

        var edited = await app.Recipes.EditNoteAsync(recipe.Id, first.Id, "First");

        Assert.Equal(first.CreatedAt, edited.CreatedAt);
        Assert.Equal(["Second", "First"], (await ViewAsync(app, recipe)).RecipeNotes.Select(n => n.Text));
    }

    [Fact]
    public async Task A_note_is_deleted()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);
        var note = await app.Recipes.AddNoteAsync(recipe.Id, null, "Too salty");

        await app.Recipes.DeleteNoteAsync(recipe.Id, note.Id);

        Assert.Empty((await ViewAsync(app, recipe)).Recipe.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_note_is_refused(string text)
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);
        var note = await app.Recipes.AddNoteAsync(recipe.Id, null, "Good");

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.AddNoteAsync(recipe.Id, null, text));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.EditNoteAsync(recipe.Id, note.Id, text));
    }

    [Fact]
    public async Task A_note_needs_a_line_or_step_that_is_in_the_recipe()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Recipes.AddNoteAsync(recipe.Id, "no-such-line", "Hmm"));
    }

    [Fact]
    public async Task Removing_a_line_or_step_in_the_editor_deletes_its_notes_and_keeps_the_rest()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);
        await app.Recipes.AddNoteAsync(recipe.Id, null, "On the Recipe");
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Lines[0].Id, "On the sugar");
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Steps[0].Id, "On step 1");
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Steps[1].Id, "On step 2");

        var editor = (await app.Recipes.EditRecipeAsync(recipe.Id))!;
        editor.RemoveLine(editor.Sections[0].Lines[0]);
        editor.RemoveStep(editor.Steps[0]);
        var saved = await app.Recipes.SaveAsync(editor);

        Assert.Equal(["On the Recipe", "On step 2"], saved.Notes.Select(n => n.Text));
    }

    [Fact]
    public async Task A_note_added_while_the_editor_was_open_is_kept_when_it_saves()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app);
        var editor = (await app.Recipes.EditRecipeAsync(recipe.Id))!;

        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Steps[0].Id, "Added meanwhile");
        editor.Title = "Fudgy brownies";
        var saved = await app.Recipes.SaveAsync(editor);

        Assert.Equal(["Added meanwhile"], saved.Notes.Select(n => n.Text));
    }

    [Fact]
    public async Task Drafts_can_have_notes()
    {
        var app = CreateApp();
        var draft = await app.Recipes.AddRecipeAsync("Try this");

        await app.Recipes.AddNoteAsync(draft.Id, null, "From Sam's party");

        Assert.Single((await ViewAsync(app, draft)).RecipeNotes);
    }
}
