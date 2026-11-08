using RecipeTracker.Core.Books;
using RecipeTracker.Core.Recipes;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class SourceAndBookTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static RecipeEditor NewRecipe(AppCore app, string title = "Caesar salad")
    {
        var editor = app.Recipes.NewRecipe();
        editor.Title = title;
        editor.AddStep("Toss everything together.");
        return editor;
    }

    private static async Task<Recipe> SaveWithSourceAsync(AppCore app, Action<EditedSource> setSource, string title = "Caesar salad")
    {
        var editor = NewRecipe(app, title);
        setSource(editor.Source);
        return await app.Recipes.SaveAsync(editor);
    }

    private static Action<EditedSource> FromBook(Book book, string page = "") => source =>
    {
        source.Kind = SourceKind.Book;
        source.BookId = book.Id;
        source.Page = page;
    };

    private static async Task<ShownSource?> ShownAsync(AppCore app, Recipe recipe) =>
        (await app.Recipes.GetRecipeAsync(recipe.Id))!.Source;

    // Books

    [Fact]
    public async Task Books_are_added_and_listed_by_title()
    {
        var app = CreateApp();
        await app.Books.AddBookAsync("Salt Fat Acid Heat", "Samin Nosrat");
        await app.Books.AddBookAsync("  Jerusalem  ", " Yotam Ottolenghi ");

        var books = await CreateApp().Books.ListBooksAsync();

        Assert.Equal(["Jerusalem", "Salt Fat Acid Heat"], books.Select(b => b.Title));
        Assert.Equal("Yotam Ottolenghi", books[0].Author);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_book_needs_a_title(string title)
    {
        await Assert.ThrowsAsync<RuleViolationException>(() => CreateApp().Books.AddBookAsync(title));
    }

    [Fact]
    public async Task Two_books_cannot_share_a_title()
    {
        var app = CreateApp();
        await app.Books.AddBookAsync("Jerusalem");
        var other = await app.Books.AddBookAsync("Plenty");

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Books.AddBookAsync("jerusalem"));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Books.EditBookAsync(other.Id, "JERUSALEM", ""));
    }

    [Fact]
    public async Task Editing_a_book_changes_it_on_every_recipe_from_it()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Salt Fat Acid Hat");
        var recipe = await SaveWithSourceAsync(app, FromBook(book));

        _clock.UtcNow += TimeSpan.FromMinutes(1);
        var edited = await app.Books.EditBookAsync(book.Id, "Salt Fat Acid Heat", "Samin Nosrat");

        Assert.Equal(_clock.UtcNow, edited.UpdatedAt);
        Assert.Equal("Salt Fat Acid Heat", (await ShownAsync(app, recipe))!.Name);
    }

    [Fact]
    public async Task Book_summaries_count_the_recipes_from_each_book_including_drafts()
    {
        var app = CreateApp();
        var jerusalem = await app.Books.AddBookAsync("Jerusalem");
        await app.Books.AddBookAsync("Plenty");
        await SaveWithSourceAsync(app, FromBook(jerusalem), "Shakshuka");
        var draft = NewRecipe(app, "Hummus");
        FromBook(jerusalem)(draft.Source);
        await app.Recipes.SaveDraftAsync(draft);
        var deleted = await SaveWithSourceAsync(app, FromBook(jerusalem), "Gone");
        await app.Recipes.DeleteRecipeAsync(deleted.Id);

        var summaries = await app.Books.ListBookSummariesAsync();

        Assert.Equal([("Jerusalem", 2), ("Plenty", 0)], summaries.Select(s => (s.Book.Title, s.RecipeCount)));
    }

    [Fact]
    public async Task A_book_in_use_cannot_be_deleted()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Jerusalem");
        var recipe = await SaveWithSourceAsync(app, FromBook(book));

        var error = await Assert.ThrowsAsync<RuleViolationException>(() => app.Books.DeleteBookAsync(book.Id));
        Assert.Contains("1 Recipe", error.Message);

        await app.Recipes.DeleteRecipeAsync(recipe.Id);
        await app.Books.DeleteBookAsync(book.Id);
        Assert.Empty(await app.Books.ListBooksAsync());
    }

    [Fact]
    public async Task Deleting_a_book_leaves_a_tombstone()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Plenty");
        _clock.UtcNow += TimeSpan.FromMinutes(1);

        await app.Books.DeleteBookAsync(book.Id);

        var stored = await _store.LoadAsync<Book>("books", book.Id);
        Assert.True(stored!.Deleted);
        Assert.Equal(_clock.UtcNow, stored.UpdatedAt);
    }

    // Source on save

    [Theory]
    [InlineData("https://www.seriouseats.com/the-food-lab-caesar", "", "https://www.seriouseats.com/the-food-lab-caesar", "seriouseats.com")]
    [InlineData("seriouseats.com/caesar", "", "https://seriouseats.com/caesar", "seriouseats.com")]
    [InlineData("https://smittenkitchen.com/x", "Smitten Kitchen", "https://smittenkitchen.com/x", "Smitten Kitchen")]
    [InlineData("", "Grandma's blog", "", "Grandma's blog")]
    public async Task A_website_source_fills_in_its_site_name_from_the_url_when_blank(string url, string name, string savedUrl, string savedName)
    {
        var recipe = await SaveWithSourceAsync(CreateApp(), s => { s.Kind = SourceKind.Website; s.Url = url; s.Name = name; });

        Assert.Equal(new Source { Kind = SourceKind.Website, Url = savedUrl, Name = savedName }, recipe.Source);
    }

    [Fact]
    public async Task A_video_source_keeps_its_url_and_channel()
    {
        var recipe = await SaveWithSourceAsync(CreateApp(), s =>
        {
            s.Kind = SourceKind.Video;
            s.Url = "https://www.youtube.com/watch?v=abc";
            s.Name = "Ethan Chlebowski";
        });

        Assert.Equal(new Source { Kind = SourceKind.Video, Url = "https://www.youtube.com/watch?v=abc", Name = "Ethan Chlebowski" }, recipe.Source);
    }

    [Theory]
    [InlineData("not a web address")]
    [InlineData("ftp://example.com/file")]
    [InlineData("localhost")]
    public async Task A_url_that_is_not_a_web_address_is_refused(string url)
    {
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            SaveWithSourceAsync(CreateApp(), s => { s.Kind = SourceKind.Website; s.Url = url; }));
    }

    [Fact]
    public async Task Saving_keeps_only_the_chosen_kinds_fields()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Plenty");

        // The Cook typed a URL, then switched to Book.
        var recipe = await SaveWithSourceAsync(app, s =>
        {
            s.Kind = SourceKind.Website;
            s.Url = "https://example.com";
            FromBook(book, " 212 ")(s);
        });

        Assert.Equal(new Source { Kind = SourceKind.Book, BookId = book.Id, Page = "212" }, recipe.Source);
    }

    [Fact]
    public async Task A_book_source_needs_a_book_that_exists()
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Plenty");
        await app.Books.DeleteBookAsync(book.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() => SaveWithSourceAsync(app, s => s.Kind = SourceKind.Book));
        await Assert.ThrowsAsync<RuleViolationException>(() => SaveWithSourceAsync(app, FromBook(book)));
    }

    [Fact]
    public async Task A_source_with_nothing_filled_in_is_no_source()
    {
        var app = CreateApp();

        Assert.Null((await SaveWithSourceAsync(app, s => { s.Kind = SourceKind.Website; s.Url = "  "; })).Source);
        Assert.Null((await SaveWithSourceAsync(app, s => { s.Kind = SourceKind.Other; s.Text = "  "; })).Source);
        Assert.Null((await SaveWithSourceAsync(app, s => s.Kind = null)).Source);
    }

    [Fact]
    public async Task Editing_a_recipe_starts_from_its_saved_source_and_can_remove_it()
    {
        var app = CreateApp();
        var recipe = await SaveWithSourceAsync(app, s => { s.Kind = SourceKind.Other; s.Text = "Grandma"; });

        var editor = (await app.Recipes.EditRecipeAsync(recipe.Id))!;
        Assert.Equal((SourceKind.Other, "Grandma"), (editor.Source.Kind!.Value, editor.Source.Text));

        editor.Source.Kind = null;
        Assert.Null((await app.Recipes.SaveAsync(editor)).Source);
    }

    // Source on the recipe page

    [Theory]
    [InlineData("212", ", p. 212")]
    [InlineData("12-13", ", pp. 12–13")]
    [InlineData("12 – 13", ", pp. 12–13")]
    [InlineData("intro", ", intro")]
    [InlineData("", "")]
    public async Task A_book_source_shows_the_book_title_and_page(string page, string pageText)
    {
        var app = CreateApp();
        var book = await app.Books.AddBookAsync("Salt Fat Acid Heat");
        var recipe = await SaveWithSourceAsync(app, FromBook(book, page));

        var shown = await ShownAsync(app, recipe);

        Assert.Equal((SourceKind.Book, "Salt Fat Acid Heat", (string?)null, pageText), (shown!.Kind, shown.Name, shown.Url, shown.PageText));
    }

    [Fact]
    public async Task A_website_source_shows_its_site_name_with_a_link()
    {
        var app = CreateApp();
        var recipe = await SaveWithSourceAsync(app, s => { s.Kind = SourceKind.Website; s.Url = "https://www.seriouseats.com/caesar"; });

        var shown = await ShownAsync(app, recipe);

        Assert.Equal(("seriouseats.com", "https://www.seriouseats.com/caesar"), (shown!.Name, shown.Url));
    }

    [Fact]
    public async Task A_video_without_a_channel_shows_its_host()
    {
        var app = CreateApp();
        var recipe = await SaveWithSourceAsync(app, s => { s.Kind = SourceKind.Video; s.Url = "https://youtu.be/abc"; });

        Assert.Equal("youtu.be", (await ShownAsync(app, recipe))!.Name);
    }

    [Fact]
    public async Task An_other_source_shows_its_text_without_a_link()
    {
        var app = CreateApp();
        var recipe = await SaveWithSourceAsync(app, s => { s.Kind = SourceKind.Other; s.Text = "Grandma"; });

        var shown = await ShownAsync(app, recipe);

        Assert.Equal(("Grandma", (string?)null), (shown!.Name, shown.Url));
    }

    [Fact]
    public async Task A_recipe_without_a_source_shows_none()
    {
        var app = CreateApp();
        var recipe = await app.Recipes.SaveAsync(NewRecipe(app));

        Assert.Null(await ShownAsync(app, recipe));
    }
}
