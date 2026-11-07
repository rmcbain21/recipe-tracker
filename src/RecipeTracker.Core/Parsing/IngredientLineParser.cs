using System.Text.RegularExpressions;
using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Parsing;

/// <summary>
/// Splits one line of recipe text, such as "1 1/2 cups all-purpose flour, sifted", into its
/// quantity, unit, Ingredient name, preparation and optional flag, keeping the original text.
/// </summary>
public static partial class IngredientLineParser
{
    private static readonly HashSet<string> Units = new(StringComparer.OrdinalIgnoreCase)
    {
        "cup", "cups", "c",
        "tablespoon", "tablespoons", "tbsp", "tbsps", "tbs", "tbl", "tbls", "tblsp",
        "teaspoon", "teaspoons", "tsp", "tsps",
        "ounce", "ounces", "oz",
        "pound", "pounds", "lb", "lbs",
        "gram", "grams", "g", "gr",
        "kilogram", "kilograms", "kg", "kilo", "kilos",
        "milliliter", "milliliters", "millilitre", "millilitres", "ml",
        "liter", "liters", "litre", "litres", "l",
        "quart", "quarts", "qt", "pint", "pints", "pt", "gallon", "gallons", "gal",
        "clove", "cloves", "can", "cans", "tin", "tins", "jar", "jars",
        "package", "packages", "pkg", "packet", "packets", "bag", "bags", "box", "boxes", "bottle", "bottles",
        "pinch", "pinches", "dash", "dashes", "stick", "sticks", "slice", "slices", "piece", "pieces",
        "bunch", "bunches", "sprig", "sprigs", "handful", "handfuls", "head", "heads", "stalk", "stalks",
        "sheet", "sheets", "drop", "drops",
    };

