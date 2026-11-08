using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Recipes;

/// <summary>
/// A dish the Cook has captured, with its Ingredient Sections, Ingredient Lines and Steps nested
/// inside it. Lists are kept in the order the Cook sees them. Tags, Notes and Ingredient Uses
/// arrive in later slices.
/// </summary>
public sealed record Recipe : StoredRecord
{
    /// <summary>What the Cook calls it, such as "Weeknight chili". Never blank.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// True for a Recipe saved before its review was finished. A Draft's lines may have no
    /// Ingredient yet, or one that's only suggested.
    /// </summary>
    public bool IsDraft { get; init; }

    public Source? Source { get; init; }

    /// <summary>The ids of the Tags on the Recipe.</summary>
    public IReadOnlyList<string> TagIds { get; init; } = [];

    public string Description { get; init; } = "";

    /// <summary>How much it makes, such as "4 servings"; null if the Source doesn't say.</summary>
    public Yield? Yield { get; init; }

    public StatedTimes StatedTimes { get; init; } = new();

    /// <summary>When the Recipe was first saved. Recipes saved before this field existed have the default value.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The ingredient headings, in order. Lines before the first heading have no section.</summary>
    public IReadOnlyList<IngredientSection> Sections { get; init; } = [];

    /// <summary>
    /// Every Ingredient Line in the order shown: first the lines with no section, then each
    /// section's lines in section order.
    /// </summary>
    public IReadOnlyList<IngredientLine> Lines { get; init; } = [];

    /// <summary>The numbered Steps, in order. A Recipe's Steps are one list, not split by section.</summary>
    public IReadOnlyList<Step> Steps { get; init; } = [];

    /// <summary>The Cook's Notes on the Recipe, its lines and its Steps, in the order they were added.</summary>
    public IReadOnlyList<Note> Notes { get; init; } = [];
}

/// <summary>
/// The Cook's comment on a Recipe, an Ingredient Line or a Step, such as "used half the sugar,
/// still sweet enough". A Note never changes the Recipe.
/// </summary>
/// <param name="TargetId">The Ingredient Line's or Step's id, or null for a Note on the whole Recipe.</param>
public sealed record Note(string Id, string? TargetId, string Text, DateTimeOffset CreatedAt);

/// <summary>How much a Recipe makes: a number and what it counts ("4 servings"), or just a label ("one 9×13 pan").</summary>
public sealed record Yield(decimal? Amount, string Label)
{
    public override string ToString() =>
        Amount is null ? Label : $"{Quantity.FormatNumber(Amount.Value)} {Label}".Trim();
}

/// <summary>The prep, cook and total time the Source gives, in minutes. Each is optional.</summary>
public sealed record StatedTimes(int? PrepMinutes = null, int? CookMinutes = null, int? TotalMinutes = null)
{
    public bool IsEmpty => PrepMinutes is null && CookMinutes is null && TotalMinutes is null;

    /// <summary>Shows minutes as "45 min", "1 hr" or "1 hr 15 min".</summary>
    public static string Format(int minutes) => (minutes / 60, minutes % 60) switch
    {
        (0, var m) => $"{m} min",
        (var h, 0) => $"{h} hr",
        (var h, var m) => $"{h} hr {m} min",
    };
}

/// <summary>A heading that groups some of a Recipe's Ingredient Lines, such as "For the dough".</summary>
public sealed record IngredientSection(string Id, string Heading);

/// <summary>One line of a Recipe's ingredients, pointing to a shared Ingredient (ADR 0003).</summary>
public sealed record IngredientLine
{
    public required string Id { get; init; }

    /// <summary>The section it's under, or null for lines before the first heading.</summary>
    public string? SectionId { get; init; }

    /// <summary>The amount as written (ADR 0002), or null for lines such as "salt to taste".</summary>
    public Quantity? Quantity { get; init; }

    /// <summary>The unit as written, such as "cups" or "cloves"; blank for a count ("2 eggs").</summary>
    public string Unit { get; init; } = "";

    /// <summary>The shared Ingredient. Only a Draft's line can have none.</summary>
    public string? IngredientId { get; init; }

    /// <summary>
    /// On a Draft, true when the Ingredient (or <see cref="SuggestedName"/>) is the capture's
    /// suggestion and the Cook hasn't confirmed it yet.
    /// </summary>
    public bool MatchUnconfirmed { get; init; }

    /// <summary>On an unconfirmed line, the Ingredient name the parser read, such as "scallions".</summary>
    public string SuggestedName { get; init; } = "";

    /// <summary>How it's prepared, such as "diced" or "softened".</summary>
    public string Preparation { get; init; } = "";

    public bool Optional { get; init; }

    /// <summary>The text the line was captured from, kept so that a bad parse can be fixed.</summary>
    public string OriginalText { get; init; } = "";
}

/// <summary>One numbered instruction in a Recipe.</summary>
public sealed record Step(string Id, string Text);
