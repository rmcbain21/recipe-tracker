using RecipeTracker.Core.Organizing;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class TagTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<Tag> TagAsync(AppCore app, string name) => (await app.Tags.FindTagByNameAsync(name))!;

    private static async Task<Recipe> SaveRecipeAsync(AppCore app, string title, params Tag[] tags)
    {
        var editor = app.Recipes.NewRecipe();
        editor.Title = title;
        editor.AddStep("Cook it.");
        editor.TagIds.AddRange(tags.Select(t => t.Id));
        return await app.Recipes.SaveAsync(editor);
    }

    private static async Task<string[]> TagNamesOnAsync(AppCore app, Recipe recipe) =>
        (await app.Recipes.GetRecipeAsync(recipe.Id))!.Tags.SelectMany(g => g.Tags).Select(t => t.Name).ToArray();

    private static async Task<string[]> GroupNamesAsync(AppCore app) =>
        (await app.Tags.ListTagGroupsAsync()).Select(g => g.Name).ToArray();

    // Starters

    [Fact]
    public async Task First_run_creates_the_starter_groups_and_tags()
    {
        var grouped = await CreateApp().Tags.ListGroupedTagsAsync();

        Assert.Equal(["Meal", "Cuisine", "Equipment", "Diet", "Occasion", null], grouped.Select(g => g.Group?.Name));
        Assert.Equal(["breakfast", "dessert", "dinner", "lunch", "side", "snack"], grouped[0].Tags.Select(t => t.Name));
        Assert.Empty(grouped[1].Tags);
        Assert.Contains(grouped[2].Tags, t => t.Name == "instant pot");
        Assert.Equal(["dairy-free", "gluten-free", "vegan", "vegetarian"], grouped[3].Tags.Select(t => t.Name));
        Assert.Equal(["holiday", "meal prep", "want to try", "weeknight"], grouped[4].Tags.Select(t => t.Name));
        Assert.Empty(grouped[5].Tags);
    }

    [Fact]
    public async Task Starters_have_fixed_ids_and_the_epoch_so_two_devices_create_the_same_records()
    {
        var phone = new InMemoryRecordStore();
        var tablet = new InMemoryRecordStore();
        var phoneTags = await new AppCore(phone, _clock).Tags.ListTagsAsync();
        _clock.UtcNow += TimeSpan.FromDays(3);
        var tabletTags = await new AppCore(tablet, _clock).Tags.ListTagsAsync();

        Assert.Equal(phoneTags, tabletTags);
        Assert.All(phoneTags, tag => Assert.Equal(DateTimeOffset.UnixEpoch, tag.UpdatedAt));
        Assert.Equal(await new AppCore(phone, _clock).Tags.ListTagGroupsAsync(), await new AppCore(tablet, _clock).Tags.ListTagGroupsAsync());
    }

    [Fact]
    public async Task Starters_are_not_recreated_once_deleted()
    {
        var app = CreateApp();
        foreach (var group in await app.Tags.ListTagGroupsAsync())
        {
            await app.Tags.DeleteTagGroupAsync(group.Id);
        }

        foreach (var tag in await app.Tags.ListTagsAsync())
        {
            await app.Tags.DeleteTagAsync(tag.Id);
        }

        Assert.Empty(await CreateApp().Tags.ListTagGroupsAsync());
        Assert.Empty(await CreateApp().Tags.ListTagsAsync());
    }

    // Names

    [Fact]
    public async Task Tag_names_are_tidied_and_unique_ignoring_case()
    {
        var app = CreateApp();

        var tag = await app.Tags.AddTagAsync("  one   pot ");

        Assert.Equal("one pot", tag.Name);
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Tags.AddTagAsync("One Pot"));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Tags.AddTagAsync("DINNER"));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Tags.AddTagAsync("   "));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Tags.RenameTagAsync(tag.Id, "Lunch"));
        Assert.Equal(tag.Id, (await app.Tags.FindTagByNameAsync("ONE POT"))?.Id);
    }

    [Fact]
    public async Task Tag_group_names_are_unique_ignoring_case()
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Tags.AddTagGroupAsync(" meal "));
        var group = await app.Tags.AddTagGroupAsync("Season");
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Tags.RenameTagGroupAsync(group.Id, "diet"));
    }

    // Tags on Recipes

    [Fact]
    public async Task Renaming_a_tag_shows_on_every_recipe()
    {
        var app = CreateApp();
        var crockpot = await TagAsync(app, "crockpot");
        var recipe = await SaveRecipeAsync(app, "Chili", crockpot);

        await app.Tags.RenameTagAsync(crockpot.Id, "slow cooker");

        Assert.Equal(["slow cooker"], await TagNamesOnAsync(app, recipe));
    }

    [Fact]
    public async Task The_recipe_page_groups_tags_in_group_order_with_ungrouped_last()
    {
        var app = CreateApp();
        var spicy = await app.Tags.AddTagAsync("spicy");
        var recipe = await SaveRecipeAsync(app, "Chili",
            spicy, await TagAsync(app, "weeknight"), await TagAsync(app, "dinner"), await TagAsync(app, "crockpot"), await TagAsync(app, "lunch"));

        var groups = (await app.Recipes.GetRecipeAsync(recipe.Id))!.Tags;

        Assert.Equal(["Meal", "Equipment", "Occasion", null], groups.Select(g => g.Group?.Name));
        Assert.Equal(["dinner", "lunch"], groups[0].Tags.Select(t => t.Name));
        Assert.Equal(["spicy"], groups[3].Tags.Select(t => t.Name));
    }

    [Fact]
    public async Task Tags_are_set_from_the_recipe_page()
    {
        var app = CreateApp();
        var recipe = await SaveRecipeAsync(app, "Chili");
        var dinner = await TagAsync(app, "dinner");
        _clock.UtcNow += TimeSpan.FromMinutes(1);

        await app.Recipes.SetTagsAsync(recipe.Id, [dinner.Id, dinner.Id, "deleted-or-unknown"]);

        var stored = (await app.Recipes.GetRecipeAsync(recipe.Id))!.Recipe;
        Assert.Equal([dinner.Id], stored.TagIds);
        Assert.Equal(_clock.UtcNow, stored.UpdatedAt);
    }

    [Fact]
    public async Task The_editor_starts_from_the_recipes_tags_and_saves_changes()
    {
        var app = CreateApp();
        var dinner = await TagAsync(app, "dinner");
        var recipe = await SaveRecipeAsync(app, "Chili", dinner);

        var editor = (await app.Recipes.EditRecipeAsync(recipe.Id))!;
        Assert.Equal([dinner.Id], editor.TagIds);
        editor.TagIds.Clear();
        editor.TagIds.Add((await TagAsync(app, "lunch")).Id);
        await app.Recipes.SaveAsync(editor);

        Assert.Equal(["lunch"], await TagNamesOnAsync(app, recipe));
    }

    [Fact]
    public async Task Merging_a_tag_repoints_recipes_and_tombstones_it()
    {
        var app = CreateApp();
        var crockpot = await TagAsync(app, "crockpot");
        var slowCooker = await app.Tags.AddTagAsync("slow cooker");
        var chili = await SaveRecipeAsync(app, "Chili", crockpot);
        var stew = await SaveRecipeAsync(app, "Stew", crockpot, slowCooker);
        await SaveRecipeAsync(app, "Salad");

        var changed = await app.Tags.MergeTagAsync(crockpot.Id, slowCooker.Id);

        Assert.Equal(2, changed);
        Assert.Equal(["slow cooker"], await TagNamesOnAsync(app, chili));
        Assert.Equal([slowCooker.Id], (await app.Recipes.GetRecipeAsync(stew.Id))!.Recipe.TagIds);
        var tombstone = await _store.LoadAsync<Tag>("tags", crockpot.Id);
        Assert.True(tombstone!.Deleted);
        Assert.Equal(slowCooker.Id, tombstone.MergedInto);
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Tags.MergeTagAsync(slowCooker.Id, slowCooker.Id));
    }

    [Fact]
    public async Task Deleting_a_tag_takes_it_off_every_recipe()
    {
        var app = CreateApp();
        var dinner = await TagAsync(app, "dinner");
        var weeknight = await TagAsync(app, "weeknight");
        var recipe = await SaveRecipeAsync(app, "Chili", dinner, weeknight);

        Assert.Equal(1, await app.Tags.DeleteTagAsync(dinner.Id));

        Assert.Equal([weeknight.Id], (await app.Recipes.GetRecipeAsync(recipe.Id))!.Recipe.TagIds);
        Assert.Null(await app.Tags.FindTagByNameAsync("dinner"));
    }

    [Fact]
    public async Task Tag_uses_count_recipes_including_drafts()
    {
        var app = CreateApp();
        var dinner = await TagAsync(app, "dinner");
        await SaveRecipeAsync(app, "Chili", dinner);
        var draft = await app.Recipes.AddRecipeAsync("Maybe");
        await app.Recipes.SetTagsAsync(draft.Id, [dinner.Id]);

        var uses = (await app.Tags.ListTagUsesAsync()).SelectMany(g => g.Tags).ToDictionary(u => u.Tag.Name, u => u.RecipeCount);

        Assert.Equal(2, uses["dinner"]);
        Assert.Equal(0, uses["lunch"]);
    }

    // Tag Groups

    [Fact]
    public async Task Tag_groups_are_added_at_the_end_and_reordered()
    {
        var app = CreateApp();
        var season = await app.Tags.AddTagGroupAsync("Season");

        await app.Tags.MoveTagGroupAsync(season.Id, -1);
        await app.Tags.MoveTagGroupAsync((await app.Tags.ListTagGroupsAsync())[0].Id, -1); // already first

        Assert.Equal(["Meal", "Cuisine", "Equipment", "Diet", "Season", "Occasion"], await GroupNamesAsync(app));
    }

    [Fact]
    public async Task Moving_a_tag_to_another_group_or_none()
    {
        var app = CreateApp();
        var groups = await app.Tags.ListTagGroupsAsync();
        var snack = await TagAsync(app, "snack");

        await app.Tags.MoveTagToGroupAsync(snack.Id, groups.Single(g => g.Name == "Occasion").Id);
        Assert.Contains((await app.Tags.ListGroupedTagsAsync())[4].Tags, t => t.Name == "snack");

        await app.Tags.MoveTagToGroupAsync(snack.Id, null);
        Assert.Equal(["snack"], (await app.Tags.ListGroupedTagsAsync())[^1].Tags.Select(t => t.Name));
    }

    [Fact]
    public async Task Deleting_a_tag_group_leaves_its_tags_ungrouped()
    {
        var app = CreateApp();
        var diet = (await app.Tags.ListTagGroupsAsync()).Single(g => g.Name == "Diet");

        await app.Tags.DeleteTagGroupAsync(diet.Id);

        var grouped = await app.Tags.ListGroupedTagsAsync();
        Assert.DoesNotContain(grouped, g => g.Group?.Name == "Diet");
        Assert.Equal(["dairy-free", "gluten-free", "vegan", "vegetarian"], grouped[^1].Tags.Select(t => t.Name));
    }
}
