# Development guide

This repository contains the product plans, a first Unity slice under `unity/` and the engine-free story core with its tests under `dotnet/`. The slice plays the draft Enkare Nairobi story package with narration rendered ahead of time by `scripts/generate_narration.py` (Google Cloud Text-to-Speech). No backend exists, and the build makes no provider call at runtime.

## Check documentation and exclusions

```sh
python3 scripts/check_workspace.py
git diff --check
```

Python 3.10+ and Git are sufficient. Stage intended new Markdown files before running the checker; it uses Git's tracked-file list. The same check runs in GitHub Actions.

## Local competition context

Agents must explicitly read the repository-root `HACKATHON.local.md` before competition work. Git-ignored files may be hidden from file searches. This brief contains the current objective, official requirements, scoring, product choices and evidence expectations. It is a local aid, not a replacement for the official rules.

For a fresh clone, worktree or remote workspace:

1. Restore the brief from an authorised local workspace if accessible. Keep it at the repository root, outside runtime asset directories. Git does not transfer it automatically.
2. If no copy is available, create `HACKATHON.local.md` using the current official sources in the [reference index](../references/README.md), the PRD and the decision record. Include the objective, source URLs/check date, requirements versus guidelines, all judging weights, project-specific tradeoffs, acceptance evidence and unresolved questions. Preserve user choices; do not invent eligibility, organiser approval, device results or deadlines.
3. If source access is unavailable, record that limitation. Continue tasks independent of those facts; defer claims that depend on unverified competition context.
4. Read the completed brief and run the local check:

```sh
python3 scripts/check_workspace.py --competition
```

This mode fails if the local brief is missing or empty, or its Git exclusion is broken. It does not evaluate the rules, document freshness, implementation quality or agent adherence. The normal CI check works without the private brief while still checking its exclusion. Both modes reject a tracked copy, including one force-added to the index.

Keep the brief, copied rules and any local review evidence outside runtime prompts, Unity Resources/StreamingAssets/Addressables, container build contexts and release archives. Future packaging must explicitly select runtime files and inspect the output; `.gitignore` alone is not a packaging rule. Do not add the brief to CI secrets or upload it as an artifact.

## Implementation layout and checks

[The modular architecture](architecture.md) defines responsibilities and permitted dependencies. The current modules:

| Folder | Module | Checked by |
|---|---|---|
| `unity/Assets/Sankofa/Core` | Story core: episode model, reader, validator, story run, visit journal | .NET tests, EditMode tests |
| `unity/Assets/Sankofa/Flow` | Application flow: phases, cues, timing, pause, stale-result rejection | .NET tests |
| `unity/Assets/Sankofa/Interaction` | Engine-free gesture logic | .NET tests |
| `unity/Assets/Sankofa/Runtime` | Unity presentation, hands and composition | EditMode tests, builds |
| `unity/Assets/Sankofa/Editor` | Project setup, generated scene, build and build guards | EditMode tests, builds |
| `unity/Assets/Sankofa/Content` | Bundled episode data | .NET and EditMode tests |

The engine-free folders build outside Unity as .NET Standard 2.1 with C# 9, the level Unity 6 accepts, with warnings as errors. With the .NET 10 SDK (for example installed in `~/.dotnet` by Microsoft's [install script](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script)):

```sh
dotnet test dotnet/Sankofa.Tests
```

GitHub Actions runs these tests alongside the documentation check. Unity commands, the simulator and headset installation are in the [Unity README](../unity/README.md). Run the .NET tests after changing the core, flow, gestures or episode data; run the EditMode tests and regenerate the scene after changing runtime or editor code.

Keep approved runtime assets with source provenance in the [asset register](asset-register.md). Raw downloads go in ignored `vendor/`; experiments, logs and builds go in ignored `artifacts/`; personal data and recordings stay in their ignored folders; unlicensed candidates belong in ignored `unity/Assets/LocalOnly/`, which builds refuse to include. Keep source, synthetic fixtures, project settings and Unity `.meta` files versioned.

## Secrets and external services

Commit only deliberately empty/example configuration. Actual credentials belong in ignored local configuration or the appropriate deployment secret store. Provider access, costs, retention and output rights remain decisions in the [PRD](../Sankofa-PRD.md). Creating this repository does not provision or deploy services.
