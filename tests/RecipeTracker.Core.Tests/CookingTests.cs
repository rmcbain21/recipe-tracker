using RecipeTracker.Core.Cooking;
using RecipeTracker.Core.Parsing;
using RecipeTracker.Core.Quantities;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class CookingTests
{
    private const string Pasted = """
        Brownies

        Ingredients
        1 cup sugar
        2 eggs

        Method
        1. Beat the sugar with the eggs.
        2. Bake 25 minutes.
        """;

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<Recipe> RecipeAsync(AppCore app)
    {
        var editor = await app.Recipes.CaptureAsync(Pasted);
        editor.ConfirmAllMatches();
        return await app.Recipes.SaveAsync(editor);
    }

    [Fact]
    public async Task Starting_cooking_opens_gather_at_the_scale_given()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);

        var session = await app.Cooking.StartAsync(recipe.Id, Scale.By(1.5m));

        Assert.True(session.OnGather);
        Assert.Null(session.StepIndex);
        Assert.Equal(1.5m, session.ScaleFactor);
        Assert.Empty(session.GatherTicks);
    }

    [Fact]
    public async Task Drafts_cannot_start_cooking_mode()
    {
        var app = CreateApp();
        var draft = await app.Recipes.AddRecipeAsync("Maybe later");

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Cooking.StartAsync(draft.Id, Scale.One));
    }

    [Fact]
    public async Task Next_and_back_move_between_gather_and_the_steps_and_stop_at_the_ends()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);

        Assert.Equal(0, (await app.Cooking.BackAsync(recipe.Id)).Position);
        Assert.Equal(0, (await app.Cooking.NextAsync(recipe.Id)).StepIndex);
        Assert.Equal(1, (await app.Cooking.NextAsync(recipe.Id)).StepIndex);
        Assert.Equal(1, (await app.Cooking.NextAsync(recipe.Id)).StepIndex); // the last Step
        Assert.Equal(0, (await app.Cooking.BackAsync(recipe.Id)).StepIndex);
        Assert.True((await app.Cooking.BackAsync(recipe.Id)).OnGather);
    }

    [Fact]
    public async Task Leaving_and_coming_back_returns_to_the_same_place()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.By(2));
        await app.Cooking.TickAsync(recipe.Id, recipe.Lines[0].Id, ticked: true);
        await app.Cooking.TickAsync(recipe.Id, recipe.Lines[1].Id, ticked: true);
        await app.Cooking.TickAsync(recipe.Id, recipe.Lines[1].Id, ticked: false);
        await app.Cooking.NextAsync(recipe.Id);
        await app.Cooking.NextAsync(recipe.Id);

        var resumed = await CreateApp().Cooking.GetSessionAsync(recipe.Id);

        Assert.Equal(1, resumed!.StepIndex);
        Assert.Equal(2m, resumed.ScaleFactor);
        Assert.Equal([recipe.Lines[0].Id], resumed.GatherTicks);
    }

    [Fact]
    public async Task Start_over_replaces_the_session()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.By(2));
        await app.Cooking.TickAsync(recipe.Id, recipe.Lines[0].Id, ticked: true);
        await app.Cooking.NextAsync(recipe.Id);

        var fresh = await app.Cooking.StartAsync(recipe.Id, Scale.One);

        Assert.True(fresh.OnGather);
        Assert.Empty(fresh.GatherTicks);
        Assert.Equal(1m, (await app.Cooking.GetSessionAsync(recipe.Id))!.ScaleFactor);
    }

    [Fact]
    public async Task The_scale_can_change_while_cooking()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);

        await app.Cooking.SetScaleAsync(recipe.Id, Scale.By(3));

        Assert.Equal(3m, (await app.Cooking.GetSessionAsync(recipe.Id))!.ScaleFactor);
    }

    [Fact]
    public async Task Ending_a_session_means_there_is_nothing_to_resume()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);

        await app.Cooking.EndAsync(recipe.Id);

        Assert.Null(await app.Cooking.GetSessionAsync(recipe.Id));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Cooking.NextAsync(recipe.Id));
    }

    [Fact]
    public async Task A_session_for_a_deleted_recipe_is_not_offered()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);

        await app.Recipes.DeleteRecipeAsync(recipe.Id);

        Assert.Null(await app.Cooking.GetSessionAsync(recipe.Id));
    }

    [Fact]
    public async Task Cooking_never_changes_the_recipe_so_nothing_syncs()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        _clock.UtcNow += TimeSpan.FromHours(1);

        await app.Cooking.StartAsync(recipe.Id, Scale.By(2));
        await app.Cooking.NextAsync(recipe.Id);

        Assert.Equal(recipe.UpdatedAt, (await app.Recipes.GetRecipeAsync(recipe.Id))!.Recipe.UpdatedAt);
        Assert.Single(await _store.ListAsync<CookingSession>("cookingSessions"));
    }

    private static StepDuration TimeIn(Recipe recipe, int step) => Assert.Single(StepDurations.Find(recipe.Steps[step].Text));

    [Fact]
    public async Task A_timer_keeps_the_time_it_ends_so_it_survives_a_reload()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);

        await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[1].Id, TimeIn(recipe, 1));
        _clock.UtcNow += TimeSpan.FromMinutes(10);

        var timer = Assert.Single((await CreateApp().Cooking.GetSessionAsync(recipe.Id))!.Timers);
        Assert.Equal("Step 2 · 25 min", timer.Label);
        Assert.Equal(TimeSpan.FromMinutes(15), timer.Remaining(_clock.UtcNow));
        Assert.Equal("15:00", timer.Countdown(_clock.UtcNow));
        Assert.False(timer.IsFinished(_clock.UtcNow));
    }

    [Fact]
    public async Task A_timer_that_ended_while_the_app_was_closed_shows_as_finished()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);
        await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[1].Id, TimeIn(recipe, 1));

        _clock.UtcNow += TimeSpan.FromHours(1);

        var timer = Assert.Single((await CreateApp().Cooking.GetSessionAsync(recipe.Id))!.Timers);
        Assert.True(timer.IsFinished(_clock.UtcNow));
        Assert.Equal("0:00", timer.Countdown(_clock.UtcNow));
    }

    [Fact]
    public async Task A_range_starts_at_its_low_end()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);

        var session = await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[0].Id, StepDurations.Find("Bake 25-30 min")[0]);

        Assert.Equal(_clock.UtcNow + TimeSpan.FromMinutes(25), Assert.Single(session.Timers).EndsAt);
    }

    [Fact]
    public async Task Several_timers_run_at_once_but_one_time_starts_only_one()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);
        var bake = TimeIn(recipe, 1);

        await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[1].Id, bake);
        await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[1].Id, bake);
        var session = await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[0].Id, StepDurations.Find("Beat 3 minutes")[0]);

        Assert.Equal(["Step 2 · 25 min", "Step 1 · 3 min"], session.Timers.Select(t => t.Label));
    }

    [Fact]
    public async Task Plus_five_adds_to_a_running_timer_and_restarts_a_finished_one()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);
        var timer = (await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[1].Id, TimeIn(recipe, 1))).Timers[0];
        var five = TimeSpan.FromMinutes(5);

        var running = (await app.Cooking.AddTimeAsync(recipe.Id, timer.Id, five)).Timers[0];
        Assert.Equal(timer.EndsAt + five, running.EndsAt);

        _clock.UtcNow += TimeSpan.FromHours(1);
        var restarted = (await app.Cooking.AddTimeAsync(recipe.Id, timer.Id, five)).Timers[0];
        Assert.Equal(_clock.UtcNow + five, restarted.EndsAt);
    }

    [Fact]
    public async Task Cancelling_or_dismissing_removes_a_timer_and_start_over_clears_them()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);
        var timer = (await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[1].Id, TimeIn(recipe, 1))).Timers[0];

        Assert.Empty((await app.Cooking.RemoveTimerAsync(recipe.Id, timer.Id)).Timers);

        await app.Cooking.StartTimerAsync(recipe.Id, recipe.Steps[1].Id, TimeIn(recipe, 1));
        Assert.Empty((await app.Cooking.StartAsync(recipe.Id, Scale.One)).Timers);
    }

    [Fact]
    public async Task A_timer_needs_a_step_still_in_the_recipe()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);

        await Assert.ThrowsAsync<RuleViolationException>(() =>
            app.Cooking.StartTimerAsync(recipe.Id, "gone", TimeIn(recipe, 1)));
    }

    [Fact]
    public async Task A_step_shows_its_own_notes_then_notes_on_the_lines_it_uses()
    {
        var app = CreateApp();
        var recipe = await RecipeAsync(app);
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Lines[0].Id, "Used half");
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Steps[0].Id, "Beat for 3 minutes");
        await app.Recipes.AddNoteAsync(recipe.Id, recipe.Lines[1].Id, "Room temperature");
        await app.Recipes.AddNoteAsync(recipe.Id, null, "Not shown on a Step");

        var view = (await app.Recipes.GetRecipeAsync(recipe.Id))!;
        var notes = view.NotesForStep(view.Recipe.Steps[0]);

        Assert.Equal(
            [(null, "Beat for 3 minutes"), ("sugar", "Used half"), ("eggs", "Room temperature")],
            notes.Select(n => (n.About, n.Note.Text)));
        Assert.Empty(view.NotesForStep(view.Recipe.Steps[1]));
    }
}
