using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Story.Mod;
using Xunit;

namespace Story.GameTests;

public sealed class HookTests
{
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
