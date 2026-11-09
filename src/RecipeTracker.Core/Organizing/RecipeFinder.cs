using RecipeTracker.Core.Books;
using RecipeTracker.Core.Cooking;
using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Ports;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Organizing;

/// <summary>The home list: search, filters, sort and Surprise me, over every Recipe on this device.</summary>
public sealed class RecipeFinder
{
    /// <summary>Not a synced table: issue 02's sync must not list it.</summary>
    internal const string QueryTable = "homeQuery";

    private const string QueryId = "00000000-0000-0000-0000-000000000001";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly TagCatalogue _tags;

    public RecipeFinder(IRecordStore store, IClock clock, TagCatalogue tags)
    {
        _store = store;
        _clock = clock;
        _tags = tags;
    }

    /// <summary>The Recipes the query lets through: Drafts pinned first, then the rest in the query's order.</summary>
    public async Task<RecipeList> FindAsync(RecipeQuery query)
    {
        var data = await LoadAsync();
        var search = query.Search.Trim();
        var tagFilter = TagFilter(query.TagIds, data);

        var rows = data.Recipes
            .Where(recipe => search.Length == 0 || Matches(recipe, search, data.Ingredients))
            .Where(recipe => tagFilter.All(anyOf => recipe.TagIds.Any(anyOf.Contains)))
            .Where(recipe => query.BookId is null
                ? query.SourceKind is null || recipe.Source?.Kind == query.SourceKind
                : recipe.Source is { Kind: SourceKind.Book } source && source.BookId == query.BookId)
            .Where(recipe => query.MaxTotalMinutes is not int max || TotalMinutes(recipe) is int total && total <= max)
            .Where(recipe => query.Cooked switch
            {
                CookedFilter.Cooked => data.LastCooked.ContainsKey(recipe.Id),
                CookedFilter.NeverCooked => !data.LastCooked.ContainsKey(recipe.Id),
                _ => true,
            })
            .Select(recipe => (Recipe: recipe, Row: RowFor(recipe, data)));

        var ordered = rows.OrderByDescending(r => r.Recipe.IsDraft);
        ordered = query.Sort switch
        {
            RecipeSort.RecentlyAdded => ordered.ThenByDescending(r => AddedAt(r.Recipe)),
            RecipeSort.RecentlyCooked => ordered.ThenByDescending(r => r.Row.LastCooked ?? DateOnly.MinValue),
            _ => ordered,
        };

        var list = ordered.ThenBy(r => r.Recipe.Title, StringComparer.CurrentCultureIgnoreCase).Select(r => r.Row).ToList();
        return new RecipeList(list, data.Recipes.Count);
    }

