// ============================================================================
//  RynthCore.Engine - Plugins/UiKit.cs
//  Plugin API v89, for the windows plugins draw (docs/PLUGIN_WINDOWS.md): the
//  player's colour theme (GetUiColor), the engine's UI fonts at the current
//  panel's text size (GetUiFont), and opening or closing a panel by its key
//  (SetPanelOpen). Nothing here throws across the boundary.
// ============================================================================

using System;
using System.Runtime.InteropServices;
using RynthCore.Engine.Compatibility;
using RynthCore.Engine.ImGuiBackend;
using RynthCore.Engine.ImGuiBackend.Panels;
using RynthCore.Engine.UI;

namespace RynthCore.Engine.Plugins;

internal static unsafe class UiKit
{
    /// <summary>Longest panel key SetPanelOpen reads.</summary>
    public const int MaxKeyLength = 64;

    /// <summary>v89 GetUiColor: ImGui's packed colour (ABGR) for a token, 0 for an unknown one. Any thread.</summary>
    public static uint GetUiColor(int token)
    {
        try { return Color((UiColorToken)token); }
        catch { return 0; }
    }

    /// <summary>The theme colour behind a token (the values the engine's own faces draw with).</summary>
    internal static uint Color(UiColorToken token) => token switch
    {
        UiColorToken.Text => RynthTheme.Text,
        UiColorToken.TextMuted => RynthTheme.MuteAlt,
        UiColorToken.Accent => RynthTheme.Accent,
        UiColorToken.Highlight => RynthTheme.Highlight,
        UiColorToken.Button => RynthTheme.Button,
        UiColorToken.Border => RynthTheme.Border,
        UiColorToken.Field => RynthTheme.Field,
        UiColorToken.Selected => RynthTheme.Selected,
        UiColorToken.RowAlt => RynthTheme.RowAlt,
        UiColorToken.Background => RynthTheme.Background,
        UiColorToken.PanelGlass => RynthTheme.PanelGlass,
        UiColorToken.AccentFill => RynthTheme.AccentFill,
        UiColorToken.BoxBg => RynthTheme.BoxBg,
        UiColorToken.FrameBg => RynthTheme.FrameBg,
        UiColorToken.FrameHover => RynthTheme.FrameHover,
        UiColorToken.FrameActive => RynthTheme.FrameActive,
        UiColorToken.Green => FaceKit.Green,
        UiColorToken.Red => FaceKit.Red,
        UiColorToken.TitleBar => RynthTheme.TitleBar,
        UiColorToken.WindowBorder => RynthTheme.WindowBorder,
        _ => 0,
    };

    /// <summary>The engine font behind a v89 font token, or null for an unknown one.</summary>
    internal static UiFont? Font(UiFontToken token) => token switch
    {
        UiFontToken.Ui9 => UiFont.Ui9,
        UiFontToken.Ui10 => UiFont.Ui10,
        UiFontToken.Ui11 => UiFont.Ui11,
        UiFontToken.Ui14 => UiFont.Ui14,
        UiFontToken.Ui18 => UiFont.Ui18,
        UiFontToken.UiBold11 => UiFont.UiBold11,
        UiFontToken.UiBold14 => UiFont.UiBold14,
        UiFontToken.Mono9 => UiFont.Mono9,
        UiFontToken.Mono10 => UiFont.Mono10,
        UiFontToken.Mono12 => UiFont.Mono12,
        UiFontToken.Chat10 => UiFont.Chat10,
        UiFontToken.Chat12 => UiFont.Chat12,
        UiFontToken.Chat14 => UiFont.Chat14,
        _ => null,
    };

    /// <summary>
    /// v89 GetUiFont: the <c>ImFont*</c> for a token at the text size of the panel being drawn
    /// (the host sets it around each face), null for an unknown token or off AC's thread.
    /// </summary>
    public static IntPtr GetUiFont(int font)
    {
        try
        {
            if (!MainThreadGuard.IsOnMainThread()) return IntPtr.Zero;
            if (Font((UiFontToken)font) is not UiFont f) return IntPtr.Zero;
            return (IntPtr)ImGuiFonts.Get(f).NativePtr;
        }
        catch { return IntPtr.Zero; }
    }

    /// <summary>
    /// v89 SetPanelOpen: mode 1 open, 0 close, 2 toggle, -1 ask. 1 open / 0 closed afterwards (the
    /// request lands on the next frame), -1 for no such panel or a bad call. Any thread.
    /// </summary>
    public static int SetPanelOpen(byte* key, int mode)
    {
        try
        {
            if (key == null) return -1;
            string? title = Marshal.PtrToStringUTF8((IntPtr)key);
            if (string.IsNullOrWhiteSpace(title) || title.Length > MaxKeyLength) return -1;
            title = PanelRouter.Canonical(title.Trim());
            if (!ImGuiPanelHost.HasFace(title)) return -1;
            bool open = ImGuiPanelHost.IsOpen(title);
            switch (mode)
            {
                case -1: return open ? 1 : 0;
                case 0: if (open) ImGuiPanelHost.Close(title); return 0;
                case 1: if (!open) ImGuiPanelHost.Open(title); return 1;
                case 2: PanelRouter.Toggle(title); return open ? 0 : 1;
                default: return -1;
            }
        }
        catch { return -1; }
    }
}
