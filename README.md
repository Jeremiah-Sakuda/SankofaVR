# SankofaVR

**Sankofa — a story that notices what you notice.**

A proposed hands-first spatial storytelling experience for Meta Quest. A living fire welcomes you, listens to your interests, and brings a personalised history into the space around you. The fire is the griot. You enter through its flame and return to the hearth with a keepsake and a thread to follow next time.

**SankofaVR** is the repository/workspace name; **Sankofa** is the product name.

## Start here

| Document | Purpose |
|---|---|
| [Product requirements](Sankofa-PRD.md) | Vision, scope, journey, 18 user stories, requirements and acceptance criteria |
| [Delivery plan](Sankofa-Delivery-Plan.md) | Capacity, architecture, staged proofs and release evidence |
| [Backlog](docs/backlog.md) | Current work in dependency order |
| [Decisions](docs/decisions.md) | Confirmed direction, planning defaults and outstanding choices |
| [Development](docs/development.md) | Workspace setup, checks and future implementation boundaries |
| [Asset register](docs/asset-register.md) | Provenance and acceptance records |
| [References](references/README.md) | Official competition and original web Sankofa sources |
| [Workspace provenance](docs/workspace-provenance.md) | Where the planning documents came from and what was transferred |

## Current status

This is a fresh planning repository. The PRD, delivery plan and backlog were carried over from the previous ideation workspace. No Unity project, web backend, generated assets or runtime implementation has been imported. No headset acceptance is claimed.

The planned entry is Adapted / Significantly Updated Entertainment. The first proofs are the fire/transition and bounded personalisation, followed by integration. Context, final language pair, weekly hours, budget and current headset access remain open. AI-generated music is excluded.

## Check the workspace

Python 3.10 or newer and Git are sufficient; no Python packages are required.

```sh
python3 scripts/check_workspace.py
git diff --check
```

The check covers tracked Markdown links and local-only file protections. It does not validate a VR experience. Future implementation will introduce its own focused tests.

## Local-only material

Full supplied rules, description, reference provenance and the original chat are copied into ignored `references/hackathon/` and `references/ideation/` folders in the initial workspace. They are not included in GitHub or fresh clones.

Keep personal input, real recordings, credentials, raw vendor assets and generated builds out of Git. See [working instructions](AGENTS.md). This repository is private; no open-source licence has been selected. Reuse from the separate [web Sankofa project](https://github.com/Jeremiah-Sakuda/Sankofa) requires an explicit code/asset licensing decision.
