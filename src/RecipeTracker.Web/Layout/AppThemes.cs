using MudBlazor;
using RecipeTracker.Core.Preferences;

namespace RecipeTracker.Web.Layout;

/// <summary>The app's colour palettes, each with a light and a dark version, chosen in Settings.</summary>
public static class AppThemes
{
    /// <summary>A palette with its display name and its MudBlazor theme (light and dark).</summary>
    public sealed record Option(ColourPalette Palette, string Name, MudTheme Theme);

    public static readonly IReadOnlyList<Option> All =
    [
        new(ColourPalette.Paprika, "Paprika", Theme(
            new PaletteLight
            {
                Primary = "#B5452B", PrimaryDarken = "#8F3420", PrimaryLighten = "#D06A4F",
                Secondary = "#5E7D3A", SecondaryDarken = "#47602B", SecondaryLighten = "#7E9C5A",
                Background = "#FBF6EF", Surface = "#FFFFFF",
                AppbarBackground = "#8F3420", AppbarText = "#FFF7F2",
                DrawerBackground = "#F3EADF", DrawerText = "#6E2817", DrawerIcon = "#6E2817",
                TextPrimary = "#2B211D", TextSecondary = "#6E6058", ActionDefault = "#6E6058", Divider = "#E5D9CC",
            },
            new PaletteDark
            {
                Primary = "#EE8A6C", PrimaryDarken = "#D46B4D", PrimaryLighten = "#F6AE97", PrimaryContrastText = "#2C0F07",
                Secondary = "#A9C786", SecondaryContrastText = "#16210B",
                Background = "#1B1614", Surface = "#27201D",
                AppbarBackground = "#211A17", AppbarText = "#F5E6DE",
                DrawerBackground = "#221B18", DrawerText = "#F6AE97", DrawerIcon = "#F6AE97",
                TextPrimary = "#EFE5DF", TextSecondary = "#B3A399", ActionDefault = "#B3A399", Divider = "#3D322D",
            })),

        new(ColourPalette.BasilAndTomato, "Basil & Tomato", Theme(
            new PaletteLight
            {
                Primary = "#3F7A3A", PrimaryDarken = "#2D5C2A", PrimaryLighten = "#5E9A57",
                Secondary = "#C8382B", SecondaryDarken = "#A02A1F", SecondaryLighten = "#DE5E52",
                Background = "#F5F7EF", Surface = "#FFFFFF",
                AppbarBackground = "#2D5C2A", AppbarText = "#F3FAEF",
                DrawerBackground = "#E9EEDF", DrawerText = "#2D5C2A", DrawerIcon = "#2D5C2A",
                TextPrimary = "#1F2A1E", TextSecondary = "#5D6B5A", ActionDefault = "#5D6B5A", Divider = "#D9E0CF",
            },
            new PaletteDark
            {
                Primary = "#8CC97F", PrimaryDarken = "#6AAE5C", PrimaryLighten = "#B0DCA5", PrimaryContrastText = "#102A0C",
                Secondary = "#F27A6E", SecondaryContrastText = "#2E0905",
                Background = "#131A12", Surface = "#1D261B",
                AppbarBackground = "#161E15", AppbarText = "#E3F0DE",
                DrawerBackground = "#182016", DrawerText = "#B0DCA5", DrawerIcon = "#B0DCA5",
                TextPrimary = "#E4EBE1", TextSecondary = "#A2AE9E", ActionDefault = "#A2AE9E", Divider = "#2F3A2C",
            })),

        new(ColourPalette.Saffron, "Saffron", Theme(
            new PaletteLight
            {
                Primary = "#995C00", PrimaryDarken = "#754600", PrimaryLighten = "#C27F14",
                Secondary = "#A3201C", SecondaryDarken = "#7E1714", SecondaryLighten = "#C4423C",
                Background = "#FBF6E6", Surface = "#FFFDF6",
                AppbarBackground = "#7A4A00", AppbarText = "#FFF8E6",
                DrawerBackground = "#F2E9CC", DrawerText = "#6B4100", DrawerIcon = "#6B4100",
                TextPrimary = "#2B2312", TextSecondary = "#6F6248", ActionDefault = "#6F6248", Divider = "#E6DCC0",
            },
            new PaletteDark
            {
                Primary = "#F2B84B", PrimaryDarken = "#DA9B26", PrimaryLighten = "#F8D085", PrimaryContrastText = "#2A1A00",
                Secondary = "#F07A6E", SecondaryContrastText = "#2E0806",
                Background = "#1A1610", Surface = "#262017",
                AppbarBackground = "#1E1912", AppbarText = "#F7EAD0",
                DrawerBackground = "#201A13", DrawerText = "#F8D085", DrawerIcon = "#F8D085",
                TextPrimary = "#EFE6D6", TextSecondary = "#B5A68C", ActionDefault = "#B5A68C", Divider = "#3B3224",
            })),

        new(ColourPalette.Blueberry, "Blueberry", Theme(
            new PaletteLight
            {
                Primary = "#3B3F8F", PrimaryDarken = "#2A2D6B", PrimaryLighten = "#5C61B0",
                Secondary = "#B38F00", SecondaryDarken = "#8C7000", SecondaryLighten = "#D6B52A", SecondaryContrastText = "#1F1800",
                Background = "#F3F3F9", Surface = "#FFFFFF",
                AppbarBackground = "#2A2D6B", AppbarText = "#F1F1FB",
                DrawerBackground = "#E7E7F2", DrawerText = "#2A2D6B", DrawerIcon = "#2A2D6B",
                TextPrimary = "#1F2033", TextSecondary = "#5F6178", ActionDefault = "#5F6178", Divider = "#DADAE8",
            },
            new PaletteDark
            {
                Primary = "#A9ADF2", PrimaryDarken = "#8589D9", PrimaryLighten = "#C9CBF7", PrimaryContrastText = "#13153A",
                Secondary = "#F2D55C", SecondaryContrastText = "#2A2200",
                Background = "#111226", Surface = "#1B1C33",
                AppbarBackground = "#15162B", AppbarText = "#E3E4F7",
                DrawerBackground = "#16172D", DrawerText = "#C9CBF7", DrawerIcon = "#C9CBF7",
                TextPrimary = "#E5E6F2", TextSecondary = "#A2A4BD", ActionDefault = "#A2A4BD", Divider = "#2D2F4A",
            })),

        new(ColourPalette.Matcha, "Matcha", Theme(
            new PaletteLight
            {
                Primary = "#557A2A", PrimaryDarken = "#40601D", PrimaryLighten = "#769C48",
                Secondary = "#9C7A4B", SecondaryDarken = "#7A5E36", SecondaryLighten = "#B8986A",
                Background = "#F6F4E8", Surface = "#FFFFFB",
                AppbarBackground = "#40601D", AppbarText = "#F7F9EC",
                DrawerBackground = "#ECEBD9", DrawerText = "#3E5A1C", DrawerIcon = "#3E5A1C",
                TextPrimary = "#24291C", TextSecondary = "#646B57", ActionDefault = "#646B57", Divider = "#E0DECB",
            },
            new PaletteDark
            {
                Primary = "#B5D47E", PrimaryDarken = "#98BA5E", PrimaryLighten = "#CFE5A6", PrimaryContrastText = "#1A2608",
                Secondary = "#D9BC8C", SecondaryContrastText = "#2A1E0A",
                Background = "#161911", Surface = "#21251A",
                AppbarBackground = "#1A1E14", AppbarText = "#E8EEDB",
                DrawerBackground = "#1B2015", DrawerText = "#CFE5A6", DrawerIcon = "#CFE5A6",
                TextPrimary = "#E7EADF", TextSecondary = "#A8AE9B", ActionDefault = "#A8AE9B", Divider = "#343A2B",
            })),

        new(ColourPalette.Espresso, "Espresso", Theme(
            new PaletteLight
            {
                Primary = "#6F4E37", PrimaryDarken = "#523826", PrimaryLighten = "#8E6A50",
                Secondary = "#C08A3E", SecondaryDarken = "#9A6C2B", SecondaryLighten = "#D4A666", SecondaryContrastText = "#2A1A05",
                Background = "#F7F1EA", Surface = "#FFFCF8",
                AppbarBackground = "#3E2A1D", AppbarText = "#F8EEE4",
                DrawerBackground = "#EEE3D6", DrawerText = "#3E2A1D", DrawerIcon = "#3E2A1D",
                TextPrimary = "#2A1F18", TextSecondary = "#6D5E53", ActionDefault = "#6D5E53", Divider = "#E3D6C8",
            },
            new PaletteDark
            {
                Primary = "#D2A47E", PrimaryDarken = "#B8875F", PrimaryLighten = "#E6C3A5", PrimaryContrastText = "#2A170A",
                Secondary = "#E8BE7A", SecondaryContrastText = "#2A1A05",
                Background = "#17120F", Surface = "#231B16",
                AppbarBackground = "#1B1511", AppbarText = "#F2E5D8",
                DrawerBackground = "#1D1612", DrawerText = "#E6C3A5", DrawerIcon = "#E6C3A5",
                TextPrimary = "#EEE4DB", TextSecondary = "#B3A396", ActionDefault = "#B3A396", Divider = "#3A2E26",
            })),

        new(ColourPalette.Plum, "Plum", Theme(
            new PaletteLight
            {
                Primary = "#6E2C68", PrimaryDarken = "#521F4D", PrimaryLighten = "#8E4A87",
                Secondary = "#B8860B", SecondaryDarken = "#8F6808", SecondaryLighten = "#D4A72E", SecondaryContrastText = "#241A00",
                Background = "#F7F2F6", Surface = "#FFFFFF",
                AppbarBackground = "#521F4D", AppbarText = "#FBEFF9",
                DrawerBackground = "#EFE4ED", DrawerText = "#521F4D", DrawerIcon = "#521F4D",
                TextPrimary = "#2A1D28", TextSecondary = "#6C5A68", ActionDefault = "#6C5A68", Divider = "#E2D5DF",
            },
            new PaletteDark
            {
                Primary = "#DDA0D5", PrimaryDarken = "#C57EBC", PrimaryLighten = "#ECC2E6", PrimaryContrastText = "#2C0A28",
                Secondary = "#E8C150", SecondaryContrastText = "#2A1F02",
                Background = "#181217", Surface = "#241B22",
                AppbarBackground = "#1C151B", AppbarText = "#F3E2F0",
                DrawerBackground = "#1E161C", DrawerText = "#ECC2E6", DrawerIcon = "#ECC2E6",
                TextPrimary = "#EDE1EA", TextSecondary = "#B19EAC", ActionDefault = "#B19EAC", Divider = "#3B2D38",
            })),

        new(ColourPalette.Citrus, "Citrus", Theme(
            new PaletteLight
            {
                Primary = "#B14E0A", PrimaryDarken = "#8C3D07", PrimaryLighten = "#D86E26",
                Secondary = "#5E8C14", SecondaryDarken = "#476B0E", SecondaryLighten = "#7FAE34",
                Background = "#FBF7EE", Surface = "#FFFFFF",
                AppbarBackground = "#8C3D07", AppbarText = "#FFF5EC",
                DrawerBackground = "#F3EADB", DrawerText = "#7A3506", DrawerIcon = "#7A3506",
                TextPrimary = "#2B2118", TextSecondary = "#6E5F52", ActionDefault = "#6E5F52", Divider = "#E7DCCB",
            },
            new PaletteDark
            {
                Primary = "#FFA05C", PrimaryDarken = "#F0823A", PrimaryLighten = "#FFC08F", PrimaryContrastText = "#2E1300",
                Secondary = "#B5E06A", SecondaryContrastText = "#182600",
                Background = "#17130E", Surface = "#231D16",
                AppbarBackground = "#1B1611", AppbarText = "#FBE9D9",
                DrawerBackground = "#1D1712", DrawerText = "#FFC08F", DrawerIcon = "#FFC08F",
                TextPrimary = "#F0E6DB", TextSecondary = "#B4A595", ActionDefault = "#B4A595", Divider = "#3A3026",
            })),

        new(ColourPalette.CastIron, "Cast Iron", Theme(
            new PaletteLight
            {
                Primary = "#3A3F44", PrimaryDarken = "#26292C", PrimaryLighten = "#5A6066",
                Secondary = "#C24D18", SecondaryDarken = "#9C3D12", SecondaryLighten = "#E2703F",
                Background = "#F2F1EF", Surface = "#FFFFFF",
                AppbarBackground = "#26292C", AppbarText = "#F4F4F2",
                DrawerBackground = "#E6E5E2", DrawerText = "#26292C", DrawerIcon = "#26292C",
                TextPrimary = "#1E2023", TextSecondary = "#5E6268", ActionDefault = "#5E6268", Divider = "#D9D8D5",
            },
            new PaletteDark
            {
                Primary = "#FF8F5A", PrimaryDarken = "#F07038", PrimaryLighten = "#FFB08C", PrimaryContrastText = "#2B0E00",
                Secondary = "#B8BEC4", SecondaryContrastText = "#15181B",
                Background = "#121314", Surface = "#1C1E20",
                AppbarBackground = "#0F1011", AppbarText = "#EEEEEE",
                DrawerBackground = "#151618", DrawerText = "#FFB08C", DrawerIcon = "#FFB08C",
                TextPrimary = "#ECECEC", TextSecondary = "#A2A6AA", ActionDefault = "#A2A6AA", Divider = "#2E3134",
            })),

        new(ColourPalette.LavenderAndHoney, "Lavender & Honey", Theme(
            new PaletteLight
            {
                Primary = "#6A5A9E", PrimaryDarken = "#4F4278", PrimaryLighten = "#8B7CBF",
                Secondary = "#C8901E", SecondaryDarken = "#A0720F", SecondaryLighten = "#DDAE4E", SecondaryContrastText = "#2A1A00",
                Background = "#F6F4FA", Surface = "#FFFFFF",
                AppbarBackground = "#4F4278", AppbarText = "#F4F1FC",
                DrawerBackground = "#ECE8F4", DrawerText = "#4F4278", DrawerIcon = "#4F4278",
                TextPrimary = "#25222F", TextSecondary = "#625D72", ActionDefault = "#625D72", Divider = "#DEDAE8",
            },
            new PaletteDark
            {
                Primary = "#BCB0EC", PrimaryDarken = "#9C8FD6", PrimaryLighten = "#D6CEF5", PrimaryContrastText = "#1C1638",
                Secondary = "#F0C25E", SecondaryContrastText = "#2A1C00",
                Background = "#141219", Surface = "#1F1C27",
                AppbarBackground = "#17151E", AppbarText = "#EAE6F7",
                DrawerBackground = "#18161F", DrawerText = "#D6CEF5", DrawerIcon = "#D6CEF5",
                TextPrimary = "#E8E5F0", TextSecondary = "#A9A4B8", ActionDefault = "#A9A4B8", Divider = "#322E3D",
            })),
    ];

    /// <summary>The chosen palette; Paprika if the saved one is unknown (such as one from a newer version).</summary>
    public static Option For(ColourPalette palette) => All.FirstOrDefault(option => option.Palette == palette) ?? All[0];

    private static MudTheme Theme(PaletteLight light, PaletteDark dark) => new() { PaletteLight = light, PaletteDark = dark };
}