    // Size words read as the unit when there's no other, so "2 large eggs" is two eggs, not two "large eggs".
    private static readonly HashSet<string> Sizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "small", "medium", "large", "extra-large", "jumbo",
    };

    private static readonly HashSet<string> PreparationWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "chopped", "diced", "minced", "sliced", "grated", "shredded", "melted", "softened", "crushed",
        "peeled", "cubed", "julienned", "halved", "quartered", "beaten", "sifted", "packed", "toasted",
    };

    // An adverb takes the word after it with it: "finely chopped", "freshly ground".
    private static readonly HashSet<string> PreparationAdverbs = new(StringComparer.OrdinalIgnoreCase)
    {
        "finely", "roughly", "coarsely", "thinly", "freshly", "lightly", "firmly", "loosely",
    };

    /// <summary>
    /// Splits the line. Usually one line comes back; "salt and pepper to taste" comes back as two,
    /// one per Ingredient, sharing the original text.
    /// </summary>
    public static IReadOnlyList<ParsedLine> Parse(string text)
    {
        var original = RecipeText.Tidy(text);
        var rest = RecipeText.StripBullet(original);
        rest = NumberThenLetters().Replace(rest, "$1 $2");

        var optional = false;
        var optionalMatch = OptionalMarker().Match(rest);
        if (optionalMatch.Success)
        {
            optional = true;
            rest = RecipeText.Tidy(rest.Remove(optionalMatch.Index, optionalMatch.Length)).TrimEnd(',', ' ');
        }

        var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var (quantity, used) = ReadQuantity(tokens);
        tokens.RemoveRange(0, used);

        var unit = quantity is not null || (tokens.Count > 0 && IsUnit(tokens[0])) ? ReadUnit(tokens, quantity is not null) : "";
        if (unit.Length > 0 && tokens.Count > 0 && tokens[0].Equals("of", StringComparison.OrdinalIgnoreCase))
        {
            tokens.RemoveAt(0);
        }

        var (name, preparation) = SplitNameAndPreparation(string.Join(' ', tokens));

        var line = new ParsedLine(original, quantity, unit, name, preparation, optional);
        return SplitToTaste(line);
    }

    /// <summary>True if the line reads like an ingredient: it starts with a quantity or a unit, or says "to taste".</summary>
    public static bool LooksLikeIngredient(string text)
    {
        var rest = NumberThenLetters().Replace(RecipeText.StripBullet(RecipeText.Tidy(text)), "$1 $2");
        var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (tokens.Count == 0)
        {
            return false;
        }

        return ReadQuantity(tokens).Quantity is not null
            || (tokens.Count > 1 && IsUnit(tokens[0]) && !Sizes.Contains(tokens[0]))
            || ToTaste().IsMatch(rest)
            || OptionalMarker().IsMatch(rest);
    }

    private static (Quantity? Quantity, int Used) ReadQuantity(List<string> tokens)
    {
        // The longest run of leading words that reads as a quantity, so "2 to 3" beats "2".
        for (var count = Math.Min(4, tokens.Count); count >= 1; count--)
        {
            if (Quantity.TryParse(string.Join(' ', tokens.Take(count)), out var quantity) && quantity is not null)
            {
                return (quantity, count);
            }
        }

        // "a pinch of salt", "one can of beans"
        if (tokens.Count > 1 && tokens[0].ToLowerInvariant() is "a" or "an" or "one" && IsUnit(tokens[1]))
        {
            return (new Quantity(1), 1);
        }

        return (null, 0);
    }

    // The unit as written, with any size in brackets: "(14 oz) can", "cup (240 ml)".
    private static string ReadUnit(List<string> tokens, bool hasQuantity)
    {
        var parts = new List<string>();
        if (hasQuantity)
        {
            TakeBracket(tokens, parts);
        }

        if (tokens.Count > 1 && tokens[0].Equals("fl", StringComparison.OrdinalIgnoreCase) && tokens[1].TrimEnd('.').StartsWith("oz", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add($"{tokens[0]} {tokens[1].TrimEnd('.')}");
            tokens.RemoveRange(0, 2);
            TakeBracket(tokens, parts);
        }
        else if (tokens.Count > 1 && IsUnit(tokens[0]))
        {
            parts.Add(tokens[0].TrimEnd('.'));
            tokens.RemoveAt(0);
            TakeBracket(tokens, parts);
        }
        else if (hasQuantity && tokens.Count > 1 && Sizes.Contains(tokens[0]))
        {
            parts.Add(tokens[0]);
            tokens.RemoveAt(0);
        }

        return string.Join(' ', parts);
    }

    private static bool IsUnit(string token)
    {
        var word = token.TrimEnd('.');
        return word is "T" or "t" || Units.Contains(word);
    }

    // Moves a bracketed group such as "(14 oz)" from the front of the tokens into the unit.
    private static void TakeBracket(List<string> tokens, List<string> parts)
    {
        if (tokens.Count == 0 || !tokens[0].StartsWith('('))
        {
            return;
        }

        var end = tokens.FindIndex(token => token.EndsWith(')'));
        if (end < 0 || end == tokens.Count - 1)
        {
            return;
        }

        parts.Add(string.Join(' ', tokens.Take(end + 1)));
        tokens.RemoveRange(0, end + 1);
    }

    private static (string Name, string Preparation) SplitNameAndPreparation(string text)
    {
        var preparation = new List<string>();
        var comma = text.IndexOf(',');
        var name = comma < 0 ? text : text[..comma];
        var afterComma = comma < 0 ? "" : text[(comma + 1)..].Trim();

        // "butter (softened)": a bracket at the end of the name is preparation.
        var bracket = TrailingBracket().Match(name);
        if (bracket.Success)
        {
            preparation.Add(bracket.Groups["inside"].Value.Trim());
            name = name[..bracket.Index];
        }

        var phrase = TrailingPhrase().Match(name);
        if (phrase.Success)
        {
            preparation.Insert(0, phrase.Groups["phrase"].Value);
            name = name[..phrase.Index];
        }

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var leading = new List<string>();
        while (words.Count > 1)
        {
            if (PreparationAdverbs.Contains(words[0]) && words.Count > 2)
            {
                leading.AddRange(words.Take(2));
                words.RemoveRange(0, 2);
            }
            else if (PreparationWords.Contains(words[0]))
            {
                leading.Add(words[0]);
                words.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        if (leading.Count > 0)
        {
            preparation.Insert(0, string.Join(' ', leading));
        }

        if (afterComma.Length > 0)
        {
            preparation.Add(afterComma);
        }

        return (string.Join(' ', words).Trim().TrimEnd('.', ':'), string.Join(", ", preparation.Where(p => p.Length > 0)).TrimEnd('.'));
    }

    private static IReadOnlyList<ParsedLine> SplitToTaste(ParsedLine line)
    {
        if (line.Quantity is not null || line.Unit.Length > 0 || !line.Preparation.StartsWith("to taste", StringComparison.OrdinalIgnoreCase))
        {
            return [line];
        }

        var names = AndOrAmpersand().Split(line.Name).Select(name => name.Trim()).Where(name => name.Length > 0).ToList();
        return names.Count < 2 ? [line] : names.Select(name => line with { Name = name }).ToList();
    }

    // "2cups" → "2 cups", "500g" → "500 g", "1½tsp" → "1½ tsp".
    [GeneratedRegex(@"(\d|[½⅓⅔¼¾⅕⅖⅗⅘⅙⅚⅛⅜⅝⅞])([A-Za-z])")]
    private static partial Regex NumberThenLetters();

    [GeneratedRegex(@"\(\s*optional\s*\)|,?\s*\boptional\s*$|^optional:?\s+", RegexOptions.IgnoreCase)]
    private static partial Regex OptionalMarker();

    [GeneratedRegex(@"\bto taste\b", RegexOptions.IgnoreCase)]
    private static partial Regex ToTaste();

    [GeneratedRegex(@"\s*\((?<inside>[^()]*)\)\s*$")]
    private static partial Regex TrailingBracket();

    [GeneratedRegex(@"\s+(?<phrase>to taste|as needed|for (?:serving|garnish|garnishing|frying|greasing|dusting|the pan))\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingPhrase();

    [GeneratedRegex(@"\s+(?:and|&)\s+", RegexOptions.IgnoreCase)]
    private static partial Regex AndOrAmpersand();
}

/// <summary>One Ingredient Line as the parser read it, before it's matched to an Ingredient.</summary>
public sealed record ParsedLine(string OriginalText, Quantity? Quantity, string Unit, string Name, string Preparation, bool Optional);
