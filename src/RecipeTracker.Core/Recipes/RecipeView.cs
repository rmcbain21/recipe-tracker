using RecipeTracker.Core.Books;
using RecipeTracker.Core.Ingredients;
using RecipeTracker.Core.Organizing;
using RecipeTracker.Core.Parsing;
using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Recipes;

/// <summary>A Recipe as its recipe page shows it: ingredients grouped by section, with their Ingredients' names.</summary>
public sealed class RecipeView
{
    internal RecipeView(Recipe recipe, IReadOnlyDictionary<string, Ingredient> ingredients, Book? book, IReadOnlyList<GroupedTags<Tag>> tags, Scale? scale = null)
    {
        Recipe = recipe;
        Scale = scale ?? Scale.One;
        _ingredients = ingredients;
        _book = book;
        Tags = tags;
        Source = ShowSource(recipe.Source, book);

        var sectionIds = recipe.Sections.Select(section => section.Id).ToHashSet();
        var groups = new List<IngredientGroup>();
        var unheaded = recipe.Lines.Where(line => line.SectionId is null || !sectionIds.Contains(line.SectionId)).ToList();
        if (unheaded.Count > 0)
        {
            groups.Add(new IngredientGroup(null, unheaded.Select(line => ShowLine(line, ingredients)).ToList()));
        }

        foreach (var section in recipe.Sections)
        {
            var lines = recipe.Lines.Where(line => line.SectionId == section.Id).Select(line => ShowLine(line, ingredients)).ToList();
            groups.Add(new IngredientGroup(section.Heading, lines));
        }

        Ingredients = groups;
    }

    private readonly IReadOnlyDictionary<string, Ingredient> _ingredients;
    private readonly Book? _book;

    public Recipe Recipe { get; }

    /// <summary>The Scale every quantity is shown at; ×1 unless <see cref="AtScale"/> chose another.</summary>
    public Scale Scale { get; }

    /// <summary>The same Recipe with every line's and Ingredient Use's quantity shown at another Scale.</summary>
    public RecipeView AtScale(Scale scale) => new(Recipe, _ingredients, _book, Tags, scale);

    /// <summary>The Yield at the Scale, such as "6 servings (×1½)", or null if the Recipe has none.</summary>
    public string? YieldText => Recipe.Yield is null ? null : Scale.Format(Recipe.Yield);

    /// <summary>
    /// The Ingredient Uses shown under a Step, such as "butter, 2 tbsp" or "sugar, ½ cup (of 1 cup)".
    /// Unticked suggestions on a Draft, and uses of lines that are gone, aren't shown.
    /// </summary>
    public IReadOnlyList<ShownUse> UsesFor(Step step)
    {
        var lines = Ingredients.SelectMany(group => group.Lines).ToDictionary(shown => shown.Line.Id);
        return UsesOf(step)
            .Where(use => !use.Unticked && lines.ContainsKey(use.LineId))
            .Select(use =>
            {
                var shown = lines[use.LineId];
                var lineAmount = Amount(shown.Line.Quantity, shown.Line.Unit);
                var text = !use.HasAmount
                    ? (lineAmount.Length == 0 ? shown.IngredientName : $"{shown.IngredientName}, {MidSentence(lineAmount)}")
                    : $"{shown.IngredientName}, {MidSentence(Amount(use.Quantity, use.Unit))}{(lineAmount.Length == 0 ? "" : $" (of {MidSentence(lineAmount)})")}";
                return new ShownUse(use, shown, text);
            })
            .ToList();
    }

    /// <summary>
    /// True when no Step has any Ingredient Uses saved (a Recipe saved before they existed, or never
    /// reviewed for them), so <see cref="UsesFor"/> shows the uses suggested from each Step's text.
    /// Opening the Recipe in the editor and saving it keeps them.
    /// </summary>
    public bool UsesAreSuggested => Recipe.Lines.Count > 0 && Recipe.Steps.Count > 0 && Recipe.Steps.All(step => step.Uses.Count == 0);

    private IReadOnlyDictionary<string, IReadOnlyList<IngredientUse>>? _suggestedUses;

    private IReadOnlyList<IngredientUse> UsesOf(Step step)
    {
        if (!UsesAreSuggested)
        {
            return step.Uses;
        }

        _suggestedUses ??= SuggestUses();
        return _suggestedUses.GetValueOrDefault(step.Id) ?? [];
    }

    private Dictionary<string, IReadOnlyList<IngredientUse>> SuggestUses()
    {
        var candidates = Recipe.Lines.Select(line => new UseCandidate(line.Id, NamesOf(line))).ToList();
        return Recipe.Steps.ToDictionary(
            step => step.Id,
            step => (IReadOnlyList<IngredientUse>)IngredientUseSuggester.Suggest(step.Text, candidates).Select(id => new IngredientUse(id)).ToList());
    }

