# Project Status

## 2026-09-18 — Initial project readiness review

- Reviewed SPEC.md, AGENTS.md, BRANDING.md, and both supplied branding images.
- The project currently contains requirements and branding assets, with no implementation, build configuration, or tests. This directory is not currently a Git repository; whether a remote repository already exists was not checked.
- Assessment: sufficient for a feasibility prototype, but not yet a complete implementation specification.
- Highest-priority gaps: verified code-modding integration and supported game versions; PBEM history transfer and merging; fog-of-war visibility; event identity, persistence, reloads, and divergent save branches; explicit first-release scope and acceptance criteria.
- Other decisions needed: behavior without an LLM (local support is both deferred and required as fallback), factual grounding and long-game narrative generation, end-game detection, email delivery configuration and retries, PDF layout and asset usage, installation and update workflow.
- Branding assets establish a usable visual direction, but final logo selection, print-quality assets, typography, and attribution wording remain unspecified.
- The advanced code-modding video URL is missing from SPEC.md. The linked official game homepage was accessible, but the linked Steam modding discussion was not accessible through the research tool. No technical event-hook or UI-extension feasibility was verified.
- Recommended next step: a small in-game feasibility prototype that captures one real event, persists it across save/load, and establishes whether history can accompany a PBEM turn. Expand the specification using those results before full implementation.
- Only this status record was changed. No code was changed, and no tests were run because none exist. No repository was created or pushed as part of this review.

## 2026-09-18 — Source control baseline

- User requested creation of the GitHub repository and an initial commit and push of the current project.
- Confirmed GitHub authentication as dungeontiger and that dungeontiger/hex_of_steel_story_mod did not already exist.
- Repository setup: private GitHub repository named hex_of_steel_story_mod, with main as the initial branch and origin pointing to GitHub.
- Initial baseline includes AGENTS.md, SPEC.md, BRANDING.md, STATUS.md, and both branding images. No implementation changes were made; no tests exist to run.
- Follow-up research: the Steam modding discussion was successfully read in the Codex in-app browser without signing in. It links an advanced tutorial (https://youtu.be/5ckaIcOEZww), Harmony template and example repositories, a modding README, and a game code map. These resources still need technical review.
- User preference: notify the user if future Steam access requires signing in; the user will handle authentication.
- Created the private repository and pushed the initial baseline to main. Four additional branding source assets appeared during setup (three PSD files and one AI file); included them in a follow-up commit to capture the complete current project. Verified remote main matches the local commit after pushing.

## 2026-09-18 — Public repository documentation and license

- Confirmed the repository is public following the user's visibility change.
- Added README.md describing the planning-stage status, intended features, first-release limitations, repository contents, feedback route, and permission requirements, reflecting the updated SPEC.md without changing it.
- Added LICENSE with a custom viewing-only license in the name of Stephen D. Gibson, operating as King Tiger Books. Execution, building, modification, redistribution, and reuse require prior written permission, subject to applicable law and GitHub platform rights.
- Checked GitHub's official licensing guidance and Terms of Service: public repositories permit viewing and forking through GitHub, so the license expressly preserves those rights. The project is described as source-available, not open source.
- Future playable releases require explicit permission or separate release terms; planned Steam Workshop distribution does not grant current usage rights.
- Validation for this documentation-only change: review against the specification, local README link checks, and Git whitespace checks. No implementation or test suite exists, so code tests do not apply.

## 2026-10-21 — Validate and complete the initial implementation baseline

- Continued from substantial uncommitted implementation already present in the workspace; retained that work and reviewed the specification, source, tests, packaging, and installation documentation.
- Recorded the initial .NET solution: portable history core, Windows game adapter with Harmony hooks, F8/Story history viewer, save markers and sidecars, confirmed timeline replacement, state comparisons and turn summaries, offline branded PDF reports, and optional grounded model highlight selection with offline fallback.
- Included Debug/Release configuration, failure diagnostics, pinned dependency lock files, dependency notices/licenses, build/package verification scripts, and Windows core-test CI. Game assemblies, credentials, private saves, generated packages, and local tools remain excluded from Git.
- Fixed event validation to reject null records and null nation/date fields with InvalidDataException. Malformed imports cannot partially restore a journal, invalid appends leave the journal/save marker unchanged, and interior corruption preserves the original file.
- Added three regression tests covering invalid appends, null export records followed by successful corrected recovery, and null interior journal records. Existing unit, persistence integration, model stub, journal-to-PDF end-to-end, and installed-game compatibility tests were retained.
- Validation: full build/test/package scripts passed in Debug and Release, each with 33 core tests and 5 game integration tests, zero compiler warnings/errors. Package verification passed (69 Debug entries, 67 Release entries). Initial restricted build execution failed before restore; validation succeeded with access to the installed SDK, dependency cache, and game references.
- Performed pre-commit code review of persistence/recovery, model fallback, game hooks, viewer, configuration, PDF output, packaging, and tests. Addressed the malformed-record validation finding and verified the regression tests; no test expectations were weakened.
- Remaining acceptance work: interactive Unity/gameplay smoke tests from docs/DEVELOPMENT.md, actual PBEM round trip, readable PDF visual acceptance, and Workshop publication. Automated desktop tests do not establish full in-game hook behavior.
- Remaining requirements: automatic email delivery (player recipient addresses unavailable through the inspected adapter), maps/screenshots, richer historical prose, and later cross-player history transfer. No external email was sent and no Workshop item was published.
- Source control: configured SSH remote was reachable and matched the prior local baseline before commit. This implementation and validation record are being committed and pushed together.

## 2026-10-21 — Protect campaign attachment and retry

- Reviewed campaign switching and found that Ensure published the new map before reading and validating its journal. A failed attachment could leave the old store associated with the new map, or prevent subsequent attachment retries.
- Extracted an attachment operation that flushes pending events to the old campaign first, detaches before parsing the next marker, and publishes the new map/store only after journal and branch validation. Failed attachment remains disabled and retryable. The previous campaign's PDF shortcut clears on detachment.
- Recording readiness now requires the attached map to be the current map. This also prevents cross-campaign recording when flushing the old journal fails. Failure events are queued only for the currently attached campaign; diagnostics still capture attachment errors.
- Applied the game-testing skill. Added four installed-game integration regression cases for malformed markers, corrupt journals, unconfirmed branches, and disk-lock failures. Tests exercise the actual runtime attachment operation with real journals and game Map identities, without invoking Unity engine constructors. They verify isolation, preservation of pending events, and successful retries.
- Extended the in-game acceptance checklist with campaign-switch and recovery checks plus evidence to retain. Interactive Unity/gameplay and PBEM validation remain outstanding; no claim of in-game acceptance is made.
- Validation: complete Debug and Release build/test/package runs passed with 33 core tests and 9 game integration tests each (42 total), zero warnings/errors. Both package content checks passed.
- Pre-commit review covered attachment failure ordering, map identity checks, pending-event ownership, retry, test cleanup, and unchanged save-marker validation. The review added the map identity guard and disk-lock regression before final validation. Changes are being committed and pushed to GitHub.
