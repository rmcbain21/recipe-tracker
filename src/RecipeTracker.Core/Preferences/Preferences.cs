using System.Text.Json.Serialization;

namespace RecipeTracker.Core.Preferences;

/// <summary>The app's colours, each with a light and a dark version. Stored by name, so members can be reordered.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ColourPalette>))]
public enum ColourPalette
{
    /// <summary>Paprika red and herb green on warm paper; the app's first colours.</summary>
    Paprika,

    /// <summary>Basil green and tomato red.</summary>
    BasilAndTomato,

    /// <summary>Saffron gold and deep chilli red.</summary>
    Saffron,

    /// <summary>Blueberry indigo and lemon.</summary>
    Blueberry,

    /// <summary>Matcha green on cream.</summary>
    Matcha,

    /// <summary>Coffee brown and crema.</summary>
    Espresso,

    /// <summary>Plum purple and gold.</summary>
    Plum,

    /// <summary>Orange and lime.</summary>
    Citrus,

    /// <summary>Charcoal and ember orange.</summary>
    CastIron,

    /// <summary>Lavender and honey.</summary>
    LavenderAndHoney,
}

/// <summary>Whether the palette's light or dark version is shown.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Appearance>))]
public enum Appearance
{
    /// <summary>Follows the device's dark mode setting.</summary>
    MatchDevice,

    Light,

    Dark,
}

/// <summary>How large the app's text is, as a share of the browser's normal size. Stored by name, so members can be reordered.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TextSize>))]
public enum TextSize
{
    /// <summary>81.25%: 13px body text.</summary>
    ExtraSmall,

    /// <summary>87.5%: 14px body text.</summary>
    Small,

    /// <summary>100%: 16px body text.</summary>
    Default,

    /// <summary>112.5%: 18px body text.</summary>
    Large,

    /// <summary>125%: 20px body text, for reading from across the counter.</summary>
    ExtraLarge,
}

/// <summary>
/// The Cook's preferences that follow them between devices. There is one Settings record,
/// shared across devices by sync.
/// </summary>
public sealed record Settings : StoredRecord
{
    public ColourPalette Palette { get; init; } = ColourPalette.Paprika;

    public Appearance Appearance { get; init; } = Appearance.MatchDevice;
}

/// <summary>
/// Preferences that belong to this device only, never synced: a phone and a tablet are read from
/// different distances, so each has its own text size.
/// </summary>
public sealed record DeviceSettings : StoredRecord
{
    public TextSize TextSize { get; init; } = TextSize.Default;
}