    private IReadOnlyList<string> NamesOf(IngredientLine line)
    {
        var names = new List<string> { line.SuggestedName };
        if (line.IngredientId is not null && _ingredients.TryGetValue(line.IngredientId, out var ingredient))
        {
            names.Add(ingredient.Name);
            names.AddRange(ingredient.Aliases);
        }

        return names.Where(name => name.Length > 0).ToList();
    }

    // "Pinch" was capitalised at the start of "Pinch of salt"; after "salt, " it reads as "pinch".
    private static string MidSentence(string amount) =>
        amount.Length > 0 && char.IsUpper(amount[0]) && (amount.Length == 1 || !char.IsUpper(amount[1]))
            ? char.ToLowerInvariant(amount[0]) + amount[1..]
            : amount;

    private string Amount(Quantity? quantity, string unit) =>
        string.Join(" ", new[] { quantity is null ? null : Scale.Format(quantity), unit }.Where(part => !string.IsNullOrEmpty(part)));

    /// <summary>The Recipe's Tags, by Tag Group in the Cook's order, ungrouped last; groups it has no Tags in are left out.</summary>
    public IReadOnlyList<GroupedTags<Tag>> Tags { get; }

    /// <summary>The Notes on the whole Recipe, newest first.</summary>
    public IReadOnlyList<Note> RecipeNotes => NotesFor(null);

    /// <summary>The Notes on an Ingredient Line or Step (or on the whole Recipe, for null), newest first.</summary>
    public IReadOnlyList<Note> NotesFor(string? targetId) =>
        Recipe.Notes.Where(note => note.TargetId == targetId).Reverse().ToList();

    /// <summary>
    /// The Notes Cooking Mode shows with a Step: the Step's own, then those on the Ingredient Lines
    /// it uses, each newest first. <see cref="StepNote.About"/> names the line, or is null for the Step.
    /// </summary>
    public IReadOnlyList<StepNote> NotesForStep(Step step) =>
        NotesFor(step.Id).Select(note => new StepNote(null, note))
            .Concat(UsesFor(step).SelectMany(use => NotesFor(use.Use.LineId).Select(note => new StepNote(use.Line.IngredientName, note))))
            .ToList();

    /// <summary>The Source as shown after "From", or null if the Recipe has none.</summary>
    public ShownSource? Source { get; }

    internal static ShownSource? ShowSource(Source? source, Book? book) => source switch
    {
        null => null,
        { Kind: SourceKind.Book } => new ShownSource(SourceKind.Book, book?.Title ?? "Unknown Book", null, source.Page),
        { Kind: SourceKind.Other } => new ShownSource(SourceKind.Other, source.Text, null, ""),
        _ => new ShownSource(
            source.Kind,
            source.Name.Length > 0 ? source.Name : RecipeTracker.Core.Recipes.Source.HostName(source.Url) ?? source.Url,
            Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? source.Url : null,
            ""),
    };

    /// <summary>The Ingredient Lines grouped as shown: the lines with no section first (when there are any), then each section.</summary>
    public IReadOnlyList<IngredientGroup> Ingredients { get; }

    private ShownLine ShowLine(IngredientLine line, IReadOnlyDictionary<string, Ingredient> ingredients)
    {
        var name = line.IngredientId is null ? line.SuggestedName
            : ingredients.TryGetValue(line.IngredientId, out var ingredient) ? ingredient.Name
            : "Unknown Ingredient";
        if (name.Length == 0)
        {
            name = line.OriginalText;
        }

        var amount = Amount(line.Quantity, line.Unit);

        // "pinch of salt" rather than "pinch salt" when there's a unit but no number.
        var text = amount.Length == 0 ? name : line.Quantity is null ? $"{amount} of {name}" : $"{amount} {name}";
        if (line.Preparation.Length > 0)
        {
            text += $", {line.Preparation}";
        }

        if (line.Optional)
        {
            text += " (optional)";
        }

        return new ShownLine(line, name, text);
    }
}

/// <summary>An Ingredient Section's heading (null for the lines before the first one) and its lines.</summary>
public sealed record IngredientGroup(string? Heading, IReadOnlyList<ShownLine> Lines);

/// <summary>A Note shown with a Step in Cooking Mode, and the Ingredient it's about (null for a Note on the Step itself).</summary>
public sealed record StepNote(string? About, Note Note);

/// <summary>A Step's Ingredient Use with its line and the use written out, such as "sugar, ½ cup (of 1 cup)".</summary>
public sealed record ShownUse(IngredientUse Use, ShownLine Line, string Text);

/// <summary>An Ingredient Line with its Ingredient's name and the line written out, such as "1½ cups flour, sifted".</summary>
public sealed record ShownLine(IngredientLine Line, string IngredientName, string Text);
