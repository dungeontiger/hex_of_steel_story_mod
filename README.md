# Hex of Steel Story Mod

Turn a Hex of Steel campaign into a recorded history of its battles, diplomacy, and turning points.

A project by **Stephen D. Gibson**, operating as **King Tiger Books**.

## Current status

**Planning and feasibility stage.** This repository contains the project specification, development notes, and branding assets. There is no playable mod, installation package, implementation, build process, or test suite yet.

The project is public so friends and other interested readers can follow its development. Public visibility does not grant permission to run or reuse the project; see [License and permissions](#license-and-permissions).

## Planned features

- Record events by turn and nation, including combat losses, diplomacy, policies, equipment availability, promotions, heroes, captured objectives, and weather.
- Keep the event history alongside the saved game and make it accessible through an in-game viewer.
- Summarize each nation's accomplishments and setbacks.
- Generate a factual, readable end-of-game history as a branded PDF, with maps and images where appropriate.
- Support a player-configured language model, with local model support a possible future addition.
- Email the completed history to the game's players, subject to verifying access to player contact information.

These are intended capabilities, not currently available features. [SPEC.md](SPEC.md) is the source of truth for requirements.

## Initial scope and limitations

The mod is primarily intended for play-by-email (PBEM) games, with single-player support also planned. The initial design runs for one player, so other players' events may be absent from the history. Combining histories across players is future work.

The current specification accepts the possibility that the event viewer exposes information normally hidden by fog of war; visibility safeguards are deferred to a later release.

The specification targets **Hex of Steel 8.0.0 or later** and eventual distribution through the **Steam Workshop**. Compatibility, event capture, and installation still need to be implemented and verified. There is nothing to install yet.

## Repository guide

| File or folder | Purpose |
| --- | --- |
| [SPEC.md](SPEC.md) | Project requirements and game/modding references |
| [STATUS.md](STATUS.md) | Dated progress notes, findings, and decisions |
| [BRANDING.md](BRANDING.md) | Author and publishing identity |
| [branding_images/](branding_images/) | Branding images and editable source assets |
| [AGENTS.md](AGENTS.md) | Development, testing, and documentation instructions |
| [LICENSE](LICENSE) | Restrictive viewing permission and reserved rights |

## Feedback and permission requests

Questions and suggestions are welcome through [GitHub issues](https://github.com/dungeontiger/hex_of_steel_story_mod/issues). To request permission to use or modify the project, address a request to **Stephen D. Gibson (@dungeontiger)** there, describing the material and intended use. A request, silence, or an issue discussion does not itself grant permission; explicit written authorization is required.

Please obtain permission before preparing code or asset contributions. Do not include passwords, API keys, player email addresses, or private save data in public issues.

## License and permissions

Copyright (c) 2026 Stephen D. Gibson, operating as King Tiger Books. All rights reserved.

This is **source-available, not open-source software**. The custom [LICENSE](LICENSE) permits viewing and reading for inspection. Running, building, modifying, distributing, or incorporating the material into another project requires prior written permission, except for rights provided by applicable law or GitHub's Terms of Service.

GitHub's terms permit viewing and forking public repositories through its service. Those platform rights do not constitute general permission to use this project. See [GitHub's licensing guidance](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository).

Any future playable release will need an explicit grant of permission or separate release terms; a planned Workshop release does not grant permission now.

Hex of Steel and third-party materials remain the property of their respective owners. This is an independent project and is not presented as an official game release or endorsement.
