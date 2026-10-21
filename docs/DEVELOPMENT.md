# Development and validation

## Build

Install the .NET 8 SDK and a licensed Windows copy of Hex of Steel. From the repository root:

```powershell
./scripts/build.ps1 -Configuration Debug
./scripts/build.ps1 -Configuration Release
# Non-default Steam library:
./scripts/build.ps1 -Configuration Release -GamePath 'D:\SteamLibrary\steamapps\common\Hex of Steel'
```

The script uses `.tools/dotnet/dotnet.exe` if present, otherwise the SDK on PATH. `global.json` selects the .NET 8.0.4xx SDK family. Dependencies are pinned with NuGet lock files. Scripts stop on build or test failures. `-SkipTests` is available for an intentional packaging-only development cycle; do not use it for release validation.

Output: `artifacts/KingTigerStory-0.1.0-<configuration>.zip` and its SHA-256 file. Game assemblies, saves, local configuration, and development tools are excluded. Debug packages include symbols and verbose event diagnostics. Release diagnostics record failures only; history events are still recorded in both modes.

## Architecture

- `Story.Core`: game-independent journal, event comparisons, configuration checks, bounded model requests, narrative selection, and PDF rendering. Targets .NET Standard 2.0.
- `Story.Mod`: .NET Framework 4.8 game modification and Harmony patches, plus a Unity IMGUI viewer. Uses installed game assemblies as nonredistributed references.
- `Story.Tests`: unit, persistence integration, network-stub, and end-to-end journal-to-PDF tests on .NET 8.
- `Story.GameTests`: local .NET Framework tests against actual game assemblies. Checks hook parameter compatibility, a managed Harmony patch, mod construction, and real mod-property serialization.

The record schema is version 1. Campaign IDs and event IDs are random GUIDs. Save markers include schema, campaign, event count, and last event identity. Histories are append-only between explicit approved rewinds. JSON type-name activation is disabled. Atomic sidecar replacement retains a backup; journal appends flush to disk. There is one game-thread writer per active campaign.

Reports run on a worker using a detached snapshot; worker code never reads Unity objects. Credentials live in environment variables. Logs intentionally omit exception messages and HTTP bodies, which can contain credentials; they include failure type, stack, campaign, and turn.

## Required in-game smoke test

Use a disposable scenario and back up any save before testing.

1. Enable the Debug mod, start a scenario, and confirm F8 opens a usable history window. Click and scroll it without moving units underneath.
2. Perform a policy change, faction/alliance/war/truce change, money transfer, unit kill, promotion, hero grant, and VP capture. Check both successful and rejected actions; rejected actions must not appear as successes.
3. End turns; verify dates, weather/forecast, equipment availability, objective counts, and nation summaries.
4. Save, record additional events, then reload. Cancel once and verify the current game/history remain. Accept once and verify later events are replaced, not duplicated. Try a stale branch.
5. Copy a save plus its sidecar to a separate test location with a backed-up journal folder; verify sidecar recovery. Do not delete original history to test recovery.
6. Generate an interim PDF, trigger victory/defeat/surrender, and test manual abandonment. Confirm the outcome and full source are correct and the PDF is readable.
7. Configure an unavailable model; verify a factual PDF is still generated and future turns continue recording. Use a disposable output location for disk-failure tests.
8. Disable/re-enable the mod and verify patches and the history window do not duplicate. Repeat the main flow with the Release package.
9. Test one PBEM round trip. Confirm the same campaign identity returns, and document any server-side stripping of mod data. Email and cross-player merging are not validated.

Full Unity patch application cannot be tested in the desktop CLR: it rejects Unity internal calls. A failed exploratory full-patch desktop test was replaced with a managed-method Harmony integration test; full patch application remains an explicit in-game acceptance check.

## Source references

- Developer modding resources: https://steamcommunity.com/app/1240630/discussions/2/598538723409482593/
- Developer template: https://github.com/acorsicanfrog/Template-Mod-Harmony
- Developer examples: https://github.com/acorsicanfrog/Example-Mod-Harmony

Installed API inspection material stays under ignored `.tools/api`. No decompiled game code is committed or redistributed.
