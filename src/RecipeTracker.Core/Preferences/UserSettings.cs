using RecipeTracker.Core.Ports;

namespace RecipeTracker.Core.Preferences;

/// <summary>The Cook's preferences: colours and appearance (synced), and text size (this device only).</summary>
public sealed class UserSettings
{
    /// <summary>Synced.</summary>
    internal const string Table = "settings";

    /// <summary>Not a synced table: issue 02's sync must not list it.</summary>
    internal const string DeviceTable = "deviceSettings";

    // There is only ever one record in each table. A fixed id (rather than a new GUID per device)
    // means every device edits the same Settings record, so sync combines it instead of duplicating it.
    private const string SettingsId = "00000000-0000-0000-0000-000000000001";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public UserSettings(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>The saved colours and appearance, or the defaults if they have never been changed.</summary>
    public async Task<Settings> GetAsync() =>
        await _store.LoadAsync<Settings>(Table, SettingsId) ?? new Settings { Id = SettingsId, UpdatedAt = DateTimeOffset.MinValue };

    /// <summary>This device's text size, or the default if it has never been changed here.</summary>
    public async Task<DeviceSettings> GetDeviceAsync() =>
        await _store.LoadAsync<DeviceSettings>(DeviceTable, SettingsId) ?? new DeviceSettings { Id = SettingsId, UpdatedAt = DateTimeOffset.MinValue };

    public async Task SetPaletteAsync(ColourPalette palette)
    {
        if (!Enum.IsDefined(palette))
        {
            throw new RuleViolationException("Choose one of the palettes.");
        }

        var settings = await GetAsync();
        await _store.SaveAsync(Table, settings with { Palette = palette, UpdatedAt = _clock.UtcNow });
    }

    public async Task SetAppearanceAsync(Appearance appearance)
    {
        if (!Enum.IsDefined(appearance))
        {
            throw new RuleViolationException("Choose Match my device, Light or Dark.");
        }

        var settings = await GetAsync();
        await _store.SaveAsync(Table, settings with { Appearance = appearance, UpdatedAt = _clock.UtcNow });
    }

    public async Task SetTextSizeAsync(TextSize textSize)
    {
        if (!Enum.IsDefined(textSize))
        {
            throw new RuleViolationException("Choose one of the text sizes.");
        }

        var device = await GetDeviceAsync();
        await _store.SaveAsync(DeviceTable, device with { TextSize = textSize, UpdatedAt = _clock.UtcNow });
    }
}
