using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Parsing;

/// <summary>
/// Turns pasted recipe text into a proposed Recipe for the Cook to review. The rule-based parser
/// is the only one for now; an LLM parser could sit behind this later (ADR 0001).
/// </summary>
public interface IRecipeParser
{
    ParsedRecipe Parse(string text);
}

/// <summary>What the parser found in pasted text. Anything it couldn't find is blank or empty.</summary>
public sealed record ParsedRecipe
{
    public string Title { get; init; } = "";

    public Yield? Yield { get; init; }

    public StatedTimes StatedTimes { get; init; } = new();

    /// <summary>The ingredients in groups; the first group has no heading when lines come before any heading.</summary>
    public IReadOnlyList<ParsedSection> Sections { get; init; } = [];

    public IReadOnlyList<string> Steps { get; init; } = [];
}

/// <summary>An Ingredient Section heading (null before the first heading) and its lines.</summary>
public sealed record ParsedSection(string? Heading, IReadOnlyList<ParsedLine> Lines);
