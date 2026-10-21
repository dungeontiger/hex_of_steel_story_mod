using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Story.Mod;
using Xunit;

namespace Story.GameTests;

public sealed class HookTests
{
    [Theory]
    [InlineData("marker")]
    [InlineData("journal")]
    [InlineData("branch")]
    [InlineData("flush")]
    public void FailedCampaignSwitchDetachesOldHistoryAndCanRetry(string failure)
    {
        var runtime = typeof(StoryModification).Assembly.GetType("Story.Mod.Runtime", true)!;
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var fields = new[] { "directory", "Store", "map", "disabled", "LastReport" }
            .Select(name => runtime.GetField(name, flags)!).ToArray();
        var original = fields.Select(field => field.GetValue(null)).ToArray();
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hos-attach-" + Guid.NewGuid().ToString("N"));
        // Only reference identity is used; no Unity constructors or engine calls are required.
        var oldMap = (Map)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Map));
        var nextMap = (Map)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Map));
        var attach = runtime.GetMethod("AttachHistory", flags)!;
        var attached = runtime.GetMethod("HasHistoryFor", flags)!;
        var pending = (System.Collections.Generic.Queue<Story.Core.StoryEvent>)runtime.GetField("pending", flags)!.GetValue(null)!;
        Assert.Empty(pending);
        try
        {
            fields[0].SetValue(null, directory);
            var oldStore = new Story.Core.HistoryStore(directory, Guid.NewGuid().ToString("N"));
            oldStore.Append(new Story.Core.StoryEvent { Kind = "Baseline", Description = "Original campaign." });
            fields[1].SetValue(null, oldStore); fields[2].SetValue(null, oldMap);
            fields[3].SetValue(null, false); fields[4].SetValue(null, "old-campaign.pdf");
            Assert.Equal(true, attached.Invoke(null, new object[] { oldMap }));
            Assert.Equal(false, attached.Invoke(null, new object[] { nextMap }));
            var nextStore = new Story.Core.HistoryStore(directory, Guid.NewGuid().ToString("N"));
            var marker = nextStore.Marker();
            if (failure == "journal") System.IO.File.WriteAllText(nextStore.PathName, "BROKEN\n");
            if (failure == "branch") nextStore.Append(new Story.Core.StoryEvent { Kind = "Baseline", Description = "Later history." });
            Func<Story.Core.SaveMarker> read = () => failure == "marker"
                ? throw new System.IO.InvalidDataException("Malformed marker.") : marker;
            System.IO.FileStream? locked = null;
            TargetInvocationException error;
            try
            {
                if (failure == "flush")
                {
                    pending.Enqueue(new Story.Core.StoryEvent { Kind = "Diplomacy", Description = "Pending old-campaign event." });
                    locked = new System.IO.FileStream(oldStore.PathName, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
                }
                error = Assert.Throws<TargetInvocationException>(() => attach.Invoke(null, new object[] { nextMap, read }));
            }
            finally { locked?.Dispose(); }
            if (failure == "flush")
            {
                Assert.IsAssignableFrom<System.IO.IOException>(error.InnerException);
                Assert.Same(oldStore, fields[1].GetValue(null)); Assert.Single(pending);
            }
            else
            {
                Assert.IsType<System.IO.InvalidDataException>(error.InnerException);
                Assert.Null(fields[1].GetValue(null)); Assert.Null(fields[2].GetValue(null));
                Assert.Equal(true, fields[3].GetValue(null)); Assert.Equal("", fields[4].GetValue(null));
                Assert.Equal(false, attached.Invoke(null, new object[] { oldMap }));
            }
            Assert.Equal(false, attached.Invoke(null, new object[] { nextMap }));
            Assert.Single(new Story.Core.HistoryStore(directory, oldStore.CampaignId).Snapshot());
            if (failure == "journal") System.IO.File.Delete(nextStore.PathName);
            if (failure == "branch") marker = nextStore.Marker();
            Func<Story.Core.SaveMarker> corrected = () => marker;
            attach.Invoke(null, new object[] { nextMap, corrected });
            Assert.Equal(marker.CampaignId, ((Story.Core.HistoryStore)fields[1].GetValue(null)!).CampaignId);
            Assert.Same(nextMap, fields[2].GetValue(null)); Assert.Equal(false, fields[3].GetValue(null));
            Assert.Equal(true, attached.Invoke(null, new object[] { nextMap }));
            Assert.Equal(false, attached.Invoke(null, new object[] { oldMap }));
            Assert.Empty(pending);
            Assert.Equal(failure == "flush" ? 2 : 1, new Story.Core.HistoryStore(directory, oldStore.CampaignId).Snapshot().Length);
            Assert.DoesNotContain(((Story.Core.HistoryStore)fields[1].GetValue(null)!).Snapshot(), e => e.Description == "Pending old-campaign event.");
        }
        finally
        {
            pending.Clear();
            for (int i = 0; i < fields.Length; i++) fields[i].SetValue(null, original[i]);
            if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
        }
    }
    [Fact]
    public void PdfGenerationWorksUnderFrameworkWithGameDependencies()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "hos-framework-" + Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            Story.Core.HistoryPdf.Write(path, new[] { "Framework compatibility", "A recorded campaign history." });
            Assert.True(new System.IO.FileInfo(path).Length > 1000);
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }
    [Fact]
    public void HarmonyCanPatchManagedGameMethodAndCleanlyUnpatch()
    {
        // Unity internal calls cannot be JIT-compiled by the desktop CLR. Exercise a managed hook here;
        // patching every Unity method belongs to the in-game smoke checklist.
        var harmony = new Harmony("books.kingtiger.story.tests");
        var patch = typeof(StoryModification).Assembly.GetType("Story.Mod.KillPatch", true)!;
        try
        {
            harmony.CreateClassProcessor(patch).Patch();
            Assert.Contains(AccessTools.Method(typeof(Unit), "AddToKillCount"), harmony.GetPatchedMethods());
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Assert.Empty(harmony.GetPatchedMethods());
    }
    [Fact]
    public void AllDeclaredHooksMatchInstalledGameSignatures()
    {
        int hooks = 0;
        foreach (var type in typeof(StoryModification).Assembly.GetTypes())
        {
            var patch = type.GetCustomAttributes(typeof(HarmonyPatch), false).Cast<HarmonyPatch>().FirstOrDefault();
            if (patch == null) continue;
            if (patch.info.declaringType == null)
            {
                var targets = type.GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic)!;
                var methods = ((System.Collections.Generic.IEnumerable<MethodBase>)targets.Invoke(null, null)).ToArray();
                Assert.True(methods.Length >= 9); hooks += methods.Length; continue;
            }
            var original = AccessTools.Method(patch.info.declaringType, patch.info.methodName);
            Assert.NotNull(original);
            foreach (var hook in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.Name == "Prefix" || m.Name == "Postfix"))
                foreach (var parameter in hook.GetParameters().Where(p => !p.Name!.StartsWith("__", StringComparison.Ordinal)))
                {
                    var source = original.GetParameters().SingleOrDefault(p => p.Name == parameter.Name);
                    Assert.NotNull(source);
                    Assert.Equal(source.ParameterType, parameter.ParameterType);
                }
            hooks++;
        }
        Assert.True(hooks >= 20);
    }
    [Fact]
    public void GameRecognizesPublicModConstructorAndLifecycle()
    {
        Assert.True(typeof(GameModification).IsAssignableFrom(typeof(StoryModification)));
        Assert.NotNull(typeof(StoryModification).GetConstructor(new[] { typeof(global::Mod) }));
        Assert.Equal(typeof(StoryModification), typeof(StoryModification).GetMethod("OnModInitialization")!.DeclaringType);
        Assert.Equal(typeof(StoryModification), typeof(StoryModification).GetMethod("OnModUnloaded")!.DeclaringType);
    }
    [Fact]
    public void SaveMarkerSurvivesRealGamePropertyBagSerialization()
    {
        var bag = new ModPropertyBag();
        // The game's key hashing uses pure managed code; no Unity engine is required.
        var expected = Story.Core.Json.Encode(new Story.Core.SaveMarker { Count = 3, LastId = "event" });
        Assert.True(bag.TrySet("books.kingtiger.story.marker.v1", expected));
        using (var memory = new System.IO.MemoryStream())
        {
            var serializer = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            serializer.Serialize(memory, bag); memory.Position = 0;
            var restored = (ModPropertyBag)serializer.Deserialize(memory);
            Assert.True(restored.TryGet("books.kingtiger.story.marker.v1", out string actual));
            Assert.Equal(expected, actual);
        }
    }
}
