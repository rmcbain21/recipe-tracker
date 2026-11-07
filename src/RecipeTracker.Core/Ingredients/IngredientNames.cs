using System.Text.RegularExpressions;

namespace RecipeTracker.Core.Ingredients;

/// <summary>
/// Compares Ingredient names the way the Cook would: ignoring case, extra spaces and a
/// trailing plural, so "Eggs", "egg" and " tomatoes " match "egg" and "tomato".
/// </summary>
internal static partial class IngredientNames
{
    public static string Tidy(string name) => Whitespace().Replace(name.Trim(), " ");

    public static bool Same(string a, string b) => Forms(a).Overlaps(Forms(b));

    /// <summary>True if <paramref name="text"/> appears in the name, ignoring case and spacing.</summary>
    public static bool Contains(string name, string text) =>
        Lower(name).Contains(Lower(text), StringComparison.Ordinal);

    private static string Lower(string name) => Tidy(name).ToLowerInvariant();

    // The name as written plus the name without a trailing "s" or "es", so that both
    // "olives"/"olive" and "tomatoes"/"tomato" meet in the middle.
    private static HashSet<string> Forms(string name)
    {
        var lower = Lower(name);
        var forms = new HashSet<string>(StringComparer.Ordinal) { lower };
        if (lower.EndsWith("es", StringComparison.Ordinal) && lower.Length > 3)
        {
            forms.Add(lower[..^2]);
        }

        if (lower.EndsWith('s') && !lower.EndsWith("ss", StringComparison.Ordinal) && lower.Length > 2)
        {
            forms.Add(lower[..^1]);
        }

        return forms;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
