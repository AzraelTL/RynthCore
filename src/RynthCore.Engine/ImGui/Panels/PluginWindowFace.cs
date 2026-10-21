// ============================================================================
//  RynthCore.Engine - ImGui/Panels/PluginWindowFace.cs
//  A plugin's own window (its manifest's "windows", UI/Sections/PluginWindows.cs):
//  the panel host draws the window, this face hands the body to the plugin's
//  RynthPluginRenderEmbed(surface, width, height) in the default UI font, under
//  an ID scope of its own. Popped out, PluginManager.RenderEmbed adds ":popout"
//  to the surface name. An export that throws is switched off (PluginManager
//  logs it); the body then says so and asks again every 5 s, so a reloaded
//  plugin draws again without the window being reopened.
//  docs/PLUGIN_WINDOWS.md. AC's render thread.
// ============================================================================

using System;
using System.Numerics;
using System.Text;
using RynthCore.Engine.Plugins;
using static RynthCore.Engine.ImGuiBackend.Panels.FaceKit;

namespace RynthCore.Engine.ImGuiBackend.Panels;

internal sealed class PluginWindowFace : IImGuiPanel
{
    private readonly string _plugin, _title;
    private readonly byte[] _surface;   // NUL-terminated UTF-8, built once
    private readonly string _idScope;
    // The export threw: PluginManager switched the plugin's surfaces off. Asked again every
    // RetryMs, so a reloaded plugin (a fresh export) draws again without reopening the window.
    private long _failedAtMs = -1;
    private const long RetryMs = 5000;

    public PluginWindowFace(string plugin, string surface, string title)
    {
        _plugin = plugin;
        _title = title;
        _surface = Encoding.UTF8.GetBytes(surface + "\0");
        _idScope = "##pw_" + surface;
    }

    public void Draw()
    {
        Vector2 start = ImGuiNET.ImGui.GetCursorScreenPos();
        Vector2 avail = ImGuiNET.ImGui.GetContentRegionAvail();
        long now = Environment.TickCount64;
        int result = PluginManager.EmbedFailed;
        if (_failedAtMs < 0 || now - _failedAtMs >= RetryMs)
        {
            ImGuiNET.ImGui.PushID(_idScope);
            ImGuiNET.ImGui.PushFont(ImGuiFonts.Get(UiFont.Ui11));
            try { result = PluginManager.RenderEmbed(_plugin, _surface, avail, out _); }
            finally
            {
                ImGuiNET.ImGui.PopFont();
                ImGuiNET.ImGui.PopID();
            }
            if (result == PluginManager.EmbedFailed) _failedAtMs = now;
            else if (result == PluginManager.EmbedDrawn) _failedAtMs = -1;
            else if (_failedAtMs >= 0) { _failedAtMs = now; result = PluginManager.EmbedFailed; }   // still off since the throw
        }
        if (result == PluginManager.EmbedDrawn) return;

        ImGuiNET.ImGui.SetCursorScreenPos(start + new Vector2(6, 6));
        Label(result switch
        {
            PluginManager.EmbedNoPlugin => $"{_title} comes with {_plugin}; it isn't loaded.",
            PluginManager.EmbedFailed => $"{_plugin} couldn't draw {_title} (see the log). It comes back when the plugin reloads.",
            _ => $"Waiting for {_plugin}…",
        }, Mute);
    }
}
