using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RecipeTracker.Core.Quantities;

/// <summary>
/// An Ingredient Line's amount as its Source wrote it (ADR 0002): an exact number, or a range
/// from <see cref="Low"/> to <see cref="High"/>. A line with no quantity ("salt to taste") has
/// no Quantity at all, rather than a zero one.
/// </summary>
public sealed partial record Quantity(decimal Low, decimal? High = null)
{
    private static readonly (char Glyph, int Numerator, int Denominator)[] Fractions =
    [
        ('½', 1, 2),
        ('⅓', 1, 3), ('⅔', 2, 3),
        ('¼', 1, 4), ('¾', 3, 4),
        ('⅕', 1, 5), ('⅖', 2, 5), ('⅗', 3, 5), ('⅘', 4, 5),
        ('⅙', 1, 6), ('⅚', 5, 6),
        ('⅛', 1, 8), ('⅜', 3, 8), ('⅝', 5, 8), ('⅞', 7, 8),
    ];

    /// <summary>How close a value must be to a fraction to be shown as it, so that ⅓ survives being stored as 0.333….</summary>
    private const decimal FractionTolerance = 0.002m;

    public bool IsRange => High is not null;

    /// <summary>
    /// Reads a quantity such as "2", "1.5", "1½", "1 1/2", "½", "2-3" or "2 to 3". Blank text
    /// is valid and means no quantity, so <paramref name="quantity"/> is null.
    /// </summary>
    /// <returns>False if the text isn't a quantity.</returns>
    public static bool TryParse(string? text, out Quantity? quantity)
    {
        quantity = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var normalized = Normalize(text);
        var range = RangePattern().Match(normalized);
        if (range.Success)
        {
            if (!TryParseNumber(range.Groups["low"].Value, out var low) ||
                !TryParseNumber(range.Groups["high"].Value, out var high))
            {
                return false;
            }

            // "1-1/2" is how some recipes write one and a half, not a range down to a half.
            if (range.Groups["sep"].Value == "-" && IsWholeNumber(range.Groups["low"].Value) && high < 1 && high < low)
            {
                quantity = new Quantity(low + high);
                return true;
            }

            if (low <= 0 || high < low)
            {
                return false;
            }

            quantity = high == low ? new Quantity(low) : new Quantity(low, high);
            return true;
        }

        if (!TryParseNumber(normalized, out var exact) || exact <= 0)
        {
            return false;
        }

        quantity = new Quantity(exact);
        return true;
    }

    /// <summary>The quantity for showing: fractions as "1½", ranges as "2–3".</summary>
    public override string ToString() =>
        High is null ? FormatNumber(Low) : $"{FormatNumber(Low)}–{FormatNumber(High.Value)}";

    /// <summary>Shows a number as a whole number with a fraction glyph where one fits, otherwise as a decimal.</summary>
    public static string FormatNumber(decimal value)
    {
        var whole = decimal.Truncate(value);
        var rest = value - whole;
        if (rest < FractionTolerance)
        {
            return whole.ToString(CultureInfo.InvariantCulture);
        }

        if (1 - rest < FractionTolerance)
        {
            return (whole + 1).ToString(CultureInfo.InvariantCulture);
        }

        foreach (var (glyph, numerator, denominator) in Fractions)
        {
            if (Math.Abs(rest - (decimal)numerator / denominator) < FractionTolerance)
            {
                return whole == 0 ? glyph.ToString() : $"{whole.ToString(CultureInfo.InvariantCulture)}{glyph}";
            }
        }

        return Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Normalize(string text)
    {
        var builder = new StringBuilder(text.Trim().ToLowerInvariant());
        builder.Replace('⁄', '/').Replace('–', '-').Replace('—', '-');
        return WhitespacePattern().Replace(builder.ToString(), " ");
    }

    private static bool IsWholeNumber(string text) => text.All(char.IsAsciiDigit);

    private static bool TryParseNumber(string text, out decimal value)
    {
        value = 0;
        var match = NumberPattern().Match(text.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (match.Groups["whole"].Success)
        {
            value = decimal.Parse(match.Groups["whole"].Value, CultureInfo.InvariantCulture);
        }

        if (match.Groups["decimal"].Success)
        {
            value = decimal.Parse(match.Groups["decimal"].Value, CultureInfo.InvariantCulture);
        }

        if (match.Groups["glyph"].Success)
        {
            var (_, numerator, denominator) = Fractions.Single(f => f.Glyph == match.Groups["glyph"].Value[0]);
            value += (decimal)numerator / denominator;
        }

        if (match.Groups["num"].Success)
        {
            var denominatorValue = decimal.Parse(match.Groups["den"].Value, CultureInfo.InvariantCulture);
            if (denominatorValue == 0)
            {
                return false;
            }

            value += decimal.Parse(match.Groups["num"].Value, CultureInfo.InvariantCulture) / denominatorValue;
        }

        return true;
    }

    // One number: "2", "1.5", ".5", "1½", "1 ½", "½", "1 1/2" or "1/2".
    // A whole number before a slash fraction needs a space, so "11/2" isn't read as 1½.
    [GeneratedRegex(@"^(?:(?<decimal>\d*\.\d+)|(?<whole>\d+)|(?:(?<whole>\d+) ?)?(?<glyph>[½⅓⅔¼¾⅕⅖⅗⅘⅙⅚⅛⅜⅝⅞])|(?:(?<whole>\d+) )?(?<num>\d+)/(?<den>\d+))$")]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"^(?<low>[^-]+?)\s?(?<sep>-|to)\s?(?<high>[^-]+)$")]
    private static partial Regex RangePattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
