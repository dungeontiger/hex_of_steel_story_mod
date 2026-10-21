# Hex of Steel Story Mod

Record a campaign's events and turn them into a readable history of its battles, diplomacy, and turning points.

A project by **Stephen D. Gibson**, operating as **King Tiger Books**.

## Current status

**Version 0.1.0: initial Windows build, ready for in-game validation.** The project now builds a Hex of Steel code mod, a durable history recorder, an in-game viewer, and a branded PDF report generator. Debug and Release configurations and automated tests are included.

The complete mod still needs an interactive smoke test in Hex of Steel. Automatic email delivery is blocked on access to recipient addresses; maps/screenshots in reports and cross-player history merging are not implemented. This is not yet a published Steam Workshop release.

## Implemented capabilities

- Observe diplomacy, policies, factions, money transfers, unit kills, promotions, heroes, victory points, equipment availability, weather, and turn summaries through game hooks and state comparisons.
- Append events durably, with in-game dates, real-world timestamps, nation, event type, and event identity.
- Associate history with saves; warn before replacing later history when reloading an older save, and recover a missing journal from its matching save-adjacent history.
- View recorded history using **F8** or the **Story** button.
- Generate a King Tiger Books PDF automatically at game end or manually for an interim or abandoned campaign.
- Produce a factual history without an LLM. An optional compatible remote or local model can select recorded highlights; unavailable or invalid model output falls back to the offline report.

Only this installation's observed events are recorded. Other PBEM players' events may be absent, and the viewer can reveal fog-of-war information, as accepted in the initial specification. The record starts when the mod is enabled; it does not reconstruct prior turns.

## Build and install

Requirements: Windows, the .NET 8 SDK for building, and a licensed installed copy of Hex of Steel with code-modding support. The mod targets .NET Framework 4.8, matching the developer's template. The first build was compiled and checked against Steam build **25347843**; compatibility with every version from the specification's 8.0.0 minimum is not claimed.

```powershell
./scripts/build.ps1 -Configuration Release
```

The script builds, runs tests, and creates `artifacts/KingTigerStory-0.1.0-Release.zip`. Use `-Configuration Debug` for symbols and verbose diagnostics, or `-GamePath` for another Steam library.

See [Installation and use](docs/INSTALL.md) for enabling the mod, report locations, optional model configuration, recovery, and known limits. See [Development and validation](docs/DEVELOPMENT.md) for the architecture and in-game acceptance checklist.

## Repository guide

| Path | Purpose |
| --- | --- |
| [SPEC.md](SPEC.md) | Requirements and game/modding references |
| [STATUS.md](STATUS.md) | Dated progress, findings, and remaining work |
| [src/](src/) | Portable core and game adapter |
| [tests/](tests/) | Core, integration, and journal-to-PDF tests |
| [scripts/](scripts/) | Build and package scripts |
| [docs/](docs/) | Installation and development instructions |
| [BRANDING.md](BRANDING.md), [branding_images/](branding_images/) | Author identity and branding assets |
| [AGENTS.md](AGENTS.md) | Development and review instructions |
| [LICENSE](LICENSE) | Viewing permission and reserved rights |
| [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) | Dependency licenses and exclusions |

## Feedback and permission requests

Questions and suggestions are welcome through [GitHub issues](https://github.com/dungeontiger/hex_of_steel_story_mod/issues). Address permission requests to **Stephen D. Gibson (@dungeontiger)**, describing the material and intended use. Explicit written authorization is required; a request or silence does not grant permission.

Obtain permission before preparing contributions. Do not post passwords, API keys, player email addresses, or private save data in public issues. Game assemblies and decompiled API references are not distributed in this repository.

## License and permissions

Copyright (c) 2026 Stephen D. Gibson, operating as King Tiger Books. All rights reserved.

This is **source-available, not open-source software**. The custom [LICENSE](LICENSE) permits viewing and reading for inspection. Building, running, modifying, distributing, or reusing original project material requires prior written permission, subject to applicable law and GitHub's Terms of Service. Third-party libraries retain their own license terms.

GitHub permits viewing and forking public repositories through its service; this does not constitute a general license to use the project. See [GitHub's licensing guidance](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository).

A planned Workshop release does not grant permission now. Hex of Steel and third-party materials remain the property of their respective owners. This independent project is not an official game release or endorsement.
