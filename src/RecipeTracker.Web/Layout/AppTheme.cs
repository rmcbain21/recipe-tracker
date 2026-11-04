using MudBlazor;

namespace RecipeTracker.Web.Layout;

/// <summary>The app's colours, a light and a dark version: paprika with an herb-green accent on warm paper.</summary>
public static class AppTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#B5452B", PrimaryDarken = "#8F3420", PrimaryLighten = "#D06A4F",
            Secondary = "#5E7D3A", SecondaryDarken = "#47602B", SecondaryLighten = "#7E9C5A",
            Background = "#FBF6EF", Surface = "#FFFFFF",
            AppbarBackground = "#8F3420", AppbarText = "#FFF7F2",
            DrawerBackground = "#F3EADF", DrawerText = "#6E2817", DrawerIcon = "#6E2817",
            TextPrimary = "#2B211D", TextSecondary = "#6E6058", ActionDefault = "#6E6058", Divider = "#E5D9CC",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#EE8A6C", PrimaryDarken = "#D46B4D", PrimaryLighten = "#F6AE97", PrimaryContrastText = "#2C0F07",
            Secondary = "#A9C786", SecondaryContrastText = "#16210B",
            Background = "#1B1614", Surface = "#27201D",
            AppbarBackground = "#211A17", AppbarText = "#F5E6DE",
            DrawerBackground = "#221B18", DrawerText = "#F6AE97", DrawerIcon = "#F6AE97",
            TextPrimary = "#EFE5DF", TextSecondary = "#B3A399", ActionDefault = "#B3A399", Divider = "#3D322D",
        },
    };
}
