using System.Text.RegularExpressions;

namespace RecipeTracker.Core.Parsing;

/// <summary>Small clean-ups for lines of pasted recipe text.</summary>
internal static partial class RecipeText
{
    /// <summary>Trims, collapses spaces and removes Markdown emphasis and heading marks.</summary>
    public static string Tidy(string line)
    {
        var tidy = line.Replace(' ', ' ').Replace("**", "").Replace("__", "");
        tidy = MarkdownHeading().Replace(tidy, "");
        return Whitespace().Replace(tidy, " ").Trim();
    }

    /// <summary>Removes a leading bullet or checkbox, such as "-", "•" or the "▢" recipe sites put before ingredients.</summary>
    public static string StripBullet(string line) => Bullet().Replace(line, "").Trim();

    /// <summary>
    /// Removes a leading step number such as "1.", "2)", "Step 3:" or "Step 4 -". Returns the
    /// rest, and whether a number was there.
    /// </summary>
    public static (string Text, bool Numbered) StripStepNumber(string line)
    {
        var match = StepNumber().Match(line);
        return match.Success ? (line[match.Length..].Trim(), true) : (line, false);
    }

    /// <summary>True if the line is only a step number, as some books and videos put on its own line.</summary>
    public static bool IsBareStepNumber(string line) => BareStepNumber().IsMatch(line);

    [GeneratedRegex(@"^#{1,6}\s+")]
    private static partial Regex MarkdownHeading();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^(?:[-–—*•·◦▪▢☐□✓✔>]+\s*)+")]
    private static partial Regex Bullet();

    [GeneratedRegex(@"^(?:step\s*\d+\s*[.):\-–]?|\d{1,2}\s*[.):]|\d{1,2}\s+[-–]\s)\s*", RegexOptions.IgnoreCase)]
    private static partial Regex StepNumber();

    [GeneratedRegex(@"^(?:step\s*)?\d{1,2}\s*[.):]?$", RegexOptions.IgnoreCase)]
    private static partial Regex BareStepNumber();
}
