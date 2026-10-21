namespace RynthCore.PluginSdk;

// ─── Plugin windows: the engine's look (engine API v89) ─────────────────────
// RynthCoreHost.GetUiColor / GetUiFont / SetPanelOpen, for the windows a plugin
// declares in its sections manifest ("windows") and draws itself through
// RynthPluginRenderEmbed (RynthCore docs/PLUGIN_WINDOWS.md). Check
// RynthCoreHost.HasUiTheme / HasSetPanelOpen first: older engines have neither.
// The numbers match the engine's UiColorToken / UiFontToken and only grow.

/// <summary>A colour of the player's theme (RynthCoreHost.GetUiColor).</summary>
public enum RynthUiColor
{
    Text = 0, TextMuted = 1,
    /// <summary>The theme's accent (teal by default): titles, links, the current item.</summary>
    Accent = 2,
    /// <summary>The highlight (amber by default): warnings, things to notice.</summary>
    Highlight = 3,
    Button = 4, Border = 5, Field = 6, Selected = 7, RowAlt = 8, Background = 9, PanelGlass = 10,
    AccentFill = 11, BoxBg = 12, FrameBg = 13, FrameHover = 14, FrameActive = 15,
    /// <summary>Fixed, whatever the theme: good / bad.</summary>
    Green = 16, Red = 17,
    TitleBar = 18, WindowBorder = 19,
}

/// <summary>A font the engine bakes (RynthCoreHost.GetUiFont): sizes in pixels at 100% text size.</summary>
public enum RynthUiFont
{
    Ui9 = 0, Ui10 = 1,
    /// <summary>The default for plugin windows.</summary>
    Ui11 = 2,
    Ui14 = 3, Ui18 = 4, UiBold11 = 5, UiBold14 = 6,
    Mono9 = 7, Mono10 = 8, Mono12 = 9, Chat10 = 10, Chat12 = 11, Chat14 = 12,
}

/// <summary>What RynthCoreHost.SetPanelOpen does.</summary>
public enum RynthPanelOpen
{
    Ask = -1, Close = 0, Open = 1, Toggle = 2,
}
