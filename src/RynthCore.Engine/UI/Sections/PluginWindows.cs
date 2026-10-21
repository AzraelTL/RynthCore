// ============================================================================
//  RynthCore.Engine - UI/Sections/PluginWindows.cs
//  The windows plugins draw (their manifests' "windows", plugin API v89;
//  docs/PLUGIN_WINDOWS.md), hosted as ImGui panels: the engine owns the window
//  (header, pop-out, close, resize, remembered place, bar button), the plugin
//  draws the body through RynthPluginRenderEmbed (PluginWindowFace).
//
//  Sync runs after every SectionRegistry rebuild (AC's render thread): it
//  registers a dynamic panel for each window the manifests ask for (a window
//  with "server" only while that server is active) and unregisters the rest.
//  The panel key is the window's title, so its saved place, hotkeys and /rc
//  commands use the name players see; a title an engine panel has is refused.
//  A window seen for the first time this session opens if it was open when the
//  game last closed (Remember Open Windows); one that went with a plugin reload
//  opens again when the plugin comes back.
//
//  WindowPlan.cs is the pure part (which windows, which are refused and why).
// ============================================================================

using System;
using System.Collections.Generic;
using System.Numerics;
using RynthCore.Engine.ImGuiBackend;
using RynthCore.Engine.ImGuiBackend.Panels;

namespace RynthCore.Engine.UI.Sections;

internal static class PluginWindows
{
    private sealed record Hosted(SectionWindowDecl Decl, string Signature);

    private static readonly Dictionary<string, Hosted> Live = new(StringComparer.OrdinalIgnoreCase);   // render thread
    private static readonly HashSet<string> SeenThisSession = new(StringComparer.OrdinalIgnoreCase);    // render thread
    private static readonly HashSet<string> ReopenWhenBack = new(StringComparer.OrdinalIgnoreCase);     // render thread
    private static readonly HashSet<string> RefusalsLogged = new(StringComparer.Ordinal);               // render thread
    private static volatile SectionWindowDecl[] _barWindows = Array.Empty<SectionWindowDecl>();
    private static int _version;

    /// <summary>Hosted windows that asked for a bar button, in manifest order. Any thread.</summary>
    public static SectionWindowDecl[] BarWindows => _barWindows;

    /// <summary>Bumped whenever the hosted set changes (the bar rebuilds its buttons). Any thread.</summary>
    public static int Version => System.Threading.Volatile.Read(ref _version);

    /// <summary>True when <paramref name="title"/> is a plugin window hosted now. Render thread.</summary>
    public static bool IsHosted(string title) => Live.ContainsKey(title);

    /// <summary>After a SectionRegistry rebuild: register, update and drop the windows' panels. Render thread.</summary>
    public static void Sync(IReadOnlyList<SectionManifest> manifests, IReadOnlyList<ServerState> servers)
    {
        var refused = new List<string>();
        Dictionary<string, SectionWindowDecl> want = WindowPlan.Build(manifests, servers,
            title => ImGuiPanelHost.HasFace(title) && !Live.ContainsKey(title), refused);
        foreach (string r in refused)
            if (RefusalsLogged.Add(r)) RynthLog.UI("PluginWindows: " + r + " - not shown.");

        bool changed = false;
        var gone = new List<string>();
        foreach (string title in Live.Keys)
            if (!want.ContainsKey(title)) gone.Add(title);
        foreach (string title in gone)
        {
            if (ImGuiPanelHost.IsOpen(title)) ReopenWhenBack.Add(title);
            ImGuiPanelHost.Unregister(title);
            Live.Remove(title);
            changed = true;
        }

        foreach ((string title, SectionWindowDecl w) in want)
        {
            string sig = WindowPlan.Signature(w);
            if (Live.TryGetValue(title, out Hosted? h) && h.Signature == sig) continue;
            SectionWindowDecl decl = w;
            ImGuiPanelHost.RegisterDynamic(title, title, Spec(decl), () => new PluginWindowFace(decl.Plugin, decl.Surface, decl.Title));
            bool first = h == null;
            Live[title] = new Hosted(decl, sig);
            changed = true;
            if (!first) continue;
            bool reopen = ReopenWhenBack.Remove(title);
            if (!reopen && SeenThisSession.Add(title) && Plugins.EngineSettings.RememberWindowLayout
                && PanelStateStore.TryGetPanel(title, out PanelStateStore.PanelEntry saved) && saved.Open)
                reopen = true;
            SeenThisSession.Add(title);
            if (reopen) ImGuiPanelHost.Open(title);
        }

        if (!changed) return;
        var bar = new List<SectionWindowDecl>();
        foreach (Hosted h in Live.Values)
            if (h.Decl.Bar) bar.Add(h.Decl);
        _barWindows = bar.ToArray();
        System.Threading.Interlocked.Increment(ref _version);
    }

    private static PanelSpec Spec(SectionWindowDecl w) => new(
        new Vector2(w.Width, w.Height), new Vector2(w.MinWidth, w.MinHeight),
        EdgeToEdge: w.EdgeToEdge, GripInBody: w.EdgeToEdge);
}
