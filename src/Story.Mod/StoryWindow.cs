using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Story.Mod;

public sealed class StoryWindow : MonoBehaviour
{
    private static StoryWindow? current;
    private void Awake() => current = this;
    private void OnDestroy() { if (current == this) current = null; }
    internal static bool PointerOverWindow()
    {
        if (current == null || MapGO.instance == null) return false;
        var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return new Rect(12, 80, 100, 28).Contains(mouse) || (current.open && current.window.Contains(mouse));
    }
    private bool open;
    private bool confirmAbandoned;
    private Rect window = new Rect(80, 80, 750, 540);
    private Vector2 scroll;
    private float nextScan;
    private string display = "";
    private int page;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8)) { open = !open; Refresh(); }
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 2;
        Runtime.Tick();
        if (Runtime.IsReady) Runtime.Guard("Observe nation state", () => Runtime.Scan());
        if (open) Refresh();
    }
    private void Refresh()
    {
        int count = Runtime.Store?.Marker().Count ?? 0;
        int pages = Math.Max(1, (count + 99) / 100);
        page = Math.Max(0, Math.Min(page, pages - 1));
        var events = Runtime.Store?.ReadPage(page * 100, 100) ?? Array.Empty<Story.Core.StoryEvent>();
        display = string.Join("\n\n", events.Select(e => $"Turn {e.Turn} | {e.GameDate} | {e.RecordedAt:O}\n{e.Nation}: {e.Description}"));
        if (events.Length == 0) display = "No events recorded yet.";
    }
    private void OnGUI()
    {
        if (MapGO.instance != null && GUI.Button(new Rect(12, 80, 100, 28), "Story (F8)")) { open = !open; Refresh(); }
        if (!open) return;
        window.width = Math.Min(750, Screen.width - 24);
        window.height = Math.Min(540, Screen.height - 24);
        window.x = Mathf.Clamp(window.x, 0, Math.Max(0, Screen.width - window.width));
        window.y = Mathf.Clamp(window.y, 0, Math.Max(0, Screen.height - window.height));
        window = GUILayout.Window(749311, window, Draw, "King Tiger Books - Campaign history");
    }
    private void Draw(int id)
    {
        GUILayout.Label(Runtime.Notice);
        GUILayout.Label("Observed history may expose fog-of-war information. Other players' events may be absent.");
        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(window.height - 210));
        GUILayout.TextArea(display);
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous 100")) { page--; Refresh(); }
        GUILayout.Label("Page " + (page + 1));
        if (GUILayout.Button("Next 100")) { page++; Refresh(); }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUI.enabled = Runtime.IsReady && !Runtime.IsReporting;
        if (GUILayout.Button("Create history PDF")) Runtime.Guard("Report requested", Runtime.GenerateReport);
        if (GUILayout.Button("Mark abandoned")) confirmAbandoned = true;
        GUI.enabled = !string.IsNullOrEmpty(Runtime.LastReport);
        if (GUILayout.Button("Open PDF")) Runtime.Guard("Open PDF", () => Process.Start(new ProcessStartInfo(Runtime.LastReport) { UseShellExecute = true }));
        GUI.enabled = true;
        if (GUILayout.Button("Close")) open = false;
        GUILayout.EndHorizontal();
        if (confirmAbandoned)
        {
            GUILayout.Label("Record this campaign as abandoned and generate its history?");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Confirm abandonment")) { confirmAbandoned = false; Runtime.Guard("Abandoned campaign", () => Runtime.End("abandoned by player declaration")); }
            if (GUILayout.Button("Cancel")) confirmAbandoned = false;
            GUILayout.EndHorizontal();
        }
        GUI.DragWindow(new Rect(0, 0, 10000, 25));
    }
}
