using System;
using System.Collections.Generic;
using System.Linq;

namespace Story.Core;

public sealed class NationState
{
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    public string Faction { get; set; } = "";
    public string Outcome { get; set; } = "NONE";
    public int Objectives { get; set; }
    public string[] Policies { get; set; } = Array.Empty<string>();
    public string[] Wars { get; set; } = Array.Empty<string>();
    public string[] Allies { get; set; } = Array.Empty<string>();
    public string[] Truces { get; set; } = Array.Empty<string>();
    public string[] Equipment { get; set; } = Array.Empty<string>();
}

public static class StateChanges
{
    public static IEnumerable<StoryEvent> Compare(NationState before, NationState after, int turn, string date)
    {
        StoryEvent Make(string kind, string text) => new StoryEvent { Turn = turn, GameDate = date, Nation = after.Country, Kind = kind, Description = text };
        if (before.Faction != after.Faction) yield return Make("Faction", $"{after.Country} changed faction from {before.Faction} to {after.Faction}.");
        if (before.Outcome != after.Outcome) yield return Make("Outcome", $"{after.Country}'s recorded outcome changed to {after.Outcome}.");
        if (before.Objectives != after.Objectives) yield return Make("Objectives", $"{after.Country} held {after.Objectives} victory points (previously {before.Objectives}).");
        foreach (var p in after.Policies.Except(before.Policies)) yield return Make("Policy", $"{after.Country} adopted policy {p}.");
        foreach (var p in before.Policies.Except(after.Policies)) yield return Make("Policy", $"{after.Country} removed policy {p}.");
        foreach (var p in after.Wars.Except(before.Wars)) yield return Make("Diplomacy", $"{after.Country} entered a state of war with {p}.");
        foreach (var p in before.Wars.Except(after.Wars)) yield return Make("Diplomacy", $"{after.Country}'s state of war with {p} ended.");
        foreach (var p in after.Allies.Except(before.Allies)) yield return Make("Diplomacy", $"{after.Country} formed an alliance with {p}.");
        foreach (var p in before.Allies.Except(after.Allies)) yield return Make("Diplomacy", $"{after.Country}'s alliance with {p} ended.");
        foreach (var p in after.Truces.Except(before.Truces)) yield return Make("Diplomacy", $"{after.Country} entered a truce with {p}.");
        foreach (var p in before.Truces.Except(after.Truces)) yield return Make("Diplomacy", $"{after.Country}'s truce with {p} ended.");
        foreach (var p in after.Equipment.Except(before.Equipment)) yield return Make("Equipment", $"{p} became available to {after.Country}.");
    }
    public static string Summary(IEnumerable<StoryEvent> events, string nation, int turn)
    {
        var items = events.Where(e => e.Nation == nation && e.Turn == turn && e.Kind != "TurnSummary").ToArray();
        return $"{nation}, turn {turn}: " + (items.Length == 0 ? "no observed events were recorded."
            : string.Join("; ", items.GroupBy(e => e.Kind).OrderBy(g => g.Key).Select(g => $"{g.Count()} {g.Key} event(s)")) + ". This reflects observed events, not unrecorded plans or intentions.");
    }
}
