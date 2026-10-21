using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Story.Core;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Story.Mod;

public sealed class StoryModification : GameModification
{
    private Harmony? harmony;
    private GameObject? host;
    public StoryModification(global::Mod mod) : base(mod) { }
    public override void OnModInitialization(global::Mod mod)
    {
        Runtime.Initialize(mod.InstallLocation);
        harmony = new Harmony("books.kingtiger.hexofsteel.story");
        try
        {
            harmony.PatchAll(typeof(StoryModification).Assembly);
            host = new GameObject("KingTigerStory");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<StoryWindow>();
        }
        catch (Exception ex)
        {
            harmony.UnpatchAll(harmony.Id);
            Runtime.Fail("Initialization", ex);
            throw;
        }
    }
    public override void OnModUnloaded()
    {
        Runtime.Shutdown();
        harmony?.UnpatchAll(harmony.Id);
        if (host != null) UnityEngine.Object.Destroy(host);
    }
}

internal static class Runtime
{
    internal const string MarkerKey = "books.kingtiger.story.marker.v1";
    internal static HistoryStore? Store;
    internal static string? LoadingPath;
    internal static string Notice = "Load or start a game to record its history.";
    internal static string LastReport = "";
    private static string directory = "";
    private static string install = "";
    private static DiagnosticLog? log;
    private static Map? map;
    private static bool disabled;
    private static SaveMarker? approvedRewind;
    private static bool failing;
    private static readonly Dictionary<string, string> objectiveOwners = new Dictionary<string, string>();
    private static readonly Dictionary<string, NationState> previous = new Dictionary<string, NationState>();
    private static readonly Queue<StoryEvent> pending = new Queue<StoryEvent>();
    private static readonly ConcurrentQueue<Action> completions = new ConcurrentQueue<Action>();
    private static CancellationTokenSource lifetime = new CancellationTokenSource();
    private static int reporting;
    internal static bool IsReporting => Volatile.Read(ref reporting) != 0;
    internal static bool HasHistoryFor(Map? candidate) => !disabled && Store != null && map != null && ReferenceEquals(map, candidate);
    internal static bool IsReady => HasHistoryFor(GameData.Instance.map) && MapGO.instance != null && !GameData.Instance.isFromMapEditor;
    internal static int Turn => GameData.Instance.map?.turnCount ?? 0;
    internal static string Date => GameData.Instance.map?.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    internal static void Initialize(string location)
    {
        install = location;
        directory = Path.Combine(Application.persistentDataPath, "Saves", "StoryHistory");
        Directory.CreateDirectory(directory);
#if DEBUG
        const bool debug = true;
#else
        const bool debug = false;
#endif
        log = new DiagnosticLog(Path.Combine(directory, "story-diagnostics.log"), debug);
        lifetime = new CancellationTokenSource();
        log.Trace("Initialize mod 0.1.0; Unity " + Application.unityVersion + "; OS " + Environment.OSVersion);
    }
    internal static void Shutdown() { Guard("Flush on unload", Flush); lifetime.Cancel(); Store = null; map = null; previous.Clear(); objectiveOwners.Clear(); }
    internal static void Guard(string context, Action action) { try { action(); } catch (Exception ex) { Fail(context, ex); } }
    internal static void Fail(string context, Exception ex)
    {
        log?.Failure(context + "; turn=" + Turn + "; campaign=" + (Store?.CampaignId ?? "none"), ex);
        Notice = "Story mod: " + context + " failed (" + ex.GetType().Name + "). See story-diagnostics.log. Recording will retry where possible.";
        if (failing || !HasHistoryFor(GameData.Instance.map)) return;
        failing = true;
        try { pending.Enqueue(new StoryEvent { Turn = Turn, GameDate = Date, Kind = "Failure", Description = context + " failed: " + ex.GetType().Name + "." }); }
        finally { failing = false; }
    }
    internal static bool PrepareLoad(GameData data)
    {
        approvedRewind = null;
        if (pending.Count > 0)
        {
            try { Flush(); } catch (Exception ex) { Fail("Flush before load", ex); return false; }
        }
        try
        {
            if (!data.ModDataBag.TryGet(MarkerKey, out string text)) return true;
            var marker = Json.Decode<SaveMarker>(text);
            var candidate = new HistoryStore(directory, marker.CampaignId);
            if (candidate.Marker().Count == 0 && marker.Count > 0 && LoadingPath != null && File.Exists(LoadingPath + ".story.json"))
            {
                HistoryStore.RestoreFromExport(directory, marker, LoadingPath + ".story.json");
                candidate = new HistoryStore(directory, marker.CampaignId);
            }
            if (candidate.NeedsRewind(marker))
            {
                var answer = NativeDialog.MessageBox(IntPtr.Zero, "This save predates the recorded history. Continuing replaces all recorded events after this save with the replayed timeline. A recovery backup will be retained.\n\nContinue loading? Choose No to keep the current game and history.", "King Tiger Books - Replace history?", 0x00000004u | 0x00000030u | 0x00000100u);
                if (answer != 6) { Notice = "Load cancelled; current history retained."; return false; }
                approvedRewind = marker;
            }
            return true;
        }
        catch (Exception ex)
        {
            Fail("Validate loaded history", ex);
            NativeDialog.MessageBox(IntPtr.Zero, "This save's matching story journal is missing or incompatible. The load has been cancelled to protect recorded history. Restore the matching Saves/StoryHistory folder or disable the mod to load without recording.", "King Tiger Books", 0x00000030u);
            return false;
        }
    }
    internal static void Loaded()
    {
        if (approvedRewind != null) { new HistoryStore(directory, approvedRewind.CampaignId).Rewind(approvedRewind, true); approvedRewind = null; }
        Store = null; map = null; previous.Clear(); objectiveOwners.Clear(); disabled = false;
        Ensure();
    }
    internal static void Ensure()
    {
        var data = GameData.Instance;
        if (data.map == null || data.listOfPlayers == null || data.isFromMapEditor) return;
        if (ReferenceEquals(map, data.map)) return;
        AttachHistory(data.map, () =>
        {
            if (data.ModDataBag.TryGet(MarkerKey, out string value)) return Json.Decode<SaveMarker>(value);
            var marker = new SaveMarker();
            if (!data.ModDataBag.TrySet(MarkerKey, Json.Encode(marker))) throw new InvalidOperationException("Cannot store story identity in save.");
            return marker;
        });
        Notice = "Recording this installation's observed events. F8 opens the history.";
        if (Store!.RecoveredTail) Fail("Recovered incomplete journal tail; original retained", new InvalidDataException());
        log?.Trace("Attached campaign " + Store.CampaignId);
    }
    internal static void AttachHistory(Map nextMap, Func<SaveMarker> readMarker)
    {
        // Flush the old campaign first. Publish the new attachment only after all validation succeeds.
        if (pending.Count > 0) Flush();
        disabled = true;
        map = null; Store = null;
        previous.Clear(); objectiveOwners.Clear(); LastReport = "";
        var marker = readMarker();
        var candidate = new HistoryStore(directory, marker.CampaignId);
        // PrepareLoad already reconciles rewinds. Ensure never silently replaces data.
        if (candidate.NeedsRewind(marker)) throw new InvalidDataException("Unconfirmed history branch.");
        Store = candidate;
        map = nextMap;
        disabled = false;
    }
    internal static void Record(string kind, string nation, string description)
    {
        if (!IsReady) return;
        pending.Enqueue(new StoryEvent { Turn = Turn, GameDate = Date, Kind = kind, Nation = nation ?? "", Description = description });
        Flush();
    }
    internal static void Flush()
    {
        if (Store == null) return;
        while (pending.Count > 0) { Store.Append(pending.Peek()); log?.Trace("Recorded " + pending.Peek().Kind + " turn=" + pending.Peek().Turn + " id=" + pending.Peek().Id); pending.Dequeue(); }
    }
    internal static void Tick()
    {
        while (completions.TryDequeue(out var action)) action();
        if (MapGO.instance == null) return;
        Guard("Attach history", Ensure);
        if (IsReady) Guard("Retry pending events", Flush);
    }
    internal static void Scan(bool equipment = false)
    {
        if (!IsReady) return;
        if (MapGO.victoryPoints != null)
            foreach (var point in MapGO.victoryPoints)
            {
                var key = point.tile.GetCoord().ToString();
                if (objectiveOwners.TryGetValue(key, out var owner)) VictoryPoint(point.tile, owner);
                objectiveOwners[key] = point.tile.OwnerName;
            }
        foreach (var player in GameData.Instance.listOfPlayers)
        {
            var state = Capture(player, equipment);
            if (previous.TryGetValue(player.Name, out var old))
            {
                if (!equipment) state.Equipment = old.Equipment;
                foreach (var e in StateChanges.Compare(old, state, Turn, Date)) pending.Enqueue(e);
            }
            else
                pending.Enqueue(new StoryEvent { Turn = Turn, GameDate = Date, Kind = "Baseline", Nation = player.Country,
                    Description = $"Recording began for {player.Country}: faction {player.Faction}, {state.Objectives} victory points. Earlier events are not reconstructed." });
            previous[player.Name] = state;
        }
        Flush();
    }
    internal static void VictoryPoint(Tile tile, string oldOwner)
    {
        if (!IsReady) return;
        var key = tile.GetCoord().ToString();
        if (objectiveOwners.TryGetValue(key, out var observed) && observed == tile.OwnerName) return;
        var description = ObjectiveChanges.Describe(tile.Name, key, oldOwner, tile.OwnerName);
        if (description != null) Record("VictoryPoint", tile.ownerPlayer?.Country ?? tile.OwnerName, description);
        objectiveOwners[key] = tile.OwnerName;
    }
    internal static NationState Capture(Player p, bool equipment)
    {
        var available = p.ListOfUnitsResearched?.ToArray() ?? Array.Empty<string>();
        if (equipment && MapGO.instance != null)
            available = MapGO.instance.listOfAllUnits.Where(u => u.Country == p.Country && EquipmentEligibility.IsAvailable(
                GameData.Instance.map.Date, u.ProductionYear, u.ProductionMonth, u.EndUseYear, u.IsAvailable(p), u.IsPartisans,
                u.IsPrototype, PlayerSettings.Instance.PrototypesDisabled, u.IsCommander, GameData.Instance.map.IsCampaign,
                PlayerSettings.Instance.TechTree, p.ListOfUnitsResearched != null && p.ListOfUnitsResearched.Contains(u.Name)))
                .Select(u => u.Name).Distinct().ToArray();
        return new NationState { Name = p.Name, Country = p.Country, Faction = p.Faction, Outcome = p.GameOutcome.ToString(), Objectives = p.CountOurVPs(),
            Policies = p.ListActivePolicies?.Select(x => x.Type.ToString()).ToArray() ?? Array.Empty<string>(),
            Wars = p.PlayersAtWar?.ToArray() ?? Array.Empty<string>(), Allies = p.AlliedPlayers?.ToArray() ?? Array.Empty<string>(),
            Truces = p.Truces?.Keys.ToArray() ?? Array.Empty<string>(), Equipment = available };
    }
    internal static void TurnStarted()
    {
        Ensure(); Scan(true);
        var weather = GameData.Instance.map.GetCurrentTurnWeatherPredictionData();
        var forecast = GameData.Instance.map.GetNextTurnWeatherPredictionData();
        Record("Weather", TurnManager.currPlayer?.Country ?? "", $"Turn {Turn}: weather {weather.actualWeather}; next-turn forecast {forecast.predictedWeather} ({forecast.predictionAccuracy:0}% confidence).");
    }
    internal static void TurnEnding()
    {
        Scan(true);
        if (!IsReady || TurnManager.currPlayer == null) return;
        var nation = TurnManager.currPlayer.Country;
        Record("TurnSummary", nation, StateChanges.Summary(Store!.ReadTurn(nation, Turn), nation, Turn));
        Record("Objectives", nation, $"{nation} ended turn {Turn} holding {TurnManager.currPlayer.CountOurVPs()} victory points.");
    }
    internal static void Saving(GameData data)
    {
        if (!ReferenceEquals(data, GameData.Instance)) return;
        Ensure(); Scan(true); Flush();
        if (Store == null || disabled) return;
        if (!data.ModDataBag.TrySet(MarkerKey, Json.Encode(Store.Marker()))) throw new InvalidOperationException("Could not update story save marker.");
    }
    internal static void Saved(GameData data, string path)
    {
        if (ReferenceEquals(data, GameData.Instance) && IsReady) Store!.ExportBesideSave(path, Title);
    }
    internal static string Title => string.IsNullOrWhiteSpace(GameData.Instance.map?.ScenarioName) ? "Hex of Steel campaign" : GameData.Instance.map!.ScenarioName;
    internal static void End(string outcome)
    {
        if (!IsReady) return;
        Scan(true);
        if (!Store!.HasKind("EndGame")) Record("EndGame", TurnManager.currPlayer?.Country ?? "", "The campaign ended: " + outcome + ".");
        GenerateReport();
    }
    internal static void GenerateReport()
    {
        if (!IsReady || Interlocked.CompareExchange(ref reporting, 1, 0) != 0) return;
        try
        {
            Flush();
            var history = Store!;
            var events = history.Snapshot();
            var title = Title;
            var path = Path.Combine(directory, history.CampaignId + "-history.pdf");
            var configPath = Path.Combine(install, "config.local.json");
            var logo = Path.Combine(install, "Graphics", "logo.png");
            var token = lifetime.Token;
            Notice = "Preparing campaign history. Event recording continues.";
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await ReportService.GenerateAsync(path, title, events, async cancellation =>
                    {
                        var config = StoryConfiguration.Load(configPath);
                        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
                        using var http = new HttpClient(handler);
                        return await new HighlightSelector(http).SelectAsync(events, config,
                            Environment.GetEnvironmentVariable(config.ApiKeyEnvironmentVariable), cancellation).ConfigureAwait(false);
                    }, logo, token).ConfigureAwait(false);
                    completions.Enqueue(() =>
                    {
                        if (result.ModelFailure != null)
                        {
                            if (ReferenceEquals(Store, history)) Fail("Optional model unavailable; factual report generated", result.ModelFailure);
                            else log?.Failure("Model unavailable for completed report " + history.CampaignId, result.ModelFailure);
                        }
                        LastReport = path; Notice = "History PDF saved: " + path;
                    });
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { completions.Enqueue(() => Fail("Generate PDF", ex)); }
                finally { Interlocked.Exchange(ref reporting, 0); }
            }, token);
        }
        catch { Interlocked.Exchange(ref reporting, 0); throw; }
    }
}



internal static class NativeDialog
{
    // Windows-only release; use a blocking native dialog so cancellation precedes game-state mutation.
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    internal static extern int MessageBox(IntPtr owner, string text, string caption, uint type);
}