    /// <summary>
    /// The filter sheet's choices: the Tags on at least one Recipe (plus any already picked), in
    /// Tag Group rows with ungrouped last, and the Books at least one Recipe comes from.
    /// </summary>
    public async Task<RecipeFilterOptions> GetFilterOptionsAsync(RecipeQuery query)
    {
        var data = await LoadAsync();
        var picked = query.TagIds.Select(id => Resolve(id, data.AllTags)).OfType<string>().ToHashSet();
        var used = data.Recipes.SelectMany(recipe => recipe.TagIds).Concat(picked).ToHashSet();
        var tags = (await _tags.ListGroupedTagsAsync())
            .Select(group => group with { Tags = group.Tags.Where(tag => used.Contains(tag.Id)).ToList() })
            .Where(group => group.Tags.Count > 0)
            .ToList();

        var bookIds = data.Recipes.Select(recipe => recipe.Source).OfType<Source>()
            .Where(source => source.Kind == SourceKind.Book && source.BookId is not null)
            .Select(source => source.BookId!)
            .Append(query.BookId)
            .ToHashSet();
        var books = data.Books.Where(book => !book.Deleted && bookIds.Contains(book.Id))
            .OrderBy(book => book.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        return new RecipeFilterOptions(tags, books);
    }

    /// <summary>The filters in use, each with the query it leaves when removed, for the chips above the list.</summary>
    public async Task<IReadOnlyList<ActiveFilter>> DescribeAsync(RecipeQuery query)
    {
        var data = await LoadAsync();
        var filters = new List<ActiveFilter>();
        if (query.Search.Trim() is { Length: > 0 } search)
        {
            filters.Add(new ActiveFilter($"\"{search}\"", query with { Search = "" }));
        }

        foreach (var id in query.TagIds)
        {
            var name = Resolve(id, data.AllTags) is { } live ? data.AllTags.First(tag => tag.Id == live).Name : null;
            if (name is not null)
            {
                filters.Add(new ActiveFilter(name, query.WithTag(id, on: false)));
            }
        }

        if (query.BookId is { } bookId)
        {
            var title = data.Books.FirstOrDefault(book => book.Id == bookId)?.Title ?? "Unknown Book";
            filters.Add(new ActiveFilter(title, query with { BookId = null, SourceKind = null }));
        }
        else if (query.SourceKind is { } kind)
        {
            filters.Add(new ActiveFilter(SourceKindName(kind), query with { SourceKind = null }));
        }

        if (query.MaxTotalMinutes is int max)
        {
            filters.Add(new ActiveFilter($"≤ {StatedTimes.Format(max)}", query with { MaxTotalMinutes = null }));
        }

        if (query.Cooked != CookedFilter.Any)
        {
            filters.Add(new ActiveFilter(query.Cooked == CookedFilter.Cooked ? "Cooked" : "Never cooked", query with { Cooked = CookedFilter.Any }));
        }

        return filters;
    }

    /// <summary>The query the home list was last left with on this device, or everything by title.</summary>
    public async Task<RecipeQuery> GetSavedQueryAsync() =>
        (await _store.LoadAsync<SavedRecipeQuery>(QueryTable, QueryId))?.Query ?? RecipeQuery.Everything;

    /// <summary>Remembers the query on this device, so the home list opens the same way after a reload.</summary>
    public Task SaveQueryAsync(RecipeQuery query) =>
        _store.SaveAsync(QueryTable, new SavedRecipeQuery { Id = QueryId, UpdatedAt = _clock.UtcNow, Query = query });

    public static string SourceKindName(SourceKind kind) => kind switch
    {
        SourceKind.Website => "Website",
        SourceKind.Book => "Book",
        SourceKind.Video => "Video",
        _ => "Other",
    };

    /// <summary>The Source's total time, or prep plus cook when it gives only those.</summary>
    internal static int? TotalMinutes(Recipe recipe) => recipe.StatedTimes switch
    {
        { TotalMinutes: int total } => total,
        { PrepMinutes: null, CookMinutes: null } => null,
        var times => (times.PrepMinutes ?? 0) + (times.CookMinutes ?? 0),
    };

    // One set per Tag Group with picks (a Recipe needs any one of the set), and one set per
    // ungrouped Tag (so each is needed). A Tag merged away counts as the Tag it went into;
    // a deleted one is ignored.
    private static List<HashSet<string>> TagFilter(IReadOnlyList<string> tagIds, Data data)
    {
        var live = data.AllTags.Where(tag => !tag.Deleted).ToDictionary(tag => tag.Id);
        var liveGroups = data.Groups.Where(group => !group.Deleted).Select(group => group.Id).ToHashSet();
        var picked = tagIds.Select(id => Resolve(id, data.AllTags)).OfType<string>().Distinct().Select(id => live[id]);

        return picked
            .GroupBy(tag => tag.GroupId is { } g && liveGroups.Contains(g) ? g : $"ungrouped:{tag.Id}")
            .Select(group => group.Select(tag => tag.Id).ToHashSet())
            .ToList();
    }

    private static string? Resolve(string id, IReadOnlyList<Tag> allTags)
    {
        for (var hops = 0; hops < 20; hops++)
        {
            var tag = allTags.FirstOrDefault(t => t.Id == id);
            if (tag is null)
            {
                return null;
            }

            if (!tag.Deleted)
            {
                return tag.Id;
            }

            if (tag.MergedInto is not { } next)
            {
                return null;
            }

            id = next;
        }

        return null;
    }

    private static bool Matches(Recipe recipe, string search, IReadOnlyDictionary<string, Ingredient> ingredients)
    {
        bool Has(string? text) => text?.Contains(search, StringComparison.CurrentCultureIgnoreCase) == true;

        return Has(recipe.Title)
            || recipe.Notes.Any(note => Has(note.Text))
            || recipe.Lines.Any(line =>
                Has(line.SuggestedName) ||
                line.IngredientId is { } id && ingredients.TryGetValue(id, out var ingredient) &&
                (Has(ingredient.Name) || ingredient.Aliases.Any(Has)));
    }

    private static RecipeRow RowFor(Recipe recipe, Data data)
    {
        var book = recipe.Source is { Kind: SourceKind.Book, BookId: { } bookId } ? data.Books.FirstOrDefault(b => b.Id == bookId) : null;
        var tags = TagCatalogue.GroupForRecipe(recipe.TagIds, data.AllTags, data.Groups)
            .SelectMany(group => group.Tags.Select(tag => tag.Name))
            .ToList();
        return new RecipeRow(
            recipe.Id,
            recipe.Title,
            recipe.IsDraft,
            RecipeView.ShowSource(recipe.Source, book)?.Name is { Length: > 0 } name ? name : null,
            TotalMinutes(recipe),
            tags,
            data.LastCooked.TryGetValue(recipe.Id, out var last) ? last : null);
    }

    // Recipes saved before CreatedAt existed fall back to when they were last changed.
    private static DateTimeOffset AddedAt(Recipe recipe) => recipe.CreatedAt == default ? recipe.UpdatedAt : recipe.CreatedAt;

    private sealed record Data(
        IReadOnlyList<Recipe> Recipes,
        IReadOnlyDictionary<string, Ingredient> Ingredients,
        IReadOnlyList<Tag> AllTags,
        IReadOnlyList<TagGroup> Groups,
        IReadOnlyList<Book> Books,
        IReadOnlyDictionary<string, DateOnly> LastCooked);

    private async Task<Data> LoadAsync()
    {
        var recipes = (await _store.ListAsync<Recipe>(RecipeCatalogue.Table)).Where(recipe => !recipe.Deleted).ToList();
        var lastCooked = (await _store.ListAsync<CookLogEntry>(CookingCatalogue.CookLogTable))
            .Where(entry => !entry.Deleted)
            .GroupBy(entry => entry.RecipeId)
            .ToDictionary(group => group.Key, group => group.Max(entry => entry.Date));
        return new Data(
            recipes,
            (await _store.ListAsync<Ingredient>(IngredientCatalogue.Table)).ToDictionary(ingredient => ingredient.Id),
            await _store.ListAsync<Tag>(TagCatalogue.TagTable),
            await _store.ListAsync<TagGroup>(TagCatalogue.GroupTable),
            await _store.ListAsync<Book>(BookCatalogue.Table),
            lastCooked);
    }
}
