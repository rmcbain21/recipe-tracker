using System.Globalization;
using System.Text.RegularExpressions;
using RecipeTracker.Core.Recipes;

namespace RecipeTracker.Core.Parsing;

/// <summary>
/// Reads recipe text pasted from a website, a video description or a book page (via Google Lens).
/// It finds the ingredients and the steps from headings such as "Ingredients" and "Method", or,
/// with no headings, from the shape of each line: lines that start with a quantity are ingredients.
/// </summary>
public sealed partial class RuleBasedRecipeParser : IRecipeParser
{
    private enum Region
    {
        Preamble,
        Ingredients,
        Steps,
        Ignored,
    }

    private static readonly string[] IngredientHeadings =
        ["ingredients", "ingredient", "ingredient list", "what you'll need", "what you need", "you'll need", "you will need"];

    private static readonly string[] StepHeadings =
    [
        "instructions", "instruction", "method", "directions", "direction", "steps", "preparation",
        "how to make it", "how to make", "procedure", "to make", "recipe instructions", "cooking instructions",
    ];

    private static readonly string[] StopHeadings =
    [
        "notes", "note", "recipe notes", "nutrition", "nutrition facts", "nutrition information", "equipment",
        "tips", "video", "storage", "to store", "serving suggestions", "faq", "faqs", "did you make this recipe?",
    ];

    public ParsedRecipe Parse(string text)
    {
        var lines = text.Replace("\r", "").Split('\n')
            .Select(RecipeText.Tidy)
            .Where(line => line.Length > 0 && RecipeText.StripBullet(line).Length > 0)
            .ToList();

        var hasIngredientHeading = lines.Any(line => HeadingKind(line) == Region.Ingredients);
        var hasStepHeading = lines.Any(line => HeadingKind(line) == Region.Steps);

        var preamble = new List<string>();
        var ingredientLines = new List<string>();
        var stepLines = new List<string>();
        Yield? yield = null;
        var times = new StatedTimes();

        var region = Region.Preamble;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var heading = HeadingKind(line);
            if (heading is not null)
            {
                region = heading.Value;
                continue;
            }

            if (region == Region.Ignored)
            {
                continue;
            }

            if (region != Region.Steps && TryReadFacts(lines, ref index, ref yield, ref times))
            {
                continue;
            }

            if (IsJunk(line))
            {
                // Social links and sign-offs at the end of a video description end the steps.
                if (region == Region.Steps && IsSignOff(line))
                {
                    region = Region.Ignored;
                }

                continue;
            }

            switch (region)
            {
                case Region.Preamble:
                    preamble.Add(line);
                    break;
                case Region.Ingredients when !hasStepHeading && LooksLikeStep(line) && !IngredientLineParser.LooksLikeIngredient(line):
                    region = Region.Steps;
                    stepLines.Add(line);
                    break;
                case Region.Ingredients:
                    ingredientLines.Add(line);
                    break;
                case Region.Steps:
                    stepLines.Add(line);
                    break;
            }
        }

        // With no ingredients heading, the ingredients are the preamble lines shaped like ingredients
        // (and any headings among them); with no steps heading either, the steps are the long lines.
        string title;
        if (!hasIngredientHeading)
        {
            var (titleLine, ingredients, steps) = SplitByShape(preamble, wantSteps: !hasStepHeading);
            title = titleLine;
            ingredientLines = ingredients;
            stepLines.InsertRange(0, steps);
        }
        else
        {
            title = PickTitle(preamble);
        }

