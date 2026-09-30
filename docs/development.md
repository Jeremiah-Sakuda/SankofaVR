# Development guide

This repository currently contains product planning and workspace checks. There is no runnable app, Unity project or backend here yet.

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

## Proposed implementation layout

When implementation is authorised, introduce engine-independent story logic and a separate Unity application under `unity/`. Pin the actual editor/SDK/importer versions after verifying build compatibility. The [delivery plan](../Sankofa-Delivery-Plan.md) records the previous workspace's versions only as possible reuse context.

Follow [the modular architecture](architecture.md) when those components are introduced. It defines responsibilities and permitted dependencies without committing to a provider, service count or package layout prematurely.

Keep approved runtime assets with source provenance. Raw downloads go in ignored `vendor/`; experiments and generated builds go in ignored `artifacts/`; personal data and recordings stay in their ignored folders. Keep source, synthetic fixtures, project settings and Unity `.meta` files versioned. Add implementation-specific checks when those components exist.

## Secrets and external services

Commit only deliberately empty/example configuration. Actual credentials belong in ignored local configuration or the appropriate deployment secret store. Provider access, costs, retention and output rights remain decisions in the [PRD](../Sankofa-PRD.md). Creating this repository does not provision or deploy services.
