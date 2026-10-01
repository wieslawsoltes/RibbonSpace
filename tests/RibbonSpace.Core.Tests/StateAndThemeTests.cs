using System.Globalization;
using RibbonSpace.Localization;
using RibbonSpace.State;
using RibbonSpace.Theming;

namespace RibbonSpace.Core.Tests;

public class StateTests
{
    [Fact]
    public void State_round_trips_through_json()
    {
        var state = new RibbonState
        {
            SelectedTabId = "insert",
            DisplayMode = RibbonDisplayMode.Simplified,
            VisibilityMode = RibbonVisibilityMode.TabsOnly,
            QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
            QuickAccessItemIds = ["save", "undo"],
            Customization = new RibbonCustomization
            {
                HiddenTabIds = ["mailings"],
                Labels = { ["home"] = "Start" },
                CustomTabs = [new RibbonCustomTab { Id = "custom.tab", Label = "Mine", Groups = [new RibbonCustomGroup { Id = "custom.group", Label = "Tools", ItemIds = ["bold"] }] }],
            },
        };

        var json = RibbonStateSerializer.Serialize(state);
        Assert.Contains("\"simplified\"", json, StringComparison.OrdinalIgnoreCase);
        var restored = RibbonStateSerializer.Deserialize(json)!;
        Assert.Equal("insert", restored.SelectedTabId);
        Assert.Equal(RibbonDisplayMode.Simplified, restored.DisplayMode);
        Assert.Equal(RibbonVisibilityMode.TabsOnly, restored.VisibilityMode);
        Assert.Equal(["save", "undo"], restored.QuickAccessItemIds!);
        Assert.Equal("Start", restored.Customization.Labels["home"]);
        Assert.Equal("bold", restored.Customization.CustomTabs[0].Groups[0].ItemIds[0]);
        Assert.False(restored.Customization.IsEmpty);
    }

    [Fact]
    public void Invalid_or_future_state_is_rejected()
    {
        Assert.Null(RibbonStateSerializer.Deserialize("{ not json"));
        Assert.Null(RibbonStateSerializer.Deserialize("{\"schemaVersion\": 99}"));
        Assert.Null(RibbonStateSerializer.Deserialize(""));
        Assert.NotNull(RibbonStateSerializer.Deserialize("{}"));
    }

    [Fact]
    public async Task State_saves_and_loads_files()
    {
        var path = Path.Combine(Path.GetTempPath(), "ribbonspace-tests", Guid.NewGuid() + ".json");
        await RibbonStateSerializer.SaveAsync(new RibbonState { SelectedTabId = "view" }, path);
        var loaded = await RibbonStateSerializer.LoadAsync(path);
        Assert.Equal("view", loaded!.SelectedTabId);
        Assert.Null(await RibbonStateSerializer.LoadAsync(path + ".missing"));
        File.Delete(path);
    }
}

public class ColorTests
{
    [Theory]
    [InlineData("#FF0000", 255, 255, 0, 0)]
    [InlineData("80FF0000", 128, 255, 0, 0)]
    [InlineData("#abc", 255, 0xAA, 0xBB, 0xCC)]
    public void Parses_hex(string text, byte a, byte r, byte g, byte b)
    {
        Assert.Equal(new RibbonColor(a, r, g, b), RibbonColor.Parse(text));
    }

    [Fact]
    public void Formats_and_rejects_invalid()
    {
        Assert.Equal("#185ABD", RibbonColor.Parse("#185abd").ToHex());
        Assert.Equal("#80FF0000", RibbonColor.Parse("#80FF0000").ToHex());
        Assert.False(RibbonColor.TryParse("#12345", out _));
        Assert.False(RibbonColor.TryParse("zzzzzz", out _));
    }

    [Fact]
    public void Hsl_round_trip()
    {
        var color = RibbonColor.Parse("#4472C4");
        var (h, s, l) = color.ToHsl();
        var back = RibbonColor.FromHsl(h, s, l);
        Assert.InRange(Math.Abs(back.R - color.R), 0, 1);
        Assert.InRange(Math.Abs(back.G - color.G), 0, 1);
        Assert.InRange(Math.Abs(back.B - color.B), 0, 1);
    }

    [Fact]
    public void Lighten_darken_and_contrast()
    {
        var blue = RibbonColor.Parse("#4472C4");
        Assert.True(blue.Lighten(0.4).Lightness > blue.Lightness);
        Assert.True(blue.Darken(0.25).Lightness < blue.Lightness);
        Assert.Equal(RibbonColor.White, RibbonColor.Black.Lighten(1));
        Assert.Equal(21, Math.Round(RibbonColor.ContrastRatio(RibbonColor.Black, RibbonColor.White)));
        Assert.True(RibbonColor.Parse("#FFFF00").PrefersDarkForeground);
        Assert.False(RibbonColor.Parse("#185ABD").PrefersDarkForeground);
    }

    [Fact]
    public void Office_theme_grid_has_six_rows_of_ten()
    {
        var grid = RibbonColorPalette.BuildThemeGrid();
        Assert.Equal(6, grid.Count);
        Assert.All(grid, row => Assert.Equal(10, row.Count));
        Assert.Contains("Darker 5%", grid[1][0].Name, StringComparison.Ordinal); // white column darkens
        Assert.Contains("Lighter 50%", grid[1][1].Name, StringComparison.Ordinal); // black column lightens
        Assert.Contains("Lighter 80%", grid[1][4].Name, StringComparison.Ordinal);
        Assert.Contains("Darker 50%", grid[5][4].Name, StringComparison.Ordinal);
        Assert.Equal(10, RibbonColorPalette.StandardColors.Count);
    }

    [Fact]
    public void Palettes_provide_accessible_accents()
    {
        foreach (var palette in RibbonThemePalette.Presets)
        {
            Assert.True(RibbonColor.ContrastRatio(palette.Accent, RibbonColor.White) >= 4.5, palette.Name);
            Assert.True(RibbonColor.ContrastRatio(palette.AccentDark, RibbonColor.Parse("#292929")) >= 3, palette.Name);
        }

        var custom = RibbonThemePalette.FromAccent(RibbonColor.Parse("#123456"));
        Assert.True(custom.AccentDark.Lightness > custom.Accent.Lightness);
    }
}

public class LocalizationTests
{
    [Fact]
    public void Built_in_cultures_have_all_keys()
    {
        foreach (var culture in RibbonStrings.BuiltInCultures)
        {
            var strings = RibbonStrings.ForCulture(new CultureInfo(culture));
            foreach (var key in RibbonStrings.Keys)
            {
                Assert.False(string.IsNullOrWhiteSpace(strings[key]), $"{culture}:{key}");
            }
        }
    }

    [Fact]
    public void Falls_back_to_english_and_supports_overrides()
    {
        var strings = RibbonStrings.ForCulture(new CultureInfo("ja-JP"));
        Assert.Equal("File", strings.File);
        Assert.Equal("Datei", RibbonStrings.ForCulture(new CultureInfo("de-AT")).File);
        strings.Override("File", "Home");
        Assert.Equal("Home", strings.File);
        Assert.Equal("3x4 Table", RibbonStrings.ForCulture(CultureInfo.InvariantCulture).Format(nameof(RibbonStrings.TablePickerFormat), 3, 4));
        RibbonStrings.Register("cs", new Dictionary<string, string> { ["File"] = "Soubor" });
        Assert.Equal("Soubor", RibbonStrings.ForCulture(new CultureInfo("cs-CZ")).File);
        Assert.Equal("Search", RibbonStrings.ForCulture(new CultureInfo("cs-CZ")).Search);
    }
}
