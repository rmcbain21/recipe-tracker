using RecipeTracker.Core.Preferences;
using RecipeTracker.Core.Tests.Fakes;

namespace RecipeTracker.Core.Tests;

public class SettingsTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 8, 18, 0, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    [Fact]
    public async Task Defaults_are_paprika_matching_the_device_at_the_default_text_size()
    {
        var app = CreateApp();

        var settings = await app.Settings.GetAsync();
        var device = await app.Settings.GetDeviceAsync();

        Assert.Equal((ColourPalette.Paprika, Appearance.MatchDevice), (settings.Palette, settings.Appearance));
        Assert.Equal(TextSize.Default, device.TextSize);
    }

    [Fact]
    public async Task Palette_appearance_and_text_size_are_saved()
    {
        var app = CreateApp();

        await app.Settings.SetPaletteAsync(ColourPalette.Blueberry);
        await app.Settings.SetAppearanceAsync(Appearance.Dark);
        await app.Settings.SetTextSizeAsync(TextSize.ExtraLarge);

        var reopened = CreateApp();
        var settings = await reopened.Settings.GetAsync();
        Assert.Equal((ColourPalette.Blueberry, Appearance.Dark), (settings.Palette, settings.Appearance));
        Assert.Equal(_clock.UtcNow, settings.UpdatedAt);
        Assert.Equal(TextSize.ExtraLarge, (await reopened.Settings.GetDeviceAsync()).TextSize);
    }

    [Fact]
    public async Task Colours_are_in_the_synced_settings_and_text_size_in_this_devices_own()
    {
        var app = CreateApp();
        await app.Settings.SetPaletteAsync(ColourPalette.Matcha);
        await app.Settings.SetTextSizeAsync(TextSize.Large);

        var synced = Assert.Single(await _store.ListAsync<Settings>("settings"));
        var device = Assert.Single(await _store.ListAsync<DeviceSettings>("deviceSettings"));

        Assert.Equal(ColourPalette.Matcha, synced.Palette);
        Assert.Equal(TextSize.Large, device.TextSize);
    }

    [Fact]
    public async Task Unknown_choices_are_refused()
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Settings.SetPaletteAsync((ColourPalette)99));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Settings.SetAppearanceAsync((Appearance)99));
        await Assert.ThrowsAsync<RuleViolationException>(() => app.Settings.SetTextSizeAsync((TextSize)99));
    }
}
