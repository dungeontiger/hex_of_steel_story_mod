using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Story.Core;

public sealed class StoryEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Turn { get; set; }
    public string GameDate { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Nation { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Description { get; set; } = "";
}

public sealed class SaveMarker
{
    public int Schema { get; set; } = 1;
    public string CampaignId { get; set; } = Guid.NewGuid().ToString("N");
    public int Count { get; set; }
    public string LastId { get; set; } = "";
}

public static class Json
{
    // Never honor CLR type names in save-adjacent files or endpoint responses.
    public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    { TypeNameHandling = TypeNameHandling.None, MaxDepth = 32, DateParseHandling = DateParseHandling.None };
    public static string Encode(object value) => JsonConvert.SerializeObject(value, Settings);
    public static T Decode<T>(string text) => JsonConvert.DeserializeObject<T>(text, Settings)
        ?? throw new InvalidDataException("Empty JSON document.");
}

public static class AtomicFile
{
    public static void Write(string path, string contents)
    {
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        var temp = full + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = new UTF8Encoding(false).GetBytes(contents);
                file.Write(bytes, 0, bytes.Length);
                file.Flush(true);
            }
            if (File.Exists(full)) File.Replace(temp, full, full + ".bak");
            else File.Move(temp, full);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

/// <summary>Single-writer durable journal. Each line is a complete event; no game objects are serialized.</summary>
public sealed class HistoryStore
{
    private readonly List<StoryEvent> events = new List<StoryEvent>();
    private readonly HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
    private readonly object gate = new object();
    public string PathName { get; }
    public string CampaignId { get; }
    public bool RecoveredTail { get; private set; }
    public HistoryStore(string directory, string campaignId)
    {
        if (!Guid.TryParseExact(campaignId, "N", out _)) throw new InvalidDataException("Invalid campaign identity.");
        CampaignId = campaignId;
        Directory.CreateDirectory(directory);
        PathName = Path.Combine(directory, campaignId + ".journal.jsonl");
        if (!File.Exists(PathName)) return;
        var text = File.ReadAllText(PathName, Encoding.UTF8);
        var lines = text.Split('\n');
        for (int index = 0; index < lines.Length; index++)
        {
            if (lines[index].Length == 0 && index == lines.Length - 1) break;
            StoryEvent item;
            try { item = Json.Decode<StoryEvent>(lines[index]); Validate(item); }
            catch (Exception ex) when (ex is JsonException || ex is InvalidDataException)
            {
                // A failed append can leave only the final unterminated record incomplete.
                if (index != lines.Length - 1) throw new InvalidDataException("Corrupt history record " + (index + 1), ex);
                File.Copy(PathName, PathName + ".incomplete-" + Guid.NewGuid().ToString("N"));
                AtomicFile.Write(PathName, Serialize(events));
                RecoveredTail = true;
                break;
            }
            if (!ids.Add(item.Id)) throw new InvalidDataException("Duplicate event identity.");
            events.Add(item);
        }
        // A complete JSON record without a newline is valid, but needs a delimiter before the next append.
        if (!RecoveredTail && text.Length > 0 && !text.EndsWith("\n", StringComparison.Ordinal))
            AtomicFile.Write(PathName, Serialize(events));
    }
    public static void RestoreFromExport(string directory, SaveMarker expected, string exportPath)
    {
        var target = new HistoryStore(directory, expected.CampaignId);
        if (target.Marker().Count != 0) throw new InvalidOperationException("Recovery never overwrites a nonempty journal.");
        var root = Json.Decode<Newtonsoft.Json.Linq.JObject>(File.ReadAllText(exportPath));
        var marker = (root["marker"] == null ? null : Json.Decode<SaveMarker>(root["marker"]!.ToString())) ?? throw new InvalidDataException("Missing export marker.");
        var items = (root["events"] == null ? null : Json.Decode<StoryEvent[]>(root["events"]!.ToString())) ?? throw new InvalidDataException("Missing exported events.");
        if ((int?)root["schema"] != 1 || marker.Schema != 1 || expected.Schema != 1
            || marker.CampaignId != expected.CampaignId || marker.Count != expected.Count
            || marker.LastId != expected.LastId || items.Length != expected.Count
            || (items.LastOrDefault()?.Id ?? "") != expected.LastId)
            throw new InvalidDataException("Export does not match the save marker.");
        foreach (var item in items) Validate(item);
        if (items.Select(e => e.Id).Distinct().Count() != items.Length) throw new InvalidDataException("Duplicate exported event identity.");
        AtomicFile.Write(target.PathName, Serialize(items));
    }
    private static void Validate(StoryEvent? item)
    {
        if (item == null || !Guid.TryParseExact(item.Id, "N", out _) || item.Turn < 0
            || item.Nation == null || item.GameDate == null || string.IsNullOrWhiteSpace(item.Kind)
            || string.IsNullOrWhiteSpace(item.Description)) throw new InvalidDataException("Invalid event record.");
    }
    public StoryEvent[] Snapshot() { lock (gate) return events.Select(e => Json.Decode<StoryEvent>(Json.Encode(e))).ToArray(); }
    public StoryEvent[] ReadPage(int start, int count)
    {
        if (start < 0 || count < 0 || count > 1000) throw new ArgumentOutOfRangeException();
        lock (gate) return events.Skip(start).Take(count).Select(e => Json.Decode<StoryEvent>(Json.Encode(e))).ToArray();
    }
    public StoryEvent[] ReadTurn(string nation, int turn)
    {
        lock (gate) return events.Where(e => e.Nation == nation && e.Turn == turn).Select(e => Json.Decode<StoryEvent>(Json.Encode(e))).ToArray();
    }
    public bool HasKind(string kind) { lock (gate) return events.Any(e => e.Kind == kind); }
    public SaveMarker Marker()
    {
        lock (gate) return new SaveMarker { CampaignId = CampaignId, Count = events.Count, LastId = events.LastOrDefault()?.Id ?? "" };
    }
    public bool Append(StoryEvent item)
    {
        Validate(item);
        lock (gate)
        {
            if (ids.Contains(item.Id)) return false;
            var bytes = Encoding.UTF8.GetBytes(Json.Encode(item) + "\n");
            using (var file = new FileStream(PathName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
            {
                long start = file.Length;
                file.Position = start;
                try { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                catch { file.SetLength(start); throw; }
            }
            events.Add(Json.Decode<StoryEvent>(Json.Encode(item)));
            ids.Add(item.Id);
            return true;
        }
    }
    public bool NeedsRewind(SaveMarker marker)
    {
        lock (gate)
        {
            if (marker.Schema != 1 || marker.CampaignId != CampaignId || marker.Count < 0 || marker.Count > events.Count)
                throw new InvalidDataException("The save's history is missing or incompatible. Restore its matching journal.");
            var expected = marker.Count == 0 ? "" : events[marker.Count - 1].Id;
            if (expected != marker.LastId) throw new InvalidDataException("This save belongs to a replaced history branch.");
            return marker.Count < events.Count;
        }
    }
    public void Rewind(SaveMarker marker, bool confirmed)
    {
        lock (gate)
        {
            if (!NeedsRewind(marker)) return;
            if (!confirmed) throw new InvalidOperationException("History replacement requires confirmation.");
            var retained = events.Take(marker.Count).ToArray();
            AtomicFile.Write(PathName, Serialize(retained));
            events.Clear(); events.AddRange(retained);
            ids.Clear(); foreach (var item in events) ids.Add(item.Id);
        }
    }
    public void ExportBesideSave(string savePath, string title)
    {
        lock (gate) AtomicFile.Write(savePath + ".story.json", Json.Encode(new { schema = 1, title, marker = Marker(), events }));
    }
    private static string Serialize(IEnumerable<StoryEvent> items) => string.Concat(items.Select(e => Json.Encode(e) + "\n"));
}
