# Installation and use

## Status and prerequisites

This is version 0.1.0, an initial Windows build for in-game validation, not a Steam Workshop release. It compiles against a locally installed Hex of Steel code-modding API. The first build was checked against Steam build 25347843. The specification's 8.0.0 minimum is a target, not a claim that every older version has been tested.

A full interactive gameplay smoke test is still required. Automated tests cover the portable logic, game hook signatures, a managed Harmony patch, and the game's save-property serialization. They cannot simulate the Unity engine or its multiplayer service.

Running this project requires the author's permission under LICENSE. Third-party dependencies retain their own licenses.

## Local installation

1. Close Hex of Steel before copying or updating mod DLLs.
2. Extract `KingTigerStory-0.1.0-Release.zip` into `%USERPROFILE%\AppData\LocalLow\War Frogs Studio\Hex of Steel\MODS` so that `King Tiger Story\Manifest.json` and `King Tiger Story\Libraries\KingTiger.StoryMod.dll` exist.
3. Start the game and enable **King Tiger Story** in its mods menu. Do not enable both Debug and Release copies.
4. Start a disposable test scenario or load a backed-up save. Press **F8** or click **Story (F8)** to view events.
5. Use **Create history PDF** for an interim history. Victory or defeat generates a report automatically. For a game abandoned outside the game's normal victory flow, use **Mark abandoned** and confirm.

Use the game's own mod creation/Workshop interface for eventual distribution after validation. This repository does not upload a Workshop item automatically.

## Files and recovery

- Durable journal: `Saves\StoryHistory\<campaign-id>.journal.jsonl` under the game's data directory.
- Save-adjacent history: `<save-name>.sav.story.json`, containing the corresponding save marker and complete event history.
- PDF: `Saves\StoryHistory\<campaign-id>-history.pdf`.
- Complete source for that report: the same PDF path followed by `.events.json`.
- Diagnostics: `Saves\StoryHistory\story-diagnostics.log`.

Keep the history folder with your backups. If its journal is missing, loading a save with its matching `.story.json` restores it. If the history is incompatible, the mod cancels loading instead of mixing timelines. Disable the mod if you need to load a game without recording; this does not reconstruct missing events.

Loading an older save prompts before changing game state. **No** cancels the load. **Yes** replaces later active history with the saved timeline; a `.bak` file preserves a recovery copy. A save from an already-replaced branch is rejected rather than silently merged. A corrupt final interrupted journal record is backed up and repaired; corruption inside the journal stops recording and is reported.

Disk-write failures retain pending events in memory and retry. An unrecoverable disk failure followed by a process/OS crash can still lose pending events; no mod can guarantee persistence when the storage device refuses writes. Do not ignore a recording-failure notice.

## Optional language model

No model or account is needed for the default factual history. To enable event selection by a model:

1. Copy `config.example.json` to `config.local.json` in the installed mod directory.
2. Set `Endpoint` to the full URL of a compatible chat-completions endpoint, and set `Model` to an available model name.
3. If required, set the API key in the environment variable named by `ApiKeyEnvironmentVariable` before launching Steam/the game. Never put the key in this repository.

Remote endpoints must use HTTPS. Local endpoints may use HTTP on localhost, for example a locally installed compatible model server. Local model software and weights are not bundled. Endpoint redirects are disabled. Config is reloaded when a report is requested.

Enabling a remote endpoint transmits a bounded selection of recorded event descriptions to that provider. The model may select only known event IDs; factual wording comes from recorded events. Invalid output, timeout, or provider failure falls back to the offline report and is recorded as a failure. Long histories use bounded highlights and retain the full source file.

## Known first-release limits

- Only events observed on this installation are available. No merging of histories across PBEM players.
- The viewer can expose fog-of-war information, as permitted by the current specification.
- Periodic comparisons catch state changes between observations. A change reversed entirely between observations without a direct hook may be absent. Named capture hooks cover combat, promotions, heroes, transfers, and diplomacy.
- Equipment availability covers a nation's own equipment and the installed game's date/research rules, not every potential lend-lease purchase.
- Maps and gameplay screenshots are not included in reports yet; the report uses King Tiger Books branding and source references.
- Automatic email delivery is not implemented: local player records expose names and IDs, but recipient email addresses were not found. A recipient/transport alternative needs the author's decision. PDFs are available locally regardless.
- The mod preserves a marker rather than the full journal inside the game's limited mod-data bag. PBEM/cloud transfer of a save alone does not transfer the complete history.
- Full historical causality is not inferred. The initial report is a concise factual chronicle, with model-assisted highlight selection if configured.
