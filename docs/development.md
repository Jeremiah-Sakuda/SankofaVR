# Development guide

This repository currently contains product planning and workspace checks. There is no runnable app, Unity project or backend here yet.

## Check documentation and exclusions

```sh
python3 scripts/check_workspace.py
git diff --check
```

Python 3.10+ and Git are sufficient. Stage intended new Markdown files before running the checker; it uses Git's tracked-file list. The same check runs in GitHub Actions.

## Proposed implementation layout

When implementation is authorised, introduce engine-independent story logic and a separate Unity application under `unity/`. Pin the actual editor/SDK/importer versions after verifying build compatibility. The [delivery plan](../Sankofa-Delivery-Plan.md) records the previous workspace's versions only as possible reuse context.

Keep approved runtime assets with source provenance. Raw downloads go in ignored `vendor/`; experiments and generated builds go in ignored `artifacts/`; personal data and recordings stay in their ignored folders. Keep source, synthetic fixtures, project settings and Unity `.meta` files versioned. Add implementation-specific checks when those components exist.

## Secrets and external services

Commit only deliberately empty/example configuration. Actual credentials belong in ignored local configuration or the appropriate deployment secret store. Provider access, costs, retention and output rights remain decisions in the [PRD](../Sankofa-PRD.md). Creating this repository does not provision or deploy services.
