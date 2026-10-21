using System.Windows;
using System.Windows.Media;

namespace NexusIA;

public static class ThemeManager
{
    private sealed record Palette(
        string Name,
        string LightAccent,
        string DarkAccent,
        string LightAccentDark,
        string DarkAccentDark,
        string LightAccentSoft,
        string DarkAccentSoft);

    private static readonly IReadOnlyDictionary<string, Palette> Palettes =
        new Dictionary<string, Palette>(StringComparer.OrdinalIgnoreCase)
        {
            ["green"] = new("Menta", "#0E8277", "#46CFC2", "#0A625C", "#2AA89D", "#DFF5F1", "#163B3C"),
            ["cyan"] = new("Ciano", "#087F95", "#43D6E7", "#066779", "#1DAFC3", "#DDF6F9", "#123941"),
            ["blue"] = new("Azul", "#245DB3", "#7AA8FF", "#194A91", "#4E82D8", "#E7F0FF", "#213757"),
            ["purple"] = new("Roxo", "#7040A6", "#C2A0FF", "#542E82", "#966ED5", "#F0E8FA", "#342443"),
            ["pink"] = new("Rosa", "#A12C69", "#FF79B8", "#812052", "#DB5596", "#FAE5EF", "#472338"),
            ["orange"] = new("Laranja", "#A64810", "#FF9A52", "#85390C", "#DF7130", "#FBEBDD", "#493021"),
            ["amber"] = new("Âmbar", "#985A00", "#FFC46B", "#754300", "#D6983B", "#FFF1D8", "#47351B")
        };

    public static bool IsLight { get; private set; }
    public static string CurrentPalette { get; private set; } = "green";
    public static string CurrentPaletteName => Palettes[CurrentPalette].Name;

    public static void Apply(bool light, string? paletteName)
    {
        IsLight = light;
        CurrentPalette = paletteName is not null && Palettes.ContainsKey(paletteName) ? paletteName : "green";
        var palette = Palettes[CurrentPalette];
        var accent = light ? palette.LightAccent : palette.DarkAccent;
        var accentDark = light ? palette.LightAccentDark : palette.DarkAccentDark;
        var accentSoft = light ? palette.LightAccentSoft : palette.DarkAccentSoft;
        var background = light ? "#F2F6F7" : "#101821";
        var panel = light ? "#FFFFFF" : "#16232D";
        var card = light ? "#FFFFFF" : "#1B2B36";
        var cardHover = light ? "#EDF4F6" : "#223743";
        var line = light ? "#D9E4E8" : "#2A3D49";
        var lineStrong = light ? "#B5C6CE" : "#415763";
        var foreground = light ? "#1C2C35" : "#E7F0F4";
        var muted = light ? "#536773" : "#A1B1BB";
        var faint = light ? "#71838E" : "#788B96";
        var closeHover = light ? "#FCE9E8" : "#3A292B";

        var resources = Application.Current.Resources;
        SetBrush(resources, "Bg", background);
        SetBrush(resources, "Panel", panel);
        SetBrush(resources, "Card", card);
        SetBrush(resources, "CardHover", cardHover);
        SetBrush(resources, "Line", line);
        SetBrush(resources, "LineStrong", lineStrong);
        SetBrush(resources, "Fg", foreground);
        SetBrush(resources, "Muted", muted);
        SetBrush(resources, "Faint", faint);
        SetBrush(resources, "Ink", light ? "#FFFFFF" : "#102028");
        SetBrush(resources, "Accent", accent);
        SetBrush(resources, "AccentDark", accentDark);
        SetBrush(resources, "AccentLight", accentSoft);
        SetBrush(resources, "AccentGlow", accent);
        SetBrush(resources, "ChartSecondary", light ? "#A66B1B" : "#F0B45B");
        SetBrush(resources, "CloseHover", closeHover);
        SetBrush(resources, "Hover", cardHover);
        SetBrush(resources, "ThumbBrush", light ? "#BDCEC2" : "#53645A");
        SetBrush(resources, "ThumbHover", light ? "#81958A" : "#809187");
        SetBrush(resources, "ThumbDrag", accent);
        SetBrush(resources, SystemColors.ControlBrushKey, panel);
        SetBrush(resources, SystemColors.HighlightBrushKey, accent);
        SetBrush(resources, SystemColors.InactiveSelectionHighlightBrushKey, accentSoft);
        SetGradient(resources, "AccentGradient", accent, accentDark);
        SetGradient(resources, "HeroGradient", foreground, accent);
        SetGradient(resources, "HeroTitleGradient", foreground, accent);
        SetGradient(resources, "PanelGradient", panel, background);
    }

    private static void SetBrush(ResourceDictionary resources, object key, string hex)
    {
        var brush = new SolidColorBrush(ParseColor(hex));
        brush.Freeze();
        resources[key] = brush;
    }

    private static void SetGradient(ResourceDictionary resources, object key, string start, string end)
    {
        var brush = new LinearGradientBrush(ParseColor(start), ParseColor(end), new Point(0, 0), new Point(1, 0));
        brush.Freeze();
        resources[key] = brush;
    }

    private static Color ParseColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
