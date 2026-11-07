using RecipeTracker.Core.Ingredients;

namespace RecipeTracker.Core.Recipes;

/// <summary>A Recipe as its recipe page shows it: ingredients grouped by section, with their Ingredients' names.</summary>
public sealed class RecipeView
{
    internal RecipeView(Recipe recipe, IReadOnlyDictionary<string, Ingredient> ingredients)
    {
        Recipe = recipe;

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

    public Recipe Recipe { get; }

    /// <summary>The Ingredient Lines grouped as shown: the lines with no section first (when there are any), then each section.</summary>
    public IReadOnlyList<IngredientGroup> Ingredients { get; }

    private static ShownLine ShowLine(IngredientLine line, IReadOnlyDictionary<string, Ingredient> ingredients)
    {
        var name = ingredients.TryGetValue(line.IngredientId, out var ingredient) ? ingredient.Name : "Unknown Ingredient";
        var amount = string.Join(" ", new[] { line.Quantity?.ToString(), line.Unit }.Where(part => !string.IsNullOrEmpty(part)));
        var text = amount.Length == 0 ? name : $"{amount} {name}";
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

/// <summary>An Ingredient Line with its Ingredient's name and the line written out, such as "1½ cups flour, sifted".</summary>
public sealed record ShownLine(IngredientLine Line, string IngredientName, string Text);
