using RecipeTracker.Core.Organizing;
using RecipeTracker.Core.Quantities;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class RecipeFinderTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 1, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private async Task<Recipe> AddAsync(AppCore app, string title, string[]? tags = null, string ingredients = "1 onion",
        int? totalMinutes = null, Action<EditedSource>? source = null)
    {
        _clock.UtcNow += TimeSpan.FromMinutes(1);
        var editor = await app.Recipes.CaptureAsync($"{title}\n\nIngredients\n{ingredients}\n\nMethod\n1. Cook it.");
        editor.ConfirmAllMatches();
        editor.TotalMinutes = totalMinutes;
        source?.Invoke(editor.Source);
        var recipe = await app.Recipes.SaveAsync(editor);
        if (tags is not null)
        {
            var ids = new List<string>();
            foreach (var name in tags)
            {
                ids.Add(((await app.Tags.FindTagByNameAsync(name)) ?? await app.Tags.AddTagAsync(name)).Id);
            }

            await app.Recipes.SetTagsAsync(recipe.Id, ids);
        }

        return recipe;
    }

    private static async Task<string> TagIdAsync(AppCore app, string name) => (await app.Tags.FindTagByNameAsync(name))!.Id;

    private static async Task<List<string>> TitlesAsync(AppCore app, RecipeQuery query) =>
        (await app.Finder.FindAsync(query)).Rows.Select(row => row.Title).ToList();

    [Fact]
    public async Task Tags_are_OR_within_a_group_and_AND_across_groups()
    {
        var app = CreateApp();
        await AddAsync(app, "Chili", ["dinner", "crockpot"], totalMinutes: 50);
        await AddAsync(app, "Curry", ["dinner", "instant pot"], totalMinutes: 40);
        await AddAsync(app, "Oats", ["breakfast", "crockpot"], totalMinutes: 30);
        await AddAsync(app, "Roast", ["dinner", "sheet pan"], totalMinutes: 45);
        await AddAsync(app, "Stew", ["dinner", "crockpot"], totalMinutes: 240);

        // "crockpot or instant pot, dinner, under an hour"
        var query = new RecipeQuery
        {
            TagIds = [await TagIdAsync(app, "crockpot"), await TagIdAsync(app, "instant pot"), await TagIdAsync(app, "dinner")],
            MaxTotalMinutes = 60,
        };

        Assert.Equal(["Chili", "Curry"], await TitlesAsync(app, query));
    }

    [Fact]
    public async Task Each_ungrouped_tag_is_needed()
    {
        var app = CreateApp();
        await AddAsync(app, "Lasagne", ["family", "freezer"]);
        await AddAsync(app, "Soup", ["family"]);

        var query = new RecipeQuery { TagIds = [await TagIdAsync(app, "family"), await TagIdAsync(app, "freezer")] };

        Assert.Equal(["Lasagne"], await TitlesAsync(app, query));
    }

    [Fact]
    public async Task Search_matches_titles_ingredient_names_aliases_and_notes_ignoring_case()
    {
        var app = CreateApp();
        await AddAsync(app, "Fried rice", ingredients: "2 green onions");
        var salad = await AddAsync(app, "Salad", ingredients: "1 lettuce");
        await AddAsync(app, "Toast", ingredients: "1 bread");
        await app.Recipes.AddNoteAsync(salad.Id, null, "Great with SCALLIONS on top");
        var greenOnion = (await app.Ingredients.ListIngredientsAsync()).Single(i => i.Name.Contains("green onion"));
        await _store.SaveAsync("ingredients", greenOnion with { Aliases = ["spring onion"] });

        Assert.Equal(["Fried rice"], await TitlesAsync(app, new RecipeQuery { Search = "GREEN onion" }));
        Assert.Equal(["Fried rice"], await TitlesAsync(app, new RecipeQuery { Search = "spring onion" }));
        Assert.Equal(["Salad"], await TitlesAsync(app, new RecipeQuery { Search = "scallion" }));
        Assert.Equal(["Toast"], await TitlesAsync(app, new RecipeQuery { Search = " toa " }));
    }

    [Fact]
    public async Task Filters_by_source_kind_book_and_cooked()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Jerusalem");
        var other = await app.Books.AddBookAsync("Plenty");
        var shakshuka = await AddAsync(app, "Shakshuka", source: s => { s.Kind = SourceKind.Book; s.BookId = book.Id; });
        await AddAsync(app, "Salad", source: s => { s.Kind = SourceKind.Book; s.BookId = other.Id; });
        await AddAsync(app, "Tacos", source: s => { s.Kind = SourceKind.Website; s.Url = "https://example.com/tacos"; });
        await AddAsync(app, "Toast");
        await app.Cooking.StartAsync(shakshuka.Id, Scale.One);
        await app.Cooking.FinishAsync(shakshuka.Id);

        Assert.Equal(["Salad", "Shakshuka"], await TitlesAsync(app, new RecipeQuery { SourceKind = SourceKind.Book }));
        Assert.Equal(["Shakshuka"], await TitlesAsync(app, new RecipeQuery { BookId = book.Id }));
        Assert.Equal(["Tacos"], await TitlesAsync(app, new RecipeQuery { SourceKind = SourceKind.Website }));
        Assert.Equal(["Shakshuka"], await TitlesAsync(app, new RecipeQuery { Cooked = CookedFilter.Cooked }));
        Assert.Equal(["Salad", "Tacos", "Toast"], await TitlesAsync(app, new RecipeQuery { Cooked = CookedFilter.NeverCooked }));
    }

    [Fact]
    public async Task Time_filter_uses_total_or_prep_plus_cook_and_leaves_out_recipes_with_no_time()
    {
        var app = CreateApp();
        await AddAsync(app, "Quick", totalMinutes: 25);
        await AddAsync(app, "Unknown");
        var editor = await app.Recipes.CaptureAsync("Split\n\nIngredients\n1 egg\n\nMethod\n1. Cook.");
        editor.ConfirmAllMatches();
        editor.PrepMinutes = 10;
        editor.CookMinutes = 15;
        await app.Recipes.SaveAsync(editor);

        Assert.Equal(["Quick", "Split"], await TitlesAsync(app, new RecipeQuery { MaxTotalMinutes = 30 }));
    }

    [Fact]
    public async Task Sorts_by_title_recently_added_or_recently_cooked_with_drafts_pinned()
    {
        var app = CreateApp();
        var b = await AddAsync(app, "Banana bread");
        var a = await AddAsync(app, "Apple pie");
        await AddAsync(app, "Carrot cake");
        await app.Recipes.AddRecipeAsync("Draft idea");
        foreach (var (recipe, day) in new[] { (b, 3), (a, 5) })
        {
            _clock.UtcNow = new DateTimeOffset(2026, 11, day, 18, 0, 0, TimeSpan.Zero);
            await app.Cooking.StartAsync(recipe.Id, Scale.One);
            await app.Cooking.FinishAsync(recipe.Id);
        }

        Assert.Equal(["Draft idea", "Apple pie", "Banana bread", "Carrot cake"], await TitlesAsync(app, new RecipeQuery()));
        Assert.Equal(["Draft idea", "Carrot cake", "Apple pie", "Banana bread"], await TitlesAsync(app, new RecipeQuery { Sort = RecipeSort.RecentlyAdded }));
        Assert.Equal(["Draft idea", "Apple pie", "Banana bread", "Carrot cake"], await TitlesAsync(app, new RecipeQuery { Sort = RecipeSort.RecentlyCooked }));
    }

    [Fact]
    public async Task Rows_show_source_total_time_tags_and_last_cooked()
    {
        var app = CreateApp();
        var recipe = await AddAsync(app, "Tacos", ["dinner", "weeknight"], totalMinutes: 75,
            source: s => { s.Kind = SourceKind.Website; s.Url = "https://www.example.com/tacos"; });
        _clock.UtcNow = new DateTimeOffset(2026, 11, 4, 18, 0, 0, TimeSpan.Zero);
        await app.Cooking.StartAsync(recipe.Id, Scale.One);
        await app.Cooking.FinishAsync(recipe.Id);

        var row = Assert.Single((await app.Finder.FindAsync(RecipeQuery.Everything)).Rows);

        Assert.Equal("example.com", row.SourceName);
        Assert.Equal("1 hr 15 min", row.TotalTimeText);
        Assert.Equal(["dinner", "weeknight"], row.TagNames);
        Assert.Equal(new DateOnly(2026, 11, 4), row.LastCooked);
    }

    [Fact]
    public async Task Surprise_me_picks_a_listed_recipe_that_is_not_a_draft()
    {
        var app = CreateApp();
        await AddAsync(app, "Chili", ["dinner"]);
        await AddAsync(app, "Oats", ["breakfast"]);
        var draft = await app.Recipes.AddRecipeAsync("Draft dinner");
        await app.Recipes.SetTagsAsync(draft.Id, [await TagIdAsync(app, "dinner")]);

        var dinners = await app.Finder.FindAsync(new RecipeQuery { TagIds = [await TagIdAsync(app, "dinner")] });
        Assert.Equal(["Draft dinner", "Chili"], dinners.Rows.Select(r => r.Title));
        for (var seed = 0; seed < 20; seed++)
        {
            Assert.Equal("Chili", dinners.Surprise(new Random(seed))!.Title);
        }

        var draftsOnly = await app.Finder.FindAsync(new RecipeQuery { Search = "draft" });
        Assert.False(draftsOnly.CanSurprise);
        Assert.Null(draftsOnly.Surprise(new Random(1)));
        Assert.False((await app.Finder.FindAsync(new RecipeQuery { Search = "nothing like this" })).CanSurprise);
    }

    [Fact]
    public async Task Active_filters_describe_themselves_and_remove_one_at_a_time()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Jerusalem");
        await AddAsync(app, "Shakshuka", ["dinner"], source: s => { s.Kind = SourceKind.Book; s.BookId = book.Id; });
        var query = new RecipeQuery
        {
            Search = "egg",
            TagIds = [await TagIdAsync(app, "dinner")],
            BookId = book.Id,
            MaxTotalMinutes = 60,
            Cooked = CookedFilter.NeverCooked,
            Sort = RecipeSort.RecentlyAdded,
        };

        var active = await app.Finder.DescribeAsync(query);

        Assert.Equal(["\"egg\"", "dinner", "Jerusalem", "≤ 1 hr", "Never cooked"], active.Select(f => f.Label));
        Assert.Empty(active[1].Without.TagIds);
        Assert.Null(active[2].Without.BookId);
        Assert.Equal(RecipeSort.RecentlyAdded, active[4].Without.Sort);
        Assert.Equal(4, query.FilterCount); // a Tag, the Book, the time and cooked; the search isn't counted
        Assert.True(query.Cleared().IsEverything);
        Assert.Equal(RecipeSort.RecentlyAdded, query.Cleared().Sort);
    }

    [Fact]
    public async Task A_merged_tag_filters_as_the_tag_it_went_into_and_a_deleted_one_is_ignored()
    {
        var app = CreateApp();
        await AddAsync(app, "Chili", ["supper"]);
        await AddAsync(app, "Oats", ["breakfast"]);
        var supper = await TagIdAsync(app, "supper");
        await app.Tags.MergeTagAsync(supper, await TagIdAsync(app, "dinner"));

        Assert.Equal(["Chili"], await TitlesAsync(app, new RecipeQuery { TagIds = [supper] }));
        Assert.Equal(["Chili", "Oats"], await TitlesAsync(app, new RecipeQuery { TagIds = ["no-such-tag"] }));
    }

    [Fact]
    public async Task Filter_options_offer_tags_and_books_in_use()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Jerusalem");
        await app.Books.AddBookAsync("Unused");
        await AddAsync(app, "Chili", ["dinner", "crockpot", "family"], source: s => { s.Kind = SourceKind.Book; s.BookId = book.Id; });

        var options = await app.Finder.GetFilterOptionsAsync(RecipeQuery.Everything);

        Assert.Equal(
            [("Meal", "dinner"), ("Equipment", "crockpot"), (null, "family")],
            options.Tags.SelectMany(g => g.Tags.Select(t => (g.Group?.Name, t.Name))));
        Assert.Equal(["Jerusalem"], options.Books.Select(b => b.Title));
    }

    [Fact]
    public async Task The_query_is_remembered_on_this_device()
    {
        var app = CreateApp();
        var query = new RecipeQuery { Search = "soup", Sort = RecipeSort.RecentlyCooked, MaxTotalMinutes = 30, Cooked = CookedFilter.Cooked };

        await app.Finder.SaveQueryAsync(query);

        var saved = await CreateApp().Finder.GetSavedQueryAsync();
        Assert.Equal(query.Search, saved.Search);
        Assert.Equal(query.Sort, saved.Sort);
        Assert.Equal(query.MaxTotalMinutes, saved.MaxTotalMinutes);
        Assert.Equal(query.Cooked, saved.Cooked);
        Assert.Equal(RecipeSort.Title, (await new AppCore(new InMemoryRecordStore(), _clock).Finder.GetSavedQueryAsync()).Sort);
    }
}
