using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Ports;
using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Recipes;

/// <summary>The Cook's Recipes.</summary>
public sealed class RecipeCatalogue
{
    internal const string Table = "recipes";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public RecipeCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>Adds a Recipe with only a title.</summary>
    public async Task<Recipe> AddRecipeAsync(string title)
    {
        var editor = NewRecipe();
        editor.Title = title;
        return await SaveAsync(editor);
    }

    /// <summary>Every Recipe that isn't deleted, by title.</summary>
    public async Task<IReadOnlyList<Recipe>> ListRecipesAsync()
    {
        var recipes = await _store.ListAsync<Recipe>(Table);
        return recipes
            .Where(recipe => !recipe.Deleted)
            .OrderBy(recipe => recipe.Title, StringComparer.CurrentCultureIgnoreCase)
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
    /// Checks and saves the editor's Recipe, creating any new Ingredients it picked, all in one go.
    /// Blank Ingredient Lines and Steps are dropped.
    /// </summary>
    /// <exception cref="RuleViolationException">The Recipe breaks a rule, such as having no title.</exception>
    public async Task<Recipe> SaveAsync(RecipeEditor editor)
    {
        if (string.IsNullOrWhiteSpace(editor.Title))
        {
            throw new RuleViolationException("Give the Recipe a title.");
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

                lines.Add(new IngredientLine
                {
                    Id = line.Id,
                    SectionId = section.IsHeaded ? section.Id : null,
                    Quantity = quantity,
                    Unit = line.Unit.Trim(),
                    IngredientId = IngredientIdFor(line.Ingredient, known, created, now),
                    Preparation = line.Preparation.Trim(),
                    Optional = line.Optional,
                    OriginalText = line.OriginalText,
                });
            }
        }

        var label = editor.YieldLabel.Trim();
        // Starting from the saved Recipe keeps its id, CreatedAt and fields this editor doesn't cover.
        var start = editor.Original ?? new Recipe { Id = Guid.NewGuid().ToString(), UpdatedAt = now, Title = "", CreatedAt = now };
        var recipe = start with
        {
            UpdatedAt = now,
            Title = editor.Title.Trim(),
            Description = editor.Description.Trim(),
            Yield = editor.YieldAmount is null && label.Length == 0 ? null : new Yield(editor.YieldAmount, label),
            StatedTimes = new StatedTimes(editor.PrepMinutes, editor.CookMinutes, editor.TotalMinutes),
            Sections = headed.Select(section => new IngredientSection(section.Id, section.Heading!.Trim())).ToList(),
            Lines = lines,
            Steps = editor.Steps
                .Where(step => !string.IsNullOrWhiteSpace(step.Text))
                .Select(step => new Step(step.Id, step.Text.Trim()))
                .ToList(),
        };

        var records = created.Select(ingredient => new RecordToSave(IngredientCatalogue.Table, ingredient))
            .Append(new RecordToSave(Table, recipe))
            .ToList();
        await _store.SaveAllAsync(records);
        return recipe;
    }

    /// <summary>Deletes the Recipe, leaving a tombstone so that the deletion syncs.</summary>
    public async Task DeleteRecipeAsync(string id)
    {
        var recipe = await LoadAsync(id);
        if (recipe is not null)
        {
            await _store.SaveAsync(Table, recipe with { Deleted = true, UpdatedAt = _clock.UtcNow });
        }
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
    private static string IngredientIdFor(IngredientChoice? choice, List<Ingredient> known, List<Ingredient> created, DateTimeOffset now)
    {
        if (choice is null)
        {
            throw new RuleViolationException("Pick an Ingredient for every Ingredient Line.");
        }

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
}
