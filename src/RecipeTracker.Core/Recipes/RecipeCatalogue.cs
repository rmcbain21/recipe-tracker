using System.Text.RegularExpressions;
using RecipeTracker.Core.Ingredients;
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
        return recipe is null ? null : new RecipeView(recipe, await IngredientsByIdAsync());
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
        editor.Source = string.IsNullOrWhiteSpace(url) ? null : Source.Website(url);
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

    /// <summary>Deletes the Recipe, leaving a tombstone so that the deletion syncs.</summary>
    public async Task DeleteRecipeAsync(string id)
    {
        var recipe = await LoadAsync(id);
        if (recipe is not null)
        {
            await _store.SaveAsync(Table, recipe with { Deleted = true, UpdatedAt = _clock.UtcNow });
        }
    }

    private async Task<Recipe> SaveAsync(RecipeEditor editor, bool asDraft)
    {
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

        var now = _clock.UtcNow;
        var known = (await _store.ListAsync<Ingredient>(IngredientCatalogue.Table)).Where(i => !i.Deleted).ToList();
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

        var steps = editor.Steps
            .Where(step => !string.IsNullOrWhiteSpace(step.Text))
            .Select(step => new Step(step.Id, step.Text.Trim()))
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
            Source = editor.Source,
            Description = editor.Description.Trim(),
            Yield = editor.YieldAmount is null && label.Length == 0 ? null : new Yield(editor.YieldAmount, label),
            StatedTimes = new StatedTimes(editor.PrepMinutes, editor.CookMinutes, editor.TotalMinutes),
            Sections = headed.Select(section => new IngredientSection(section.Id, section.Heading!.Trim())).ToList(),
            Lines = lines,
            Steps = steps,
        };

        var records = created.Select(ingredient => new RecordToSave(IngredientCatalogue.Table, ingredient))
            .Append(new RecordToSave(Table, recipe))
            .ToList();
        await _store.SaveAllAsync(records);
        return recipe;
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
