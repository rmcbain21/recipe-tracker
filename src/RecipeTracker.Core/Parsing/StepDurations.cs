using System.Text.RegularExpressions;
using RecipeTracker.Core.Quantities;

namespace RecipeTracker.Core.Parsing;

/// <summary>
/// A time found in a Step's text, such as "20 minutes" or "25-30 min", where it sits in the text,
/// so that Cooking Mode can show it as a timer chip. A range runs from <see cref="Low"/> to
/// <see cref="High"/>, and its timer starts at the low end.
/// </summary>
public sealed record StepDuration(int Start, int Length, TimeSpan Low, TimeSpan? High = null)
{
    public bool IsRange => High is not null;

    /// <summary>For showing, such as "20 min", "1 hr 30 min" or "25–30 min".</summary>
    public string Label => High is { } high && high != Low
        ? StepDurations.FormatRange(Low, high)
        : StepDurations.Format(Low);
}

/// <summary>
/// Finds the times in a Step's text ("simmer 20 minutes", "bake 25-30 min", "1½ hours", "an
/// hour", "1 hour 15 minutes"). Pure, so it's tested on its own.
/// </summary>
public static partial class StepDurations
{
    /// <summary>Longer than this isn't a timer anyone wants ("refrigerate for 3 days").</summary>
    private static readonly TimeSpan Longest = TimeSpan.FromHours(72);

