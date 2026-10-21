using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Story.Core;
using Xunit;

namespace Story.Tests;

public sealed class CoreTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "hos-story-tests-" + Guid.NewGuid().ToString("N"));
    private readonly string id = Guid.NewGuid().ToString("N");
    private HistoryStore Store() => new HistoryStore(dir, id);
    private static StoryEvent Event(int turn = 1, string kind = "Diplomacy") => new StoryEvent { Turn = turn, GameDate = "1940-05-10", Nation = "France", Kind = kind, Description = "France formed an alliance with Britain." };
    public void Dispose() { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

    [Fact] public void ObjectiveTransferRecordsBothOwnersAndIgnoresNoChange()
    {
        Assert.Null(ObjectiveChanges.Describe("Paris", "1,2", "France", "France"));
        var text = ObjectiveChanges.Describe("Paris", "1,2", "France", "Germany");
        Assert.Contains("Germany", text); Assert.Contains("lost by France", text);
    }
    [Fact] public void DurableAppendSurvivesRestartAndDeduplicates()
    {
        var store = Store(); var item = Event();
        Assert.True(store.Append(item)); Assert.False(store.Append(item));
        item.Description = "mutated";
        var read = Store().Snapshot();
        Assert.Single(read); Assert.NotEqual(item.Description, read[0].Description);
        Assert.Equal(item.RecordedAt, read[0].RecordedAt);
    }
    [Fact] public void ReloadReplacesOnlyConfirmedFutureEvents()
    {
        var store = Store(); store.Append(Event()); var marker = store.Marker(); store.Append(Event(2));
        Assert.True(store.NeedsRewind(marker));
        Assert.Throws<InvalidOperationException>(() => store.Rewind(marker, false));
        Assert.Equal(2, Store().Snapshot().Length);
        store.Rewind(marker, true); store.Append(Event(2));
        Assert.Equal(2, Store().Snapshot().Length); Assert.True(File.Exists(store.PathName + ".bak"));
        Assert.Equal(marker.LastId, Store().Snapshot()[0].Id);
    }
    [Fact] public void ReplacedBranchCannotBeSilentlyMixed()
    {
        var store = Store(); store.Append(Event()); var early = store.Marker(); store.Append(Event(2)); var future = store.Marker();
        store.Rewind(early, true); store.Append(Event(2));
        Assert.Throws<InvalidDataException>(() => store.NeedsRewind(future));
    }
    [Fact] public void MissingHistoryAndUnknownSchemaFailClosed()
    {
        var store = Store(); Assert.Throws<InvalidDataException>(() => store.NeedsRewind(new SaveMarker { CampaignId = id, Count = 2 }));
        Assert.Throws<InvalidDataException>(() => store.NeedsRewind(new SaveMarker { CampaignId = id, Schema = 99 }));
        Assert.Throws<InvalidDataException>(() => new HistoryStore(dir, "../../escape"));
    }
    [Fact] public void TornFinalRecordRecoversAndRetainsOriginal()
    {
        var store = Store(); store.Append(Event()); File.AppendAllText(store.PathName, "{\"Id\":");
        var recovered = Store(); Assert.True(recovered.RecoveredTail); Assert.Single(recovered.Snapshot());
        Assert.Single(Directory.GetFiles(dir, "*.incomplete-*"));
        recovered.Append(Event(2)); Assert.Equal(2, Store().Snapshot().Length);
    }
    [Fact] public void InteriorCorruptionIsNeverSilentlyDiscarded()
    {
        var store = Store(); store.Append(Event()); File.AppendAllText(store.PathName, "BROKEN\n" + Json.Encode(Event()) + "\n");
        Assert.Throws<InvalidDataException>(() => Store());
        Assert.Contains("BROKEN", File.ReadAllText(store.PathName));
    }
    [Fact] public void ValidFinalRecordWithoutNewlineGetsDelimiter()
    {
        var store = Store(); File.WriteAllText(store.PathName, Json.Encode(Event()));
        Store().Append(Event(2)); Assert.Equal(2, Store().Snapshot().Length);
    }
    [Fact] public void FailedWriteDoesNotAdvanceSaveMarker()
    {
        var store = Store(); store.Append(Event()); var marker = store.Marker();
        using (var locked = new FileStream(store.PathName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Assert.Throws<IOException>(() => store.Append(Event(2)));
        Assert.Equal(marker.LastId, store.Marker().LastId); Assert.Equal(marker.Count, store.Marker().Count);
    }
    [Fact] public void ExportMatchesSaveAndPreservesUnicode()
    {
        var store = Store(); var item = Event(); item.Description = "Łódź – opération française"; store.Append(item);
        store.ExportBesideSave(Path.Combine(dir, "My Campaign.sav"), "Campaign");
        var text = File.ReadAllText(Path.Combine(dir, "My Campaign.sav.story.json"));
        Assert.Contains(item.Id, text); Assert.Contains("Łódź", text); Assert.Contains(id, text);
    }
    [Fact] public void SaveSidecarRestoresMissingJournalAndRejectsWrongSave()
    {
        var source = Store(); source.Append(Event());
        var save = Path.Combine(dir, "portable.sav"); source.ExportBesideSave(save, "Portable");
        var destination = Path.Combine(dir, "restored");
        HistoryStore.RestoreFromExport(destination, source.Marker(), save + ".story.json");
        var restored = new HistoryStore(destination, id);
        Assert.Equal(source.Marker().LastId, restored.Marker().LastId);
        Assert.Throws<InvalidOperationException>(() => HistoryStore.RestoreFromExport(destination, source.Marker(), save + ".story.json"));
        var wrong = source.Marker(); wrong.LastId = Guid.NewGuid().ToString("N");
        Assert.Throws<InvalidDataException>(() => HistoryStore.RestoreFromExport(Path.Combine(dir, "wrong"), wrong, save + ".story.json"));
    }
    [Fact] public void InvalidEventDoesNotChangeJournalOrMarker()
    {
        var store = Store(); store.Append(Event());
        var original = File.ReadAllText(store.PathName);
        var marker = store.Marker();
        var missingNation = Event(); missingNation.Nation = null!;
        var missingDate = Event(); missingDate.GameDate = null!;
        foreach (var item in new[] { null!, missingNation, missingDate })
            Assert.Throws<InvalidDataException>(() => store.Append(item));
        Assert.Equal(original, File.ReadAllText(store.PathName));
        Assert.Equal(marker.LastId, store.Marker().LastId);
        Assert.Equal(marker.Count, store.Marker().Count);
    }
    [Fact] public void NullRecordInExportCannotPartiallyRestoreHistory()
    {
        var source = Store(); source.Append(Event()); source.Append(Event(2));
        var marker = source.Marker();
        var export = Path.Combine(dir, "invalid.story.json");
        File.WriteAllText(export, Json.Encode(new { schema = 1, marker, events = new StoryEvent[] { null!, source.Snapshot()[1] } }));
        var destination = Path.Combine(dir, "destination");
        Assert.Throws<InvalidDataException>(() => HistoryStore.RestoreFromExport(destination, marker, export));
        Assert.Empty(new HistoryStore(destination, id).Snapshot());
        Assert.False(File.Exists(Path.Combine(destination, id + ".journal.jsonl")));
        // Correcting the sidecar must allow a subsequent complete recovery.
        source.ExportBesideSave(Path.Combine(dir, "valid.sav"), "Campaign");
        HistoryStore.RestoreFromExport(destination, marker, Path.Combine(dir, "valid.sav.story.json"));
        Assert.Equal(marker.LastId, new HistoryStore(destination, id).Marker().LastId);
    }
    [Fact] public void NullInteriorRecordRetainsOriginalJournal()
    {
        var store = Store(); store.Append(Event());
        File.AppendAllText(store.PathName, "null\n" + Json.Encode(Event(2)) + "\n");
        var original = File.ReadAllText(store.PathName);
        Assert.Throws<InvalidDataException>(() => Store());
        Assert.Equal(original, File.ReadAllText(store.PathName));
    }
    [Fact] public void ViewerPagesAndTurnSummariesReadOnlyTheirOwnRecords()
    {
        var store = Store(); store.Append(Event(1)); store.Append(Event(2)); store.Append(Event(3));
        Assert.Equal(2, Assert.Single(store.ReadPage(1, 1)).Turn);
        Assert.Equal(3, Assert.Single(store.ReadTurn("France", 3)).Turn);
        Assert.Empty(store.ReadTurn("Germany", 3)); Assert.True(store.HasKind("Diplomacy"));
        Assert.Throws<ArgumentOutOfRangeException>(() => store.ReadPage(-1, 10));
    }
    [Fact] public void SnapshotCannotMutateStoredEvents()
    {
        var store = Store(); store.Append(Event()); store.Snapshot()[0].Description = "bad";
        Assert.DoesNotContain("bad", store.Snapshot()[0].Description);
    }
    [Fact] public void JsonDoesNotInstantiateTypesFromFiles()
    {
        var item = Json.Decode<StoryEvent>("{\"$type\":\"System.IO.FileInfo, mscorlib\",\"Kind\":\"Test\"}");
        Assert.IsType<StoryEvent>(item);
    }
    [Fact] public void StateDiffCapturesAdditionsAndRemovalsWithoutDuplicates()
    {
        var before = new NationState { Country = "France", Faction = "Neutral", Policies = new[] { "Old" }, Wars = new[] { "Germany" } };
        var after = new NationState { Country = "France", Faction = "Allies", Policies = new[] { "New" }, Allies = new[] { "Britain" }, Equipment = new[] { "Tank" }, Objectives = 2 };
        var changes = StateChanges.Compare(before, after, 4, "1940-01-01").ToArray();
        Assert.Equal(7, changes.Length); Assert.Contains(changes, e => e.Kind == "Equipment");
        Assert.Contains(changes, e => e.Description.Contains("war with Germany ended"));
        Assert.Empty(StateChanges.Compare(after, after, 4, "1940-01-01"));
    }
    [Theory]
    [InlineData("http://example.com/v1/chat/completions")]
    [InlineData("https://user:secret@example.com/api")]
    [InlineData("https://example.com/api?key=secret")]
    [InlineData("file:///secrets")]
    public void ConfigurationRejectsUnsafeEndpoints(string endpoint)
    {
        Assert.Throws<ArgumentException>(() => new StoryConfiguration { Endpoint = endpoint, Model = "model" }.Validate());
    }
    [Theory]
    [InlineData("http://localhost:1234/v1/chat/completions")]
    [InlineData("https://example.com/v1/chat/completions")]
    public void ConfigurationAcceptsRemoteTlsAndLocalModels(string endpoint) => new StoryConfiguration { Endpoint = endpoint, Model = "model" }.Validate();

    private sealed class Handler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> response;
        public Handler(Func<HttpRequestMessage, HttpResponseMessage> response) { this.response = response; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(response(request));
    }
    [Fact] public async Task ModelCanOnlySelectKnownEvents()
    {
        using var client = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Json.Encode(new { choices = new[] { new { message = new { content = "{\"eventIds\":[\"invented\"]}" } } } })) }));
        await Assert.ThrowsAsync<InvalidDataException>(() => new HighlightSelector(client).SelectAsync(new[] { Event() }, new StoryConfiguration { Endpoint = "https://example.com/api", Model = "local" }, "secret", CancellationToken.None));
    }
    [Fact] public async Task ModelSelectionPreservesFactsAndUsesBearerHeader()
    {
        var item = Event();
        using var client = new HttpClient(new Handler(request => {
            Assert.Equal("Bearer", request.Headers.Authorization!.Scheme); Assert.Equal("secret", request.Headers.Authorization.Parameter);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Json.Encode(new { choices = new[] { new { message = new { content = Json.Encode(new { eventIds = new[] { item.Id } }) } } } })) };
        }));
        var ids = await new HighlightSelector(client).SelectAsync(new[] { item }, new StoryConfiguration { Endpoint = "https://example.com/api", Model = "test" }, "secret", CancellationToken.None);
        Assert.Contains(item.Description, string.Join(" ", Narrative.Compose("History", new[] { item }, ids)));
    }
    [Fact] public async Task NoModelDoesNotSendAnyNetworkRequest()
    {
        using var client = new HttpClient(new Handler(_ => throw new Exception("Should not send")));
        Assert.Empty(await new HighlightSelector(client).SelectAsync(new[] { Event() }, new StoryConfiguration(), null, CancellationToken.None));
    }
    [Fact] public void DiagnosticsExcludeSecretsAndReleaseTrace()
    {
        var path = Path.Combine(dir, "log"); var log = new DiagnosticLog(path, false);
        log.Trace("hidden"); Assert.False(File.Exists(path));
        log.Failure("Model request", new Exception("SECRET-API-KEY"));
        Assert.DoesNotContain("SECRET", File.ReadAllText(path)); Assert.Contains("System.Exception", File.ReadAllText(path));
    }
    [Fact] public void HistoricalEligibilityRespectsCampaignAndResearchModes()
    {
        var date = new DateTime(1940, 5, 1);
        bool Allowed(int month, bool tech = false, bool researched = false, bool campaign = false) => EquipmentEligibility.IsAvailable(date, 1940, month, 1945, true, false, false, false, false, campaign, tech, researched);
        Assert.True(Allowed(5)); Assert.False(Allowed(6)); Assert.True(Allowed(0));
        Assert.False(Allowed(1, true)); Assert.True(Allowed(6, true, true)); Assert.False(Allowed(6, true, true, true));
    }
    [Fact] public async Task ModelFailureStillProducesPdfAndFullSource()
    {
        var path = Path.Combine(dir, "fallback.pdf");
        var result = await ReportService.GenerateAsync(path, "Fallback campaign", new[] { Event() },
            _ => throw new HttpRequestException("provider unavailable"), null, CancellationToken.None);
        Assert.IsType<HttpRequestException>(result.ModelFailure);
        Assert.True(File.Exists(path)); Assert.True(File.Exists(path + ".events.json"));
    }
    [Fact] public async Task CancelledReportDoesNotProduceOutput()
    {
        var path = Path.Combine(dir, "cancelled.pdf");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReportService.GenerateAsync(path, "Cancelled", new[] { Event() },
            token => Task.FromCanceled<string[]>(token), null, cancellation.Token));
        Assert.False(File.Exists(path));
    }
    [Fact] public void LongNarrativeAndLongNamesPaginateWithoutLosingText()
    {
        var path = Path.Combine(dir, "long.pdf");
        var paragraphs = Enumerable.Range(0, 90).Select(i => "Turn " + i + ": " + new string('W', 160) + " Łódź and France.").ToArray();
        HistoryPdf.Write(path, paragraphs, Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../branding_images/logo_idea.png")));
        using var document = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.True(document.PageCount > 2);
    }
    [Fact] public void EndToEndHistorySurvivesReplayAndProducesReadablePdf()
    {
        var store = Store(); store.Append(Event()); var marker = store.Marker(); store.Append(Event(2));
        store = Store(); store.Rewind(marker, true);
        var end = Event(2, "EndGame"); end.Description = "The campaign ended: victory."; store.Append(end);
        var paragraphs = Narrative.Compose("Campaign of France", store.Snapshot());
        var path = Path.Combine(dir, "history.pdf"); HistoryPdf.Write(path, paragraphs, Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../branding_images/logo_idea.png")));
        Assert.True(new FileInfo(path).Length > 1000);
        using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(path, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.InRange(pdf.PageCount, 1, 2); Assert.Equal("Campaign of France", pdf.Info.Title);
        Assert.Contains(paragraphs, p => p.Contains("victory"));
        // Retain one non-private fixture for visual inspection, outside source control.
        var output = Environment.GetEnvironmentVariable("STORY_TEST_ARTIFACTS");
        if (!string.IsNullOrEmpty(output)) { Directory.CreateDirectory(output); File.Copy(path, Path.Combine(output, "sample-history.pdf"), true); }
    }
}
