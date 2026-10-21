using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Story.Core;

namespace Story.Mod;

[HarmonyPatch(typeof(GameData), nameof(GameData.SetData))]
internal static class LoadPatch
{
    static void Prefix(GameData p_gameData) { if (!Runtime.PrepareLoad(p_gameData)) throw new OperationCanceledException("Story history load cancelled. Current game and history retained."); }
    static void Postfix() => Runtime.Guard("Load history", Runtime.Loaded);
}

[HarmonyPatch(typeof(GameData), "SaveData")]
internal static class SavePatch
{
    static void Prefix(GameData __instance) => Runtime.Guard("Prepare save marker", () => Runtime.Saving(__instance));
    static void Postfix(GameData __instance, string p_fullPath) => Runtime.Guard("Export history beside save", () => Runtime.Saved(__instance, p_fullPath));
    static Exception? Finalizer(Exception? __exception) { if (__exception != null) Runtime.Fail("Game save", __exception); return __exception; }
}

[HarmonyPatch(typeof(GameData), "SaveDataAsync")]
internal static class AsyncSavePatch
{
    static void Prefix(GameData __instance, string p_fullPath)
    {
        Runtime.Guard("Prepare async save history", () => { Runtime.Saving(__instance); Runtime.Saved(__instance, p_fullPath); });
    }
}

[HarmonyPatch(typeof(TurnManager), "Start")]
internal static class StartPatch
{
    static void Postfix() => Runtime.Guard("Start turn recording", Runtime.TurnStarted);
}

[HarmonyPatch(typeof(TurnManager), nameof(TurnManager.NextTurn))]
internal static class TurnPatch
{
    static void Prefix() => Runtime.Guard("Turn summary", Runtime.TurnEnding);
    static void Postfix() => Runtime.Guard("Next turn recording", Runtime.TurnStarted);
}

[HarmonyPatch]
internal static class NationPatch
{
    static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new HashSet<string> { "AddPolicy", "RemovePolicy", "ChangeFaction", "AddWarWith", "RemoveWarWith", "AddAllianceWith", "RemoveAllianceWith", "AddTruceWith", "RemoveTruceWith" };
        return typeof(Player).GetMethods().Where(m => names.Contains(m.Name));
    }
    static void Prefix() => Runtime.Guard("Before diplomacy change", () => Runtime.Scan());
    static void Postfix() => Runtime.Guard("Diplomacy change", () => Runtime.Scan());
}

[HarmonyPatch(typeof(Unit), nameof(Unit.AddToKillCount))]
internal static class KillPatch
{
    static void Postfix(Unit __instance, Unit p_unitKilled) => Runtime.Guard("Unit destruction", () =>
        Runtime.Record("UnitDestroyed", __instance.OwnerPlayer?.Country ?? __instance.OwnerName,
            $"{__instance.GetUnitName()} (unit {__instance.ID}, {__instance.OwnerName}) destroyed {p_unitKilled.GetUnitName()} (unit {p_unitKilled.ID}, {p_unitKilled.OwnerName})."));
}

[HarmonyPatch(typeof(Unit), nameof(Unit.AddXP))]
internal static class RankPatch
{
    static void Prefix(Unit __instance, out byte __state) => __state = __instance.Lvl;
    static void Postfix(Unit __instance, byte __state) => Runtime.Guard("Unit promotion", () =>
    {
        if (__instance.Lvl > __state) Runtime.Record("Promotion", __instance.OwnerPlayer?.Country ?? __instance.OwnerName,
            $"{__instance.GetUnitName()} (unit {__instance.ID}) advanced from rank {__state} to {__instance.Lvl}.");
    });
}

[HarmonyPatch(typeof(Unit), "AddHero")]
internal static class HeroPatch
{
    static void Prefix(Unit __instance, out string? __state) => __state = __instance.Hero?.Name;
    static void Postfix(Unit __instance, string? __state) => Runtime.Guard("Hero emerged", () =>
    {
        if (__instance.Hero != null && __instance.Hero.Name != __state)
            Runtime.Record("Hero", __instance.OwnerPlayer?.Country ?? __instance.OwnerName, $"{__instance.Hero.Name} emerged with {__instance.GetUnitName()} (unit {__instance.ID}).");
    });
}

[HarmonyPatch(typeof(UnitGO), nameof(UnitGO.TryCaptureTile))]
internal static class CapturePatch
{
    static void Prefix(Tile p_tile, out string __state) => __state = p_tile.OwnerName;
    static void Postfix(Tile p_tile, bool __result, string __state) => Runtime.Guard("Victory point capture", () =>
    {
        if (__result && p_tile.IsVictoryPoint && p_tile.OwnerName != __state)
            Runtime.VictoryPoint(p_tile, __state);
    });
}

[HarmonyPatch(typeof(DiplomacyGO), "SendMoneyTo")]
internal static class MoneyPatch
{
    static void Prefix(out int __state) => __state = TurnManager.currPlayer?.MoneyAmount ?? 0;
    static void Postfix(string p_playerName, int p_amount, int __state) => Runtime.Guard("Money transfer", () =>
    {
        var sender = TurnManager.currPlayer;
        if (sender != null && p_amount > 0 && sender.MoneyAmount == __state - p_amount)
            Runtime.Record("MoneyTransfer", sender.Country, $"{sender.Country} sent {p_amount} to {p_playerName}.");
    });
}

[HarmonyPatch(typeof(UIManager), nameof(UIManager.ShowRelationshipChangeMessage))]
internal static class RelationshipPatch
{
    static void Postfix(string country1, string country2, string text) => Runtime.Guard("Diplomatic announcement", () =>
    {
        var capitulated = LocalizationManager.Translate("relationship-capitulated");
        var kind = !string.IsNullOrEmpty(capitulated) && text.IndexOf(capitulated, StringComparison.OrdinalIgnoreCase) >= 0 ? "Capitulation" : "DiplomaticAnnouncement";
        Runtime.Record(kind, country1, country1 + (string.IsNullOrEmpty(country2) ? "" : " / " + country2) + ": " + text);
    });
}

[HarmonyPatch(typeof(UIManager), nameof(UIManager.Forfeit))]
internal static class SurrenderPatch
{
    internal static bool InSurrender;
    static void Prefix() => InSurrender = true;
    static Exception? Finalizer(Exception? __exception) { InSurrender = false; return __exception; }
}

[HarmonyPatch(typeof(UIManager), nameof(UIManager.EndTheGame))]
internal static class EndPatch
{
    static void Postfix(GameOutcomes p_outcome) => Runtime.Guard("End game", () =>
        Runtime.End(SurrenderPatch.InSurrender ? "surrender" : p_outcome.ToString().ToLowerInvariant()));
}



[HarmonyPatch(typeof(Utils), nameof(Utils.IsPointerOverUIObject))]
internal static class PointerPatch
{
    static void Postfix(ref bool __result) { if (StoryWindow.PointerOverWindow()) __result = true; }
}

[HarmonyPatch(typeof(GameData), nameof(GameData.Load))]
internal static class LoadPathPatch
{
    static void Prefix(string p_filePath) => Runtime.LoadingPath = p_filePath;
    static Exception? Finalizer(Exception? __exception) { Runtime.LoadingPath = null; return __exception; }
}
