using RecipeTracker.Core.Ports;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Books;

/// <summary>The Cook's Books.</summary>
public sealed class BookCatalogue
{
    internal const string Table = "books";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public BookCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>Every Book that isn't deleted, by title.</summary>
    public async Task<IReadOnlyList<Book>> ListBooksAsync() =>
        (await _store.ListAsync<Book>(Table))
            .Where(book => !book.Deleted)
            .OrderBy(book => book.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>Every Book that isn't deleted, by title, with how many Recipes come from each.</summary>
    public async Task<IReadOnlyList<BookSummary>> ListBookSummariesAsync()
    {
        var counts = (await RecipesAsync())
            .Where(recipe => recipe.Source is { Kind: SourceKind.Book, BookId: not null })
            .GroupBy(recipe => recipe.Source!.BookId!)
            .ToDictionary(group => group.Key, group => group.Count());
        return (await ListBooksAsync())
            .Select(book => new BookSummary(book, counts.GetValueOrDefault(book.Id)))
            .ToList();
    }

    /// <exception cref="RuleViolationException">The title is blank, or another Book has it.</exception>
    public async Task<Book> AddBookAsync(string title, string author = "")
    {
        var book = new Book
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Title = await CheckTitleAsync(title, id: null),
            Author = author.Trim(),
        };
        await _store.SaveAsync(Table, book);
        return book;
    }

    /// <summary>Changes a Book's title and author. Every Recipe from it shows the change.</summary>
    /// <exception cref="RuleViolationException">The Book is gone, the title is blank, or another Book has it.</exception>
    public async Task<Book> EditBookAsync(string id, string title, string author)
    {
        var book = await LoadAsync(id) ?? throw new RuleViolationException("That Book has been deleted.");
        book = book with { UpdatedAt = _clock.UtcNow, Title = await CheckTitleAsync(title, id), Author = author.Trim() };
        await _store.SaveAsync(Table, book);
        return book;
    }

    /// <summary>Deletes the Book, leaving a tombstone so that the deletion syncs.</summary>
    /// <exception cref="RuleViolationException">A Recipe (or Draft) still has the Book as its Source.</exception>
    public async Task DeleteBookAsync(string id)
    {
        var book = await LoadAsync(id);
        if (book is null)
        {
            return;
        }

        var uses = (await RecipesAsync()).Count(recipe => recipe.Source is { Kind: SourceKind.Book } source && source.BookId == id);
        if (uses > 0)
        {
            throw new RuleViolationException(
                $"\"{book.Title}\" is the Source of {uses} {(uses == 1 ? "Recipe" : "Recipes")}. Give {(uses == 1 ? "it" : "them")} another Source first.");
        }

        await _store.SaveAsync(Table, book with { Deleted = true, UpdatedAt = _clock.UtcNow });
    }

    internal async Task<Book?> LoadAsync(string id)
    {
        var book = await _store.LoadAsync<Book>(Table, id);
        return book is { Deleted: false } ? book : null;
    }

    private async Task<string> CheckTitleAsync(string title, string? id)
    {
        title = string.Join(' ', (title ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (title.Length == 0)
        {
            throw new RuleViolationException("Give the Book a title.");
        }

        var books = await ListBooksAsync();
        if (books.Any(book => book.Id != id && string.Equals(book.Title, title, StringComparison.CurrentCultureIgnoreCase)))
        {
            throw new RuleViolationException($"You already have a Book called \"{title}\".");
        }

        return title;
    }

    private async Task<IEnumerable<Recipe>> RecipesAsync() =>
        (await _store.ListAsync<Recipe>(RecipeCatalogue.Table)).Where(recipe => !recipe.Deleted);
}