    private static readonly Dictionary<string, decimal> Words = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = 1, ["an"] = 1, ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
        ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10, ["eleven"] = 11,
        ["twelve"] = 12, ["fifteen"] = 15, ["twenty"] = 20, ["thirty"] = 30, ["forty"] = 40,
        ["forty-five"] = 45, ["sixty"] = 60, ["ninety"] = 90, ["a couple of"] = 2, ["a couple"] = 2,
    };

    /// <summary>The times in the text, in the order they appear. Parts such as "1 hour 15 minutes" make one time.</summary>
    public static IReadOnlyList<StepDuration> Find(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var parts = new List<(int Start, int End, TimeSpan Low, TimeSpan? High, int Rank)>();
        foreach (Match match in Part().Matches(text))
        {
            if (TryRead(match, out var low, out var high))
            {
                parts.Add((match.Index, match.Index + match.Length, low, high, Rank(match.Groups["unit"].Value)));
            }
        }

        var found = new List<StepDuration>();
        for (var i = 0; i < parts.Count; i++)
        {
            var (start, end, low, high, rank) = parts[i];

            // "1 hour 15 minutes", "1 hr, 20 min", "2 minutes and 30 seconds": a larger unit then a
            // smaller one, with only a space, comma or "and" between, is one time.
            while (high is null && i + 1 < parts.Count && parts[i + 1].High is null && parts[i + 1].Rank < rank &&
                   Joiner().IsMatch(text[end..parts[i + 1].Start]))
            {
                i++;
                low += parts[i].Low;
                end = parts[i].End;
                rank = parts[i].Rank;
            }

            if (low > TimeSpan.Zero && (high ?? low) <= Longest)
            {
                found.Add(new StepDuration(start, end - start, low, high));
            }
        }

        return found;
    }

    /// <summary>A length of time for showing: "45 sec", "20 min", "1 hr", "1 hr 30 min".</summary>
    public static string Format(TimeSpan time)
    {
        time = TimeSpan.FromSeconds(Math.Round(time.TotalSeconds));
        var hours = (int)time.TotalHours;
        if (hours == 0 && time.Minutes == 0)
        {
            return $"{time.Seconds} sec";
        }

        var text = hours > 0 ? $"{hours} hr" : "";
        if (time.Minutes > 0)
        {
            text += (text.Length > 0 ? " " : "") + $"{time.Minutes} min";
        }

        if (time.Seconds > 0)
        {
            text += $" {time.Seconds} sec";
        }

        return text;
    }

    /// <summary>A range for showing: "25–30 min", or "1 hr – 1 hr 30 min" once it reaches an hour.</summary>
    internal static string FormatRange(TimeSpan low, TimeSpan high)
    {
        if (low.TotalHours < 1 && high.TotalHours < 1 && low.Seconds == 0 && high.Seconds == 0)
        {
            return $"{(int)low.TotalMinutes}–{(int)high.TotalMinutes} min";
        }

        return $"{Format(low)} – {Format(high)}";
    }

    private static bool TryRead(Match match, out TimeSpan low, out TimeSpan? high)
    {
        low = TimeSpan.Zero;
        high = null;

        decimal lowAmount;
        decimal? highAmount = null;
        if (match.Groups["word"].Success)
        {
            var word = Regex.Replace(match.Groups["word"].Value, @"\s+", " ");

            // "Add a second egg" isn't a timer.
            if (word.ToLowerInvariant() is "a" or "an" && Rank(match.Groups["unit"].Value) == 1)
            {
                return false;
            }
            lowAmount = word.Contains("half", StringComparison.OrdinalIgnoreCase) ? 0.5m : Words[word];
        }
        else
        {
            // Quantity reads fractions, "1-1/2" and ranges the way Ingredient Lines are read.
            var number = Regex.Replace(match.Groups["number"].Value, @"\s+or\s+", " to ", RegexOptions.IgnoreCase);
            if (!Quantity.TryParse(number, out var quantity) || quantity is null)
            {
                return false;
            }

            lowAmount = quantity.Low;
            highAmount = quantity.High;
        }

        // "an hour and a half", "1 and a half hours".
        if (match.Groups["half"].Success || match.Groups["halfAfter"].Success)
        {
            lowAmount += 0.5m;
            highAmount += 0.5m;
        }

        var unit = Unit(match.Groups["unit"].Value);
        low = unit * (double)lowAmount;
        high = highAmount is { } h ? unit * (double)h : null;
        return true;
    }

    private static TimeSpan Unit(string unit) => Rank(unit) switch
    {
        3 => TimeSpan.FromHours(1),
        2 => TimeSpan.FromMinutes(1),
        _ => TimeSpan.FromSeconds(1),
    };

    private static int Rank(string unit) => char.ToLowerInvariant(unit[0]) switch
    {
        'h' => 3,
        'm' => 2,
        _ => 1,
    };

    // A number or a range of numbers ("20", "1½", "1 1/2", "25-30", "10 to 15", "2 or 3") or a word
    // ("an", "ten", "half an", "a couple of"), then an optional "and a half", then a unit. The unit
    // may be joined by a hyphen, as in "a 20-minute rest".
    private const string Number = @"(?:\d+(?:\.\d+)?(?:\s?[½⅓⅔¼¾⅛⅜⅝⅞]|\s\d+/\d+)?|\d+/\d+|[½⅓⅔¼¾⅛⅜⅝⅞])";

    [GeneratedRegex(
        @"(?<![\w/.½⅓⅔¼¾⅛⅜⅝⅞-])(?:" +
        @"(?<number>" + Number + @"(?:\s*(?:-|–|—|to|or)\s*" + Number + @")?)" +
        @"|(?<word>half\s+an?|an?\s+half|a\s+couple(?:\s+of)?|forty-five|an?|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|fifteen|twenty|thirty|forty|sixty|ninety)" +
        @")(?<half>\s+and\s+a\s+half)?[\s-]?" +
        @"(?<unit>hours?|hrs?|h|minutes?|mins?|seconds?|secs?)\b" +
        @"(?<halfAfter>\s+and\s+a\s+half)?",
        RegexOptions.IgnoreCase)]
    private static partial Regex Part();

    [GeneratedRegex(@"^\s*,?\s*(?:and\s+)?$", RegexOptions.IgnoreCase)]
    private static partial Regex Joiner();
}
