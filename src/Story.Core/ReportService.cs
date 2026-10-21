using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Story.Core;

public sealed class ReportResult
{
    public string Path { get; set; } = "";
    public Exception? ModelFailure { get; set; }
}

public static class ReportService
{
    public static async Task<ReportResult> GenerateAsync(string path, string title, StoryEvent[] events,
        Func<CancellationToken, Task<string[]>> selectHighlights, string? logo, CancellationToken cancellation)
    {
        var result = new ReportResult { Path = path };
        string[] selected = Array.Empty<string>();
        try { selected = await selectHighlights(cancellation).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { throw; }
        catch (Exception ex) { result.ModelFailure = ex; }
        cancellation.ThrowIfCancellationRequested();
        // Always retain the full source alongside the abbreviated history.
        AtomicFile.Write(path + ".events.json", Json.Encode(events));
        HistoryPdf.Write(path, Narrative.Compose(title, events, selected), logo);
        return result;
    }
}
