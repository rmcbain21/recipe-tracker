using System.Text.RegularExpressions;
using RecipeTracker.Core.Books;
using RecipeTracker.Core.Cooking;
using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Organizing;
using RecipeTracker.Core.Parsing;
using RecipeTracker.Core.Ports;
using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Recipes;

/// <summary>The Cook's Recipes.</summary>
public sealed partial class RecipeCatalogue
{
    internal const string Table = "recipes";

    private const string UntitledDraft = "Untitled Recipe";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly IRecipeParser _parser;

    public RecipeCatalogue(IRecordStore store, IClock clock, IRecipeParser parser)
    {
        _store = store;
        _clock = clock;
        _parser = parser;
    }

    /// <summary>Adds a Draft with only a title, as a quick capture to fill in later.</summary>
    public async Task<Recipe> AddRecipeAsync(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new RuleViolationException("Give the Recipe a title.");
        }

        var editor = NewRecipe();
        editor.Title = title;
        return await SaveDraftAsync(editor);
    }

    /// <summary>Every Recipe that isn't deleted: Drafts first, then the rest, each by title.</summary>
    public async Task<IReadOnlyList<Recipe>> ListRecipesAsync()
    {
        var recipes = await _store.ListAsync<Recipe>(Table);
        return recipes
            .Where(recipe => !recipe.Deleted)
            .OrderByDescending(recipe => recipe.IsDraft)
            .ThenBy(recipe => recipe.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>The Recipe ready to show on its recipe page, or null if there isn't one with this id.</summary>
    public async Task<RecipeView?> GetRecipeAsync(string id)
    {
        var recipe = await LoadAsync(id);
        if (recipe is null)
        {
            return null;
        }

        // Includes deleted Books, so that a Book deleted on another device still shows its title.
        var book = recipe.Source is { Kind: SourceKind.Book, BookId: { } bookId } ? await _store.LoadAsync<Book>(BookCatalogue.Table, bookId) : null;
        var tags = TagCatalogue.GroupForRecipe(
            recipe.TagIds,
            await _store.ListAsync<Tag>(TagCatalogue.TagTable),
            await _store.ListAsync<TagGroup>(TagCatalogue.GroupTable));
        return new RecipeView(recipe, await IngredientsByIdAsync(), book, tags);
    }

    /// <summary>An editor for a new, unsaved Recipe.</summary>
    public RecipeEditor NewRecipe() => new(null, new Dictionary<string, Ingredient>());

    /// <summary>An editor holding the Recipe as saved, or null if there isn't one with this id.</summary>
    public async Task<RecipeEditor?> EditRecipeAsync(string id)
    {
        var recipe = await LoadAsync(id);
        return recipe is null ? null : new RecipeEditor(recipe, await IngredientsByIdAsync());
    }

    /// <summary>
    /// Reads pasted or shared recipe text into an unsaved editor for the review screen. Each line
    /// suggests an Ingredient (matched by name or alias, or new) for the Cook to confirm. A URL
    /// shared with the text, or on a line of its own in it, becomes the Website Source.
    /// </summary>
    /// <param name="title">A title shared with the text (such as the page's), used when the text has none.</param>
    public async Task<RecipeEditor> CaptureAsync(string text, string? title = null, string? url = null)
    {
        text ??= "";
        if (string.IsNullOrWhiteSpace(url))
        {
            var urlLine = UrlLine().Match(text);
            if (urlLine.Success)
            {
                url = urlLine.Groups["url"].Value;
                text = text.Remove(urlLine.Index, urlLine.Length);
            }
        }

        var parsed = _parser.Parse(text);
        var ingredients = (await _store.ListAsync<Ingredient>(IngredientCatalogue.Table)).Where(i => !i.Deleted).ToList();

        var editor = NewRecipe();
        editor.Title = parsed.Title.Length > 0 ? parsed.Title : (title ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(url))
        {
            editor.Source.Kind = SourceKind.Website;
            editor.Source.Url = url.Trim();
        }
        editor.YieldAmount = parsed.Yield?.Amount;
        editor.YieldLabel = parsed.Yield?.Label ?? "";
        editor.PrepMinutes = parsed.StatedTimes.PrepMinutes;
        editor.CookMinutes = parsed.StatedTimes.CookMinutes;
        editor.TotalMinutes = parsed.StatedTimes.TotalMinutes;

        foreach (var parsedSection in parsed.Sections)
        {
            var section = parsedSection.Heading is null ? editor.Sections[0] : editor.AddSection(parsedSection.Heading);
            foreach (var parsedLine in parsedSection.Lines)
            {
                var line = editor.AddLine(section);
                line.QuantityText = parsedLine.Quantity?.ToString() ?? "";
                line.Unit = parsedLine.Unit;
                line.Preparation = parsedLine.Preparation;
                line.Optional = parsedLine.Optional;
                line.OriginalText = parsedLine.OriginalText;
                if (parsedLine.Name.Length > 0)
                {
                    var (ingredient, match) = IngredientCatalogue.Suggest(ingredients, parsedLine.Name);
                    line.Suggest(ingredient is null ? IngredientChoice.New(parsedLine.Name) : IngredientChoice.Existing(ingredient), match);
                    line.SuggestedName = parsedLine.Name;
                }
            }
        }

        foreach (var step in parsed.Steps)
        {
            editor.AddStep(step);
        }

        editor.RefreshUses();
        return editor;
    }

    /// <summary>
    /// Checks and saves the editor's Recipe as finished (no longer a Draft), creating any new
    /// Ingredients it picked, all in one go. Blank Ingredient Lines and Steps are dropped.
    /// </summary>
    /// <exception cref="RuleViolationException">
    /// The Recipe has no title, no lines or Steps, or a line whose Ingredient isn't picked and confirmed.
    /// </exception>
    public Task<Recipe> SaveAsync(RecipeEditor editor) => SaveAsync(editor, asDraft: false);

    /// <summary>
    /// Saves the editor's Recipe as a Draft, however far its review got. Suggested Ingredients
    /// stay suggestions, and new ones aren't created until they're confirmed.
    /// </summary>
    public Task<Recipe> SaveDraftAsync(RecipeEditor editor) => SaveAsync(editor, asDraft: true);

    /// <summary>Deletes the Recipe and its Cook Log Entries, leaving tombstones so that the deletion syncs.</summary>
    public async Task DeleteRecipeAsync(string id)
    {
        var recipe = await LoadAsync(id);
        if (recipe is null)
        {
            return;
        }

        // Its Cook Log Entries go with it, as tombstones so that the deletion syncs.
        var now = _clock.UtcNow;
        var records = (await _store.ListAsync<CookLogEntry>(CookingCatalogue.CookLogTable))
            .Where(entry => !entry.Deleted && entry.RecipeId == id)
            .Select(entry => new RecordToSave(CookingCatalogue.CookLogTable, entry with { Deleted = true, UpdatedAt = now }))
            .Append(new RecordToSave(Table, recipe with { Deleted = true, UpdatedAt = now }))
            .ToList();
        await _store.SaveAllAsync(records);
    }

    /// <summary>Adds a Note to the Recipe, or to one of its Ingredient Lines or Steps.</summary>
    /// <param name="targetId">The line's or Step's id, or null for the whole Recipe.</param>
    /// <exception cref="RuleViolationException">The Note is blank, or the Recipe, line or Step is gone.</exception>
    public async Task<Note> AddNoteAsync(string recipeId, string? targetId, string text)
    {
        var recipe = await LoadForNoteAsync(recipeId);
        if (targetId is not null && !NoteTargets(recipe).Contains(targetId))
        {
            throw new RuleViolationException("That line or Step has been removed from the Recipe.");
        }

        var note = new Note(Guid.NewGuid().ToString(), targetId, NoteText(text), _clock.UtcNow);
        await _store.SaveAsync(Table, recipe with { UpdatedAt = _clock.UtcNow, Notes = [.. recipe.Notes, note] });
        return note;
    }

    /// <summary>Changes a Note's text. It keeps its place and the date it was added.</summary>
    /// <exception cref="RuleViolationException">The Note is blank, or it or its Recipe is gone.</exception>
    public async Task<Note> EditNoteAsync(string recipeId, string noteId, string text)
    {
        var recipe = await LoadForNoteAsync(recipeId);
        var note = recipe.Notes.FirstOrDefault(n => n.Id == noteId) ?? throw new RuleViolationException("That Note has been deleted.");
        var edited = note with { Text = NoteText(text) };
        await _store.SaveAsync(Table, recipe with
        {
            UpdatedAt = _clock.UtcNow,
            Notes = recipe.Notes.Select(n => n.Id == noteId ? edited : n).ToList(),
        });
        return edited;
    }

    public async Task DeleteNoteAsync(string recipeId, string noteId)
    {
        var recipe = await LoadAsync(recipeId);
        if (recipe is not null && recipe.Notes.Any(n => n.Id == noteId))
        {
            await _store.SaveAsync(Table, recipe with
            {
                UpdatedAt = _clock.UtcNow,
                Notes = recipe.Notes.Where(n => n.Id != noteId).ToList(),
            });
        }
    }

    /// <summary>Sets the Recipe's Tags, as picked on the recipe page. Ids of deleted Tags are dropped.</summary>
    /// <exception cref="RuleViolationException">The Recipe is gone.</exception>
    public async Task SetTagsAsync(string recipeId, IEnumerable<string> tagIds)
    {
        var recipe = await LoadAsync(recipeId) ?? throw new RuleViolationException("That Recipe has been deleted.");
        await _store.SaveAsync(Table, recipe with { UpdatedAt = _clock.UtcNow, TagIds = await LiveTagIdsAsync(tagIds) });
    }

    private async Task<IReadOnlyList<string>> LiveTagIdsAsync(IEnumerable<string> tagIds)
    {
        var live = (await _store.ListAsync<Tag>(TagCatalogue.TagTable)).Where(tag => !tag.Deleted).Select(tag => tag.Id).ToHashSet();
        return tagIds.Distinct().Where(live.Contains).ToList();
    }

    private async Task<Recipe> LoadForNoteAsync(string recipeId) =>
        await LoadAsync(recipeId) ?? throw new RuleViolationException("That Recipe has been deleted.");

    private static string NoteText(string text) =>
        string.IsNullOrWhiteSpace(text) ? throw new RuleViolationException("Write something in the Note.") : text.Trim();

    private static HashSet<string> NoteTargets(Recipe recipe) =>
        recipe.Lines.Select(line => line.Id).Concat(recipe.Steps.Select(step => step.Id)).ToHashSet();

    private async Task<Recipe> SaveAsync(RecipeEditor editor, bool asDraft)
    {
        editor.RefreshUses();
        var title = editor.Title.Trim();
        if (title.Length == 0)
        {
            title = asDraft ? UntitledDraft : throw new RuleViolationException("Give the Recipe a title.");
        }

        var headed = editor.Sections.Where(section => section.IsHeaded).ToList();
        if (headed.Any(section => string.IsNullOrWhiteSpace(section.Heading)))
        {
            throw new RuleViolationException("Give every Ingredient Section a heading.");
        }

        if (editor.YieldAmount is <= 0)
        {
            throw new RuleViolationException("The Yield needs to be more than zero.");
        }

        if (new[] { editor.PrepMinutes, editor.CookMinutes, editor.TotalMinutes }.Any(minutes => minutes is < 0))
        {
            throw new RuleViolationException("Times can't be negative.");
        }

        var books = (await _store.ListAsync<Book>(BookCatalogue.Table)).Where(book => !book.Deleted).Select(book => book.Id).ToHashSet();
        var source = editor.Source.ToSource(books.Contains);
        var tagIds = await LiveTagIdsAsync(editor.TagIds);

        var now = _clock.UtcNow;
        var known =(await _store.ListAsync<Ingredient>(IngredientCatalogue.Table)).Where(i => !i.Deleted).ToList();
        var created = new List<Ingredient>();
        var lines = new List<IngredientLine>();

        foreach (var section in editor.Sections)
        {
            foreach (var line in section.Lines.Where(line => !line.IsBlank))
            {
                if (!Quantity.TryParse(line.QuantityText, out var quantity))
                {
                    throw new RuleViolationException($"\"{line.QuantityText.Trim()}\" isn't a quantity. Try \"2\", \"1½\", \"1 1/2\" or \"2-3\", or leave it blank.");
                }

                var stored = new IngredientLine
                {
                    Id = line.Id,
                    SectionId = section.IsHeaded ? section.Id : null,
                    Quantity = quantity,
                    Unit = line.Unit.Trim(),
                    Preparation = line.Preparation.Trim(),
                    Optional = line.Optional,
                    OriginalText = line.OriginalText,
                };
                lines.Add(asDraft ? DraftLine(stored, line, known, created, now) : FinishedLine(stored, line, known, created, now));
            }
        }

        // Uses of lines that weren't saved (removed or blank) go with them. A Draft keeps unticked
        // suggestions so they aren't suggested again; a finished Recipe keeps only the ticked uses.
        var savedLineIds = lines.Select(line => line.Id).ToHashSet();
        var steps = editor.Steps
            .Where(step => !string.IsNullOrWhiteSpace(step.Text))
            .Select(step => new Step(step.Id, step.Text.Trim())
            {
                Uses = step.Uses
                    .Where(use => savedLineIds.Contains(use.LineId) && (use.Ticked || asDraft))
                    .Select(use => use.Ticked ? UseFor(use) : new IngredientUse(use.LineId) { Unticked = true })
                    .ToList(),
            })
            .ToList();
        if (!asDraft && lines.Count == 0 && steps.Count == 0)
        {
            throw new RuleViolationException("Add at least one Ingredient Line or Step, or save it as a Draft.");
        }

        var label = editor.YieldLabel.Trim();

        // Starting from the saved Recipe keeps its id, CreatedAt and fields this editor doesn't cover.
        var start = editor.Original ?? new Recipe { Id = Guid.NewGuid().ToString(), UpdatedAt = now, Title = "", CreatedAt = now };
        var recipe = start with
        {
            UpdatedAt = now,
            Title = title,
            IsDraft = asDraft,
            Source = source,
            TagIds = tagIds,
            Description = editor.Description.Trim(),
            Yield = editor.YieldAmount is null && label.Length == 0 ? null : new Yield(editor.YieldAmount, label),
            StatedTimes = new StatedTimes(editor.PrepMinutes, editor.CookMinutes, editor.TotalMinutes),
            Sections = headed.Select(section => new IngredientSection(section.Id, section.Heading!.Trim())).ToList(),
            Lines = lines,
            Steps = steps,
        };

        // Notes aren't part of the editor. Take them from the Recipe as stored now (a Note may have
        // been added since the editor opened), dropping those on lines and Steps that are gone.
        var notes = editor.Original is null ? [] : (await _store.LoadAsync<Recipe>(Table, editor.Original.Id))?.Notes ?? editor.Original.Notes;
        var targets = NoteTargets(recipe);
        recipe = recipe with { Notes = notes.Where(note => note.TargetId is null || targets.Contains(note.TargetId)).ToList() };

        var records = created.Select(ingredient => new RecordToSave(IngredientCatalogue.Table, ingredient))
            .Append(new RecordToSave(Table, recipe))
            .ToList();
        await _store.SaveAllAsync(records);
        return recipe;
    }

    private static IngredientUse UseFor(EditedUse use)
    {
        if (!Quantity.TryParse(use.QuantityText, out var quantity))
        {
            throw new RuleViolationException($"\"{use.QuantityText.Trim()}\" isn't an amount. Try \"½\", \"1 1/2\" or \"2-3\", or leave it blank for the whole line.");
        }

        return new IngredientUse(use.LineId, quantity, use.Unit.Trim());
    }

    private static IngredientLine FinishedLine(IngredientLine stored, EditedLine line, List<Ingredient> known, List<Ingredient> created, DateTimeOffset now)
    {
        if (line.Ingredient is null)
        {
            throw new RuleViolationException("Pick an Ingredient for every Ingredient Line.");
        }

        if (!line.IsConfirmed)
        {
            throw new RuleViolationException("Confirm the Ingredient for every Ingredient Line, or save it as a Draft.");
        }

        return stored with { IngredientId = IngredientIdFor(line.Ingredient, known, created, now) };
    }

    // A Draft keeps a suggestion as a suggestion: an existing Ingredient's id, or just the name for a new one.
    private static IngredientLine DraftLine(IngredientLine stored, EditedLine line, List<Ingredient> known, List<Ingredient> created, DateTimeOffset now)
    {
        if (line.Ingredient is null)
        {
            return stored;
        }

        if (!line.IsConfirmed)
        {
            return stored with
            {
                IngredientId = line.Ingredient.IngredientId,
                MatchUnconfirmed = true,
                SuggestedName = line.SuggestedName.Length > 0 ? line.SuggestedName : line.Ingredient.Name,
            };
        }

        return stored with { IngredientId = IngredientIdFor(line.Ingredient, known, created, now) };
    }

    private async Task<Recipe?> LoadAsync(string id)
    {
        var recipe = await _store.LoadAsync<Recipe>(Table, id);
        return recipe is { Deleted: false } ? recipe : null;
    }

    // Includes deleted Ingredients, so that a line whose Ingredient was deleted on another device still shows a name.
    private async Task<IReadOnlyDictionary<string, Ingredient>> IngredientsByIdAsync() =>
        (await _store.ListAsync<Ingredient>(IngredientCatalogue.Table)).ToDictionary(ingredient => ingredient.Id);

    // A new Ingredient whose name matches one that exists, or one created earlier in this save, uses that one instead.
    private static string IngredientIdFor(IngredientChoice choice, List<Ingredient> known, List<Ingredient> created, DateTimeOffset now)
    {
        if (!choice.IsNew)
        {
            return choice.IngredientId!;
        }

        if (string.IsNullOrWhiteSpace(choice.Name))
        {
            throw new RuleViolationException("Give the new Ingredient a name.");
        }

        var existing = IngredientCatalogue.FindByName(known.Concat(created), choice.Name);
        if (existing is not null)
        {
            return existing.Id;
        }

        var ingredient = new Ingredient { Id = Guid.NewGuid().ToString(), UpdatedAt = now, Name = choice.Name };
        created.Add(ingredient);
        return ingredient.Id;
    }

    // A line that is only a web address, as Chrome adds after shared text.
    [GeneratedRegex(@"^[ \t]*(?<url>https?://\S+)[ \t\r]*$\n?", RegexOptions.Multiline)]
    private static partial Regex UrlLine();
}