        return new ParsedRecipe
        {
            Title = title,
            Yield = yield,
            StatedTimes = times,
            Sections = ReadSections(ingredientLines),
            Steps = ReadSteps(stepLines),
        };
    }

    private static (string Title, List<string> Ingredients, List<string> Steps) SplitByShape(List<string> lines, bool wantSteps)
    {
        var title = "";
        var ingredients = new List<string>();
        var steps = new List<string>();
        var seenIngredient = false;

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var nextIsIngredient = index + 1 < lines.Count && IngredientLineParser.LooksLikeIngredient(lines[index + 1]);

            if (IngredientLineParser.LooksLikeIngredient(line))
            {
                ingredients.Add(line);
                seenIngredient = true;
            }
            else if (IsSectionHeading(line) && nextIsIngredient)
            {
                ingredients.Add(line);
            }
            else if (wantSteps && (seenIngredient || title.Length > 0) && LooksLikeStep(line))
            {
                steps.Add(line);
            }
            else if (seenIngredient && ingredients.Count > 0 && IsContinuation(ingredients[^1], line))
            {
                ingredients[^1] += " " + line;
            }
            else if (seenIngredient && steps.Count == 0 && line.Split(' ').Length <= 5 && !EndsSentence(line) && !IsSectionHeading(line))
            {
                // A short line among the ingredients, such as "Flaky sea salt", is an ingredient with no quantity.
                ingredients.Add(line);
            }
            else if (title.Length == 0 && !seenIngredient && IsTitleLike(line))
            {
                title = line;
            }
            else if (title.Length > 0 && !seenIngredient && steps.Count == 0 && IsAllCaps(title) && IsAllCaps(line) && IsTitleLike(line))
            {
                title += " " + line;
            }
            else if (wantSteps && steps.Count > 0)
            {
                steps.Add(line);
            }
        }

        return (title, ingredients, steps);
    }

    private static List<ParsedSection> ReadSections(List<string> lines)
    {
        var sections = new List<ParsedSection>();
        string? heading = null;
        var current = new List<ParsedLine>();
        string? previous = null;
        var previousCount = 0;

        foreach (var line in lines)
        {
            if (IsSectionHeading(line))
            {
                if (current.Count > 0 || heading is not null)
                {
                    sections.Add(new ParsedSection(heading, current));
                }

                heading = RecipeText.StripBullet(line).TrimEnd(':').Trim();
                current = [];
                previous = null;
                continue;
            }

            // A line wrapped by a book scan or a narrow page: "1 cup butter, softened to room" / "temperature".
            if (previous is not null && IsContinuation(previous, line))
            {
                var joined = previous + " " + line;
                current.RemoveRange(current.Count - previousCount, previousCount);
                var rejoined = IngredientLineParser.Parse(joined);
                current.AddRange(rejoined);
                previous = joined;
                previousCount = rejoined.Count;
                continue;
            }

            var parsed = IngredientLineParser.Parse(line);
            current.AddRange(parsed);
            previous = line;
            previousCount = parsed.Count;
        }

        if (current.Count > 0 || heading is not null)
        {
            sections.Add(new ParsedSection(heading, current));
        }

        return sections;
    }

    private static List<string> ReadSteps(List<string> lines)
    {
        var steps = new List<string>();
        var numbered = lines.Any(line => RecipeText.StripStepNumber(RecipeText.StripBullet(line)).Numbered || RecipeText.IsBareStepNumber(line));
        var startNext = false;

        foreach (var raw in lines)
        {
            var line = RecipeText.StripBullet(raw);
            var bulleted = line.Length != raw.Length;
            if (RecipeText.IsBareStepNumber(line))
            {
                startNext = true;
                continue;
            }

            var (rest, hasNumber) = RecipeText.StripStepNumber(line);
            if (rest.Length == 0)
            {
                startNext = true;
                continue;
            }

            var startsStep = numbered
                ? hasNumber || startNext || bulleted || steps.Count == 0
                : steps.Count == 0 || bulleted || !(char.IsLower(rest[0]) && !EndsSentence(steps[^1]));

            if (startsStep)
            {
                steps.Add(rest);
            }
            else
            {
                steps[^1] += " " + rest;
            }

            startNext = false;
        }

        return steps;
    }

    // Yield and Stated Times, as "Serves 4" or "Prep Time: 15 minutes", or as a label line with its value on the next lines.
    private static bool TryReadFacts(List<string> lines, ref int index, ref Yield? yield, ref StatedTimes times)
    {
        var line = lines[index];
        var label = FactLabel().Match(line);
        if (!label.Success)
        {
            return false;
        }

        var value = label.Groups["value"].Value.Trim();
        var consumed = 0;
        while (value.Length == 0 || (BareNumber().IsMatch(value) && index + consumed + 1 < lines.Count && UnitWordOnly().IsMatch(lines[index + consumed + 1])))
        {
            if (index + consumed + 1 >= lines.Count || lines[index + consumed + 1].Length > 30)
            {
                break;
            }

            consumed++;
            value = $"{value} {lines[index + consumed]}".Trim();
        }

        var kind = label.Groups["label"].Value.ToLowerInvariant();
        if (kind.EndsWith("time", StringComparison.Ordinal) || kind == "total")
        {
            var minutes = ReadMinutes(value);
            if (minutes is null)
            {
                return false;
            }

            times = kind switch
            {
                _ when kind.StartsWith("prep", StringComparison.Ordinal) => times with { PrepMinutes = minutes },
                _ when kind.StartsWith("total", StringComparison.Ordinal) => times with { TotalMinutes = minutes },
                _ => times with { CookMinutes = minutes },
            };
        }
        else
        {
            var amount = LeadingNumber().Match(value);
            if (!amount.Success && value.Length == 0)
            {
                return false;
            }

            var count = amount.Success ? decimal.Parse(amount.Groups["low"].Value, CultureInfo.InvariantCulture) : (decimal?)null;
            var rest = amount.Success ? value[amount.Length..].Trim() : value;
            if (rest.Length == 0 && kind is "serves" or "servings")
            {
                rest = "servings";
            }

            yield ??= new Yield(count, rest);
        }

        index += consumed;
        return true;
    }

    private static int? ReadMinutes(string text)
    {
        var match = Duration().Match(text);
        if (!match.Success || (!match.Groups["hours"].Success && !match.Groups["minutes"].Success))
        {
            return null;
        }

        var hours = match.Groups["hours"].Success ? int.Parse(match.Groups["hours"].Value, CultureInfo.InvariantCulture) : 0;
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture) : 0;
        return hours * 60 + minutes;
    }

    private static Region? HeadingKind(string line)
    {
        var heading = HeadingText(line);
        if (heading.Length > 40)
        {
            return null;
        }

        if (Matches(heading, IngredientHeadings))
        {
            return Region.Ingredients;
        }

        if (Matches(heading, StepHeadings))
        {
            return Region.Steps;
        }

        return Matches(heading, StopHeadings) ? Region.Ignored : null;
    }

    // "Ingredients", "INGREDIENTS:", "Ingredients (serves 4)" and "Ingredients for 4" all count.
    private static bool Matches(string heading, string[] headings) =>
        headings.Any(h => heading == h || (heading.StartsWith(h + " ", StringComparison.Ordinal) && heading.Split(' ').Length <= h.Split(' ').Length + 3));

    private static string HeadingText(string line) =>
        RecipeText.StripBullet(line).TrimEnd(':', ' ').ToLowerInvariant();

    private static bool IsSectionHeading(string line)
    {
        var text = RecipeText.StripBullet(line);
        if (IngredientLineParser.LooksLikeIngredient(text) || text.Split(' ').Length > 6)
        {
            return false;
        }

        return text.EndsWith(':') || text.StartsWith("for the ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeStep(string line)
    {
        var text = RecipeText.StripBullet(line);
        if (RecipeText.StripStepNumber(text).Numbered || RecipeText.IsBareStepNumber(text))
        {
            return true;
        }

        return text.Split(' ').Length >= 6 && !IngredientLineParser.LooksLikeIngredient(text);
    }

    private static bool IsTitleLike(string line) =>
        line.Length <= 80 && line.Split(' ').Length <= 12 && !EndsSentence(line) && !line.Contains('!') &&
        !IngredientLineParser.LooksLikeIngredient(line) && !IsJunk(line) && !line.Contains(':');

    private static bool EndsSentence(string text) => text.TrimEnd().EndsWith('.') || text.TrimEnd().EndsWith('?') || text.TrimEnd().EndsWith('!');

    private static string PickTitle(List<string> lines)
    {
        var index = lines.FindIndex(IsTitleLike);
        if (index < 0)
        {
            return "";
        }

        // A book title set in capitals over two lines: "BROWN BUTTER" / "CHOCOLATE CHIP COOKIES".
        var title = lines[index];
        while (index + 1 < lines.Count && IsAllCaps(title) && IsAllCaps(lines[index + 1]) && IsTitleLike(lines[index + 1]))
        {
            title += " " + lines[++index];
        }

        return title;
    }

    private static bool IsAllCaps(string line) => line.Any(char.IsLetter) && !line.Any(char.IsLower);

    // A wrapped ingredient line: the first part ends with a comma, "and" or an open bracket, or it has a
    // preparation after a comma and the next part is a few lower-case words ("at room" / "temperature").
    private static bool IsContinuation(string previous, string line) =>
        !IngredientLineParser.LooksLikeIngredient(line) && !IsSectionHeading(line) &&
        (ContinuesPrevious(previous) || (previous.Contains(',') && char.IsLower(line[0]) && line.Split(' ').Length <= 4));

    // A line that clearly runs on to the next: it ends with a comma, "and"/"or", or an open bracket.
    private static bool ContinuesPrevious(string line) =>
        line.EndsWith(',') || line.EndsWith(" and", StringComparison.OrdinalIgnoreCase) || line.EndsWith(" or", StringComparison.OrdinalIgnoreCase) ||
        line.Count(c => c == '(') > line.Count(c => c == ')');

    private static bool IsJunk(string line) => JunkLine().IsMatch(line) || IsSignOff(line);

    private static bool IsSignOff(string line) => SignOff().IsMatch(line);

    [GeneratedRegex(@"^(?<label>prep(?:aration)? time|cook(?:ing)? time|bake time|baking time|total time|total|serves|servings|yield|yields|makes)\b\s*:?\s*(?<value>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex FactLabel();

    // The first duration in the text, with at least hours or minutes.
    [GeneratedRegex(@"(?:(?<hours>\d+)\s*(?:hours?|hrs?|h)\b\s*(?:(?<minutes>\d+)\s*(?:minutes?|mins?|m)\b)?|(?<minutes>\d+)\s*(?:minutes?|mins?|m)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex Duration();

    [GeneratedRegex(@"^(?<low>\d+(?:\.\d+)?)(?:\s*(?:-|–|to)\s*\d+(?:\.\d+)?)?")]
    private static partial Regex LeadingNumber();

    [GeneratedRegex(@"^\d+$")]
    private static partial Regex BareNumber();

    [GeneratedRegex(@"^(?:minutes?|mins?|hours?|hrs?|servings?|people|portions?)$", RegexOptions.IgnoreCase)]
    private static partial Regex UnitWordOnly();

    [GeneratedRegex(@"jump to|print recipe|pin recipe|save recipe|rate this|\bvotes?\b|\breviews?\b|^by\s|^author\b|^course\b|^cuisine\b|^keyword|^calories|kcal\b|^(?:us customary|metric)(?:\s+(?:us customary|metric))*$|^\d+x(?:\s+\d+x)*$|^\d{1,2}:\d{2}\b|^#\S", RegexOptions.IgnoreCase)]
    private static partial Regex JunkLine();

    [GeneratedRegex(@"https?://|www\.|\bsubscribe\b|\binstagram\b|\bfacebook\b|\btiktok\b|\bpatreon\b|\baffiliate\b|thanks for watching", RegexOptions.IgnoreCase)]
    private static partial Regex SignOff();
}
