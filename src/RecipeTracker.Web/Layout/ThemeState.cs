using RecipeTracker.Core.Preferences;

namespace RecipeTracker.Web.Layout;

/// <summary>
/// The palette, appearance and text size in use, shared so that a change made in Settings shows
/// straight away: the Settings page saves the choice and sets it here, and MainLayout applies it.
/// </summary>
public sealed class ThemeState
{
    public ColourPalette Palette { get; private set; }

    public Appearance Appearance { get; private set; }

    // Default until the saved settings load, not the enum's first member (Extra small).
    public TextSize TextSize { get; private set; } = TextSize.Default;

    public event Action? Changed;

    public void Set(ColourPalette palette, Appearance appearance, TextSize textSize)
    {
        Palette = palette;
        Appearance = appearance;
        TextSize = textSize;
        Changed?.Invoke();
    }

    /// <summary>
    /// The page's root font size. MudBlazor sizes its text in rem, so this scales all of it;
    /// spacing set in px stays the same.
    /// </summary>
    public static string RootFontSize(TextSize textSize) => textSize switch
    {
        TextSize.ExtraSmall => "81.25%",
        TextSize.Small => "87.5%",
        TextSize.Large => "112.5%",
        TextSize.ExtraLarge => "125%",
        _ => "100%",
    };
}
