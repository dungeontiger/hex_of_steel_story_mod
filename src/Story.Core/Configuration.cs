using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Story.Core;

public sealed class StoryConfiguration
{
    public string Endpoint { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKeyEnvironmentVariable { get; set; } = "HOS_STORY_API_KEY";
    public int TimeoutSeconds { get; set; } = 45;
    public void Validate()
    {
        if (TimeoutSeconds < 1 || TimeoutSeconds > 300) throw new ArgumentException("Timeout must be between 1 and 300 seconds.");
        if (string.IsNullOrWhiteSpace(Endpoint)) return;
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)
            || (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
            throw new ArgumentException("Use an HTTPS endpoint, or HTTP on localhost, without embedded credentials, query, or fragment.");
        if (string.IsNullOrWhiteSpace(Model)) throw new ArgumentException("A model name is required when an endpoint is configured.");
        if (!Regex.IsMatch(ApiKeyEnvironmentVariable, "^[A-Za-z_][A-Za-z0-9_]*$")) throw new ArgumentException("Invalid API key environment variable name.");
    }
    public static StoryConfiguration Load(string path)
    {
        var config = File.Exists(path) ? Json.Decode<StoryConfiguration>(File.ReadAllText(path)) : new StoryConfiguration();
        config.Validate(); return config;
    }
}

public sealed class DiagnosticLog
{
    private readonly string path;
    private readonly bool verbose;
    public DiagnosticLog(string path, bool verbose) { this.path = path; this.verbose = verbose; }
    public void Trace(string message) { if (verbose) Write("DEBUG", message); }
    public void Failure(string context, Exception error)
    {
        // Exception messages can contain credentials or request bodies. Record type and stack only.
        Write("ERROR", context + " | " + error.GetType().FullName + " | " + error.StackTrace);
    }
    private void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024)
            {
                var previous = path + ".previous";
                if (File.Exists(previous)) File.Delete(previous);
                File.Move(path, previous);
            }
            File.AppendAllText(path, DateTimeOffset.UtcNow.ToString("O") + " " + level + " " + message + Environment.NewLine);
        }
        catch { /* Logging must never crash the host game. */ }
    }
}
