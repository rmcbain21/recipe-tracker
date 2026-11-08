using System.Text.RegularExpressions;
using RecipeTracker.Core.Ingredients;

namespace RecipeTracker.Core.Parsing;

/// <summary>A Recipe's Ingredient Line as the suggester sees it: its id and the names it could be called.</summary>
/// <param name="Names">The Ingredient's name and aliases (or the name the parser read, for a new one).</param>
public sealed record UseCandidate(string LineId, IReadOnlyList<string> Names);

/// <summary>
/// Suggests which Ingredient Lines a Step uses, from the Ingredient names and aliases that appear in
/// its text. Pure, so that the review screen and editor can call it as the Cook types.
/// </summary>
public static class IngredientUseSuggester
{
    /// <summary>
    /// The lines whose Ingredient is named in the Step's text, in the order given. Names match as
    /// whole words, ignoring case, spacing and plurals ("eggs" names "egg", "berry" names
    /// "berries"). Longer names win where they overlap, so "brown sugar" in the text names only
    /// the brown sugar line, not the sugar line as well.
    /// </summary>
    public static IReadOnlyList<string> Suggest(string stepText, IEnumerable<UseCandidate> lines)
    {
        var candidates = lines.ToList();
        if (string.IsNullOrWhiteSpace(stepText) || candidates.Count == 0)
        {
            return [];
        }

        var found = new List<(string LineId, int Start, int Length)>();
        foreach (var line in candidates)
        {
            foreach (var name in line.Names.Where(n => !string.IsNullOrWhiteSpace(n)))
            {
                foreach (Match match in Pattern(name).Matches(stepText))
                {
                    found.Add((line.LineId, match.Index, match.Length));
                }
            }
        }

        // Longest first; a shorter name inside a span already taken doesn't count.
        var taken = new List<(int Start, int End)>();
        var used = new HashSet<string>();
        foreach (var (lineId, start, length) in found.OrderByDescending(f => f.Length).ThenBy(f => f.Start))
        {
            var end = start + length;
            if (taken.Any(t => start < t.End && t.Start < end && !(t.Start == start && t.End == end)))
            {
                continue;
            }

            taken.Add((start, end));
            used.Add(lineId);
        }

        return candidates.Where(line => used.Contains(line.LineId)).Select(line => line.LineId).Distinct().ToList();
    }

    // "brown sugar" → \bbrown\s+sugar(?:e?s)?\b; "berries" also gets "berry"; "tomatoes" also "tomato".
    private static Regex Pattern(string name)
    {
        var lower = IngredientNames.Tidy(name).ToLowerInvariant();
        var stems = new HashSet<string> { lower };
        if (lower.EndsWith("ies", StringComparison.Ordinal) && lower.Length > 4)
        {
            stems.Add(lower[..^3] + "y");
        }

        if (lower.EndsWith("es", StringComparison.Ordinal) && lower.Length > 3)
        {
            stems.Add(lower[..^2]);
        }

        if (lower.EndsWith('s') && !lower.EndsWith("ss", StringComparison.Ordinal) && lower.Length > 2)
        {
            stems.Add(lower[..^1]);
        }

        var alternatives = stems.Select(stem =>
        {
            var escaped = Regex.Escape(stem).Replace("\\ ", "\\s+", StringComparison.Ordinal);
            return stem.EndsWith('y') ? $"{escaped[..^1]}(?:y|ies)" : $"{escaped}(?:e?s)?";
        });
        return new Regex($@"(?<![\w-])(?:{string.Join('|', alternatives)})(?![\w-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
